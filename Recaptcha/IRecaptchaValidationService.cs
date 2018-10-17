using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CaseTunisia.Recaptcha
{
    public interface IRecaptchaValidationService
    {
        Task<RecaptchaValidationResponse> ValidateAsync(HttpContext context);
        Task<RecaptchaValidationResponse> ValidateAsync(string recaptchaResponse, string remoteIp);
        string ValidationMessage { get; }
    }
}
