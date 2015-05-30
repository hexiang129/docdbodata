using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using System.Web.OData;
using System.Web.OData.Query;
using System.Web.OData.Routing;
using System.Web.OData.Extensions;
using Model;
using Microsoft.Data.OData;
using QueryTranslator;

namespace docdbwebservice.Controllers
{
    public abstract class BaseController : ODataController
    {
        protected static ODataValidationSettings validationSettings = new ODataValidationSettings();

        /// <summary>
        /// Initializes a new instance of the <see cref="TDataModelType"/> class.
        /// </summary>
        /// <param name="dataModel">The data model.</param>
        protected BaseController()
        {
            validationSettings.AllowedQueryOptions = AllowedQueryOptions.Filter | AllowedQueryOptions.SkipToken;
            validationSettings.MaxAnyAllExpressionDepth = 2;
        }


        /// <summary>
        /// validate odata query using validationSettings.
        /// return null if no exception happens.
        /// otherwise return the exception.
        /// </summary>
        protected Exception ValidateQuery<T>(ODataQueryOptions<T> queryOptions)
        {
            try
            {
                queryOptions.Validate(validationSettings);
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
    }
}