
namespace YITC.Proxy.Authorization.AccessManagement
{
    public class RolePermissionSet
    {
        public RolePermissionSet()
        {
            this.Controllers = new();
        }
        public string Role { get; set; }
        public List<ControllerPermissionSet> Controllers { get; set; }
    }


}
