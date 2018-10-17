namespace CaseTunisia.Recaptcha
{
    public interface IRecaptchaConfigurationService
    {
        bool Enabled { get; set; }

        int FailAttemptBeforeCaptcha { get; }

        string JavaScriptUrl { get; }

        string ValidationMessage { get; }

        string SiteKey { get; }

        RecaptchaControlSettings ControlSettings { get; }

        string LanguageCode { get; }

        string FormField { get; }
    }
}
