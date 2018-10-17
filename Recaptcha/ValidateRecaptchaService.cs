using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CaseTunisia.Recaptcha
{
    public class ValidateRecaptchaService
    {
        private IRecaptchaValidationService _service;
        private ILogger<ValidateRecaptchaService> _logger;
        private readonly IRecaptchaConfigurationService _configurationService;
        private readonly HttpContext _context;

        public ValidateRecaptchaService(IRecaptchaValidationService service, IRecaptchaConfigurationService configurationService, ILoggerFactory loggerFactory)
        {
            service.CheckArgumentNull(nameof(service));
            service.CheckArgumentNull(nameof(configurationService));
            loggerFactory.CheckArgumentNull(nameof(loggerFactory));

            _service = service;
            _configurationService = configurationService;
            _logger = loggerFactory.CreateLogger<ValidateRecaptchaService>();
        }
        
        public async Task<(bool isValid, string errorMessage)> Validate(HttpContext context)
        {
            context.CheckArgumentNull(nameof(context));

            if (ShouldValidate(context))
            {
                var formField = "g-recaptcha-response";
                try
                {
                    if (!context.Request.HasFormContentType)
                    {
                        throw new RecaptchaValidationException(string.Format(Resources.MainResource.Exception_MissingFormContent, context.Request.ContentType), false);
                    }

                    var form = await context.Request.ReadFormAsync();
                    var response = form[formField];
                    var remoteIp = context.Connection?.RemoteIpAddress?.ToString();


                    if (string.IsNullOrEmpty(response))
                    {
                        return (false, "Not verified human!");
                    }

                    var result = await _service.ValidateAsync(response, remoteIp);

                    if (result.Success)
                    {
                        return (result.Success, "");
                    }

                    return (result.Success, string.Join(", ", result.ErrorCodes));
                }
                catch (RecaptchaValidationException ex)
                {
                    _logger.ValidationException(ex.Message, ex);
                    return (ex.InvalidResponse, ex.Message);
                }
            }
            else
            {
                return (true, "No validation is needed!");
            }
        }

        protected  bool ShouldValidate(HttpContext context)
        {
            return _configurationService.Enabled && string.Equals("POST", context.Request.Method, StringComparison.OrdinalIgnoreCase);
        }
    }
}
