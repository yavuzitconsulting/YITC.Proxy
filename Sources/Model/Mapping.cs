using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YITC.Proxy.Model
{
    public class Mapping
    {
        public Mapping()
        {
            this.Role = String.Empty;
            this.Controller = String.Empty;
            this.Route = String.Empty;
        }

        public Mapping(string role, string controller, string route)
        {
            this.Role = role;
            this.Controller = controller;
            this.Route = route;
        }
        public string Role { get; set; }
        public string Controller { get; set; }
        public string Route { get; set; }
    }
}
