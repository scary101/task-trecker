using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.SubscriptionsDTOs;
using steptreck.Web.Services;
using System.Net.Http.Json;
using System.Text.Json;

namespace steptreck.Web.ViewModel
{
    public sealed class SubscriptionsViewModel
    {
        public record ApiResult(bool Success, string? Message = null, string? Error = null);
        public record ApiResult<T>(bool Success, T? Data = default, string? Message = null, string? Error = null);
        public record DownloadedFile(string FileName, string ContentType, byte[] Content);

        private readonly HttpClient _http;
        private readonly ISubscriptionCheckState _subscriptionCheckState;

        public SubscriptionsViewModel(HttpClient http, ISubscriptionCheckState subscriptionCheckState)
        {
            _http = http;
            _subscriptionCheckState = subscriptionCheckState;
        }

        public async Task<ApiResult> SubscribeAsync(BySubDto dto)
        {
            try
            {
                var res = await _http.PostAsJsonAsync(
                    "api/subscriptions/subscribe", dto
                );

                if (res.IsSuccessStatusCode)
                {
                    await _subscriptionCheckState.RefreshAsync();
                    var message = await ReadMessageAsync(res) ?? "Подписка оформлена.";
                    return new ApiResult(true, message);
                }

                var error = await ReadMessageAsync(res) ?? "Не удалось оформить подписку.";
                return new ApiResult(false, null, error);
            }
            catch
            {
                return new ApiResult(false, null, "Ошибка запроса.");
            }
        }

        public async Task<bool> HasActiveAsync()
        {
            try
            {
                var res = await _http.GetAsync("api/subscriptions/active");
                if (!res.IsSuccessStatusCode)
                    return false;

                var dto = await res.Content.ReadFromJsonAsync<bool>();
                return dto;
            }
            catch
            {
                return false;
            }
        }

        public async Task<CurrentSubscriptionDto?> GetCurrentAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<CurrentSubscriptionDto>(
                    "api/subscriptions/current"
                );
            }
            catch
            {
                return null;
            }
        }

        public async Task<ApiResult> ExtendAsync(int months)
        {
            try
            {
                var payload = new { MonthCount = months };
                var res = await _http.PostAsJsonAsync("api/subscriptions/extend", payload);
                var msg = await res.Content.ReadFromJsonAsync<ApiMessageDto>();
                if (res.IsSuccessStatusCode)
                    await _subscriptionCheckState.RefreshAsync();
                return res.IsSuccessStatusCode
                    ? new ApiResult(true, msg?.Message)
                    : new ApiResult(false, null, msg?.Error ?? "Не удалось продлить подписку.");
            }
            catch
            {
                return new ApiResult(false, null, "Ошибка запроса.");
            }
        }

        public async Task<ApiResult> CancelAsync()
        {
            try
            {
                var res = await _http.PostAsync("api/subscriptions/cancel", null);
                var msg = await res.Content.ReadFromJsonAsync<ApiMessageDto>();
                if (res.IsSuccessStatusCode)
                    await _subscriptionCheckState.RefreshAsync();
                return res.IsSuccessStatusCode
                    ? new ApiResult(true, msg?.Message)
                    : new ApiResult(false, null, msg?.Error ?? "Не удалось отменить подписку.");
            }
            catch
            {
                return new ApiResult(false, null, "Ошибка запроса.");
            }
        }

        public async Task<IReadOnlyList<PaymentReadDto>> GetPaymentsAsync(
            int page = 1,
            int pageSize = 20,
            string? status = null,
            string? provider = null,
            int? subscriptionId = null)
        {
            var query = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(status))
                query.Add($"status={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrWhiteSpace(provider))
                query.Add($"provider={Uri.EscapeDataString(provider)}");
            if (subscriptionId.HasValue)
                query.Add($"subscriptionId={subscriptionId.Value}");

            var url = "api/payments";
            if (query.Count > 0)
                url += "?" + string.Join("&", query);

            try
            {
                return await _http.GetFromJsonAsync<List<PaymentReadDto>>(url)
                       ?? new List<PaymentReadDto>();
            }
            catch
            {
                return new List<PaymentReadDto>();
            }
        }

        public async Task<PaymentReadDto?> GetPaymentAsync(long paymentId)
        {
            try
            {
                return await _http.GetFromJsonAsync<PaymentReadDto>(
                    $"api/payments/{paymentId}"
                );
            }
            catch
            {
                return null;
            }
        }

        public async Task<(ApiResult Result, DownloadedFile? File)> DownloadReceiptAsync(long paymentId)
        {
            try
            {
                var res = await _http.GetAsync($"api/receipts/{paymentId}/receipt");
                if (!res.IsSuccessStatusCode)
                    return (new ApiResult(false), null);

                var bytes = await res.Content.ReadAsByteArrayAsync();
                var contentType = res.Content.Headers.ContentType?.ToString()
                                  ?? "application/pdf";

                var cd = res.Content.Headers.ContentDisposition;
                var fileName =
                    cd?.FileNameStar?.Trim('"')
                    ?? cd?.FileName?.Trim('"')
                    ?? $"receipt_{paymentId}.pdf";

                return (new ApiResult(true), new DownloadedFile(fileName, contentType, bytes));
            }
            catch
            {
                return (new ApiResult(false), null);
            }
        }

        public async Task<IReadOnlyList<SubscriptionItemDto>> GetConfigAsync(CancellationToken ct = default)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<SubscriptionItemDto>>(
                           "api/subscriptions/config", ct)
                       ?? new List<SubscriptionItemDto>();
            }
            catch
            {
                return new List<SubscriptionItemDto>();
            }
        }

        public async Task<ApiResult<long>> CreateCustomSubscriptionAsync(CreateCustomSubDto dto, CancellationToken ct = default)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/subscriptions/custom", dto, ct);
                if (res.IsSuccessStatusCode)
                {
                    var paymentId = await res.Content.ReadFromJsonAsync<long>(cancellationToken: ct);
                    return new ApiResult<long>(true, paymentId, "Кастомная подписка создана.");
                }

                var error = await ReadMessageAsync(res, ct) ?? "Не удалось создать кастомную подписку.";
                return new ApiResult<long>(false, default, null, error);
            }
            catch
            {
                return new ApiResult<long>(false, default, null, "Ошибка запроса.");
            }
        }

        private static Task<string?> ReadMessageAsync(HttpResponseMessage response)
            => ReadMessageAsync(response, CancellationToken.None);

        private static async Task<string?> ReadMessageAsync(HttpResponseMessage response, CancellationToken ct)
        {
            try
            {
                if (response.Content.Headers.ContentLength == 0)
                    return null;

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("message", out var message))
                        return message.GetString();

                    if (doc.RootElement.TryGetProperty("error", out var error))
                        return error.GetString();
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.String)
                {
                    return doc.RootElement.GetString();
                }
            }
            catch
            {
                try
                {
                    var text = await response.Content.ReadAsStringAsync(ct);
                    return string.IsNullOrWhiteSpace(text) ? null : text;
                }
                catch
                {
                }
            }

            return null;
        }
    }
}
