using ConnectHub.Authentication.Models.DTOs;

namespace ConnectHub.Authentication.Service.Interface
{
    public interface IOAuthService
    {
        Task<OAuthResultDto> ProcessLoginAsync(string email, string displayName, string provider);
    }
}
