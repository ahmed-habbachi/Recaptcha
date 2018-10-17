using System;
using System.Net.Http;

namespace CaseTunisia.Recaptcha
{
    public class RecaptchaOptions
    {
        public string ResponseValidationEndpoint { get; set; } = RecaptchaDefaults.ResponseValidationEndpoint;

        public string JavaScriptUrl { get; set; } = RecaptchaDefaults.JavaScriptUrl;

        public string SiteKey { get; set; }

        public bool Enabled { get; set; } = true;

        public int FailAttemptBeforeCaptcha { get; set; }

        public string SecretKey { get; set; }

        public HttpMessageHandler BackchannelHttpHandler { get; set; }

        public TimeSpan BackchannelTimeout { get; set; } = TimeSpan.FromSeconds(60);

        public RecaptchaControlSettings ControlSettings { get; set; } = new RecaptchaControlSettings();

        public string ValidationMessage { get; set; }

        public string LanguageCode { get; set; }

        public string FormField { get; set; } = "g-recaptcha-response";
    }
}
