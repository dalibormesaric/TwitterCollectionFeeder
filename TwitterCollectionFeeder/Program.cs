using CoreTweet;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace TwitterCollectionFeeder
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var configuration = builder.Build();

            var tokens = Tokens.Create(configuration["consumerKey"], configuration["consumerSecret"], configuration["accessToken"], configuration["accessSecret"]);
            long? userId;
            try
            {
                var userResponse = await tokens.Account.VerifyCredentialsAsync();
                userId = userResponse.Id;
            }
            catch (Exception)
            {
                var session = await OAuth.AuthorizeAsync(configuration["consumerKey"], configuration["consumerSecret"]);

                Console.WriteLine("Open url in the browser and authorize the app ...");
                Console.WriteLine(session.AuthorizeUri);

                Console.WriteLine("Enter pin code:");
                string pincode = Console.ReadLine();

                tokens = await session.GetTokensAsync(pincode);
                userId = tokens.UserId;

                Console.WriteLine($"AccessToken - {tokens.AccessToken}");
                Console.WriteLine($"AccessTokenSecret - {tokens.AccessTokenSecret}");
            }

            var searchPhrase = configuration["searchPhrase"];
            var collectionName = configuration["collectionName"];

            var collections = await tokens.Collections.ListAsync(userId);
            string collectionId = collections.Results.FirstOrDefault(collection => collection.Name == collectionName).Id;

            Console.WriteLine("Searching ...");

            while (true)
            {
                var searchResultList = await tokens.Search.TweetsAsync(searchPhrase, count: 100, result_type: "recent");

                foreach (var searchResult in searchResultList)
                {
                    var result = await tokens.Collections.EntriesAddAsync(id: collectionId, tweet_id: searchResult.Id);

                    if (!result.Any())
                    {
                        Console.WriteLine(searchResult.Text);
                    }
                }

                System.Threading.Thread.Sleep(30000);
            }
        }
    }
}
