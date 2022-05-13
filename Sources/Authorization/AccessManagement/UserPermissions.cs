

using Microsoft.Extensions.Configuration;

namespace YITC.Proxy.Authorization.AccessManagement
{
    public static class UserPermissions
    {
        public static List<RolePermissionSet> RolePermissions = new List<RolePermissionSet>();

        public static void Add(RolePermissionSet permissionSet)
        {
            RolePermissions.Add(permissionSet);
        }

        public static void Configure(List<RolePermissionSet> rolePermissions)
        {
            RolePermissions = rolePermissions;
        }
        /// <summary>
        /// Recommended way to configure
        /// </summary>
        /// <param name="configuration"></param>
        public static void Configure(IConfiguration configuration)
        {
            var rolesSection = configuration.GetSection("Roles");
            foreach (var role in rolesSection.GetChildren())
            {
                RolePermissionSet parsedRole = new RolePermissionSet();

                foreach (var rolePermissions in role.GetChildren())
                {
                    parsedRole.Role = rolePermissions.Key;
                    foreach (var controller in rolePermissions.GetChildren())
                    {
                        var controllerName = controller.GetValue<string>("Controller");
                        var controllerActions = controller.GetSection("Actions").Get<string[]>();
                        foreach (string action in controllerActions)
                        {
                            parsedRole.Controllers.Add(new ControllerPermissionSet(controllerName, action));
                        }
                    }

                }
                RolePermissions.Add(parsedRole);
            }
        }

        public static bool Validate(string role, string controller, string action)
        {
            var permissionSet = RolePermissions.FirstOrDefault(x => x.Role == role);
            if (permissionSet == null) return false;
            return permissionSet.Controllers.Exists(x =>
                        (x.ControllerName == "*" && x.ControllerMethod == "*") || //Support for wildcard controllers (e.g. admin)
                        (x.ControllerName == controller && x.ControllerMethod == "*") ||
                        (x.ControllerName == controller && x.ControllerMethod == action)); //Support for Wildcard actions
        }

    }


}
