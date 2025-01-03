﻿using ExcelDataReader;
using Library.Common;
using Library.Models;
using Library.Request;
using Library.Response;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Data;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using WebApi.IRepository;

namespace WebApi.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly QuizManagementContext dbContext;
        private readonly ILogHistoryRepository logRepository;
        private readonly IConfiguration config;

        public UserRepository(QuizManagementContext dbContext, ILogHistoryRepository logRepository, IConfiguration config)
        {
            this.dbContext = dbContext;
            this.logRepository = logRepository;
            this.config = config;
        }
        public async Task<RequestResponse> CreateAsync(UserRequest user)
        {
            try
            {
                var data = await this.dbContext.Users.FirstOrDefaultAsync(x => x.Mail.ToLower().Equals(user.Email.ToLower()));

                if (data != null)
                {
                    return new RequestResponse
                    {
                        IsSuccessful = false,
                        Message = "Mail already exists!"
                    };
                }
                else
                {
                    var newUser = new User
                    {
                        Mail = user.Email + "@fpt.edu.vn",
                        FullName = user.UserName,
                        PhoneNumber = user.Phone,
                        RoleId = user.RoleId,
                        CampusId = user.CampusId,
                        CreateDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        IsActive = user.IsActive.Value,
                    };

                    // Thêm người dùng mới vào cơ sở dữ liệu
                    await dbContext.Users.AddAsync(newUser);
                    await dbContext.SaveChangesAsync();

                }
                //await logRepository.LogAsync($"Create user {user.Email}");
                return new RequestResponse
                {
                    IsSuccessful = true,
                    Message = "Create account successfully!"
                };
            }
            catch (Exception ex)
            {
                return new RequestResponse
                {
                    IsSuccessful = false,
                    Message = ex.Message,
                };
            }
        }

        public async Task<RequestResponse> CreateHeadAsync(UserSubjectRequest req)
        {
            try
            {
                RequestResponse response = new RequestResponse();
                string emailFe = $"{req.MailFe}@fe.edu.vn";
                string emailFpt = $"{req.Email}@fpt.edu.vn";

                var emailExists = await this.dbContext.Users.AnyAsync(x => x.Mail.ToLower() == emailFpt.ToLower() || x.EmailFe.ToLower() == emailFe.ToLower());

                if (emailExists)
                {
                    return new RequestResponse
                    {
                        IsSuccessful = false,
                        Message = "The email already exists in the system"
                    };
                }
                var newUser = new User
                {
                    CampusId = req.CampusId,
                    PhoneNumber = req.Phone,
                    EmailFe = req.MailFe + "@fe.edu.vn",
                    RoleId = 4,
                    FullName = req.UserName,
                    Mail = req.Email + "@fpt.edu.vn",
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now,
                    IsActive = true,
                };
                var existingCampusUserFacultyRecord = await dbContext.CampusUserFaculties
                    .FirstOrDefaultAsync(c => c.FacultyId == req.FacultyId && c.CampusId == req.CampusId && c.UserId != null);
                if (existingCampusUserFacultyRecord != null)
                {
                    return new RequestResponse
                    {
                        IsSuccessful = false,
                        Message = "The department has been managed by another head deparment."

                    };
                }
                await this.dbContext.Users.AddAsync(newUser);
                await this.dbContext.SaveChangesAsync();

                var newData = new CampusUserFaculty
                {
                    UserId = newUser.UserId,
                    FacultyId = req.FacultyId,
                    CampusId = newUser.CampusId,
                };

                await this.dbContext.CampusUserFaculties.AddAsync(newData);

                await this.dbContext.SaveChangesAsync();

                response.IsSuccessful = true;
                response.Message = "Add Head Department successfully";

                return response;
            }
            catch (Exception ex)
            {
                return new RequestResponse
                {
                    IsSuccessful = false,
                    Message = ex.Message
                };
            }
        }
        public async Task<ResultResponse<UserResponse>> GetUserForAdmin(string filterQuery)
        {
            var data = (from u in this.dbContext.Users
                        join c in this.dbContext.Campuses on u.CampusId equals c.CampusId into campusJoin
                        from c in campusJoin.DefaultIfEmpty() // Left join for Campuses
                        join r in this.dbContext.UserRoles on u.RoleId equals r.RoleId into roleJoin
                        from r in roleJoin.DefaultIfEmpty() // Left join for UserRoles
                        where (string.IsNullOrEmpty(filterQuery) || u.Mail.ToLower().Contains(filterQuery.ToLower()))
                        && (u.RoleId == 1 || u.RoleId == 2 || u.RoleId == 5 || u.RoleId == null)
                        select new UserResponse
                        {
                            Email = u.Mail,
                            Tel = u.PhoneNumber,
                            UserName = u.FullName,
                            CampusName = c != null ? c.CampusName : null, // Handle possible null from left join
                            IsActive = u.IsActive,
                            RoleName = r != null ? r.RoleName : null,     // Handle possible null from left join
                            UserId = u.UserId,
                            UpdateDt = u.UpdateDate,
                        }).ToList();

            return new ResultResponse<UserResponse>
            {
                IsSuccessful = true,
                Items = data.OrderByDescending(x => x.UpdateDt).ToList(),
            };
        }

        public async Task<ResultResponse<UserResponse>> GetUserForExaminer(int userId, string filterQuery)
        {
            var campusId = (await this.dbContext.Users.FirstOrDefaultAsync(x => x.UserId == userId))?.CampusId;

            var data = (from u in this.dbContext.Users
                        join c in this.dbContext.Campuses on u.CampusId equals c.CampusId into campusJoin
                        from c in campusJoin.DefaultIfEmpty() // Left join for Campuses
                        join r in this.dbContext.UserRoles on u.RoleId equals r.RoleId into roleJoin
                        from r in roleJoin.DefaultIfEmpty() // Left join for UserRoles
                        where (string.IsNullOrEmpty(filterQuery) || u.Mail.ToLower().Contains(filterQuery.ToLower()))
                        && (u.RoleId == 4 || u.RoleId == null)
                        && u.CampusId == campusId
                        select new UserResponse
                        {
                            Email = u.Mail,
                            CampusName = c != null ? c.CampusName : null, // Handle possible null from left join
                            IsActive = u.IsActive,
                            UserName = u.FullName,
                            Tel = u.PhoneNumber,
                            RoleName = r != null ? r.RoleName : null,     // Handle possible null from left join
                            UserId = u.UserId,
                            UpdateDt = u.UpdateDate,
                        }).ToList();

            return new ResultResponse<UserResponse>
            {
                IsSuccessful = true,
                Items = data.OrderByDescending(x => x.UpdateDt).ToList(),
            };
        }
        public async Task<RequestResponse> DeleteAsync(int id)
        {
            try
            {
                var existingUser = await dbContext.Users.FirstOrDefaultAsync(x => x.UserId == id);
                if (existingUser == null)
                {
                    return new RequestResponse
                    {
                        IsSuccessful = false,
                        Message = "Account no exist"
                    };
                }
                dbContext.Users.RemoveRange(existingUser);
                await dbContext.SaveChangesAsync();
                return new RequestResponse
                {
                    IsSuccessful = true,
                    Message = " Delete success "
                };
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx && sqlEx.Message.Contains("REFERENCE constraint"))
                {
                    return new RequestResponse
                    {
                        IsSuccessful = false,
                        Message = "Cannot delete because there is some data connect to this user"
                    };
                }
                else
                {
                    return new RequestResponse
                    {
                        IsSuccessful = false,
                        Message = ex.Message,
                    };
                }
            }
            catch (Exception ex)
            {
                return new RequestResponse
                {
                    IsSuccessful = false,
                    Message = ex.Message,
                };
            }
        }

        public async Task<ResultResponse<UserRequest>> GetByIdAsync(int id)
        {
            var data = (from u in this.dbContext.Users
                        join c in this.dbContext.Campuses on u.CampusId equals c.CampusId into campusJoin
                        from c in campusJoin.DefaultIfEmpty() // Left join for Campuses
                        join r in this.dbContext.UserRoles on u.RoleId equals r.RoleId into roleJoin
                        from r in roleJoin.DefaultIfEmpty() // Left join for UserRoles
                        where u.UserId == id
                        select new UserRequest
                        {
                            Email = u.Mail.Replace("@fpt.edu.vn", string.Empty),
                            MailFe = u.EmailFe.Replace("@fe.edu.vn", string.Empty),
                            Phone = u.PhoneNumber,
                            UserName = u.FullName,
                            CampusId = u.CampusId,                        // Keep the CampusId from the Users table
                            CampusName = c != null ? c.CampusName : null, // Handle possible null from left join
                            IsActive = u.IsActive,
                            RoleId = u.RoleId,                            // Keep the RoleId from the Users table
                            RoleName = r != null ? r.RoleName : null,     // Handle possible null from left join
                            UserId = u.UserId,
                        }).FirstOrDefault();
            return new ResultResponse<UserRequest>
            {
                IsSuccessful = true,
                Item = data,
            };
        }

        public async Task<ResultResponse<UserSubjectRequest>> GetUserSubjectByIdAsync(int id)
        {
            var data = (from u in this.dbContext.Users
                        join c in this.dbContext.Campuses on u.CampusId equals c.CampusId into campusJoin
                        from c in campusJoin.DefaultIfEmpty() // Left join for Campuses
                        join r in this.dbContext.UserRoles on u.RoleId equals r.RoleId into roleJoin
                        from r in roleJoin.DefaultIfEmpty() // Left join for UserRoles
                        join cuf in this.dbContext.CampusUserFaculties on u.UserId equals cuf.UserId
                        where u.UserId == id
                        select new UserSubjectRequest
                        {
                            Email = u.Mail.Replace("@fpt.edu.vn", string.Empty),
                            CampusId = u.CampusId,
                            CampusName = c != null ? c.CampusName : null,
                            IsActive = u.IsActive,
                            RoleId = u.RoleId,
                            RoleName = r != null ? r.RoleName : null,
                            UserId = u.UserId,
                            Phone = u.PhoneNumber,
                            UserName = u.FullName,
                            FacultyId = cuf.FacultyId,
                        }).FirstOrDefault();

            return new ResultResponse<UserSubjectRequest>
            {
                IsSuccessful = true,
                Item = data,
            };
        }
        public async Task<ResultResponse<UserSubjectRequest>> GetUserFacutyByIdAsync(int id)
        {
            var data = (from u in this.dbContext.Users
                        join c in this.dbContext.Campuses on u.CampusId equals c.CampusId into campusJoin
                        from c in campusJoin.DefaultIfEmpty() // Left join for Campuses
                        join r in this.dbContext.UserRoles on u.RoleId equals r.RoleId into roleJoin
                        from r in roleJoin.DefaultIfEmpty() // Left join for UserRoles
                        join cuf in this.dbContext.CampusUserFaculties on u.UserId equals cuf.UserId into cufJoin
                        from cuf in cufJoin.DefaultIfEmpty() // Left join for CampusUserFaculties
                        where u.UserId == id
                        select new UserSubjectRequest
                        {
                            Email = u.Mail.Replace("@fpt.edu.vn", string.Empty),
                            CampusId = u.CampusId,
                            CampusName = c != null ? c.CampusName : null,
                            IsActive = u.IsActive,
                            RoleId = u.RoleId,
                            RoleName = r != null ? r.RoleName : null,
                            UserId = u.UserId,
                            Phone = u.PhoneNumber,
                            UserName = u.FullName,
                            FacultyId = cuf != null ? cuf.FacultyId : null,
                            FacultyName = cuf != null && cuf.Faculty != null ? cuf.Faculty.FacultyName : null,
                            SubjectResponses = (from cus in this.dbContext.CampusUserSubjects
                                                join subj in this.dbContext.Subjects on cus.SubjectId equals subj.SubjectId
                                                where cus.UserId == u.UserId
                                                select new SubjectResponse
                                                {
                                                    SubjectId = subj.SubjectId,
                                                    SubjectName = subj.SubjectName,
                                                    SubjectCode = subj.SubjectCode
                                                }).ToList()
                        }).FirstOrDefault();

            if (data == null)
            {
                return new ResultResponse<UserSubjectRequest>
                {
                    IsSuccessful = false,
                    Message = "User not found for the given ID.",
                };
            }

            return new ResultResponse<UserSubjectRequest>
            {
                IsSuccessful = true,
                Item = data,
            };
        }



        public async Task<RequestResponse> UpdateAsync(UserRequest user)
        {
            try
            {
                RequestResponse response = new RequestResponse();

                // Kiểm tra xem người dùng có tồn tại không
                var existingUser = await dbContext.Users.FirstOrDefaultAsync(x => x.UserId == user.UserId);
                if (existingUser == null)
                {
                    return new RequestResponse
                    {
                        IsSuccessful = false,
                        Message = "User not exist"
                    };
                }

                // Cập nhật thông tin người dùng
                existingUser.Mail = user.Email + "@fpt.edu.vn";
                existingUser.FullName = user.UserName;
                existingUser.EmailFe = user.Email + "@fpt.edu.vn";
                existingUser.PhoneNumber = user.Phone;
                existingUser.RoleId = user.RoleId;
                existingUser.CampusId = user.CampusId;
                existingUser.IsActive = user.IsActive.Value;

                // Nếu RoleId là 5, cập nhật bảng CampusUserSubject
                //if (user.RoleId == 5)
                //{
                //    // Xóa các môn học cũ của người dùng
                //    var currentSubjects = await dbContext.CampusUserSubjects
                //        .Where(x => x.UserId == user.UserId && x.CampusId == user.CampusId)
                //        .ToListAsync();
                //    dbContext.CampusUserSubjects.RemoveRange(currentSubjects); // Xóa tất cả môn học cũ

                //    // Thêm các môn học mới mà admin đã chọn
                //    foreach (var subjectId in user.SelectedSubjectIds)
                //    {
                //        dbContext.CampusUserSubjects.Add(new CampusUserSubject
                //        {
                //            UserId = user.UserId,
                //            CampusId = user.CampusId,
                //            SubjectId = subjectId,
                //            IsProgramer = true,  // Cập nhật nếu cần
                //            IsSelect = false     // Cập nhật nếu cần
                //        });
                //    }
                //}

                // Lưu các thay đổi vào cơ sở dữ liệu
                await dbContext.SaveChangesAsync();

                response.IsSuccessful = true;
                response.Message = "Update account successfully";

                // Ghi log hành động cập nhật
                //await logRepository.LogAsync($"Update user {user.Email}");

                return response;
            }
            catch (Exception ex)
            {
                return new RequestResponse
                {
                    Message = ex.Message
                };
            }
        }




        public async Task<RequestResponse> ExaminerUpdateUserAsync(UserSubjectRequest user)
        {
            try
            {
                var response = new RequestResponse();

                var existingUser = await dbContext.Users.FirstOrDefaultAsync(x => x.UserId == user.UserId);
                if (existingUser == null)
                {
                    return new RequestResponse
                    {
                        IsSuccessful = false,
                        Message = "User does not exist"
                    };
                }

                // Cập nhật thông tin người dùng
                existingUser.Mail = $"{user.Email}@fpt.edu.vn";
                existingUser.RoleId = user.RoleId;
                existingUser.CampusId = user.CampusId;
                existingUser.IsActive = user.IsActive ?? false;
                existingUser.FullName = user.UserName;
                existingUser.PhoneNumber = user.Phone;

                if (user.FacultyId.HasValue)
                {
                    // Kiểm tra FacultyId hiện tại trong CampusUserFaculties
                    var currentFaculty = await dbContext.CampusUserFaculties
                        .FirstOrDefaultAsync(cus => cus.UserId == user.UserId && cus.CampusId == user.CampusId);

                    if (currentFaculty != null)
                    {
                        // Nếu đã tồn tại, cập nhật FacultyId
                        currentFaculty.FacultyId = user.FacultyId.Value;
                    }
                    else
                    {
                        // Nếu chưa tồn tại, thêm mới
                        var newCampusUserFaculty = new CampusUserFaculty
                        {
                            UserId = user.UserId,
                            FacultyId = user.FacultyId.Value,
                            CampusId = user.CampusId
                        };
                        await dbContext.CampusUserFaculties.AddAsync(newCampusUserFaculty);
                    }

                }
                else
                {
                    // Nếu không có FacultyId, xóa FacultyId hiện tại (nếu có)
                    var currentFaculty = await dbContext.CampusUserFaculties
                        .FirstOrDefaultAsync(cus => cus.UserId == user.UserId && cus.CampusId == user.CampusId);
                    if (currentFaculty != null)
                    {
                        dbContext.CampusUserFaculties.Remove(currentFaculty);
                    }
                }
                var currentSubjectIds = this.dbContext.CampusUserSubjects
                .Where(cus => cus.UserId == user.UserId && cus.CampusId == user.CampusId)
                 .Select(cus => cus.SubjectId)
                .ToList();

                // Lọc các SubjectId mới, loại bỏ null
                var validNewSubjectIds = user.SubjectResponses.Select(id => id.SubjectId).ToList();

                // Tìm các SubjectId cần thêm
                List<int> subjectsToAdd = new List<int>();
                foreach (var subjectId in validNewSubjectIds)
                {
                    if (!currentSubjectIds.Contains(subjectId))
                    {
                        subjectsToAdd.Add(subjectId); // Thêm vào danh sách cần thêm nếu không có trong currentSubjectIds
                    }
                }

                // Tìm các SubjectId cần xóa
                List<int> subjectsToRemove = new List<int>();
                foreach (var subjectId in currentSubjectIds)
                {
                    if (!validNewSubjectIds.Contains(subjectId.Value))
                    {
                        subjectsToRemove.Add(subjectId.Value); // Thêm vào danh sách cần xóa nếu không có trong newSubjectIds
                    }
                }

                // Xóa các môn học đã bị loại bỏ khỏi CampusUserSubjects
                var campusUserSubjectsToRemove = this.dbContext.CampusUserSubjects
                    .Where(cus => cus.UserId == user.UserId && cus.CampusId == user.CampusId && subjectsToRemove.Contains(cus.SubjectId.Value))
                    .ToList();

                if (campusUserSubjectsToRemove.Any())
                {
                    this.dbContext.CampusUserSubjects.RemoveRange(campusUserSubjectsToRemove);
                    this.dbContext.SaveChanges(); // Lưu thay đổi
                }

                // Thêm các môn học mới vào CampusUserSubjects
                var newCampusUserSubjects = subjectsToAdd.Select(subjectId => new CampusUserSubject
                {
                    UserId = user.UserId,
                    SubjectId = subjectId,
                    CampusId = user.CampusId,
                    IsProgramer = true,
                }).ToList();

                if (newCampusUserSubjects.Any())
                {
                    this.dbContext.CampusUserSubjects.AddRange(newCampusUserSubjects);
                    this.dbContext.SaveChanges(); // Lưu thay đổi
                }
                // Lưu các thay đổi vào cơ sở dữ liệu
                await dbContext.SaveChangesAsync();

                response.IsSuccessful = true;
                response.Message = "Account updated successfully";
                return response;
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ
                return new RequestResponse
                {
                    IsSuccessful = false,
                    Message = ex.Message
                };
            }
        }


        public async Task<ResultResponse<UserResponse>> GetHeadOfDepartment(int subjectId, int campusId)
        {
            try
            {
                var user = await (from u in dbContext.Users
                                  join cus in dbContext.CampusUserSubjects on u.UserId equals cus.UserId
                                  join s in dbContext.Subjects on cus.SubjectId equals s.SubjectId
                                  join c in dbContext.Campuses on cus.CampusId equals c.CampusId
                                  where s.SubjectId == subjectId && c.CampusId == campusId
                                  select new UserResponse
                                  {
                                      Email = u.Mail,
                                      UserId = u.UserId,
                                  }).FirstOrDefaultAsync();

                if (user == null)
                {
                    return new ResultResponse<UserResponse>
                    {
                        IsSuccessful = true,
                        Message = "Cannot find Head of Department.",
                    };
                }

                return new ResultResponse<UserResponse>
                {
                    IsSuccessful = true,
                    Item = user
                };
            }
            catch (Exception ex)
            {
                return new ResultResponse<UserResponse>
                {
                    IsSuccessful = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ResultResponse<UserResponse>> GetLectureBySubject(int subjectId, int campusId)
        {
            var data = await (from u in this.dbContext.Users
                              join cus in this.dbContext.CampusUserSubjects on u.UserId equals cus.UserId
                              where u.CampusId == campusId
                              && cus.SubjectId == subjectId
                              && cus.IsProgramer == false
                              select new UserResponse
                              {
                                  Email = u.Mail,
                                  UserId = u.UserId,
                              }).ToListAsync();

            return new ResultResponse<UserResponse>
            {
                IsSuccessful = true,
                Items = data,
            };
        }
        private bool IsValidFptEmail(string email)
        {
            try
            {
                // Kiểm tra định dạng email
                var addr = new System.Net.Mail.MailAddress(email);

                // Kiểm tra xem email có kết thúc bằng .fpt.edu.vn không
                if (addr.Address == email && email.EndsWith("@fpt.edu.vn"))
                {
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidFeEmail(string email)
        {
            try
            {
                // Kiểm tra định dạng email
                var addr = new System.Net.Mail.MailAddress(email);

                // Kiểm tra xem email có kết thúc bằng .fpt.edu.vn không
                if (addr.Address == email && email.EndsWith("@fe.edu.vn"))
                {
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        // Phương thức nhập cho Examiner
        private async Task<List<string>> ImportForExaminer(List<string> facultyList, List<string> subjectList, User user, int? currentUserCampusId)
        {
            var facultiesToAdd = new List<CampusUserFaculty>(); // Danh sách các đối tượng cần thêm vào DB
            var subjectsToAdd = new List<CampusUserSubject>(); // Danh sách các đối tượng cần thêm vào DB
            var errors = new List<string>(); // Danh sách lỗi (nếu có)

            // Kiểm tra các bộ môn (Faculty) và chuẩn bị thêm vào DB
            foreach (var data in facultyList)
            {
                var faculty = await dbContext.Faculties
                    .FirstOrDefaultAsync(f => f.FacultyName.ToLower() == data.ToLower());

                if (faculty == null)
                {
                    errors.Add($"Faculty '{data}' does not exist.");
                    continue;
                }

                var existingCampusUserFacultyRecord = await dbContext.CampusUserFaculties
                    .FirstOrDefaultAsync(c => c.FacultyId == faculty.FacultyId && c.CampusId == currentUserCampusId && c.UserId != null);

                if (existingCampusUserFacultyRecord != null)
                {
                    errors.Add($"Faculty '{data}' at this campus already has a head of department. Please modify the faculty in the system.");
                    continue;
                }

                var existingCampusUserFaculty = await dbContext.CampusUserFaculties
                    .FirstOrDefaultAsync(c => c.UserId == user.UserId && c.FacultyId == faculty.FacultyId && c.CampusId == currentUserCampusId);

                if (existingCampusUserFaculty != null)
                {
                    errors.Add($"The record for User '{user.FullName}' with Faculty '{data}' already exists.");
                    continue;
                }

                // Kiểm tra số lượng bộ môn mà user đang quản lý tại campus này trong DB
                var facultyCountInDb = await dbContext.CampusUserFaculties
                    .CountAsync(c => c.UserId == user.UserId && c.CampusId == currentUserCampusId);

                // Kiểm tra số lượng bộ môn sắp được thêm trong danh sách `facultiesToAdd`
                var facultyCountInMemory = facultiesToAdd
                    .Count(f => f.UserId == user.UserId && f.CampusId == currentUserCampusId);

                // Tổng số bộ môn đang được quản lý (bao gồm cả trong DB và danh sách chờ thêm)
                var totalFacultyCount = facultyCountInDb + facultyCountInMemory;

                if (totalFacultyCount >= 1)
                {
                    errors.Add($"User '{user.FullName}' is only allowed to manage 1 faculty at this campus. If modifications are needed, please check again in the system.");
                    continue;
                }

                facultiesToAdd.Add(new CampusUserFaculty
                {
                    CampusId = currentUserCampusId,
                    UserId = user.UserId,
                    FacultyId = faculty.FacultyId
                });
            }

            // Nếu có bản ghi Faculty mới cần thêm vào DB, lưu vào cơ sở dữ liệu
            if (facultiesToAdd.Any())
            {
                try
                {
                    await dbContext.CampusUserFaculties.AddRangeAsync(facultiesToAdd);
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    errors.Add($"Error while saving Faculty to the database: {ex.Message}");
                }
            }

            // Kiểm tra các môn học (Subject)
            foreach (var data in subjectList)
            {
                var subject = await dbContext.Subjects
                    .FirstOrDefaultAsync(s => s.SubjectCode.ToLower() == data.ToLower());

                if (subject == null)
                {
                    errors.Add($"Subject '{data}' does not exist.");
                    continue;
                }

                var existingCampusUserSubject = await dbContext.CampusUserSubjects
                    .FirstOrDefaultAsync(c => c.UserId == user.UserId && c.SubjectId == subject.SubjectId && c.CampusId == currentUserCampusId);

                if (existingCampusUserSubject != null)
                {
                    errors.Add($"The record for User '{user.FullName}' with Subject '{data}' already exists.");
                    continue;
                }

                // Kiểm tra xem môn học này có thuộc bộ môn mà CNBM đang quản lý không
                var managedFaculties = await dbContext.CampusUserFaculties
                    .Where(c => c.UserId == user.UserId && c.CampusId == currentUserCampusId)
                    .Select(c => c.FacultyId)
                    .ToListAsync();

                var isSubjectManagedByFaculty = await dbContext.Subjects
                    .AnyAsync(s => s.SubjectId == subject.SubjectId && managedFaculties.Contains(s.FacultyId));

                if (!isSubjectManagedByFaculty)
                {
                    errors.Add($"Subject '{data}' does not belong to any Faculty managed by HOD '{user.FullName}' at the current Campus.");
                    continue;
                }

                subjectsToAdd.Add(new CampusUserSubject
                {
                    CampusId = currentUserCampusId,
                    UserId = user.UserId,
                    SubjectId = subject.SubjectId,
                    IsProgramer = true
                });
            }

            // Nếu có bản ghi Subject mới cần thêm vào DB, lưu vào cơ sở dữ liệu
            if (subjectsToAdd.Any())
            {
                try
                {
                    await dbContext.CampusUserSubjects.AddRangeAsync(subjectsToAdd);
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    errors.Add($"Error while saving Subject to the database: {ex.Message}");
                }
            }

            // Trả về danh sách lỗi
            return errors;
        }


        // Phương thức nhập cho Head of Department
        private async Task<List<string>> ImportForHeadOfDepartment(List<string> facultyOrSubjectList, User user, int? currentUserCampusId)
        {
            var subjectsToAdd = new List<CampusUserSubject>(); // Danh sách các đối tượng cần thêm vào DB
            var errors = new List<string>(); // Danh sách lỗi (nếu có)

            foreach (var subjectCode in facultyOrSubjectList)
            {
                var subject = await dbContext.Subjects
                    .FirstOrDefaultAsync(s => s.SubjectCode.ToLower() == subjectCode);

                if (subject == null)
                {
                    // Thêm lỗi nếu môn học không tồn tại
                    errors.Add($"Subject '{subjectCode}' does not exist.");
                    continue;
                }

                // Kiểm tra bản ghi đã tồn tại trong bảng CampusUserSubject
                var existingCampusUserSubject = await dbContext.CampusUserSubjects
                    .FirstOrDefaultAsync(c => c.UserId == user.UserId && c.SubjectId == subject.SubjectId && c.CampusId == currentUserCampusId);

                if (existingCampusUserSubject != null)
                {
                    // Thêm lỗi nếu bản ghi đã tồn tại
                    errors.Add($"The record for User '{user.FullName}' with Subject '{subjectCode}' already exists.");
                    continue;
                }

                // Nếu không tồn tại, thêm bản ghi mới
                subjectsToAdd.Add(new CampusUserSubject
                {
                    CampusId = currentUserCampusId,
                    UserId = user.UserId,
                    SubjectId = subject.SubjectId,
                    IsProgramer = false,
                });
            }

            // Nếu có bản ghi cần thêm, lưu vào DB
            if (subjectsToAdd.Any())
            {
                try
                {
                    await dbContext.CampusUserSubjects.AddRangeAsync(subjectsToAdd);
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // Thêm lỗi khi lưu vào DB
                    errors.Add($"Error while saving to the database: {ex.Message}");
                }
            }

            // Trả về danh sách lỗi (nếu không có lỗi, danh sách sẽ rỗng)
            return errors;
        }


        public async Task<ResultResponse<AccountClaims>> GetCurrentUserInfoAsync(ClaimsPrincipal currentUser)
        {
            // Lấy thông tin người dùng hiện tại từ Claims
            var userId = int.TryParse(currentUser.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

            // Có được id của người dùng từ hệ thống thì liên kết tới database
            var myUser = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (myUser == null)
            {
                return new ResultResponse<AccountClaims>
                {
                    IsSuccessful = false,
                    Message = "User not found."
                };
            }

            // Lấy RoleId và CampusId từ đối tượng người dùng
            var currentUserRoleId = myUser.RoleId;
            var currentUserCampusId = myUser.CampusId;

            // Tạo đối tượng AccountClaims
            var accountClaims = new AccountClaims
            {
                Id = userId,
                RoleId = currentUserRoleId,
                Email = myUser.Mail,
                FirstName = myUser.FullName,
                CampusId = currentUserCampusId
            };

            return new ResultResponse<AccountClaims>
            {
                IsSuccessful = true,
                Item = accountClaims
            };
        }
        // Hàm để kiểm tra tính hợp lệ của số điện thoại
        private bool IsValidPhoneNumber(string phoneNumber)
        {
            // Xóa khoảng trắng và ký tự đặc biệt
            var cleanedNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // Kiểm tra xem số điện thoại có chứa tất cả các ký tự là số hay không
            if (cleanedNumber.Length == 0)
            {
                return false;
            }

            // Kiểm tra độ dài số điện thoại
            if (cleanedNumber.Length < 9 || cleanedNumber.Length > 11)
            {
                return false;
            }

            // Kiểm tra xem có bắt đầu bằng mã vùng hợp lệ
            // Ví dụ: mã vùng di động tại Việt Nam có thể là 09xx, 01xx, 03xx, 07xx, 08xx
            // Nếu bạn có các mã vùng khác, hãy thêm vào danh sách này
            var validPrefixes = new[] { "9", "1", "3", "7", "8" };

            // Kiểm tra xem số điện thoại có bắt đầu bằng các mã vùng hợp lệ
            if (!validPrefixes.Any(prefix => cleanedNumber.StartsWith(prefix)))
            {
                return false;
            }

            return true;
        }
        public async Task<RequestResponse> ImportUsersFromExcel(IFormFile file, ClaimsPrincipal currentUser)
        {
            //khai báo biến
            var response = new RequestResponse();
            var errors = new List<string>();
            var usersToAdd = new List<User>();
            var campusUserFacultiesToAdd = new List<CampusUserFaculty>();
            var campusUserSubjectsToAdd = new List<CampusUserSubject>();
            // Tạo một HashSet để theo dõi các bản ghi đã được thêm
            var existingUserSet = new HashSet<string>();

            try
            {
                // Đăng ký mã hóa hỗ trợ cho Excel
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                // Kiểm tra nếu file không tồn tại hoặc không có dữ liệu
                if (file == null || file.Length == 0)
                {
                    return new RequestResponse
                    {
                        IsSuccessful = false,
                        Message = "No file uploaded!",
                    };
                }

                var userInfoResponse = await GetCurrentUserInfoAsync(currentUser);
                if (!userInfoResponse.IsSuccessful)
                {
                    return new ResultResponse<AccountClaims>
                    {
                        IsSuccessful = false,
                        Message = "Failed to retrieve user information. Please try again later."
                    };
                }

                var currentUserRoleId = userInfoResponse.Item.RoleId;
                var currentUserCampusId = userInfoResponse.Item.CampusId;

                // Lấy RoleName dựa trên RoleId
                var currentUserRoleName = await dbContext.UserRoles
                    .Where(r => r.RoleId == currentUserRoleId)
                    .Select(r => r.RoleName)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(currentUserRoleName))
                {
                    return new ResultResponse<AccountClaims>
                    {
                        IsSuccessful = false,
                        Message = "RoleName not found for the given RoleId."
                    };
                }

                // Thiết lập thư mục lưu file upload
                var uploadsFolder = $"{Directory.GetCurrentDirectory()}\\Uploads\\Users";
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Đường dẫn file
                var filePath = Path.Combine(uploadsFolder, Path.GetFileName(file.FileName));

                // Ghi file lên server
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                using (var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {

                        // Chuyển đổi toàn bộ dữ liệu Excel thành DataSet
                        var dataSet = reader.AsDataSet();

                        foreach (DataTable table in dataSet.Tables)

                        {
                            // Kiểm tra nếu sheet không có dữ liệu (chỉ có header hoặc hoàn toàn trống)
                            if (table.Rows.Count <= 1)
                                continue;
                            for (int i = 1; i < table.Rows.Count; i++) // Bỏ qua header (dòng đầu tiên)
                            {
                                var row = table.Rows[i];


                                // Đọc dữ liệu từ các cột sau hàng tiêu đề
                                var userImportRequest = new UserImportRequest
                                {
                                    Mail = row[1]?.ToString(),  // Cột Email
                                    FullName = row[2]?.ToString(),  // Cột FullName
                                    PhoneNumber = row[3]?.ToString(),  // Cột PhoneNumber
                                    EmailFe = row[4]?.ToString(),  // Cột EmailFe (có thể để trống cho một số roles)
                                    DateOfBirth = row[5]?.ToString(),  // Cột DateOfBirth
                                    Gender = row[6]?.ToString()?.ToLower(),  // Cột Gender
                                    Address = row[7]?.ToString(),  // Cột Address
                                    FacultyInCharge = (currentUserRoleName == "Examiner") // Nếu là Examiner, cho phép đọc cột 8
                                    ? (string.IsNullOrEmpty(row[8]?.ToString()?.Trim()) ? null : row[8]?.ToString()?.Trim())
                                    : null, // Nếu là Head of Department, luôn set FacultyInCharge = null

                                    SubjectInCharge = (currentUserRoleName == "Examiner" || currentUserRoleName == "Head of Department") // Cho phép cả Examiner và Head of Department đọc cột 9
                                    ? (string.IsNullOrEmpty(row[9]?.ToString()?.Trim()) ? null : row[9]?.ToString()?.Trim())
                                    : null // Nếu không thuộc hai vai trò trên, set null cho SubjectInCharge
                                };

                                var errorMessages = new List<string>();

                                // Kiểm tra định dạng của email
                                if (userImportRequest.Mail.Length > 255)
                                {
                                    errorMessages.Add("Email must not exceed 255 characters.");
                                }

                                if (!IsValidFptEmail(userImportRequest.Mail))
                                {
                                    errorMessages.Add($"This email '{userImportRequest.Mail}' is not valid.");
                                }

                                // Kiểm tra định dạng của FullName
                                if (userImportRequest.FullName.Length > 100)
                                {
                                    errorMessages.Add("Full Name must not exceed 100 characters.");
                                }

                                // Kiểm tra định dạng của PhoneNumber
                                if (!IsValidPhoneNumber(userImportRequest.PhoneNumber))
                                {
                                    errorMessages.Add($"This PhoneNumber '{userImportRequest.PhoneNumber}' is not valid. Please ensure it follows Vietnamese standards.");
                                }

                                // Kiểm tra vai trò của người dùng để quyết định vai trò nào được phép import
                                string? targetRoleName = currentUserRoleName switch
                                {
                                    "Admin" => "Examiner",  // Admin có thể import Examiner
                                    "Examiner" => "Head of Department",  // Examiner có thể import Head of Department
                                    "Head of Department" => "Lecturer",  // Head of Department có thể import Lecturer
                                    _ => null  // Các vai trò khác không được phép import
                                };

                                // Kiểm tra xem vai trò mục tiêu có hợp lệ không
                                if (targetRoleName == null)
                                {
                                    return new RequestResponse
                                    {
                                        IsSuccessful = false,
                                        Message = "Your role does not allow you to import users with specific roles."
                                    };
                                }

                                // Lấy RoleId của vai trò mục tiêu
                                var targetRoleId = await dbContext.UserRoles
                                    .Where(r => r.RoleName == targetRoleName)
                                    .Select(r => r.RoleId)
                                    .FirstOrDefaultAsync();

                                // Xử lý DateOfBirth
                                DateTime? dobValue = null;
                                if (!string.IsNullOrEmpty(userImportRequest.DateOfBirth))
                                {
                                    // Thử phân tích cú pháp với định dạng cụ thể
                                    var formats = new[]
                                     {
                                        "dd-MM-yyyy",
                                        "d/M/yyyy",
                                        "d/MM/yyyy",
                                        "dd/MM/yyyy",
                                        "MM-dd-yyyy",
                                        "dd/MM/yyyy hh:mm:ss tt",
                                        "d/MM/yyyy hh:mm:ss tt",
                                        "dd/M/yyyy hh:mm:ss tt",
                                        "d/M/yyyy hh:mm:ss tt"
                                    };

                                    if (DateTime.TryParseExact(userImportRequest.DateOfBirth, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDob))
                                    {
                                        dobValue = parsedDob;
                                    }
                                    else
                                    {
                                        errorMessages.Add($"This DateOfBirth '{userImportRequest.DateOfBirth}' is not valid.");
                                    }
                                }

                                if (dobValue.HasValue)
                                {
                                    if (dobValue.Value > DateTime.Now)
                                    {
                                        errorMessages.Add("Date of Birth cannot be in the future.");
                                    }
                                    if ((DateTime.Now.Year - dobValue.Value.Year) > 120)
                                    {
                                        errorMessages.Add("Date of Birth is not valid. User cannot be older than 120 years.");
                                    }
                                }

                                // Xử lý Gender
                                bool? gender = null;
                                if (!string.IsNullOrEmpty(userImportRequest.Gender))
                                {
                                    if (userImportRequest.Gender.Equals("male", StringComparison.OrdinalIgnoreCase))
                                    {
                                        gender = true;
                                    }
                                    else if (userImportRequest.Gender.Equals("female", StringComparison.OrdinalIgnoreCase))
                                    {
                                        gender = false;
                                    }
                                    else
                                    {
                                        errorMessages.Add($"This gender '{userImportRequest.Gender}' is not valid. Please input 'Male' or 'Female'.");
                                    }
                                }

                                // Kiểm tra vai trò của người dùng để validate EmailFe
                                if ((targetRoleName == "Lecturer" || targetRoleName == "Head of Department" || targetRoleName == "Examiner"))
                                {
                                    if (!string.IsNullOrEmpty(userImportRequest.EmailFe) && !IsValidFeEmail(userImportRequest.EmailFe))
                                    {
                                        errorMessages.Add($"This EmailFe '{userImportRequest.EmailFe}' is not valid.");
                                    }
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(userImportRequest.EmailFe))
                                    {
                                        errorMessages.Add($"Role '{targetRoleName}' is not allowed to have EmailFe.");
                                    }
                                }

                                // Tạo khóa duy nhất cho mỗi người dùng
                                string uniqueKey = $"{userImportRequest.Mail?.Trim().ToLower()}_" +
                                                   $"{userImportRequest.FullName?.Trim().ToLower()}_" +
                                                   $"{userImportRequest.PhoneNumber?.Trim()}";

                                // Kiểm tra xem người dùng đã tồn tại trong HashSet chưa
                                if (existingUserSet.Contains(uniqueKey))
                                {
                                    var errorMessage = $"Duplicate entry for Mail '{userImportRequest.Mail}', FullName '{userImportRequest.FullName}', and PhoneNumber '{userImportRequest.PhoneNumber}'.";
                                    Console.WriteLine(errorMessage);
                                    errors.Add(errorMessage); // Thêm vào danh sách lỗi chính
                                    continue; // Bỏ qua bản ghi trùng lặp

                                }

                                existingUserSet.Add(uniqueKey);

                                // Kiểm tra xem người dùng đã tồn tại trong cơ sở dữ liệu
                                var normalizedMail = userImportRequest.Mail?.Trim().ToLower();
                                Console.WriteLine($"Normalized Mail: {normalizedMail}");

                                var existingUser = await dbContext.Users
                                    .FirstOrDefaultAsync(u => u.Mail.Trim().ToLower() == normalizedMail);

                                if (existingUser != null)
                                {
                                    Console.WriteLine($"Existing User Found: {existingUser.Mail}");
                                    var errorMessage = $"User with Mail '{userImportRequest.Mail}' already exists. Please enter system to check!";
                                    errorMessages.Add(errorMessage);
                                    errors.Add(errorMessage); // Thêm lỗi vào danh sách chính
                                    continue;
                                }
                                else
                                {
                                    Console.WriteLine("No existing user found with this email.");
                                }

                                if (errorMessages.Any())
                                {
                                    errors.Add($"Error with this Mail {userImportRequest.Mail} : {string.Join(", ", errorMessages)}");
                                    continue;
                                }

                                // Tạo User nếu không có lỗi
                                var user = new User
                                {
                                    Mail = userImportRequest.Mail,
                                    CampusId = currentUserCampusId,
                                    RoleId = targetRoleId,
                                    FullName = userImportRequest.FullName,
                                    PhoneNumber = userImportRequest.PhoneNumber,
                                    EmailFe = userImportRequest.EmailFe,
                                    DateOfBirth = dobValue,
                                    Gender = gender,
                                    Address = userImportRequest.Address,
                                    IsActive = true,
                                    CreateDate = DateTime.Now
                                };

                                usersToAdd.Add(user);

                                try
                                {
                                    await dbContext.Users.AddAsync(user);
                                    await dbContext.SaveChangesAsync();

                                    if (currentUserRoleName == "Admin")
                                    {
                                        // Không xử lý FacultyOrSubjectInCharge với Admin
                                        await dbContext.SaveChangesAsync();
                                    }
                                    else
                                    {
                                        // Lấy danh sách các bộ môn hoặc môn học từ cột "FacultyOrSubjectInCharge"   

                                        var facultyList = string.IsNullOrEmpty(userImportRequest.FacultyInCharge) ? new List<string>() : userImportRequest.FacultyInCharge
                                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(f => f.Trim().ToLower())
                                        .ToList();


                                        // Lấy danh sách các bộ môn hoặc môn học từ cột "FacultyOrSubjectInCharge" 
                                        var subjectList = string.IsNullOrEmpty(userImportRequest.SubjectInCharge) ? new List<string>() : userImportRequest.SubjectInCharge
                                            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(f => f.Trim().ToLower())
                                            .ToList();


                                        if (currentUserRoleName == "Examiner")
                                        {
                                            // Gọi phương thức ImportForExaminer và thêm lỗi vào errors
                                            var examinerErrors = await ImportForExaminer(facultyList, subjectList, user, currentUserCampusId);
                                            errors.AddRange(examinerErrors); // Thêm lỗi từ ImportForExaminer vào errors

                                        }
                                        else if (currentUserRoleName == "Head of Department")
                                        {
                                            // Gọi phương thức ImportForHeadOfDepartment và thêm lỗi vào errors
                                            var hodErrors = await ImportForHeadOfDepartment(subjectList, user, currentUserCampusId);
                                            errors.AddRange(hodErrors); // Thêm lỗi từ ImportForHeadOfDepartment vào errors
                                        }

                                        if (!errors.Any())
                                        {
                                            // Không có lỗi, tiến hành lưu thay đổi
                                            await dbContext.SaveChangesAsync();
                                            Console.WriteLine("Nhập dữ liệu thành công.");
                                        }
                                        else
                                        {
                                            // Có lỗi, hiển thị thông báo và thêm lỗi vào danh sách
                                            Console.WriteLine("Nhập dữ liệu không thành công. Các lỗi sau đã xảy ra:");
                                            foreach (var error in errors)
                                            {
                                                Console.WriteLine(error);
                                            }
                                        }
                                    }
                                }

                                catch (Exception ex)
                                {
                                    errors.Add($"Đã xảy ra lỗi: {ex.Message}");
                                }
                            }
                        }
                    }

                }
                if (errors.Any())
                {
                    response.IsSuccessful = false;
                    response.Message = $"There are the following errors: {string.Join("; ", errors)}";
                }
                else
                {
                    response.IsSuccessful = true;
                    response.Message = $"Successfully added users";
                }

            }
            catch (Exception ex)
            {
                response.IsSuccessful = false;
                response.Message = $"An error occurred: {ex.Message}";
            }

            return response;
        }
        public async Task<ResultResponse<UserResponse>> GetAssignedUserByExam(int examId)
        {
            try
            {
                var data = (from e in dbContext.Exams
                            join u in dbContext.Users on e.AssignedUserId equals u.UserId
                            where e.ExamId == examId
                            select new UserResponse
                            {
                                UserId = u.UserId,
                                UserName = u.FullName,
                                Email = u.Mail
                            }).ToList();

                return new ResultResponse<UserResponse>
                {
                    IsSuccessful = true,
                    Items = data,
                };
            }
            catch (Exception ex)
            {
                return new ResultResponse<UserResponse>
                {
                    IsSuccessful = false,
                    Message = ex.Message,
                };
            }
        }

        public async Task<ResultResponse<UserResponse>> GetLectureListByHead(int userId)
        {
            try
            {
                // Lấy thông tin các bài kiểm tra đã được gán (không trùng lặp)
                var examData = (from e in dbContext.Exams
                                where e.AssignedUserId != null
                                group e by e.AssignedUserId into g
                                select new
                                {
                                    UserId = g.Key.Value, // AssignedUserId
                                    AssignedExamCount = g.Count(),
                                    TotalDuration = g.Sum(x => x.ExamDuration)
                                }).ToList();

                // Lấy danh sách giảng viên kèm thông tin liên quan
                var data = (from u in dbContext.Users
                            join cus in dbContext.CampusUserSubjects on u.UserId equals cus.UserId
                            join sj in dbContext.Subjects on cus.SubjectId equals sj.SubjectId
                            join cuf in dbContext.CampusUserFaculties on sj.FacultyId equals cuf.FacultyId
                            where cus.CampusId == cuf.CampusId
                                  && cuf.UserId == userId
                                  && cus.IsProgramer == false
                            select new
                            {
                                u.UserId,
                                u.FullName,
                                u.Mail,
                                u.PhoneNumber,
                                u.IsActive
                            }).Distinct().ToList();

                var result = data.Select(user => new UserResponse
                {
                    UserId = user.UserId,
                    UserName = user.FullName,
                    Email = user.Mail,
                    Tel = user.PhoneNumber,
                    IsActive = user.IsActive,
                    AssignedExamCount = examData.FirstOrDefault(e => e.UserId == user.UserId)?.AssignedExamCount ?? 0,
                    TotalDuration = examData.FirstOrDefault(e => e.UserId == user.UserId)?.TotalDuration ?? 0
                }).ToList();

                return new ResultResponse<UserResponse>
                {
                    IsSuccessful = true,
                    Items = result,
                };
            }
            catch (Exception ex)
            {
                return new ResultResponse<UserResponse>
                {
                    IsSuccessful = false,
                    Message = ex.Message,
                };
            }
        }


        public string GenerateToken(User acc)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var userClaims = new[]
            {
                new Claim(ClaimTypes.Email, acc.Mail!),
                new Claim(ClaimTypes.NameIdentifier, acc.UserId.ToString()),
                new Claim(ClaimTypes.Role,acc.RoleId.ToString()!),
                new Claim("CampusId",acc.CampusId.ToString()!),
            };

            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: userClaims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<AuthenticationResponse> GoogleLoginCallback(string code)
        {
            try
            {
                AuthenticationResponse response = new AuthenticationResponse();

                var tokenResponse = await GetGoogleTokenAsync(code);

                if (!string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    var userInfo = await GetGoogleUserInfoAsync(tokenResponse.AccessToken);

                    if (userInfo != null)
                    {
                        var user = await FindUserAsync(userInfo.Email);

                        if (user != null)
                        {
                            var token = GenerateToken(user);
                            return new AuthenticationResponse
                            {
                                IsSuccessful = true,
                                Token = token
                            };
                        }
                        else
                        {
                            return new AuthenticationResponse
                            {
                                IsSuccessful = false,
                                Message = "Your account does not exist in the system"
                            };
                        }
                    }
                }
                else
                {
                    return new AuthenticationResponse
                    {
                        IsSuccessful = false,
                        Message = "No Access token."
                    };
                }

                return new AuthenticationResponse
                {
                    IsSuccessful = false,
                    Message = "Login with Google failed."
                };
            }
            catch (Exception ex)
            {
                return new AuthenticationResponse
                {
                    IsSuccessful = false,
                    Message = ex.Message
                };
            }
        }

        private async Task<User?> FindUserAsync(string email)
        {
            try
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Mail.Equals(email) && x.IsActive);

                if (user == null)
                {
                    return null;
                }

                if (user.RoleId == 1)
                {
                    return user;
                }

                var campus = await dbContext.Campuses.FirstOrDefaultAsync(x => x.CampusId == user.CampusId && x.IsDeleted == false);

                return campus != null ? user : null;
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        private async Task<TokenResponse> GetGoogleTokenAsync(string code)
        {
            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token");
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", config["GoogleKeys:ClientId"] },
                { "client_secret", config["GoogleKeys:ClientSecret"] },
                { "redirect_uri", $"{config["BaseUri"]}api/user/googlelogincallback" },
                { "grant_type", "authorization_code" }
            });

            request.Content = content;
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TokenResponse>(responseContent);
        }

        private async Task<GoogleUserInfo> GetGoogleUserInfoAsync(string accessToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Cant not get user info");
            }

            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<GoogleUserInfo>(json);
        }

        public async Task<ResultResponse<UserResponse>> GetUserBySubject(int subjectid, int campusId)
        {
            try
            {
                var data = (from u in dbContext.Users
                            join cus in dbContext.CampusUserSubjects on u.UserId equals cus.UserId
                            join s in dbContext.Subjects on cus.SubjectId equals s.SubjectId
                            join e in dbContext.Exams on new { u.UserId } equals new { UserId = e.AssignedUserId ?? 0 } into examsGroup
                            from e in examsGroup.DefaultIfEmpty()
                            where s.SubjectId == subjectid
                                  && u.CampusId == campusId
                                  && cus.IsProgramer == false
                            group e by new
                            {
                                u.UserId,
                                u.FullName,
                                u.PhoneNumber,
                                u.Mail,
                                u.EmailFe,
                                cus.IsSelect
                            } into g
                            select new UserResponse
                            {
                                UserId = g.Key.UserId,
                                UserName = g.Key.FullName,
                                Tel = g.Key.PhoneNumber,
                                Email = g.Key.Mail,
                                FeEmail = g.Key.EmailFe,
                                AssignedExamCount = g.Count(e => e != null),
                                TotalDuration = g.Sum(e => e != null ? e.ExamDuration : 0),
                                IsSelect = g.Key.IsSelect
                            }).ToList();

                return new ResultResponse<UserResponse>
                {
                    IsSuccessful = true,
                    Items = data,
                };
            }
            catch (Exception ex)
            {
                return new ResultResponse<UserResponse>
                {
                    IsSuccessful = false,
                    Message = ex.Message,
                };
            }
        }

        public async Task<ResultResponse<AddLecturerSubjectRequest>> GetUserByMail(string mail, int headId)
        {
            try
            {
                var u = await this.dbContext.Users.FirstOrDefaultAsync(x => x.UserId == headId);

                var data = await this.dbContext.Users
                    .Where(x => x.Mail.ToLower() == mail.ToLower())
                    .Select(x => new AddLecturerSubjectRequest
                    {
                        UserId = x.UserId,
                        Mail = x.Mail.Replace("@fpt.edu.vn", string.Empty),
                        FullName = x.FullName,
                        MailFe = x.EmailFe.Replace("@fe.edu.vn", string.Empty),
                        PhoneNumber = x.PhoneNumber,
                        IsExist = true,
                    })
                    .FirstOrDefaultAsync();

                return new ResultResponse<AddLecturerSubjectRequest>
                {
                    IsSuccessful = data != null,
                    Item = data
                };
            }
            catch (Exception ex)
            {
                return new ResultResponse<AddLecturerSubjectRequest>
                {
                    IsSuccessful = false,
                    Message = ex.Message,
                };
            }
        }

        public async Task<RequestResponse> AddUserToSubject(AddLecturerSubjectRequest req)
        {
            try
            {
                RequestResponse response = new RequestResponse();
                var u = await this.dbContext.Users.FirstOrDefaultAsync(x => x.UserId == req.HeadId);

                if (req.IsExist)
                {
                    var existData = await this.dbContext.CampusUserSubjects.FirstOrDefaultAsync(x => x.UserId == req.UserId && x.SubjectId == req.SubjectId);

                    if (existData != null)
                    {
                        return new RequestResponse
                        {
                            IsSuccessful = false,
                            Message = "This lecturer already teaching this subject"
                        };
                    }

                    var newData = new CampusUserSubject
                    {
                        UserId = req.UserId,
                        SubjectId = req.SubjectId,
                        CampusId = u.CampusId,
                        IsProgramer = false,
                        IsSelect = false,
                    };

                    await this.dbContext.CampusUserSubjects.AddAsync(newData);
                }
                else
                {
                    string emailFe = $"{req.MailFe}@fe.edu.vn";
                    string emailFpt = $"{req.Mail}@fpt.edu.vn";

                    var emailExists = await this.dbContext.Users.AnyAsync(x => x.Mail.ToLower() == emailFpt.ToLower() || x.EmailFe.ToLower() == emailFe.ToLower());
                    if (emailExists)
                    {
                        return new RequestResponse
                        {
                            IsSuccessful = false,
                            Message = "The email already exists in the system"
                        };
                    }
                    var newUser = new User
                    {
                        CampusId = u.CampusId,
                        PhoneNumber = req.PhoneNumber,
                        EmailFe = req.MailFe + "@fe.edu.vn",
                        RoleId = 3,
                        FullName = req.FullName,
                        Mail = req.Mail + "@fpt.edu.vn",
                        CreateDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        IsActive = true,
                    };

                    await this.dbContext.Users.AddAsync(newUser);
                    await this.dbContext.SaveChangesAsync();

                    var newData = new CampusUserSubject
                    {
                        UserId = newUser.UserId,
                        SubjectId = req.SubjectId,
                        CampusId = newUser.CampusId,
                        IsProgramer = false,
                        IsSelect = false,
                    };

                    await this.dbContext.CampusUserSubjects.AddAsync(newData);
                }

                await this.dbContext.SaveChangesAsync();

                response.IsSuccessful = true;
                response.Message = "Add lecturer successfully";

                return response;
            }
            catch (Exception ex)
            {
                return new RequestResponse
                {
                    IsSuccessful = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<RequestResponse> EditLecturer(AddLecturerSubjectRequest req)
        {
            try
            {
                RequestResponse response = new RequestResponse();

                var user = await this.dbContext.Users.FirstOrDefaultAsync(x => x.UserId == req.UserId);

                if (user == null)
                {
                    return new RequestResponse
                    {
                        IsSuccessful = false,
                        Message = "Lecturer not found"
                    };
                }

                user.Mail = req.Mail;
                user.EmailFe = req.MailFe;
                user.PhoneNumber = req.PhoneNumber;
                user.FullName = req.FullName;
                user.IsActive = req.IsActive.Value;

                await this.dbContext.SaveChangesAsync();

                response.IsSuccessful = true;
                response.Message = "Update lecturer successfully";

                return response;
            }
            catch (Exception ex)
            {
                return new RequestResponse
                {
                    IsSuccessful = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<RequestResponse> RemoveLecture(int userId, int subjectId)
        {
            try
            {
                RequestResponse response = new RequestResponse();

                var data = await this.dbContext.CampusUserSubjects.FirstOrDefaultAsync(x => x.UserId == userId && x.SubjectId == subjectId);

                if (data == null)
                {
                    return new RequestResponse
                    {
                        IsSuccessful = false,
                        Message = "Something wrong!"
                    };
                }

                this.dbContext.CampusUserSubjects.Remove(data);

                await this.dbContext.SaveChangesAsync();

                response.IsSuccessful = true;
                response.Message = "Remove lecturer successfully";

                return response;
            }
            catch (Exception ex)
            {
                return new RequestResponse
                {
                    IsSuccessful = false,
                    Message = ex.Message
                };
            }
        }
    }
}
