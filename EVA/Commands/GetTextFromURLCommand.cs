using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace EVA.Commands
{
    public class GetTextFromUrlCommand : ICommand
    {
        public string Url
        {
            get => CustomProperties.ContainsKey("Url") ? CustomProperties["Url"].ToString() : null;
            set => CustomProperties["Url"] = value;
        }

        public GetTextFromUrlCommand()
        {
            CommandName = "getTextFromUrl";
            Description = "Get the text content from an arbitrary website";
            Url = "URL of the website";
        }

        public override async Task Execute()
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(Url);

                if (response.IsSuccessStatusCode)
                {
                    string htmlContent = await response.Content.ReadAsStringAsync();
                    HtmlDocument htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(htmlContent);

                    var bodyNode = htmlDoc.DocumentNode.SelectSingleNode("/html/body");
                    string textContent = bodyNode.InnerText;
                    Result = textContent;
                    return;
                }
            }

            Result = $"Unable to fetch content from \"{Url}\". Please check the URL and try again.";
            return;
        }

        public override string GetResult(string originalPrompt)
        {
            string result = $"AI assistant fetched text content from \"{Url}\" using command \"{CommandName}\". Found this result: \"{Result}\".\n";
            return result;
        }
    }
}
