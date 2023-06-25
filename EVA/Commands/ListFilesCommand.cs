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
    public class ListFilesCommand : ICommand
    {
        public string FolderPath
        {
            get => CustomProperties.ContainsKey("FolderPath") ? CustomProperties["FolderPath"].ToString() : null;
            set => CustomProperties["FolderPath"] = value;
        }

        public int ScanningDepth
        {
            get => CustomProperties.ContainsKey("ScanningDepth") ? (int)CustomProperties["ScanningDepth"] : 0;
            set => CustomProperties["ScanningDepth"] = value;
        }

        public ListFilesCommand()
        {
            CommandName = "listFiles";
            Description = "List all files and folders in \"FolderPath\" and its subfolders with a specified scanning depth \"ScanningDepth\".";
            FolderPath = "Path of the folder";
            ScanningDepth = 2;
        }

        public override async Task Execute()
        {
            try
            {
                var files = GetFiles(FolderPath, ScanningDepth);
                Result = string.Join("\n", files);
   
            }
            catch (Exception ex)
            {
                Result = $"Error while scanning files: {ex.Message}";
            }
        }

        private List<string> GetFiles(string path, int depth)
        {
            List<string> files = new List<string>();
            if (depth > 5)
                depth = 5;
            if (depth <= 0)
            {
                return files;
            }

            files.AddRange(Directory.GetFiles(path));

            foreach (string dir in Directory.GetDirectories(path))
            {
                files.Add(dir);
                files.AddRange(GetFiles(dir, depth - 1));
            }

            return files;
        }

        public override string GetResult(string originalPrompt)
        {
            string result = $"AI assistant listed files in \"{FolderPath}\" using command \"{CommandName}\" with a scanning depth of {ScanningDepth}. Found these files: \n\n{Result}\n";
            return result;
        }
    }
}
