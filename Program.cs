using Elasticsearch.Net;
using Nest;
using System;

namespace Elastic6Types
{
    class Program
    {
        static void Main(string[] args)
        {
            var indexName = "myindex";
            var settings = new ConnectionSettings(
                connectionPool: new SingleNodeConnectionPool(new Uri("http://localhost:9200")))
            .DefaultIndex(indexName)
            .DefaultMappingFor<Issue>(m => m.IndexName(indexName).TypeName("doc"))
            .DefaultMappingFor<Tag>(m => m.IndexName(indexName).TypeName("doc"));

            var client = new ElasticClient(settings);

            CreateIndex(indexName, client);

            client.IndexDocument(new Issue { Id = "1", IssueName = "Issue 1" });
            Console.WriteLine("Issue indexed");

            client.IndexDocument(new Tag { Id = "2", TagName = "Tag 2" });
            client.IndexDocument(new Tag { Id = "3", TagName = "Tag 3" });
            Console.WriteLine("Tags indexed");

            client.Refresh(indexName);
            Console.WriteLine("Index refreshed");

            Console.WriteLine("Searching issues");
            var searchResponseIssues = client.Search<Issue>(s => s
                .Query(q => q
                    .Bool(bq => bq
                        .Filter(f => f.Term(t => t.Field(x => x.Type).Value(nameof(Issue).ToLower()))))
                    )
                );

            foreach (var issue in searchResponseIssues.Documents)
            {
                Console.WriteLine("Issue {0}", issue.IssueName);
            }

            Console.WriteLine("Searching tags");
            var searchResponseTags = client.Search<Tag>(s => s
                .Query(q => q
                    .Bool(bq => bq
                        .FilterDiscriminatorType()) // same as the issue query filter but refactored into an extension method
                    )
                );

            foreach (var tag in searchResponseTags.Documents)
            {
                Console.WriteLine("Tag {0}", tag.TagName);
            }
        }

        static void CreateIndex(string indexName, ElasticClient client)
        {
            if (client.IndexExists(indexName).Exists)
            {
                client.DeleteIndex(indexName);
            }
            
            client.CreateIndex(indexName);

            Console.WriteLine("Index {0} created", indexName);
        }
    }

    public static class NestSearchQueryExtensions
    {
        public static BoolQueryDescriptor<T> FilterDiscriminatorType<T>(this BoolQueryDescriptor<T> bq) where T : Doc
        {
            return bq.Filter(f => f.Term(t => t.Field(x => x.Type).Value(typeof(T).Name.ToLower())));
        }
    }

    public abstract class Doc
    {
        public Doc()
        {
            Type = GetType().Name.ToLower();
        }

        public string Id { get; set; }

        public string Type { get; set; } // discriminator field
    }

    public class Issue : Doc
    {
        public string IssueName { get; set; }
    }

    public class Tag : Doc 
    {
        public string TagName { get; set; }
    }
}
