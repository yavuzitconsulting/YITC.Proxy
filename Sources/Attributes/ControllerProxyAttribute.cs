using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using YITC.Proxy;

namespace YITC.Proxy
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ControllerProxyAttribute : ActionFilterAttribute
    {
        private readonly string? _destinationController = "";
        private readonly HttpClient _client;
        private readonly IConfiguration _configuration;
        private readonly string[] _defaultControllerActions = { "Get", "Post", "Put", "Delete", "Any" };

        //private readonly IProxyService _proxyService;

        /// <summary>
        /// This constructor requires a path on the receiving end (Proxy-Mapping)
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="destinationController"></param>
        public ControllerProxyAttribute(IConfiguration configuration, string? destinationController = null)
        {
            this._destinationController = destinationController;
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
                //context.RouteData.Values /Keys (catchall) / Values 
                string? subRoute = String.Empty;
                context.RouteData.Values.TryGetValueAs("anyRoute", out subRoute);
                subRoute = subRoute ?? String.Empty; //dont want NULL as value, but trygetvalueas can nullify the string

                var controllerContext = ((Microsoft.AspNetCore.Mvc.ControllerBase)context.Controller).ControllerContext;
                var controllerName = controllerContext.RouteData.Values["controller"]?.ToString() ?? "";
                var route = controllerContext.RouteData.Values["action"]?.ToString() ?? "";

                //Filter out default controller actions. Not case sensitive since default always starts uppercase
                if (_defaultControllerActions.Any(x => x == route))
                {
                    route = String.Empty;
                    if (subRoute != String.Empty)
                    {
                        route = subRoute;
                    }
                }
                else
                {
                    route = $"{route}/{subRoute}";
                }

                //if no path is passed, automatically map to destination controller/method
                Uri forwardUri = _destinationController == null ? new Uri($"{context.HttpContext.Request.Scheme}://{_configuration["Proxy:ForwardDomain"]}/{controllerName}/{route}") : new Uri($"{context.HttpContext.Request.Scheme}://{_configuration["Proxy:ForwardDomain"]}/{_destinationController}/{route}");


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
