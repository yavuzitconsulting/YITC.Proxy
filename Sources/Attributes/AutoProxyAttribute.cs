using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace YITC.Proxy
{



    /// <summary>
    /// BigBrain Smart-Proxy implementation which can be used as a method and a controller attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class AutoProxyAttribute : ActionFilterAttribute
    {
        private readonly string? _destinationController = "";
        private readonly string? _destinationRoute = "";
        private readonly HttpClient _client;
        private readonly IConfiguration _configuration;
        private readonly string[] _defaultControllerActions = { "Get", "Post", "Put", "Delete", "Any" };

        //private readonly IProxyService _proxyService;

        /// <summary>
        /// This constructor requires a path on the receiving end (Proxy-Mapping)
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="destinationController"></param>
        public AutoProxyAttribute(IConfiguration configuration, string? destinationRoute = null)
        {
            string[] fullRoute = destinationRoute?.Split('/') ?? new string[] { String.Empty };
            this._destinationController = fullRoute.First(); //controller
            this._destinationRoute = string.Join('/', fullRoute.Skip(1)); //do not add controller
            this._configuration = configuration;
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
                string? subRoute = String.Empty;
                context.RouteData.Values.TryGetValueAs("anyRoute", out subRoute);
                subRoute = subRoute ?? String.Empty; //dont want NULL as value, but trygetvalueas can nullify the string


                //Filter out default controller actions. Not case sensitive since default always starts uppercase
                if (_defaultControllerActions.Any(x => x == route))
                {
                    route = String.Empty;


                }

                if (route.Length > 0) route = route.LastIndexOf('/') > 0 ? route : route + "/";
                route += subRoute;

                Uri forwardUri = null;
                if (String.IsNullOrEmpty(_destinationController) && String.IsNullOrEmpty(_destinationRoute))
                {
                    forwardUri = new Uri($"{context.HttpContext.Request.Scheme}://{_configuration["Proxy:ForwardDomain"]}/{controllerName}/{route}");
                }
                else if (!String.IsNullOrEmpty(_destinationController) && String.IsNullOrEmpty(_destinationRoute))
                {
                    forwardUri = new Uri($"{context.HttpContext.Request.Scheme}://{_configuration["Proxy:ForwardDomain"]}/{_destinationController}/{route}");
                }
                else if (!String.IsNullOrEmpty(_destinationController) && !String.IsNullOrEmpty(_destinationRoute))
                {
                    forwardUri = new Uri($"{context.HttpContext.Request.Scheme}://{_configuration["Proxy:ForwardDomain"]}/{_destinationController}/{_destinationRoute}/{subRoute}");
                }
                else if (String.IsNullOrEmpty(_destinationController) && !String.IsNullOrEmpty(_destinationRoute))
                {
                    forwardUri = new Uri($"{context.HttpContext.Request.Scheme}://{_configuration["Proxy:ForwardDomain"]}/{controllerName}/{_destinationRoute}/{subRoute}");
                }

                var request = context.HttpContext.CreateProxyHttpRequest(forwardUri);
                var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                await context.HttpContext.CopyProxyHttpResponse(response);
                //  context.Result = new OkResult();

            }
            finally
            {
                await base.OnActionExecutionAsync(context, next);
            }
        }
    }
}
