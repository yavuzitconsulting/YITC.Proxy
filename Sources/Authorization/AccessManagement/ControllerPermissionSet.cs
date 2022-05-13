
namespace YITC.Proxy.Authorization.AccessManagement
{
    public class ControllerPermissionSet
    {
        public ControllerPermissionSet(string controllerName, string controllerMethod)
        {
            this.ControllerName = controllerName;
            this.ControllerMethod = controllerMethod;
        }
        public string ControllerName { get; set; }
        public string ControllerMethod { get; set; }
    }


}
