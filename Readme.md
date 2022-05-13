
# .Net Proxy
Please Note:
*If you are planning to implement a reverse proxy, there are alternative solutions that have more functionality and a better performance than using this package to implement an Asp .Net Core Proxy.
This package is to be used in the rare cases where such an implementation is necessary or prefered for any reason.*

**THIS PACKAGE HAS NOT BEEN TESTED FOR ALL USE-CASES AS OF NOW**
```
Find alternative Reverse Proxy implementations here:

General reverse proxy:			https://www.nginx.com/
API-Gateway: 					https://www.krakend.io/
Middleware in .Net: 			https://auth0.com/blog/building-a-reverse-proxy-in-dot-net-core/
Framework in .Net: 				https://github.com/microsoft/reverse-proxy
```

#AUTOPROXY

This Proxy implements ControllerProxy and Proxy functionalities and smartly differentiates usages based on configuration.
It can be used with no configuration at all (dumb redirection of all calls, uses same controller name and endpoint method)
Minimal configuration (rerouting to other controller, automatically detects endpoint method)
And extended configuration (rerouting to other controller and endpoint method)

It can be set on a whole controller (for low-config proxying) and on single methods to determine endpoints on a per-method basis.

## AS A CONTROLLER ATTRIBUTE 

**This will map ALL controller methods to the backend endpoint controller "proxytest"**
 `[TypeFilter(typeof(AutoProxyAttribute), Arguments = new object[] { "proxytest" })]`
Example: Calling /autoproxy/something will forward to the backend function /proxytest/something

**This will map ALL controller methods to the backend endpoint controller with the same name**
 `[TypeFilter(typeof(AutoProxyAttribute))] `
Example: Calling /autoproxy/any will forward to the backend function /autoproxy/any

## AS A METHOD ATTRIBUTE 

**This will map the attributed method to the endpoint controller "proxytest", endpoint method "other"**
 `[TypeFilter(typeof(AutoProxyAttribute), Arguments = new object[] { "proxytest/other" })]`
Example: Calling /autoproxy/redirect will forward to the backend function /proxytest/other

**This will map the attributed method to the endpoint controller with the same name, endpoint method "any"**
 ` [TypeFilter(typeof(AutoProxyAttribute))]`
Example: Calling /autoproxy/any will forward to the backend function /autoproxy/any


## IMPLEMENT IN CONTROLLER


**To build a controller that forwards ALL actions and parameters to another controller of the same name:**
Set Controller-Attribute: `[TypeFilter(typeof(AutoProxyAttribute))]`
Create a Controller method named "Any", the result should look like this:


**This will forward all calls to the backend controller "proxytest":**
```
...

[TypeFilter(typeof(AutoProxyAttribute), Arguments = new object[] { "proxytest" })]
[Route("[controller]")]
public class AutoProxyController : ControllerBase
{

	[Route("{**anyRoute}")] //maps to anyroute defined by attribute on controller
	public dynamic Any()
	{
	    return new EmptyResult();
	}

}

...
```


**This will forward all calls to the backend controller "autoproxy":**
```
...

[TypeFilter(typeof(AutoProxyAttribute))]
[Route("[controller]")]
public class AutoProxyController : ControllerBase
{

	[Route("{**anyRoute}")]
	public dynamic Any()
	{
	    return new EmptyResult();
	}

}

...
```


**This will forward calls to the method "forward" to the endpoint "autoproxy/forward":**
```
...

[Route("[controller]")]
public class AutoProxyController : ControllerBase
{

	   [TypeFilter(typeof(AutoProxyAttribute))]
     [HttpGet("forward/{*anyRoute}")] //maps to internal autoproxycontroller
     public dynamic Auto()
     {
          return new EmptyResult();
     }


}

...
```


**This will forward calls to the method "forward" to the endpoint "proxytest/forward":**
```
...

[Route("[controller]")]
public class AutoProxyController : ControllerBase
{

	 [TypeFilter(typeof(AutoProxyAttribute), Arguments = new object[] { "proxytest" })]
     [HttpGet("forward/{*anyRoute}")] //maps to internal autoproxycontroller
     public dynamic Auto()
     {
          return new EmptyResult();
     }


}

...
```


**This will forward calls to the method "forward" to the endpoint "proxytest/something":**
```
...

[Route("[controller]")]
public class AutoProxyController : ControllerBase
{

	 [TypeFilter(typeof(AutoProxyAttribute), Arguments = new object[] { "proxytest", "something" })]
     [HttpGet("forward/{*anyRoute}")] //maps to internal autoproxycontroller
     public dynamic Auto()
     {
          return new EmptyResult();
     }


}

...
```




It is **absolutely important** that you return **EmptyResult** from a Method/Controller that is decorated with the  **AutoProxyAttribute** filter.

**The statement** {*anyRoute} **is a wildcard that matches anything coming after the part before, it will forward anything inside the wildcard to the backend**


That's it.


## Configuration

The Configuration is loaded into the attribute implementation by way of dependency injection of IConfiguration.
You will need to configure DI for IConfiguration like so:

Add a package reference to

```
Microsoft.Extensions.Configuration
```

Then

**For a Console application in Net6 (Program.cs)**

```
using Microsoft.Extensions.Hosting;

using IHost host = Host.CreateDefaultBuilder(args).Build();

await host.RunAsync();

```

**For ASP Net Core Net6 (Program.cs)**

```
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

```

You can configure the Parameter by using arguments

--- First argument maps to a controller
--- Second argument maps to a controller method

The backend API URI is configured via appsettings.json, [Proxy:ForwardDomain]
Like so:

*appsettings.json*
  ```
  {
  "Proxy":
	  {
	  "ForwardDomain":"localhost:7120"
	  }
  }
  ```



#ROLEMAPPINGPROXY

This Proxy is used to map backend endpoints based on a users role.
It needs a specific controller setup and method, the configuration is stored in the appsettings.

##SETUP

Your controller has to be look like this:

```
...

    [TypeFilter(typeof(RoleMappingProxyAttribute))]
    [Route("/rolemapping")]
    public sealed class RoleMappingTestController : ControllerBase
    {
        [Route("{**anyRoute}")]
        public dynamic Any()
        {
            return new EmptyResult();
        }
    }

...
```

This will direct all calls including subroutes, parameters and authentication tokens to the backend that has been configured.
The setup always stays like this for role-mapping controllers, the actual mapping logics resides in the appsettings.

Please note that Swagger will throw an error and not load if you implement the controller like this.
Since the "Any" endpoint is ambiguous for all HTTP Request types (such as GET, PATCH, DELETE, PUT, POST).
To fix the swagger error, the implementation has to look like this:


```
...

    [TypeFilter(typeof(RoleMappingProxyAttribute))]
    [Route("/rolemapping")]
    public sealed class RoleMappingTestController : ControllerBase
    {
        [HttpGet]
        [HttpPut]
        [HttpDelete]
        [HttpPatch]
        [HttpPost]
        [Route("{**anyRoute}")]
        public dynamic Any()
        {
            return new EmptyResult();
        }
    }

...
```

This will prompt swagger to display an extra UI-function for each method-type, the error will be resolved.

##Configuration

To configure the RoleMappingProxyAttribute in your appsettings:


*appsettings.json*
  ```
  "Proxy": {
    "ForwardDomain": "localhost:7176",
    "RoleMappings": [
      {
        "ControllerName": "rolemappingtest",
        "Path": "test",
        "Mapping": [
          {
            "Role": "admin",
            "Controller": "controlleradmin",
            "Route": "routeadmin"
          },
          {
            "Role": "user",
            "Controller": "controlleruser",
            "Route": "routeuser"
          },
          {
            "Role": "basic_access",
            "Controller": "data",
            "Route": "*"
          }
        ]

      },
      {
        "ControllerName": "company",
        "Path": "List",
        "Mapping": [
          {
            "Role": "admin",
            "Controller": "companyadmin",
            "Route": "List"
          },
          {
            "Role": "user",
            "Controller": "companyuser",
            "Route": "Error"
          }
        ]

      },
      {
        "ControllerName": "dataset",
        "Path": "*",
        "Mapping": [
          {
            "Role": "admin",
            "Controller": "adminctrl",
            "Route": "*"
          },
          {
            "Role": "user",
            "Controller": "userctrl",
            "Route": "*"
          }
        ]

      },
	  {
        "ControllerName": "smart",
        "Path": "data",
        "Mapping": [
          {
            "Role": "admin",
            "Controller": "",
            "Route": "admindata"
          },
          {
            "Role": "user",
            "Controller": "",
            "Route": "userdata"
          }
        ]

      },
	  {
        "ControllerName": "cool",
        "Path": "full",
        "Mapping": [
          {
            "Role": "admin",
            "Controller": "adminctrl",
            "Route": "*"
          },
          {
            "Role": "user",
            "Controller": "userctrl",
            "Route": "*"
          }
        ]

      }
    ]
  }
  ```

This configuration displays following information:

The domain we are proxying to is: "localhost:7176"

We have 5 Controllers that use Role-Based endpoint mappings.

- The first one is the "rolemappingtest" controller.
-- if the role "admin" calls the method "test" on this controller, he will be forwarded to localhost:7176/controlleradmin/routeadmin
-- if the role "user" calls the method "test" on this controller, he will be forwarded to localhost:7176/controlleruser/routeuser
-- if the role "basic_access" calls the method "test" on this controller, he will be forwarded to localhost:7176/data/test

- The second is the "company" controller.
-- if the role "admin" calls the method "list" on this controller, he will be forwarded to localhost:7176/companyadmin/list
-- if the role "user" calls the method "list" on this controller, he will be forwarded to localhost:7176/companyuser/error

- The third is the "dataset" controller.
-- if the role "admin" calls ANY METHOD on this controller, he will be forwarded to localhost:7176/adminctrl/[whatever method he called]
-- if the role "user" calls ANY METHOD on this controller, he will be forwarded to localhost:7176/userctrl/[whatever method he called]

- The forth is the "smart" controller.
-- if the role "admin" calls the method "data" on this controller, he will be forwarded to localhost:7176/admindata
-- if the role "user" calls the method "data" on this controller, he will be forwarded to localhost:7176/smart/userdata

- The fifth is the "cool" controller.
-- if the role "admin" calls ANY METHOD on this controller, he will be forwarded to localhost:7176/adminctrl/full
-- if the role "user" calls ANY METHOD on this controller, he will be forwarded to localhost:7176/userctrl/full
