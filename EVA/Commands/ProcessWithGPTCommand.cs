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

using OpenAI_API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace EVA.Commands
{
    public class ProcessWithGPTCommand : Command
    {
        public string Prompt
        {
            get => CustomProperties.ContainsKey("prompt") ? CustomProperties["prompt"].ToString() : null;
            set => CustomProperties["prompt"] = value;
        }

        public ProcessWithGPTCommand()
        {
            CommandName = "ProcessWithGPT";
            Description = "Direct the user prompt to GPT for generating an answer. The prompt of the user can be expanded with additional information";
            Prompt = "Userprompt and additional information for GPT";
        }


        public override async Task Execute()
        {
            var result = await Context.openAIApi.Chat.CreateChatCompletionAsync(Prompt);
            Context.Tokens += result.Usage.TotalTokens;
            Result = result.Choices[0].Message.Content;
        }
        public override string GetResult(string originalPrompt)
        {
            string result = $"AI assistant decided to process the input with the LLM GPT using command \"{CommandName}\". Result from GPT: \"{Result}\".\n";
            return result;
        }
    }

}
