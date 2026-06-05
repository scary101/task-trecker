using steptreck.Domain.DTOs.MemberDTOs;
using System.Net.Http.Json;

namespace steptreck.Web.ViewModel
{
    public class MemberScoreViewModel
    {
        private readonly HttpClient _http;

        public MemberScoreViewModel(HttpClient http)
        {
            _http = http;
        }

        public async Task<ScoreDto?> GetMyScore()
        {
            try
            {
                return await _http.GetFromJsonAsync<ScoreDto>(
                    "api/scores/me"
                );
            }
            catch
            {
                return null;
            }
        }
        public async Task<ScoreDto?> GetMebmerScore(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<ScoreDto>(
                    $"api/scores/member/{id}"
                );
            }
            catch
            {
                return null;
            }
        }
        public async Task<List<ScoreRowDto>> GetScoreByTeam(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<ScoreRowDto>>(
                    $"api/scores/teams/{id}"
                ) ?? new List<ScoreRowDto>();
            }
            catch
            {
                return new List<ScoreRowDto>();
            }
        }

    }
}
