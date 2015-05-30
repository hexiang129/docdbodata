namespace DocDbRepository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// helper routines to read data from document db through DocumentClient
    /// </summary>
    public static class DocDbRepository
    {
        private static DocumentClient client = null;
        private static DocumentCollection documentCollection = null;

        /// <summary>
        /// create documentdb client and cache it as a singleton
        /// get the document collection through the client
        /// throw exceptions if the collection does not exists.
        /// </summary>
        /// <returns>true if it succeeds</returns>
        public static bool Setup(string EndpointUrl, string AuthorizationKey,
            string databaseName, string documentCollectionName)
        {
            if (client == null)
            {
                var connectionPolicy = new ConnectionPolicy()
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Https
                };
                client = new DocumentClient(new Uri(EndpointUrl), AuthorizationKey, connectionPolicy);
            }

            // Check to verify a database with the id=FamilyRegistry does not exist
            var database = client.CreateDatabaseQuery().Where(db => db.Id == databaseName).AsEnumerable().FirstOrDefault();

            if (database == null)
            {
                throw new InvalidOperationException("there is no database whose name is " + databaseName);
            }

            documentCollection = client.CreateDocumentCollectionQuery(database.CollectionsLink)
                .Where(c => c.Id == documentCollectionName).AsEnumerable().FirstOrDefault();

            if (documentCollection == null)
            {
                throw new InvalidOperationException(
                    "there is no document collection whose name is " + documentCollectionName
                    + " in database " + databaseName);
            }

            return true;
        }

        // in documentdb, if you use select join, it will be like
        // select c from c join d in c.DataCenters where d="singapore"
        // the c here represent a document and other names can be used as well.
        // "select *" is not supported.
        // The returned json will be wrapped inside a property "c":
        // Hence we need to define the class to extract the returned json out of this property
        public class DocumentWrapper<A>
        {
            // The property name "c" needs to match with the query we sent to documentdb.
            // The query should always has the following format
            // select c from .....
            // if we decide to use a different query format, such as "select foo from ..."
            // then "c" needs to be changed to "foo" in the next line.
            public A c;
        };

        /// <summary>
        /// cast the dyanmic type d to an object of Tc and save it to the list l.
        /// </summary>
        private static void CastAndSave<T>(dynamic d, List<Model.Family> l) where T : Model.Family
        {
            DocumentWrapper<T> tc = d;
            l.Add(tc.c);
        }

        /// <summary>
        /// send the query to document db with an optional continuation token
        /// </summary>
        /// <typeparam name="T">we expect document db to return us a list of objects of type T.</typeparam>
        /// <param name="querystr">the query string, which should have format "select c from ..."</param>
        /// <param name="continuationToken">document db continuation token, can be null.</param>
        /// <returns>
        /// A tuple:
        /// first component is the query result: an  IEnumerable of objects of type T.
        /// second component is the continuation token. will be null there isn't any.
        /// </returns>
        public static async Task<Tuple<IEnumerable<T>, string>>
            QueryWithPagingAsyncUseContinuationToken<T>(string querystr, string continuationToken)
        {
            string colSelfLink = documentCollection.SelfLink;

            FeedOptions options = null;

            if (string.IsNullOrWhiteSpace(continuationToken))
            {
                options = new FeedOptions { MaxItemCount = 100 };
            }
            else
            {
                options = new FeedOptions { MaxItemCount = 100, RequestContinuation = continuationToken };
            }

            var query = client.CreateDocumentQuery<T>(colSelfLink, querystr, options).AsDocumentQuery();
            var result = await query.ExecuteNextAsync();
            var count = result.Count();
            var racks = new List<T>();

            foreach (var d in result)
            {
                // the querystr should have format "select c from ..."
                // the returned result will be wrapped inside a property whose name is "c".
                // hence we use this class Tc<T> to extract the result from this property
                DocumentWrapper<T> r = (dynamic)d;
                racks.Add(r.c);
            }
            string continuation = null;
            if (query.HasMoreResults)
            {
                continuation = result.ResponseContinuation;
            }
            var res = new Tuple<IEnumerable<T>, string>(racks, continuation);
            return res;
        }

        /// <summary>
        /// send the query to document db with an optional continuation token
        /// In this case we expect documentdb to return us a list of objects of type whose base class is MsfItem, 
        /// The object will be casted to different types based on its actual type (such as Racks, Chassis, etc.)
        /// </summary>
        /// <param name="querystr">the query string which should have format "select c from..."</param>
        /// <param name="continuationToken">continuation token</param>
        /// <returns>
        /// A tuple:
        /// first component is the query result: an  IEnumerable of objects of type MsfItem.
        /// second component is the continuation token. will be null there isn't any.
        /// </returns>
        public static async Task<Tuple<IEnumerable<Model.Family>, string>>
            QueryWithPagingAsyncTypeless(string querystr, string continuationToken)
        {
            string colSelfLink = documentCollection.SelfLink;

            FeedOptions options = null;

            if (string.IsNullOrWhiteSpace(continuationToken))
            {
                options = new FeedOptions { MaxItemCount = 100 };
            }
            else
            {
                options = new FeedOptions { MaxItemCount = 100, RequestContinuation = continuationToken };
            }

            var items = new List<Model.Family>();

            var query = client.CreateDocumentQuery<dynamic>(colSelfLink, querystr, options).AsDocumentQuery();

            var result = await query.ExecuteNextAsync();
            foreach (var d in result)
            {
                CastAndSave<Model.Family>(d, items);
            }
            string continuation = null;
            if (query.HasMoreResults)
            {
                continuation = result.ResponseContinuation;
            }
            var res = new Tuple<IEnumerable<Model.Family>, string>(items, continuation);
            return res;
        }
    }
}

