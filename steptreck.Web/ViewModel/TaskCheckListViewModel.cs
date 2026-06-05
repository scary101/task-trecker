using steptreck.Domain.DTOs.TaskDTOs;
using steptreck.Domain.DTOs.TaskDTOs.CheckListDTOs;
using System.Net.Http.Json;

namespace steptreck.Web.ViewModel
{
    public class TaskCheckListViewModel
    {
        public record ApiResult(bool Success, string? Message = null, string? Error = null);

        private readonly HttpClient _http;

        public TaskCheckListViewModel(HttpClient http)
        {
            _http = http;
        }

        public async Task<ApiResult> CompleteNextAsync(int taskId)
        {
            var res = await _http.PostAsync(
                $"api/tasks/{taskId}/checklist/complete-next",
                content: null
            );

            if (res.IsSuccessStatusCode)
                return new ApiResult(true);

            var msg = await TryReadError(res);
            return new ApiResult(false, Error: msg);
        }

        public async Task<ApiResult> UpdateItemAsync(PutCheckListItem dto)
        {
            var res = await _http.PutAsJsonAsync(
                "api/tasks/checklist/items",
                dto
            );

            if (res.IsSuccessStatusCode)
                return new ApiResult(true);

            var msg = await TryReadError(res);
            return new ApiResult(false, Error: msg);
        }
        public async Task<ApiResult> SaveChecklistAsync(SaveChecklistDto dto)
        {
            var res = await _http.PutAsJsonAsync(
                $"api/tasks/{dto.TaskId}/checklist",
                dto
            );

            if (res.IsSuccessStatusCode)
                return new ApiResult(true);

            var msg = await TryReadError(res);
            return new ApiResult(false, Error: msg);
        }


        private static async Task<string?> TryReadError(HttpResponseMessage res)
        {
            try
            {
                var payload = await res.Content.ReadFromJsonAsync<ApiError>();
                return payload?.Message;
            }
            catch
            {
                return res.ReasonPhrase;
            }
        }

        private sealed class ApiError
        {
            public string? Message { get; set; }
        }
    }



}
