

using YITC.Proxy.Authorization.AccessManagement;

namespace YITC.Proxy.Authorization.AuthorizationService
{
    public interface IAuthorizationService
    {
        string Username { get; }
        string Email { get; }
        string Role { get; }


        //to retrieve:
        //string actionName = this.ControllerContext.RouteData.Values["action"].ToString();
        //string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
        bool IsAuthorized(string controller, string action);
        List<RolePermissionSet> RolePermissions { get; }

    }
}
