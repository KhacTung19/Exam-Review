﻿using Library.Common;
using Library.Request;
using Library.Response;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using WebClient.IServices;

namespace WebClient.Services
{
    public class ExamService : IExamService
    {
        private readonly HttpClient _httpClient;

        private readonly ISnackbar snackbar;

        public ExamService(HttpClient httpClient, ISnackbar SnackBar)
        {
            _httpClient = httpClient;
            snackbar = SnackBar;
        }

        public async Task<ResultResponse<ExaminerExamResponse>> GetExamList(ExamSearchRequest req)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"api/Exam/GetExamList", req);

                var requestResponse = await response.Content.ReadFromJsonAsync<ResultResponse<ExaminerExamResponse>>();

                if (!requestResponse.IsSuccessful)
                {
                    snackbar.Add(requestResponse.Message, Severity.Error);
                }

                return requestResponse;
            }
            catch (Exception ex)
            {
                snackbar.Add(ex.Message, Severity.Error);

                return new ResultResponse<ExaminerExamResponse>
                {
                    IsSuccessful = false,
                };
            }
        }

        public async Task<ResultResponse<LeaderExamResponse>> GetLeaderExamList(ExamSearchRequest req)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"api/Exam/GetLeaderExamList", req);

                var requestResponse = await response.Content.ReadFromJsonAsync<ResultResponse<LeaderExamResponse>>();

                if (!requestResponse.IsSuccessful)
                {
                    snackbar.Add(requestResponse.Message, Severity.Error);
                }

                return requestResponse;
            }
            catch (Exception ex)
            {
                snackbar.Add(ex.Message, Severity.Error);

                return new ResultResponse<LeaderExamResponse>
                {
                    IsSuccessful = false,
                };
            }
        }

        public async Task<ResultResponse<LectureExamResponse>> GetLectureExamList(ExamSearchRequest req)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"api/Exam/GetLectureExamList", req);

                var requestResponse = await response.Content.ReadFromJsonAsync<ResultResponse<LectureExamResponse>>();

                if (!requestResponse.IsSuccessful)
                {
                    snackbar.Add(requestResponse.Message, Severity.Error);
                }

                return requestResponse;
            }
            catch (Exception ex)
            {
                snackbar.Add(ex.Message, Severity.Error);

                return new ResultResponse<LectureExamResponse>
                {
                    IsSuccessful = false,
                };
            }
        }

        public async Task<ResultResponse<ExaminerExamResponse>> GetExamById(int examId)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"api/Exam/GetExamById/{examId}");

                var requestResponse = await response.Content.ReadFromJsonAsync<ResultResponse<ExaminerExamResponse>>();

                if (!requestResponse.IsSuccessful)
                {
                    snackbar.Add(requestResponse.Message, Severity.Error);
                }

                return requestResponse;
            }
            catch (Exception ex)
            {
                snackbar.Add(ex.Message, Severity.Error);

                return new ResultResponse<ExaminerExamResponse>
                {
                    IsSuccessful = false,
                };
            }
        }

        public async Task<ResultResponse<LeaderExamResponse>> GetLeaderExamById(int examId)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"api/Exam/GetLeaderExamById/{examId}");

                var requestResponse = await response.Content.ReadFromJsonAsync<ResultResponse<LeaderExamResponse>>();

                if (!requestResponse.IsSuccessful)
                {
                    snackbar.Add(requestResponse.Message, Severity.Error);
                }

                return requestResponse;
            }
            catch (Exception ex)
            {
                snackbar.Add(ex.Message, Severity.Error);

                return new ResultResponse<LeaderExamResponse>
                {
                    IsSuccessful = false,
                };
            }
        }

        public async Task<ResultResponse<LectureExamResponse>> GetLectureExamById(int examId)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"api/Exam/GetLectureExamById/{examId}");

                var requestResponse = await response.Content.ReadFromJsonAsync<ResultResponse<LectureExamResponse>>();

                if (!requestResponse.IsSuccessful)
                {
                    snackbar.Add(requestResponse.Message, Severity.Error);
                }

                return requestResponse;
            }
            catch (Exception ex)
            {
                snackbar.Add(ex.Message, Severity.Error);

                return new ResultResponse<LectureExamResponse>
                {
                    IsSuccessful = false,
                };
            }
        }

        public async Task<RequestResponse> UpdateExam(ExaminerExamResponse exam)
        {
            HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"api/Exam/UpdateExam", exam);

            var requestResponse = await response.Content.ReadFromJsonAsync<RequestResponse>();

            if (!requestResponse.IsSuccessful)
            {
                snackbar.Add(requestResponse.Message, Severity.Error);
            }
            else
            {
                snackbar.Add(requestResponse.Message, Severity.Success);
            }

            return requestResponse;
        }

        public async Task<RequestResponse> ChangeStatusExam(List<ExaminerExamResponse> exam)
        {
            HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"api/Exam/ChangeStatus", exam);

            var requestResponse = await response.Content.ReadFromJsonAsync<RequestResponse>();

            if (!requestResponse.IsSuccessful)
            {
                snackbar.Add(requestResponse.Message, Severity.Error);
            }
            else
            {
                snackbar.Add(requestResponse.Message, Severity.Success);
            }

            return requestResponse;
        }

        public async Task<RequestResponse> ChangeStatusExamById(int examId, int statusId)
        {
            HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"api/Exam/ChangeStatus/{examId}", statusId);

            var requestResponse = await response.Content.ReadFromJsonAsync<RequestResponse>();

            if (!requestResponse.IsSuccessful)
            {
                snackbar.Add(requestResponse.Message, Severity.Error);
            }
            else
            {
                snackbar.Add(requestResponse.Message, Severity.Success);
            }

            return requestResponse;
        }

        public async Task<RequestResponse> CreateExam(ExamCreateRequest exam)
        {
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"api/Exam/CreateExam", exam);

            var requestResponse = await response.Content.ReadFromJsonAsync<RequestResponse>();

            if (!requestResponse.IsSuccessful)
            {
                snackbar.Add(requestResponse.Message, Severity.Error);
            }
            else
            {
                snackbar.Add(requestResponse.Message, Severity.Success);
            }

            return requestResponse;
        }
        public async Task<RequestResponse> ImportExamsFromExcel(IBrowserFile files)
        {

            try
            {
                // Create multipart form data content
                var content = new MultipartFormDataContent();

                // Add the file to the multipart form data content
                var fileStream = files.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // Adjust max size as needed
                var streamContent = new StreamContent(fileStream);
                content.Add(streamContent, "file", files.Name); // Name 'file' matches the parameter in your API

                // Send the POST request
                HttpResponseMessage response = await _httpClient.PostAsync("api/Exam/ImportExamsFromExcel", content);

                response.EnsureSuccessStatusCode();  // Throw if the response is not a success

                var requestResponse = await response.Content.ReadFromJsonAsync<RequestResponse>();

                return requestResponse;
            }
            catch (Exception ex)
            {
                snackbar.Add($"Error during file upload: {ex.Message}", Severity.Error);
                return new RequestResponse { IsSuccessful = false, Message = ex.Message };
            }
        }
        public async Task<ResultResponse<byte[]>> ExportAllExams()
        {
            //Check JWT key

            try
            {

                HttpResponseMessage response = await _httpClient.GetAsync($"api/GenerateExcel/export-all");
                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();

                    return new ResultResponse<byte[]>
                    {
                        IsSuccessful = true,
                        Item = fileBytes,
                        Message = "Export Suscess"
                    };
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    return new ResultResponse<byte[]>
                    {
                        IsSuccessful = false,
                        Message = errorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                return (new ResultResponse<byte[]>
                {
                    IsSuccessful = false,
                    Message = "An error occurred while exporting: " + ex.Message
                });
            }
        }

        public async Task<ResultResponse<LeaderExamResponse>> GetAdminExamList(ExamSearchRequest req)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"api/Exam/GetAdminExamList", req);

                var requestResponse = await response.Content.ReadFromJsonAsync<ResultResponse<LeaderExamResponse>>();

                if (!requestResponse.IsSuccessful)
                {
                    snackbar.Add(requestResponse.Message, Severity.Error);
                }

                return requestResponse;
            }
            catch (Exception ex)
            {
                snackbar.Add(ex.Message, Severity.Error);

                return new ResultResponse<LeaderExamResponse>
                {
                    IsSuccessful = false,
                };
            }
        }

        public async Task<ResultResponse<byte[]>> GenerateExcelTime()
        {
            try
            {

                HttpResponseMessage response = await _httpClient.GetAsync($"api/GenerateExcel/exportTime");
                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();

                    return new ResultResponse<byte[]>
                    {
                        IsSuccessful = true,
                        Item = fileBytes,
                        Message = "Export Suscess"
                    };
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    return new ResultResponse<byte[]>
                    {
                        IsSuccessful = false,
                        Message = errorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                return (new ResultResponse<byte[]>
                {
                    IsSuccessful = false,
                    Message = "An error occurred while exporting: " + ex.Message
                });
            }
        }

        public async Task<ResultResponse<byte[]>> GenerateExcelByStatus(int userID)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"api/GenerateExcel/status/{userID}");
                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();

                    return new ResultResponse<byte[]>
                    {
                        IsSuccessful = true,
                        Item = fileBytes,
                        Message = "Export Suscess"
                    };
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    return new ResultResponse<byte[]>
                    {
                        IsSuccessful = false,
                        Message = errorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                return (new ResultResponse<byte[]>
                {
                    IsSuccessful = false,
                    Message = "An error occurred while exporting: " + ex.Message
                });
            }
        }

    }

}




