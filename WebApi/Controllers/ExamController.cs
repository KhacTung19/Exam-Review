﻿using Library.Request;
using Library.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.IRepository;

namespace WebApi.Controllers
{
    public class ExamController : ApiBaseController
    {
        private readonly IExamRepository _examRepository;
        private readonly ILogHistoryRepository _logHistoryRepository;


        public ExamController(IExamRepository examRepository, ILogHistoryRepository logHistoryRepository)
        {
            _examRepository = examRepository;
            _logHistoryRepository = logHistoryRepository;
        }
        [AllowAnonymous]
        [HttpGet("info")]
        public async Task<IActionResult> GetExamInfo()
        {
            var examInfo = await _examRepository.GetExamInfoAsync();
            return Ok(examInfo);
        }

        [HttpPost("GetExamList")]
        public async Task<IActionResult> GetExamList([FromBody] ExamSearchRequest req)
        {
            var examInfo = await _examRepository.GetExamList(req);
            return Ok(examInfo);
        }

        [HttpPost("GetLeaderExamList")]
        public async Task<IActionResult> GetLeaderExamList([FromBody] ExamSearchRequest req)
        {
            var examInfo = await _examRepository.GetLeaderExamList(req);
            return Ok(examInfo);
        }
        [HttpPost("GetLectureExamList")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLectureExamList([FromBody] ExamSearchRequest req)
        {
            var examInfo = await _examRepository.GetLectureExamList(req);
            return Ok(examInfo);
        }
        [HttpPost("GetAdminExamList")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAdminExamList([FromBody] ExamSearchRequest req)
        {
            var examInfo = await _examRepository.GetAdminExamList(req);
            return Ok(examInfo);
        }

        [HttpGet("GetExamById/{examId}")]
        public async Task<IActionResult> GetExamById(int examId)
        {
            var examInfo = await _examRepository.GetExamById(examId);
            return Ok(examInfo);
        }

        [HttpGet("GetLeaderExamById/{examId}")]
        public async Task<IActionResult> GetLeaderExamById(int examId)
        {
            var examInfo = await _examRepository.GetLeaderExamById(examId);
            return Ok(examInfo);
        }

        [HttpGet("GetLectureExamById/{examId}")]
        public async Task<IActionResult> GetLectureExamById(int examId)
        {
            var examInfo = await _examRepository.GetLectureExamById(examId);
            return Ok(examInfo);
        }

        [HttpPut("UpdateExam")]
        public async Task<IActionResult> UpdateExam([FromBody] ExaminerExamResponse req)
        {
            var examInfo = await _examRepository.UpdateExam(req);
            await _logHistoryRepository.LogAsync($"Update exam {req.ExamCode}", JwtToken);

            return Ok(examInfo);
        }

        [HttpPut("ChangeStatus")]
        public async Task<IActionResult> ChangeStatusExam([FromBody] List<ExaminerExamResponse> req)
        {
            var examInfo = await _examRepository.ChangeStatusExam(req);
            return Ok(examInfo);
        }

        [HttpPut("ChangeStatus/{examid}")]
        public async Task<IActionResult> ChangeStatusExam(int examId, [FromBody] int statusId)
        {
            var examInfo = await _examRepository.ChangeStatusExamById(examId, statusId);
            return Ok(examInfo);
        }
        [AllowAnonymous]
        [HttpPost("CreateExam")]
        public async Task<IActionResult> CreateExam(ExamCreateRequest req)
        {
            var examInfo = await _examRepository.CreateExam(req);
            await _logHistoryRepository.LogAsync($"Create exam {req.ExamCode}", JwtToken);

            return Ok(examInfo);
        }
        [AllowAnonymous]
        [HttpPost("ImportExamsFromExcel")]
        public async Task<IActionResult> ImportExamsFromExcel([FromForm] IFormFile file)
        {
            // Lấy thông tin người dùng hiện tại từ HttpContext
            var currentUser = HttpContext.User;
            var something = await _examRepository.ImportExamsFromExcel(file, currentUser);
            await _logHistoryRepository.LogAsync($"Import exams from Excel", JwtToken);

            return Ok(something);
        }
        [AllowAnonymous]
        [HttpPost("GetExamByCampusAndSubject")]
        public async Task<IActionResult> GetExamByCampusAndSubject(UserRequest userID)
        {
            var exams = await _examRepository.GetExamByCampusAndSubject(userID);
            return Ok(exams);
        }

        [AllowAnonymous]
        [HttpGet("GetCampusReport")]
        public async Task<IActionResult> GetCampusReport()
        {
            var exams = await _examRepository.GetCampusReport();
            return Ok(exams);
        }
        [AllowAnonymous]
        [HttpPost("GetDepartmentReport")]
        public async Task<IActionResult> GetDepartmentReport(UserRequest userID)
        {
            var exams = await _examRepository.GetDepartmentReport(userID);
            return Ok(exams);
        }
        // exam by status
        [AllowAnonymous]
        [HttpGet("GetExambyStatus")]
        public async Task<IActionResult> GetExams(int? statusId = null, int? campusId = null)
        {
            var (exams, examCount) = await _examRepository.GetExamsByStatus(statusId, campusId);

            var response = new
            {
                ExamCount = examCount,
                Exams = exams
            };

            return Ok(response);
        }
        [AllowAnonymous]
        [HttpGet("searchExamBySemester")]
        public async Task<IActionResult> SearchExamsBySemester([FromQuery] int semesterId, [FromQuery] int userId)
        {
            var exams = await _examRepository.ExamBySemesterNameAndUserId(semesterId, userId);
            if (exams == null || !exams.Any())
            {
                return NotFound("No exams found for the specified semester and user.");
            }

            return Ok(exams);
        }
    }


}
