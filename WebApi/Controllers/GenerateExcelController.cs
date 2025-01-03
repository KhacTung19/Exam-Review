﻿using Library.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.IRepository;

namespace WebApi.Controllers
{
    public class GenerateExcelController : ApiBaseController
    {
        private readonly IGenerateExcelRepository _generateExcelRepository;

        public GenerateExcelController(IGenerateExcelRepository generateExcelRepository)
        {
            _generateExcelRepository = generateExcelRepository;
        }

        // Endpoint xuất tất cả các exams
        [AllowAnonymous]
        [HttpGet("export-all")]
        public IActionResult ExportAllToExcel()
        {
            try
            {
                var excelData = _generateExcelRepository.GenerateExcel();
                string fileName = "DanhSachThongKe.xlsx";

                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new ResultResponse<byte[]>
                {
                    IsSuccessful = false,
                    Message = ex.Message
                });
            }
        }
        [AllowAnonymous]
        [HttpGet("exportTime")]
        public IActionResult GenerateExcelTime()
        {
            try
            {
                var excelData = _generateExcelRepository.GenerateExcelTime();
                string fileName = "DanhSachThoiGianTest.xlsx";

                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new ResultResponse<byte[]>
                {
                    IsSuccessful = false,
                    Message = ex.Message
                });
            }
        }
        // Endpoint xuất exams theo trạng thái bằng statusId
        [AllowAnonymous]
        [HttpGet("status/{userid}")]
        public IActionResult ExportToExcelByStatus(int userid)
        {
            try
            {
                var excelData = _generateExcelRepository.GenerateExcelByStatus(userid);
                string fileName = "ChiTietDe.xlsx";

                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new ResultResponse<byte[]>
                {
                    IsSuccessful = false,
                    Message = ex.Message
                });
            }
        }
    }
}
