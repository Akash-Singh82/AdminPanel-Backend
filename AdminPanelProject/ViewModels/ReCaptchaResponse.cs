namespace AdminPanelProject.ViewModels
{
    public class ReCaptchaResponse
    {
        public bool Success { get; set; }
        public DateTime ChallengeTs { get; set; }
        public string Hostname { get; set; } = string.Empty;
        public List<string>? ErrorCodes { get; set; }
    }
}
