﻿using Library.Models.Dtos;
using Library.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using WebApi.IRepository;

namespace WebApi.Controllers
{
    public class UserController : ApiBaseController
    {
        private readonly IUserRepository userRepository;
        private readonly IConfiguration config;
        private readonly ILogHistoryRepository _logHistoryRepository;


        public UserController(IUserRepository userRepository, IConfiguration config, ILogHistoryRepository logHistoryRepository)
        {
            this.userRepository = userRepository;
            this.config = config;
            _logHistoryRepository = logHistoryRepository;
        }

        [AllowAnonymous]
        [HttpGet("GetLectureBySubject/{subjectId}/{campusId}")]
        public async Task<IActionResult> GetLecture(int subjectId, int campusId)
        {
            var data = await this.userRepository.GetLectureBySubject(subjectId, campusId);

            return Ok(data);
        }

        [AllowAnonymous]
        [HttpGet("GetUserForAdmin/{filterQuery?}")]
        public async Task<IActionResult> GetAllUserWithFilter(string filterQuery = null)
        {
            var userDomainModels = await userRepository.GetUserForAdmin(filterQuery);
            return Ok(userDomainModels);
        }
        [AllowAnonymous]
        [HttpGet("GetUserForExaminer/{userId}/{filterQuery?}")]
        public async Task<IActionResult> GetUserForExaminer(int userId, string filterQuery = null)
        {
            var userDomainModels = await userRepository.GetUserForExaminer(userId, filterQuery);
            return Ok(userDomainModels);
        }
        [AllowAnonymous]
        [HttpGet("get-by-id/{id:int}")]
        public async Task<IActionResult> GetUserById([FromRoute] int id)
        {
            var data = await this.userRepository.GetByIdAsync(id);

            return Ok(data);
        }
        [AllowAnonymous]
        [HttpGet("GetUserSubjectById/{id:int}")]

        public async Task<IActionResult> GetUserSubjectByIdAsync(int id)
        {
            var data = await this.userRepository.GetUserSubjectByIdAsync(id);

            return Ok(data);
        }
        [AllowAnonymous]
        [HttpGet("GetUserFacutyById/{id:int}")]

        public async Task<IActionResult> GetUserFacutyById(int id)
        {
            var data = await this.userRepository.GetUserFacutyByIdAsync(id);

            return Ok(data);
        }

        [HttpPost("Create")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateUser([FromBody] UserRequest user)
        {
            var data = await userRepository.CreateAsync(user);
            await _logHistoryRepository.LogAsync("Add New User", JwtToken);

            return Ok(data);
        }
        [HttpPost("CreateHead")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateHead([FromBody] UserSubjectRequest user)
        {
            var data = await userRepository.CreateHeadAsync(user);
            await _logHistoryRepository.LogAsync("Add new head of department", JwtToken);

            return Ok(data);
        }
        [AllowAnonymous]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateUser(UserRequest updateUserRequestDto)
        {
            var updatedUser = await userRepository.UpdateAsync(updateUserRequestDto);

            await _logHistoryRepository.LogAsync($"Update user {updateUserRequestDto.Email}", JwtToken);

            return Ok(updatedUser);
        }
        [AllowAnonymous]
        [HttpPut("ExaminerUpdateUser")]
        public async Task<IActionResult> ExaminerUpdateUserAsync(UserSubjectRequest updateUserRequestDto)
        {
            var updatedUser = await userRepository.ExaminerUpdateUserAsync(updateUserRequestDto);

            await _logHistoryRepository.LogAsync($"Update user {updateUserRequestDto.Email}", JwtToken);

            return Ok(updatedUser);
        }
        [AllowAnonymous]
        [HttpDelete("Delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleteUser = await userRepository.DeleteAsync(id);

            await _logHistoryRepository.LogAsync($"Delete an user", JwtToken);

            return Ok(deleteUser);
        }
        [AllowAnonymous]
        [HttpGet("GetHead/{subjectId}/{campusId}")]
        public async Task<IActionResult> GetHead(int subjectId, int campusId)
        {
            var deleteUser = await userRepository.GetHeadOfDepartment(subjectId, campusId);
            return Ok(deleteUser);
        }
        [AllowAnonymous]
        [HttpPost("ImportUsersFromExcel")]
        public async Task<IActionResult> ImportUsersFromExcel([FromForm] IFormFile file)
        {
            // Lấy thông tin người dùng hiện tại từ HttpContext
            var currentUser = HttpContext.User;
            var something = await userRepository.ImportUsersFromExcel(file, currentUser);

            await _logHistoryRepository.LogAsync($"Import users from excel", JwtToken);

            return Ok(something);
        }
        [AllowAnonymous]
        [HttpGet("GetAssignedUser/{examId}")]
        public async Task<IActionResult> GetHead(int examId)
        {
            var data = await userRepository.GetAssignedUserByExam(examId);
            return Ok(data);
        }
        [AllowAnonymous]
        [HttpGet("GetLectureListByHead/{userId}")]
        public async Task<IActionResult> GetLectureListByHead(int userId)
        {
            var data = await userRepository.GetLectureListByHead(userId);
            return Ok(data);
        }
        [AllowAnonymous]
        [HttpGet("GoogleLoginCallback")]
        public async Task<IActionResult> GoogleLoginCallback(string code)
        {
            var response = await userRepository.GoogleLoginCallback(code);
            if (response.IsSuccessful)
            {
                var token = Uri.EscapeDataString(response.Token);
                await _logHistoryRepository.LogAsync("Login in into system", token);
                return Redirect($"{config["MainUri"]}home?token={token}");
            }

            var errorMsg = Uri.EscapeDataString(response.Message); // URL encode the error message
            return Redirect($"{config["MainUri"]}login?errorMsg={errorMsg}");
        }

        [HttpGet("google-keys")]
        [AllowAnonymous]
        public IActionResult GetGoogleKeys()
        {
            var clientId = config["GoogleKeys:ClientId"];

            return Ok(new
            {
                ClientId = clientId,
            });
        }

        [AllowAnonymous]
        [HttpGet("GetUserBySubject/{subjectId}/{campusId}")]
        public async Task<IActionResult> GetUserBySubject(int subjectid, int campusId)
        {
            var data = await this.userRepository.GetUserBySubject(subjectid, campusId);

            return Ok(data);
        }
        [AllowAnonymous]
        [HttpGet("GetUserByMail/{mail}/{headId}")]
        public async Task<IActionResult> GetUserByMail(string mail, int headId)
        {
            var data = await this.userRepository.GetUserByMail(mail, headId);

            return Ok(data);
        }
        [AllowAnonymous]
        [HttpPost("AddUserToSubject")]
        public async Task<IActionResult> AddUserToSubject([FromBody] AddLecturerSubjectRequest req)
        {
            var data = await this.userRepository.AddUserToSubject(req);
            await _logHistoryRepository.LogAsync($"Add user {req.Mail} to subject", JwtToken);

            return Ok(data);
        }
        [AllowAnonymous]
        [HttpPut("EditLecturer")]
        public async Task<IActionResult> EditLecturer([FromBody] AddLecturerSubjectRequest req)
        {
            var data = await this.userRepository.EditLecturer(req);

            await _logHistoryRepository.LogAsync($"Update information of lecturer {req.Mail}", JwtToken);

            return Ok(data);
        }
        [AllowAnonymous]
        [HttpDelete("RemoveLecture/{userId}/{subjectId}")]
        public async Task<IActionResult> RemoveLecture(int userId, int subjectId)
        {
            var data = await this.userRepository.RemoveLecture(userId, subjectId);

            await _logHistoryRepository.LogAsync($"remove lecturer from subject", JwtToken);

            return Ok(data);
        }
    }
}
