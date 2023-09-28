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

    public class WriteLocalFileCommand : ICommand
    {
        public string FilePath
        {
            get => CustomProperties.ContainsKey("filePath") ? CustomProperties["filePath"].ToString() : null;
            set => CustomProperties["filePath"] = value;
        }

        public string Content
        {
            get => CustomProperties.ContainsKey("content") ? CustomProperties["content"].ToString() : null;
            set => CustomProperties["content"] = value;
        }

        public WriteLocalFileCommand()
        {
            CommandName = "WriteLocalFile";
            Description = "Write an arbitrary file with the provided content. Specify the file path and the content.";
            FilePath = "C:\\example\\file\\path.txt";
            Content = "Example content to write to the file.";
        }

        public override async Task Execute()
        {
            try
            {
                await File.WriteAllTextAsync(FilePath, Content);
                Result = $"Successfully wrote to the file: {FilePath}";
            }
            catch (Exception ex)
            {
                Result = $"Error writing to the file: {FilePath}. Exception: {ex.Message}";
            }
        }

        public override string GetResult(string originalPrompt)
        {
            string result = $"AI assistant decided to write content to the file \"{FilePath}\" using command \"{CommandName}\". Result: \"{Result}\".\n";
            return result;
        }
    }
}
