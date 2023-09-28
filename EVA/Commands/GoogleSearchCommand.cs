using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EVA.Commands
{
    public class GoogleSearchCommand : ICommand
    {
        public string SearchQuery
        {
            get => CustomProperties.ContainsKey("SearchQuery") ? CustomProperties["SearchQuery"].ToString() : null;
            set => CustomProperties["SearchQuery"] = value;
        }

        public GoogleSearchCommand()
        {
            CommandName = "googleSearch";
            Description = "Google a topic and get the top 10 search results";
            SearchQuery = "Search query";
        }

        public override async Task Execute()
        {
            // You need to have a valid Google API Key and Custom Search Engine ID
            string apiKey = "YOUR_GOOGLE_API_KEY";
            string searchEngineId = "YOUR_CUSTOM_SEARCH_ENGINE_ID";
            string requestUrl = $"https://www.googleapis.com/customsearch/v1?key={apiKey}&cx={searchEngineId}&q={Uri.EscapeDataString(SearchQuery)}";

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

                    if (jsonElement.TryGetProperty("items", out JsonElement items))
                    {
                        List<string> searchResults = new List<string>();

                        foreach (var item in items.EnumerateArray())
                        {
                            string title = item.GetProperty("title").GetString();
                            string link = item.GetProperty("link").GetString();
                            string snippet = item.GetProperty("snippet").GetString();

                            searchResults.Add($"Title: {title}\nLink: {link}\nSnippet: {snippet}\n");
                        }

                        Result = string.Join("\n", searchResults);
                        return;
                    }
                }
            }

            Result = $"No search results found for \"{SearchQuery}\". Please try a different query.";
            return;
        }

        public override string GetResult(string originalPrompt)
        {
            string result = $"AI assistant searched for \"{SearchQuery}\" using command \"{CommandName}\". Found these results: \n\n{Result}\n";
            return result;
        }
    }
}
