using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TicketSystem.Services
{
    public class RecaptchaOptions
    {
        public string SiteKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
    }

    public interface IRecaptchaService
    {
        Task<bool> VerifyAsync(string token, string? remoteIp);
    }

    public class RecaptchaService : IRecaptchaService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly RecaptchaOptions _options;

        public RecaptchaService(IHttpClientFactory httpClientFactory, IOptions<RecaptchaOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        public async Task<bool> VerifyAsync(string token, string? remoteIp)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            var client = _httpClientFactory.CreateClient();
            var url =
                $"https://www.google.com/recaptcha/api/siteverify?secret={_options.SecretKey}&response={token}&remoteip={remoteIp}";

            using var resp = await client.PostAsync(url, null);
            var json = await resp.Content.ReadAsStringAsync();

            try
            {
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("success", out var successProp))
                    return successProp.GetBoolean();
            }
            catch { /* no-op */ }

            return false;
        }
    }
}
