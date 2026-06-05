using System.Net.Http.Json;
using steptreck.Domain.DTOs;

namespace steptreck.Web.ViewModel
{
    public sealed class OpsMetricsVm
    {
        private readonly HttpClient _http;

        public OpsMetricsVm(HttpClient http)
        {
            _http = http;
        }

        public async Task<OpsDashboardDto?> GetDashboardAsync(int minutes = 60)
        {
            try
            {
                minutes = Math.Clamp(minutes, 5, 1440);
                return await _http.GetFromJsonAsync<OpsDashboardDto>($"api/ops-metrics/dashboard?minutes={minutes}");
            }
            catch { return null; }
        }

        private static int ClampMinutes(int minutes) => Math.Clamp(minutes, 5, 1440);

        private async Task<List<Series>> GetSeriesAsync(string url)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<Series>>(url)
                       ?? new List<Series>();
            }
            catch
            {
                return new List<Series>();
            }
        }
    }
}
