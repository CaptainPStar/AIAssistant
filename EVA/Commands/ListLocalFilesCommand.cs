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
using System.Text;
using System.Threading.Tasks;

namespace EVA.Commands
{
    using System.IO;
    using System.Threading.Tasks;

    public class ListLocalFilesCommand : Command
    {
        public string FolderPath
        {
            get => CustomProperties.ContainsKey("folderPath") ? CustomProperties["folderPath"].ToString() : null;
            set => CustomProperties["folderPath"] = value;
        }

        public ListLocalFilesCommand()
        {
            CommandName = "ListLocalFiles";
            Description = "Retrieve a list of files in a local folder. Specify the folder path.";
            FolderPath = "C:\\example\\folder\\path";
        }

        public override async Task Execute()
        {
            await Task.Run(() =>
            {
                if (Directory.Exists(FolderPath))
                {
                    var files = Directory.GetFiles(FolderPath);
                    Result = string.Join(", ", files);
                }
                else
                {
                    Result = $"Folder not found: {FolderPath}";
                }
            });
        }

        public override string GetResult(string originalPrompt)
        {
            string result = $"AI assistant decided to retrieve a list of files in the folder \"{FolderPath}\" using command \"{CommandName}\". Result: \"{Result}\".\n";
            return result;
        }
    }
}
