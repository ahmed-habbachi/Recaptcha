using Microsoft.Extensions.Logging;
using System;

namespace CaseTunisia.Recaptcha
{
    public static class RecaptchaLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _validationException;

        static RecaptchaLoggerExtensions()
        {
            _validationException = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                Resources.MainResource.Log_ValidationException);
        }

        public static void ValidationException(this ILogger<ValidateRecaptchaFilter> logger, string message, Exception ex)
        {
            _validationException(logger, message, ex);
        }
    }
}
