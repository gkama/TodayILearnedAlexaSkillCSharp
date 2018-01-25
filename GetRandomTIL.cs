using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;

using Newtonsoft.Json;

namespace TodayILearnedAlexaSkillCSharp
{
    public class GetRandomTIL
    {
        public string table { get; set; }
        public List<string> ids = new List<string>();
        public ILambdaContext context { get; set; }

        //Constructor
        public GetRandomTIL(string table, ILambdaContext context)
        {
            this.table = table;
            this.context = context;
            this.ids = new List<string>();
        }

        //Scan the table
        public async Task<Data.data> Child()
        {
            var client = new AmazonDynamoDBClient(Amazon.RegionEndpoint.USEast2);
            Table reddit_til_table = Table.LoadTable(client, table);

            //Scan
            ScanFilter scanFilter = new ScanFilter();
            scanFilter.AddCondition("id", ScanOperator.IsNotNull);
            ScanOperationConfig scanConfig = new ScanOperationConfig()
            {
                Filter = scanFilter,
                Select = SelectValues.SpecificAttributes,
                AttributesToGet = new List<string> { "id" }
            };

            //Search
            Search tableSearch = reddit_til_table.Scan(scanConfig);

            //Loop of all documents - al items
            List<Document> all_TILs = new List<Document>();
            do
            {
                all_TILs = await tableSearch.GetNextSetAsync();
                foreach (var document in all_TILs)
                    ids.Add(document["id"].ToString());
            } while (!tableSearch.IsDone);

            //Return
            Document doc = await getChild(client, reddit_til_table);
            return new Data().CastToData(doc);
        }

        //Get random child
        private async Task<Document> getChild(AmazonDynamoDBClient client, Table reddit_til_table)
        {
            string randomID = this.ids[getRandomInt()];

            //Get the child via get async
            Document child = await reddit_til_table.GetItemAsync(randomID);

            //Log
            context.Logger.Log(JsonConvert.SerializeObject(child, Formatting.Indented));

            //Return the child
            return child;
        }

        //Random number
        private static readonly Random random = new Random();
        private static readonly object syncLock = new object();
        private int getRandomInt()
        {
            lock (syncLock)
                return random.Next(0, this.ids.Count + 1);
        }



        //Data class
        public class Data
        {
            public data CastToData(Document doc)
            {
                data data = new data
                {
                    id = doc["id"],
                    score = Convert.ToInt32(doc["score"]),
                    title = doc["title"],
                    /*url = doc["url"],
                    subreddit = doc["subreddit"],
                    thumbnail = doc["thumbnail"],
                    subreddit_id = doc["subreddit_id"],
                    gilded = Convert.ToInt32(doc["gilded"]),
                    name = doc["name"],
                    permalink = doc["permalink"],
                    link = doc["link"],
                    author = doc["author"],
                    ups = Convert.ToInt32(doc["ups"]),
                    downs = Convert.ToInt32(doc["downs"]),
                    num_comments = Convert.ToInt32(doc["num_comments"]),*/
                    last_updated = doc["last_updated"]
                };
                return data;
            }
            public class data
            {
                public string id { get; set; }
                public int score { get; set; }
                public string title { get; set; }
                /*public string url { get; set; }
                public string subreddit { get; set; }
                public string thumbnail { get; set; }
                public string subreddit_id { get; set; }
                public string name { get; set; }
                public string permalink { get; set; }
                public string link { get; set; }
                public string author { get; set; }
                public int ups { get; set; }
                public int downs { get; set; }
                public int gilded { get; set; }
                public int num_comments { get; set; }*/
                public string last_updated { get; set; }
            }
        }
    }
}
