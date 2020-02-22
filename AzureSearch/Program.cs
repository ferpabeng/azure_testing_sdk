using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;

namespace AzureSearch
{
    class Program
    {
        static string searchServiceName = "fpabloen-search";
        static string adminApiKey = "1FBB3F027A38C412A5BD76A543B64B7D";
        static string sdkIndex = "customer-index";

        static void Main(string[] args)
        {

            SearchServiceClient democlient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));
            IndexCreation(sdkIndex, democlient);
            UploadtoIndex(sdkIndex, democlient);
            Console.ReadKey();
            Console.ReadKey();
        }

        private static void IndexCreation(string p_indexName, SearchServiceClient p_serviceClient)
        {
            var definition = new Index()
            {
                Name = p_indexName,
                Fields = FieldBuilder.BuildForType<customer>()
            };

            p_serviceClient.Indexes.Create(definition);
            Console.WriteLine("Index created");
        }
        private static void UploadtoIndex(string p_indexName, SearchServiceClient p_serviceClient)
        {
            var l_customer = new customer[]
            {
                new customer()
                {
                    Id="1",
                    Name="userA",
                    Progress="20",
                    Comment="The couse is good",
                    Course="AZ-203 Developing Solutions for Microsoft Azure"
                },
                new customer()
                {
                    Id="2",
                    Name="userB",
                    Progress="40",
                    Comment="The couse really has a lot of good aspects",
                    Course="AZ-103 Microsoft Azure Administrator"
                },
                new customer()
                {
                    Id="3",
                    Name="userB",
                    Progress="15",
                    Comment="The couse needs improvement",
                    Course="AZ-203 Developing Solutions for Microsoft Azure"
                }
            };
            var l_batch = IndexBatch.Upload(l_customer);
            ISearchIndexClient p_indexClient = p_serviceClient.Indexes.GetClient(p_indexName);
            try
            {
                Console.WriteLine("Uploading documents");
                p_indexClient.Documents.Index(l_batch);
                Console.WriteLine("All documents uploaded");
            }
            catch (IndexBatchException e)
            {
                Console.WriteLine(e.Message);
            }

        }
    }
}
