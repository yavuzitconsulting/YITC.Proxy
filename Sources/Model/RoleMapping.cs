using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YITC.Proxy.Model
{
    public class RoleMapping
    {
        public RoleMapping()
        {
            this.ControllerName = String.Empty;
            this.Mapping = new List<Mapping>();
        }
        public string ControllerName { get; set; }
        public string Path { get; set; }
        public List<Mapping> Mapping { get; set; }
    }
}
