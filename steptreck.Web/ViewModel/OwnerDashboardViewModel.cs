using System.Net.Http.Json;
using steptreck.Domain.DTOs.OwnerDashboardDTOs;

namespace steptreck.Web.ViewModel;

public sealed class OwnerDashboardViewModel
{
    private readonly HttpClient _http;

    public OwnerDashboardViewModel(HttpClient http)
    {
        _http = http;
    }

    public async Task<OwnerDashboardDto?> GetDashboardAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<OwnerDashboardDto>("api/owner/dashboard", ct);
        }
        catch
        {
            return null;
        }
    }
}
