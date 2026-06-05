using steptreck.Domain.DTOs.PlanDTOs;
using System.Net.Http.Json;

namespace steptreck.Web.ViewModel
{
    public class PlanVM
    {
        private readonly HttpClient _httpClient;

        public PlanVM(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Получить все планы (для главной)
        /// GET api/plans
        /// </summary>
        public async Task<List<PlanPublicDto>> GetAllAsync()
        {
            var plans = await _httpClient.GetFromJsonAsync<List<PlanPublicDto>>("api/plans");
            return plans ?? new List<PlanPublicDto>();
        }

        /// <summary>
        /// Получить один план по id
        /// GET api/plans/{id}
        /// </summary>
        public async Task<PlanPublicDto?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/plans/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PlanPublicDto>();
        }
    }
}
