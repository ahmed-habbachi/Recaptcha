using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;

namespace CaseTunisia.Recaptcha
{
    public class RecaptchaService : IRecaptchaValidationService, IRecaptchaConfigurationService
    {
        private RecaptchaOptions _options;
        private HttpClient _backChannel;
        private RecaptchaControlSettings _controlSettings;

        public RecaptchaService(IOptions<RecaptchaOptions> options)
        {
            options.CheckArgumentNull(nameof(options));

            _options = options.Value;

            _options.ResponseValidationEndpoint.CheckMandatoryOption(nameof(_options.ResponseValidationEndpoint));

            _options.JavaScriptUrl.CheckMandatoryOption(nameof(_options.JavaScriptUrl));

            _options.SiteKey.CheckMandatoryOption(nameof(_options.SiteKey));

            _options.SecretKey.CheckMandatoryOption(nameof(_options.SecretKey));

            if (_options.FailAttemptBeforeCaptcha > 0)
            {
                _options.Enabled = false;
            }

            _controlSettings = _options.ControlSettings ?? new RecaptchaControlSettings();
            _backChannel = new HttpClient(_options.BackchannelHttpHandler ?? new HttpClientHandler());
            _backChannel.Timeout = _options.BackchannelTimeout;
        }

        #region fields
        public bool Enabled
        {
            get
            {
                return _options.Enabled;
            }

            set
            {
                _options.Enabled = value;
            }
        }

        public int FailAttemptBeforeCaptcha
        {
            get
            {
                return _options.FailAttemptBeforeCaptcha;
            }
        }

        public string SiteKey
        {
            get
            {
                return _options.SiteKey;
            }
        }

        public string JavaScriptUrl
        {
            get
            {
                return _options.JavaScriptUrl;
            }
        }

        public string ValidationMessage
        {
            get
            {
                return _options.ValidationMessage ?? Resources.MainResource.Default_ValidationMessage;
            }
        }

        public string LanguageCode
        {
            get
            {
                return _options.LanguageCode;
            }
        }

        public string FormField
        {
            get
            {
                return _options.FormField;
            }
        } 
        #endregion

        public RecaptchaControlSettings ControlSettings
        {
            get
            {
                return _controlSettings;
            }
        }

        public async Task<RecaptchaValidationResponse> ValidateAsync(HttpContext context)
        {
            context.CheckArgumentNull(nameof(context));

            if (ShouldValidate(context))
            {
                if (!context.Request.HasFormContentType)
                {
                    throw new RecaptchaValidationException(string.Format(Resources.MainResource.Exception_MissingFormContent, context.Request.ContentType), false);
                }

                var form = await context.Request.ReadFormAsync();
                var response = form[FormField];
                var remoteIp = context.Connection?.RemoteIpAddress?.ToString();

                return await ValidateAsync(response, remoteIp);
            }
            else
            {
                return new RecaptchaValidationResponse() {Success = true, Hostname = "localhost"};
            }
        }

        public async Task<RecaptchaValidationResponse> ValidateAsync(string recaptchaResponse, string remoteIp)
        {
            if (string.IsNullOrEmpty(recaptchaResponse))
            {
                throw new RecaptchaValidationException("No recaptcha responce was found!", true);
            }

            var request = new HttpRequestMessage(HttpMethod.Post, RecaptchaDefaults.ResponseValidationEndpoint);
            var paramaters = new Dictionary<string, string>();
            paramaters["secret"] = _options.SecretKey;
            paramaters["response"] = recaptchaResponse;
            paramaters["remoteip"] = remoteIp;
            request.Content = new FormUrlEncodedContent(paramaters);

            var resp = await _backChannel.SendAsync(request);
            resp.EnsureSuccessStatusCode();

            var responseText = await resp.Content.ReadAsStringAsync();

            var validationResponse = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<RecaptchaValidationResponse>(responseText));

            if (!validationResponse.Success)
            {
                throw new RecaptchaValidationException(GetErrrorMessage(validationResponse, out bool invalidResponse), invalidResponse);
            }

            return validationResponse;
        }

        protected bool ShouldValidate(HttpContext context)
        {
            return Enabled && string.Equals("POST", context.Request.Method, StringComparison.OrdinalIgnoreCase);
        }

        private string GetErrrorMessage(RecaptchaValidationResponse validationResponse, out bool invalidResponse)
        {
            var errorList = new List<string>();
            invalidResponse = false;

            if (validationResponse.ErrorCodes != null)
            {
                foreach (var error in validationResponse.ErrorCodes)
                {
                    switch (error)
                    {
                        case "missing-input-secret":
                            errorList.Add(Resources.MainResource.ValidateError_MissingInputSecret);
                            break;
                        case "invalid-input-secret":
                            errorList.Add(Resources.MainResource.ValidateError_InvalidInputSecret);
                            break;
                        case "missing-input-response":
                            errorList.Add(Resources.MainResource.ValidateError_MissingInputResponse);
                            invalidResponse = true;
                            break;
                        case "invalid-input-response":
                            errorList.Add(Resources.MainResource.ValidateError_InvalidInputResponse);
                            invalidResponse = true;
                            break;
                        default:
                            errorList.Add(string.Format(Resources.MainResource.ValidateError_Unknown, error));
                            break;
                    }
                }
            }
            else
            {
                return Resources.MainResource.ValidateError_UnspecifiedRemoteServerError;
            }

            return string.Join(Environment.NewLine, errorList);
        }
    }
}
