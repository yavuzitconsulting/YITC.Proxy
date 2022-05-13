using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using YITC.Proxy;

namespace YITC.Proxy
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ProxyAttribute : ActionFilterAttribute
    {
        private readonly string? _path = "";
        private readonly HttpClient _client;
        private readonly IConfiguration _configuration;
        private readonly string[] _defaultControllerActions = { "Get", "Post", "Put", "Delete" };

        //private readonly IProxyService _proxyService;

        /// <summary>
        /// This constructor requires a path on the receiving end (Proxy-Mapping)
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="path"></param>
        public ProxyAttribute(IConfiguration configuration, string? path = null)
        {
            this._path = path;
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
                var controllerAction = controllerContext.RouteData.Values["action"]?.ToString() ?? "";

                //Filter out default controller actions. Not case sensitive since default always starts uppercase
                if (_defaultControllerActions.Any(x => x == controllerAction))
                {
                    controllerAction = String.Empty;
                }

                //if no path is passed, automatically map to destination controller/method
                Uri forwardUri = _path == null ? new Uri($"{context.HttpContext.Request.Scheme}://{_configuration["Proxy:ForwardDomain"]}/{controllerName}/{controllerAction}") : new Uri($"{context.HttpContext.Request.Scheme}://{_configuration["Proxy:ForwardDomain"]}/{_path}");


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
