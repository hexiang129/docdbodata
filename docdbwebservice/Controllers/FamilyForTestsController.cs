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
    /*
    The WebApiConfig class may require additional changes to add a route for this controller. Merge these statements into the Register method of the WebApiConfig class as applicable. Note that OData URLs are case sensitive.

    using System.Web.Http.OData.Builder;
    using System.Web.Http.OData.Extensions;
    using Model;
    ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
    builder.EntitySet<FamilyForTest>("FamilyForTests");
    config.Routes.MapODataServiceRoute("odata", "odata", builder.GetEdmModel());
    */
    public class FamilyForTestsController : BaseController
    {
        // GET: odata/FamilyForTests
        [HttpGet]
        public async Task<IHttpActionResult> GetFamilyForTests(ODataQueryOptions<FamilyForTest> queryOptions)
        {
            // validate the query.
            var exception = ValidateQuery(queryOptions);
            if (exception != null)
            {
                return BadRequest(exception.Message);
            }
            var translator = new DocDBQueryGenerator();
            var query = translator.TranslateToDocDBQuery(queryOptions);
            
            return Ok<string>(query);
        }

        // GET: odata/FamilyForTests(5)
        public async Task<IHttpActionResult> GetFamilyForTest([FromODataUri] string key, ODataQueryOptions<FamilyForTest> queryOptions)
        {
            // validate the query.
            var exception = ValidateQuery(queryOptions);
            if (exception != null)
            {
                return BadRequest(exception.Message);
            }

            // return Ok<FamilyForTest>(familyForTest);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // PUT: odata/FamilyForTests(5)
        public async Task<IHttpActionResult> Put([FromODataUri] string key, Delta<FamilyForTest> delta)
        {
            Validate(delta.GetEntity());

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // TODO: Get the entity here.

            // delta.Put(familyForTest);

            // TODO: Save the patched entity.

            // return Updated(familyForTest);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // POST: odata/FamilyForTests
        public async Task<IHttpActionResult> Post(FamilyForTest familyForTest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // TODO: Add create logic here.

            // return Created(familyForTest);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // PATCH: odata/FamilyForTests(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public async Task<IHttpActionResult> Patch([FromODataUri] string key, Delta<FamilyForTest> delta)
        {
            Validate(delta.GetEntity());

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // TODO: Get the entity here.

            // delta.Patch(familyForTest);

            // TODO: Save the patched entity.

            // return Updated(familyForTest);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // DELETE: odata/FamilyForTests(5)
        public async Task<IHttpActionResult> Delete([FromODataUri] string key)
        {
            // TODO: Add delete logic here.

            // return StatusCode(HttpStatusCode.NoContent);
            return StatusCode(HttpStatusCode.NotImplemented);
        }
    }
}
