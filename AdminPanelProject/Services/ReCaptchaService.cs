using AdminPanelProject.ViewModels;

namespace AdminPanelProject.Services
{
    public interface IReCaptchaService
    {
        Task<bool> VerifyTokenAsync(string token);
    }
    public class ReCaptchaService : IReCaptchaService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public ReCaptchaService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> VerifyTokenAsync(string token)
        {
            var secretKey = _config["GoogleReCaptcha:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("Google ReCaptcha SecretKey is not configured.");

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}",
                null);

            if (!response.IsSuccessStatusCode)
                return false;

            var captchaResult = await response.Content.ReadFromJsonAsync<ReCaptchaResponse>();
            return captchaResult != null && captchaResult.Success;
        }
    }

}
