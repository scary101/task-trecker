using System.Net.Http.Headers;
using steptreck.Web.Services;

namespace steptreck.Web.Services
{
    public class JwtAuthHandler : DelegatingHandler
    {
        private readonly IJwtService _jwt;

        public JwtAuthHandler(IJwtService jwt)
        {
            _jwt = jwt;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var token = await _jwt.GetTokenAsync();

            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
