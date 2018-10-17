using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CaseTunisia.Recaptcha
{
    public class ValidateRecaptchaFilter : IAsyncAuthorizationFilter
    {
        private IRecaptchaValidationService _service;
        private ILogger<ValidateRecaptchaFilter> _logger;
        private readonly IRecaptchaConfigurationService _configurationService;

        public ValidateRecaptchaFilter(IRecaptchaValidationService service, IRecaptchaConfigurationService configurationService, ILoggerFactory loggerFactory)
        {
            service.CheckArgumentNull(nameof(service));
            service.CheckArgumentNull(nameof(configurationService));
            loggerFactory.CheckArgumentNull(nameof(loggerFactory));

            _service = service;
            _configurationService = configurationService;
            _logger = loggerFactory.CreateLogger<ValidateRecaptchaFilter>();
        }

        /// <inheritdoc />
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            context.CheckArgumentNull(nameof(context));
            Action invalidResponse = () => context.ModelState.AddModelError(_configurationService.FormField, _service.ValidationMessage);
            await _service.ValidateAsync(context.HttpContext);
            try
            {
                context.HttpContext.CheckArgumentNull(nameof(context.HttpContext));
            }
            catch (RecaptchaValidationException ex)
            {
                _logger.ValidationException(ex.Message, ex);

                if (ex.InvalidResponse)
                {
                    invalidResponse();
                    return;
                }
                else
                {
                    context.Result = new BadRequestResult();
                }
            }
        }

        protected  bool ShouldValidate(AuthorizationFilterContext context)
        {
            return _configurationService.Enabled && string.Equals("POST", context.HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase);
        }
    }
}
