using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

using DocDbRepository;

namespace docdbwebservice
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            Microsoft.ApplicationInsights.Extensibility.
            TelemetryConfiguration.Active.InstrumentationKey =
            System.Web.Configuration.WebConfigurationManager.AppSettings["ApplicationInsightiKey"];

            DocDbRepository.DocDbRepository.Setup(
                ConfigurationManager.AppSettings["DocDB:EndPointUrl"],
                ConfigurationManager.AppSettings["DocDB:AuthorizationKey"],
                ConfigurationManager.AppSettings["DocDB:DatabaseName"],
                ConfigurationManager.AppSettings["DocDB:CollectionName"]
                );
        }
    }
}
