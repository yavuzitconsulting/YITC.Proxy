

using Microsoft.AspNetCore.Http;
using YITC.Proxy.Authorization.AccessManagement;

namespace YITC.Proxy.Authorization.AuthorizationService
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AuthorizationService(IHttpContextAccessor httpContextAccessor) =>
            _httpContextAccessor = httpContextAccessor;

        public string Username { get => _httpContextAccessor.HttpContext != null ? _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Username)?.Value ?? "NOTSET" : "NOTSET"; }
        public string Email { get => _httpContextAccessor.HttpContext != null ? _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value ?? "NOTSET" : "NOTSET"; }
        public string Role { get => _httpContextAccessor.HttpContext != null ? _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value ?? "NOTSET" : "NOTSET"; }

        public bool IsAuthorized(string controller, string action)
        {
            return UserPermissions.Validate(Role, controller, action);
        }

        public List<RolePermissionSet> RolePermissions { get { return UserPermissions.RolePermissions; } }




    }
}
