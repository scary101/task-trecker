using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using steptreck.Domain.DTOs;
using System.Globalization;
using System.Text.Json;


[ApiController]
[Route("api/ops-metrics")]
public class OpsMetricsController : ControllerBase
{
    private readonly PrometheusClient _prom;

    public OpsMetricsController(PrometheusClient prom) => _prom = prom;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] int minutes = 60, CancellationToken ct = default)
    {
        var requestId = HttpContext.TraceIdentifier;

        Console.WriteLine($"[OPS][{requestId}] Dashboard started. minutes={minutes}");
        Console.WriteLine($"[OPS][{requestId}] Request path={Request.Path}, query={Request.QueryString}");

        try
        {
            minutes = Math.Clamp(minutes, 5, 1440);
            var end = DateTimeOffset.UtcNow;
            var start = end.AddMinutes(-minutes);

            Console.WriteLine($"[OPS][{requestId}] Range start={start:O}, end={end:O}");

            async Task<List<Series>> Q(string q, string name)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                Console.WriteLine($"[OPS][{requestId}] Query START name={name}");
                Console.WriteLine($"[OPS][{requestId}] PromQL {name}: {q}");

                try
                {
                    var doc = await _prom.QueryRangeAsync(q, start, end, "15s", ct);

                    Console.WriteLine($"[OPS][{requestId}] Prometheus raw {name}: {doc.RootElement}");

                    var series = PromToSeries(doc, name);

                    var pointsCount = series.Sum(s => s.points?.Count ?? 0);

                    Console.WriteLine($"[OPS][{requestId}] Query OK name={name}, series={series.Count}, points={pointsCount}, elapsedMs={sw.ElapsedMilliseconds}");

                    return series;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OPS][{requestId}] Query FAILED name={name}, elapsedMs={sw.ElapsedMilliseconds}");
                    Console.WriteLine(ex);
                    return new List<Series>();
                }
            }

            var rps = await Q("sum(rate(http_server_request_duration_seconds_count[1m]))", "rps");
            var p95 = await Q(@"histogram_quantile(0.95, sum by (le) (rate(http_server_request_duration_seconds_bucket[5m])))", "p95");
            var err5 = await Q(@"sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~""5..""}[1m]))", "5xx");
            var cpu = await Q(@"sum(rate(process_cpu_seconds_total[1m]))", "cpu");
            var ram = await Q(@"dotnet_process_memory_working_set_bytes", "ram");
            var act = await Q(@"sum(http_server_active_requests)", "active");

            Console.WriteLine($"[OPS][{requestId}] Dashboard OK");

            return Ok(new { rps, p95, err5, cpu, ram, act });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OPS][{requestId}] Dashboard FAILED");
            Console.WriteLine(ex);

            return StatusCode(500, new
            {
                error = "Ops metrics dashboard failed",
                requestId,
                message = ex.Message,
                exception = ex.GetType().FullName,
                stack = ex.StackTrace
            });
        }
    }


    private static List<Series> PromToSeries(JsonDocument doc, string defaultName)
    {
        var list = new List<Series>();

        try
        {
            var root = doc.RootElement;

            if (!root.TryGetProperty("status", out var statusEl))
            {
                Console.WriteLine($"[OPS][PromToSeries][{defaultName}] No status field");
            }
            else
            {
                Console.WriteLine($"[OPS][PromToSeries][{defaultName}] status={statusEl.GetString()}");
            }

            if (!root.TryGetProperty("data", out var data))
            {
                Console.WriteLine($"[OPS][PromToSeries][{defaultName}] No data field. Root={root}");
                return list;
            }

            if (!data.TryGetProperty("result", out var results))
            {
                Console.WriteLine($"[OPS][PromToSeries][{defaultName}] No result field. Data={data}");
                return list;
            }

            Console.WriteLine($"[OPS][PromToSeries][{defaultName}] resultCount={results.GetArrayLength()}");

            foreach (var r in results.EnumerateArray())
            {
                var name = defaultName;

                if (r.TryGetProperty("metric", out var metric) &&
                    metric.ValueKind == JsonValueKind.Object &&
                    metric.EnumerateObject().Any())
                {
                    name = metric.ToString();
                }

                if (!r.TryGetProperty("values", out var values))
                {
                    Console.WriteLine($"[OPS][PromToSeries][{defaultName}] No values field. Row={r}");
                    continue;
                }

                var points = new List<Point>();

                foreach (var v in values.EnumerateArray())
                {
                    try
                    {
                        var tsSec = v[0].GetDouble();
                        var valStr = v[1].GetString();

                        var val = double.TryParse(
                            valStr,
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out var parsed)
                            ? parsed
                            : 0;

                        points.Add(new Point((long)(tsSec * 1000), val));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[OPS][PromToSeries][{defaultName}] Bad point={v}");
                        Console.WriteLine(ex);
                    }
                }

                Console.WriteLine($"[OPS][PromToSeries][{defaultName}] Added series name={name}, points={points.Count}");

                list.Add(new Series(name, points));
            }

            return list;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OPS][PromToSeries][{defaultName}] FAILED");
            Console.WriteLine(ex);
            return list;
        }
    }
}
