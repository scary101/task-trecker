using System.Globalization;
using System.Text.Json;

public class PrometheusClient
{
    private readonly HttpClient _http;
    public PrometheusClient(HttpClient http) => _http = http;

    public async Task<JsonDocument> QueryRangeAsync(
        string promQl,
        DateTimeOffset start,
        DateTimeOffset end,
        string step,
        CancellationToken ct = default)
    {
        var q = Uri.EscapeDataString(promQl);
        var url =
            $"/api/v1/query_range?query={q}" +
            $"&start={start.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}" +
            $"&end={end.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}" +
            $"&step={step}";

        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        var stream = await resp.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }
}
