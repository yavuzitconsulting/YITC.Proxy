using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using YITC.Proxy.Authorization.AuthorizationService;
using YITC.Proxy.Model;
using System.Linq;

namespace YITC.Proxy
{



    /// <summary>
    /// BigBrain Smart-Proxy implementation which can be used as a method and a controller attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class RoleMappingProxyAttribute : ActionFilterAttribute
    {
        private readonly HttpClient _client;
        private readonly IConfiguration _configuration;
        private readonly string[] _defaultControllerActions = { "Get", "Post", "Put", "Delete", "Any" };
        private readonly List<RoleMapping> _controllerMappings = new List<RoleMapping>();

        private readonly IAuthorizationService _authorizationService;


        /// <summary>
        /// This constructor requires a path on the receiving end (Proxy-Mapping)
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="destinationController"></param>
        public RoleMappingProxyAttribute(IConfiguration configuration, IAuthorizationService AuthorizationService)
        {
            this._configuration = configuration;
            this._authorizationService = AuthorizationService;
            this._controllerMappings = _configuration.GetSection("Proxy:RoleMappings").Get<List<RoleMapping>>();
            _client = new HttpClient(new HttpClientHandler()
            {
                AllowAutoRedirect = false
            });

        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {

                var controllerContext = ((Microsoft.AspNetCore.Mvc.ControllerBase)context.Controller).ControllerContext;
                var controllerName = controllerContext.RouteData.Values["controller"]?.ToString() ?? "";
                var route = controllerContext.RouteData.Values["action"]?.ToString() ?? "";
                if (route != "Any") throw new Exception($"The RoleMappingProxy Attribute requires a single route called 'Any' in the controller! You configured '{route}' instead.");
                string? subRoute = String.Empty;
                context.RouteData.Values.TryGetValueAs("anyRoute", out subRoute);
                subRoute = subRoute ?? String.Empty; //dont want NULL as value, but trygetvalueas can nullify the string

                //First, we need to check if there is more than one mapping for the same controller/path
                var verifyMappings = _controllerMappings.Where(x => x.ControllerName.Equals(controllerName, StringComparison.OrdinalIgnoreCase) && x.Path.Equals(subRoute, StringComparison.OrdinalIgnoreCase));
                var controllerMapping = verifyMappings.Count() <= 1 ? verifyMappings.FirstOrDefault() : throw new Exception($"ERROR: There are ambiguous routes for Controller: {controllerName} and Path: {subRoute}");

                //if there are no controller/route mappings at all, we try to parse a controller mapping based on anyRoute, the first try is looking for the '*' wildcard
                if (controllerMapping == null) controllerMapping = _controllerMappings.FirstOrDefault(x => x.ControllerName.Equals(controllerName, StringComparison.OrdinalIgnoreCase) && x.Path.Equals("*", StringComparison.OrdinalIgnoreCase));

                //if there was no mapping set with wildcard route, the configurer might forgot to add the PATH setting to appsettings, therefore we try to parse the controller as wildcard route without the settings
                if (controllerMapping == null) controllerMapping = _controllerMappings.FirstOrDefault(x => x.ControllerName.Equals(controllerName, StringComparison.OrdinalIgnoreCase));

                //if this still does not exist, throw.
                if (controllerMapping == null) throw new NullReferenceException($"No controllermapping found for controller {controllerName}");



                Uri? forwardUri = null;

                Mapping? currentUserRoleMapping = controllerMapping.Mapping.FirstOrDefault(x => x.Role.Equals(_authorizationService.Role, StringComparison.OrdinalIgnoreCase));
                if (currentUserRoleMapping == null) throw new NullReferenceException($"No rolemapping found for role {_authorizationService.Role} in controllermapping for ${controllerName}");
                string rdestController = currentUserRoleMapping.Controller;
                string rdestRoute = currentUserRoleMapping.Route;
                rdestRoute = rdestRoute == "/" ? String.Empty : rdestRoute;

                if (String.IsNullOrEmpty(rdestController) && String.IsNullOrEmpty(rdestRoute))
                {
                    forwardUri = new Uri($"{context.HttpContext.Request.Scheme}://{_configuration["Proxy:ForwardDomain"]}/{controllerName}/");
                }
                else if (!String.IsNullOrEmpty(rdestController) && String.IsNullOrEmpty(rdestRoute))
                {
                    forwardUri = new Uri($"{context.HttpContext.Request.Scheme}://{_configuration["Proxy:ForwardDomain"]}/{rdestController}/");
                }
                else if (!String.IsNullOrEmpty(rdestController) && !String.IsNullOrEmpty(rdestRoute) && rdestRoute != "*")
                {
                    forwardUri = new Uri($"{context.HttpContext.Request.Scheme}://{_configuration["Proxy:ForwardDomain"]}/{rdestController}/{rdestRoute}");
                }
                else if (String.IsNullOrEmpty(rdestController) && !String.IsNullOrEmpty(rdestRoute) && rdestRoute != "*")
                {
                    forwardUri = new Uri($"{context.HttpContext.Request.Scheme}://{_configuration["Proxy:ForwardDomain"]}/{controllerName}/{rdestRoute}");
                }
                else if (String.IsNullOrEmpty(rdestController) && rdestRoute == "*")
                {
                    forwardUri = new Uri($"{context.HttpContext.Request.Scheme}://{_configuration["Proxy:ForwardDomain"]}/{controllerName}/{subRoute}");
                }
                else if (!String.IsNullOrEmpty(rdestController) && rdestRoute == "*")
                {
                    forwardUri = new Uri($"{context.HttpContext.Request.Scheme}://{_configuration["Proxy:ForwardDomain"]}/{rdestController}/{subRoute}");
                }



                if (forwardUri == null) throw new NullReferenceException($"ForwardURI was not set correctly in ExtendedAutoProxyAttribute! Current Values: Role: {_authorizationService.Role}, Username: {_authorizationService.Username}");
                var request = context.HttpContext.CreateProxyHttpRequest(forwardUri);
                var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                await context.HttpContext.CopyProxyHttpResponse(response);
                //  context.Result = new OkResult();

            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                await base.OnActionExecutionAsync(context, next);
            }
        }
    }
}
