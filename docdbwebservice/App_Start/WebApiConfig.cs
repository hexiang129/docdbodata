using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;
using Model;
using Microsoft.ApplicationInsights;

namespace docdbwebservice
{
    public class TelemetryHelper
    {
        /// <summary>
        /// The telemetry client being used for AI logging.
        /// </summary>
        private TelemetryClient telemetry = new TelemetryClient();

        /// <summary>
        /// helper method to log exception
        /// </summary>
        /// <param name="e">The exception.</param>
        /// <param name="alsoLogAsCustomMetric">true means also logging this exception as a custom metric 
        /// to Application Insight service</param>
        public void LogException(Exception e)
        {
            telemetry.TrackException(e);
        }

        public static TelemetryHelper Logger = new TelemetryHelper();
    }

    public class AiExceptionLogger : ExceptionLogger
    {
        public override void Log(ExceptionLoggerContext context)
        {
            if (context != null && context.Exception != null)
            {
                var logger = TelemetryHelper.Logger;
                logger.LogException(context.Exception);
            }
            base.Log(context);
        }
    }

    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            //config.Routes.Insert(0, "homepage", new HttpRou)

            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Family>("Families");
            builder.EntitySet<FamilyForTest>("FamilyForTests");
            config.MapODataServiceRoute("odata", "odata", builder.GetEdmModel());
            config.Services.Add(typeof(IExceptionLogger), new AiExceptionLogger());
        }
    }
}
