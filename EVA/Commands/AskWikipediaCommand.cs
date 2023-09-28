// ------------------------------------------------------------------------------
// Project: AI Assistant
// Author: Dr. Dennis "Captain P. Star" Meyer
// Copyright (c) 2023 Dr. Dennis "Captain P. Star" Meyer
// Date: 22.04.2023
// License: GNU General Public License v3.0
// ------------------------------------------------------------------------------
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Speech.Synthesis;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EVA.Commands
{
    public class AskWikipediaCommand : ICommand
    {

        public string SearchTopic
        {
            get => CustomProperties.ContainsKey("SearchTopic") ? CustomProperties["SearchTopic"].ToString() : null;
            set => CustomProperties["SearchTopic"] = value;
        }
        public string Language
        {
            get => CustomProperties.ContainsKey("Language") ? CustomProperties["Language"].ToString() : null;
            set => CustomProperties["Language"] = value;
        }
        public AskWikipediaCommand()
        {
            CommandName = "askWikipedia";
            Description = "Get a short description of \"SearchTopic\" from Wikipedia. For longer content the function to get text from a website (if available) should be used.";
            SearchTopic = "Text or topic to search on Wikipedia";
            Language = "Which languages wikipedia should be asked? Value should be wikipedia subdomain, e.g. \"en\", \"de\", \"ru\",...";
        }

    /*public override string GetPrompt()
        {
            return $"{{\"action\":\"{CommandName}\",\"text\":\"{Text}\",\"language\":\"{Language}\"}}";
        }*/

        public override async Task Execute()
        {
            var httpClient = new HttpClient();
            string requestUrl = $"https://{Language}.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(SearchTopic)}";
            var response = await httpClient.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

                if (jsonElement.TryGetProperty("extract", out JsonElement extract))
                {
                    string summary = extract.GetString();
                    Result = summary;
                    return;
                }
            };

            Result = $"No information found on \"https://{Language}.wikipedia.org\". Maybe I should try a different language or abort.";
            return;
        }


        public override string GetResult(string originalPrompt)
        {
            string result = $"AI assistant decided to search \"{Language}.wikipedia.org\" for {SearchTopic} using command \"{CommandName}\". Found this result: \"{Result}\".\n";
             //"If this is enough answer to the original prompt based on the information found with the \"finalResponse\" command or issue another command to further work on the task. If the information was not found, try the askWikipedia command with another topic or in another language.";
            return result;
        }
    }
}
