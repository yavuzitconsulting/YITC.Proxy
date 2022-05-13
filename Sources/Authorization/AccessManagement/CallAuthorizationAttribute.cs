using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using YITC.Proxy.Authorization.AuthorizationService;

namespace YITC.Proxy.Authorization.AccessManagement
{
    /// <summary>
    /// This attribute is only valid on controllers and controller methods
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class CallAuthorizationAttribute : ActionFilterAttribute
    {
        private readonly IAuthorizationService _authorizationService;

        public CallAuthorizationAttribute(IAuthorizationService AuthorizationService)
        {
            this._authorizationService = AuthorizationService;
        }
        //TODO: Maybe model validation here/ **new attribute**, or in policies
        public override void OnActionExecuting(ActionExecutingContext context)
        {

            var controllerContext = ((Microsoft.AspNetCore.Mvc.ControllerBase)context.Controller).ControllerContext;
            var controllerName = controllerContext.RouteData.Values["controller"].ToString();
            var controllerAction = controllerContext.RouteData.Values["action"].ToString();
            bool authorized = _authorizationService.IsAuthorized(controllerName, controllerAction);
            if (!authorized) context.Result = new UnauthorizedObjectResult($"Your role (${_authorizationService.Role}) is not allowed to access {controllerName}/{controllerAction}");
        }
    }
}
