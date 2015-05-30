//seed the database with some random family record information
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;

namespace seeddocdb
{
    class Program
    {
        private static DocumentClient client;
        private static DocumentCollection documentCollection;

        // your docdb endpoint. 
        private static string EndpointUrl = "https://testdocdb.documents.azure.com:443/";

        // authorization key to docdb account
        private static string AuthorizationKey = "your docdb authorization key";


        public static async Task<bool> Setup(bool recreate = true)
        {

            // Check to verify a database with the id=FamilyRegistry does not exist
            var database = client.CreateDatabaseQuery().Where(db => db.Id == "FamilyRegistry").AsEnumerable().FirstOrDefault();

            if (database == null)
            {
                // Create a database
                database = await client.CreateDatabaseAsync(
                    new Database
                    {
                        Id = "FamilyRegistry"
                    });
            }

            if (recreate)
            {
                documentCollection = client.CreateDocumentCollectionQuery(database.CollectionsLink).Where(c => c.Id == "TestFamilyCollection").AsEnumerable().FirstOrDefault();

                if (documentCollection != null)
                {
                    await client.DeleteDocumentCollectionAsync(documentCollection.SelfLink);
                }
            }

            documentCollection = client.CreateDocumentCollectionQuery(database.CollectionsLink).Where(c => c.Id == "TestFamilyCollection").AsEnumerable().FirstOrDefault();

            if (documentCollection == null)
            {
                // Create a document collection
                documentCollection = await client.CreateDocumentCollectionAsync(database.CollectionsLink,
                    new DocumentCollection
                    {
                        Id = "TestFamilyCollection"
                    });
            }

            bulkInsertSP = await registerBulkInsertStoredProcedure(recreate);
            return true;
        }

        public static async Task<StoredProcedure> registerBulkInsertStoredProcedure(bool recreate)
        {
            var bulkSproc = new StoredProcedure
            {
                Id = "bulkInsertDoc",
                Body = @"
            function bulkImport(docs) {
    var collection = getContext().getCollection();
    var collectionLink = collection.getSelfLink();

    // The count of imported docs, also used as current doc index.
    var count = 0;

    // Validate input.
    if (!docs) throw new Error(""The array is undefined or null."");

    var docsLength = docs.length;
    if (docsLength == 0) {
        getContext().getResponse().setBody(0);
    }

    // Call the create API to create a document.
    tryCreate(docs[count], callback);

    // Note that there are 2 exit conditions:
    // 1) The createDocument request was not accepted. 
    //    In this case the callback will not be called, we just call setBody and we are done.
    // 2) The callback was called docs.length times.
    //    In this case all documents were created and we don’t need to call tryCreate anymore. Just call setBody and we are done.
    function tryCreate(doc, callback) {
        var isAccepted = collection.createDocument(collectionLink, doc, callback);

        // If the request was accepted, callback will be called.
        // Otherwise report current count back to the client, 
        // which will call the script again with remaining set of docs.
        if (!isAccepted) getContext().getResponse().setBody(count);
    }

    // This is called when collection.createDocument is done in order to process the result.
    function callback(err, doc, options) {
        if (err) throw err;

        // One more document has been inserted, increment the count.
        count++;

        if (count >= docsLength) {
            // If we created all documents, we are done. Just set the response.
            getContext().getResponse().setBody(count);
        } else {
            // Create next document.
            tryCreate(docs[count], callback);
        }
    }
}"
            };

            return await registerStoredProcedure(bulkSproc, recreate);
        }

        public static StoredProcedure bulkInsertSP;

        public static async Task<StoredProcedure> registerStoredProcedure(StoredProcedure sp, bool recreate)
        {
            // register stored procedure
            if (recreate)
            {
                var existingSP = client.CreateStoredProcedureQuery(documentCollection.SelfLink)
                .Where(c => c.Id == sp.Id).AsEnumerable().FirstOrDefault();

                if (existingSP != null)
                {
                    await client.DeleteStoredProcedureAsync(existingSP.SelfLink);
                }
            }

            var newSP = client.CreateStoredProcedureQuery(documentCollection.SelfLink)
            .Where(c => c.Id == sp.Id).AsEnumerable().FirstOrDefault();

            if (newSP == null)
            {
                newSP = await client.CreateStoredProcedureAsync(documentCollection.SelfLink,
                sp);
            }
            return newSP;
        }

        private static void ExceptionHandler(Microsoft.Azure.Documents.DocumentClientException e)
        {
            if ((int)e.StatusCode == 429)
            {
                Console.WriteLine("get 429. sleep for " + e.RetryAfter.ToString());
                System.Threading.Thread.Sleep(e.RetryAfter);
            }
            else
            {
                Console.WriteLine("get exception " + e.Message);
                throw e;
            }
        }

        private static async Task<long> invokeStoredProcHelper(dynamic parameters)
        {
            while (true)
            {
                try
                {
                    var response = await client.ExecuteStoredProcedureAsync<long>(bulkInsertSP.SelfLink, parameters);

                    Console.WriteLine("number of documents created:" + response.Response);
                    return response.Response;
                }
                catch (Microsoft.Azure.Documents.DocumentClientException e)
                {
                    ExceptionHandler(e);
                }
            }
        }

        public static async Task<long> SaveFamilyRecordsBulk(List<Model.Family> chassisSKUs)
        {
            return await invokeStoredProcHelper(chassisSKUs);
        }

        public static List<string> GetListFromTextFile(string fileName)
        {
            return System.IO.File.ReadAllLines(fileName).ToList();
        }

        public static List<string> Cities;
        public static List<string> Counties;
        public static List<string> States;
        public static List<string> Surnames;
        public static List<string> GivenNamesGirl;
        public static List<string> GivenNamesBoy;

        public static void InitializeLists()
        {
            Cities= GetListFromTextFile("city.txt");
            Counties= GetListFromTextFile("county.txt");
            States= GetListFromTextFile("state.txt");
            Surnames= GetListFromTextFile("surname.txt");
            GivenNamesGirl= GetListFromTextFile("givennamegirl.txt");
            GivenNamesBoy= GetListFromTextFile("givennameboy.txt");
        }

        public static T ChooseRandomlyFromAList<T>(List<T> list) where T: class
        {
            if (list == null) return null;
            var cnt = list.Count();
            if (cnt == 0) return null;
            var id = r.Next(cnt);
            if(id<0 || id >= cnt)
            {
                throw new IndexOutOfRangeException(id + " is out of bound " + cnt);
            }
            return list[id];
        }

        public static Model.Gender ChooseGender()
        {
            return r.NextDouble() > 0.5 ? Model.Gender.Male : Model.Gender.Female;
        }

        public static bool ChooseRegistration()
        {
            return r.NextDouble() > 0.5;
        }
        public static int ChooseNumberOfParents()
        {
            return r.NextDouble() < 0.9 ? 2 : 1;
        }

        public static int ChooseNumberOfKids()
        {
            return r.Next(5);
        }

        public static int ChooseNumberOfPets()
        {
            return r.Next(3);
        }

        public static string ChooseSurNames()
        {
            return ChooseRandomlyFromAList(Surnames);
        }

        public static string ChooseGivenNames(Model.Gender gender)
        {
            if(gender==Model.Gender.Female)
                return ChooseRandomlyFromAList(GivenNamesGirl);

            if (gender == Model.Gender.Male)
                return ChooseRandomlyFromAList(GivenNamesBoy);

            throw new NotImplementedException();
        }

        public static Model.Family GenerateFamilyRecords()
        {
            var f = new Model.Family();
            var familyName = ChooseSurNames();
            f.id = Guid.NewGuid().ToString();
            f.parents = new List<Model.Parent>();
            int parentsNbr = ChooseNumberOfParents();
            var parent1 = new Model.Parent();
            parent1.familyName = familyName;
            parent1.givenName = ChooseGivenNames(ChooseGender());
            f.parents.Add(parent1);
            if(parentsNbr == 2)
            {
                var parent2 = new Model.Parent();
                parent2.familyName = familyName;
                parent2.givenName = ChooseGivenNames(ChooseGender());
                f.parents.Add(parent2);
            }

            f.isRegistered = ChooseRegistration();
            f.children = new List<Model.Child>();
            int childrenNbr = ChooseNumberOfKids();
            for (int i=0;i<childrenNbr;i++)
            {
                var c = new Model.Child();
                c.pets = new List<Model.Pet>();
                int petNbr = ChooseNumberOfPets();
                for (int j=0;j<petNbr;j++)
                {
                    var p = new Model.Pet();
                    p.givenName = ChooseGivenNames(ChooseGender());
                    c.pets.Add(p);
                }
                c.familyName = familyName;
                c.gender = ChooseGender();
                c.givenName = ChooseGivenNames(c.gender);
                c.grade = r.Next(8) + 1;
                f.children.Add(c);
            }
            f.parentsCount = f.parents.Count();
            f.childrenCount = f.children.Count();

            f.address = new Model.Address();
            f.address.city = ChooseRandomlyFromAList(Cities);
            f.address.county = ChooseRandomlyFromAList(Counties);
            f.address.state = ChooseRandomlyFromAList(States);
            return f;
        }

        public static async Task SeedDocdb()
        {
            int pagesize = 100;
            //int offset = 0;
            int count = 0;
            while (count < 1000)
            {
                var listFamilies = new List<Model.Family>();
                for (int i = 0; i < pagesize; i++)
                {
                    listFamilies.Add(GenerateFamilyRecords());
                }
                long countWritten = await SaveFamilyRecordsBulk(listFamilies);
                if ((int)countWritten != pagesize)
                {
                    throw new InvalidOperationException("countWritten != count");
                }
                count += pagesize;
            }
        }

        public static Random r;
        static void Main(string[] args)
        {
            InitializeLists();
            
            r = new Random();

            var connectionPolicy = new ConnectionPolicy()
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Https
            };
            client = new DocumentClient(new Uri(EndpointUrl), AuthorizationKey, connectionPolicy);
            bool recreate = true;
            Setup(recreate).Wait();
            SeedDocdb().Wait();
        }
    }
}
