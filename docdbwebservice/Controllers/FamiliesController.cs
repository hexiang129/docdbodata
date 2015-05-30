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

//Install-Package Microsoft.AspNet.OData
//Install-Package Microsoft.OData.Core
namespace docdbwebservice.Controllers
{
    /*
    The WebApiConfig class may require additional changes to add a route for this controller. Merge these statements into the Register method of the WebApiConfig class as applicable. Note that OData URLs are case sensitive.

    using System.Web.Http.OData.Builder;
    using System.Web.Http.OData.Extensions;
    using Model;
    ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
    builder.EntitySet<Family>("Families");
    config.Routes.MapODataServiceRoute("odata", "odata", builder.GetEdmModel());
    */
    public class FamiliesController : BaseController
    {
       
        // GET: odata/Families
        [HttpGet]
        [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.Filter | AllowedQueryOptions.SkipToken, MaxAnyAllExpressionDepth=2)]
        public async Task<IHttpActionResult> GetFamilies(ODataQueryOptions<Family> queryOptions)
        {
            // validate the query.
            var exception = ValidateQuery(queryOptions);
            if (exception != null)
            {
                return BadRequest(exception.Message);
            }
            var translator = new DocDBQueryGenerator();
            var query = translator.TranslateToDocDBQuery(queryOptions);
            
            var skiptoken = queryOptions.RawValues.SkipToken;
            var filter = queryOptions.RawValues.Filter;
            var res = await DocDbRepository.DocDbRepository.QueryWithPagingAsyncTypeless(query, skiptoken);
            var response = Ok<IEnumerable<Family>>(res.Item1);
            
            if (res.Item2 != null)
            {
                response.Request.ODataProperties().NextLink =
                    GenerateNextLinkURL(queryOptions.Request.RequestUri, filter, res.Item2);
            }
            return response;
        }

        private Uri GenerateNextLinkURL(Uri requesturi, string odatafilter, string continuationtoken)
        {
            if (string.IsNullOrWhiteSpace(odatafilter))
            {
                return new Uri(string.Format("{0}://{1}{2}?$skiptoken={3}",
                    requesturi.Scheme, requesturi.Host, requesturi.LocalPath,
                    HttpUtility.UrlEncode(continuationtoken)));
            }
            else
            {
                return new Uri(string.Format("{0}://{1}{2}?$filter={4}&$skiptoken={3}",
                    requesturi.Scheme, requesturi.Host, requesturi.LocalPath,
                    HttpUtility.UrlEncode(continuationtoken),
                    HttpUtility.UrlEncode(odatafilter)));
            }
        }

        // GET: odata/Families(5)
        public async Task<IHttpActionResult> GetFamily([FromODataUri] string key, ODataQueryOptions<Family> queryOptions)
        {
            // validate the query.
            try
            {
                queryOptions.Validate(validationSettings);
            }
            catch (ODataException ex)
            {
                return BadRequest(ex.Message);
            }

            // return Ok<Family>(family);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // PUT: odata/Families(5)
        public async Task<IHttpActionResult> Put([FromODataUri] string key, Delta<Family> delta)
        {
            Validate(delta.GetEntity());

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // TODO: Get the entity here.

            // delta.Put(family);

            // TODO: Save the patched entity.

            // return Updated(family);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // POST: odata/Families
        public async Task<IHttpActionResult> Post(Family family)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // TODO: Add create logic here.

            // return Created(family);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // PATCH: odata/Families(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public async Task<IHttpActionResult> Patch([FromODataUri] string key, Delta<Family> delta)
        {
            Validate(delta.GetEntity());

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // TODO: Get the entity here.

            // delta.Patch(family);

            // TODO: Save the patched entity.

            // return Updated(family);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // DELETE: odata/Families(5)
        public async Task<IHttpActionResult> Delete([FromODataUri] string key)
        {
            // TODO: Add delete logic here.

            // return StatusCode(HttpStatusCode.NoContent);
            return StatusCode(HttpStatusCode.NotImplemented);
        }
    }
}
