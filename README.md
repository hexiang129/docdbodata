# docdbodata
This is a sample project that translates odata filter query to a document db query.
A live demo can be found at http://docdbodata.azurewebsites.net

1.       Docdbwebservice is the website project 

There are a few keys in web.config that needs to be changed to reflect your site setting:

 

    <add key="DocDB:EndPointUrl" value="https://testdocdb.documents.azure.com:443" />

    <add key="DocDB:AuthorizationKey" value="your docdb authorization key" />

    <add key="DocDB:DatabaseName" value="FamilyRegistry" />

<add key="DocDB:CollectionName" value="TestFamilyCollection" />

<add key="ApplicationInsightiKey" value="your application insight instrumentation key" />

 

2.  seeddocdb is the project that seed the docdb storage with random family records.

It takes a docdb account and creates a docdb database called FamilyRegistryin which it creates a document collection called TestFamilyCollection.

 

You will need to put your authorization key in Program.cs in this project.

private static string AuthorizationKey = "your docdb authorization key";

 

3.  Other projects:

QueryTranslator: the project that does query translation

model: data model project

DocDBRepository:  The project that reads data from docdb.

 

 

The translator does not implement Odata All operator. It does not implement odata single function call. 

(I don’t think there is a docdb counterpart for these).
