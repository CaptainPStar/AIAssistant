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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVA.Commands
{
    public class ReadLocalFileCommand : Command
    {
        public string FilePath
        {
            get => CustomProperties.ContainsKey("filePath") ? CustomProperties["filePath"].ToString() : null;
            set => CustomProperties["filePath"] = value;
        }

        public bool Summarize
        {
            get => CustomProperties.ContainsKey("summarize") && (bool)CustomProperties["summarize"];
            set => CustomProperties["summarize"] = value;
        }

        public ReadLocalFileCommand()
        {
            CommandName = "ReadLocalFile";
            Description = "Read the contents of a local file. Specify the file path and optionally request a summary using GPT.";
            FilePath = "e.g. C:\\example\\file\\path.txt, sanitize so it can be used in a JSON property.";
            Summarize = false;
        }

        public override async Task Execute()
        {
            if (File.Exists(FilePath))
            {
                string fileContent = await File.ReadAllTextAsync(FilePath);

                if (Summarize)
                {
                    var prompt = $"Please summarize the following text:\n{fileContent}";
                    var result = await Context.openAIApi.Chat.CreateChatCompletionAsync(prompt);
                    Context.Tokens += result.Usage.TotalTokens;
                    Result = result.Choices[0].Message.Content;
                }
                else
                {
                    Result = fileContent;
                }
            }
            else
            {
                Result = $"File not found: {FilePath}";
            }
        }

        public override string GetResult(string originalPrompt)
        {
            string result = $"AI assistant decided to read the contents of the file \"{FilePath}\" using command \"{CommandName}\"{(Summarize ? " and summarize it with GPT" : "")}. Result: \"{Result}\".\n";
            return result;
        }
    }
}
