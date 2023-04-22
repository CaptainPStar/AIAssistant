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
using System.Security.Cryptography.X509Certificates;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace EVA.Commands
{
    public abstract class Command
    {
        public string CommandName { get; set; }
        public string Description { get; set; }
        public Context Context { get; set; } = null;
        public Dictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
        
        public string Result = string.Empty;

        public string GetPrompt() {
            string prompt = $"{{\"action\":\"{CommandName}\"";
            foreach (var k in CustomProperties)
            {
                string valueString;

                if (k.Value is bool)
                {
                    valueString = k.Value.ToString().ToLowerInvariant();
                }
                else if (k.Value is string)
                {
                    valueString = $"\"{k.Value}\"";
                }
                else
                {
                    valueString = k.Value.ToString();
                }

                prompt += $",\"{k.Key}\":{valueString}";
            }
            prompt += $"}}";
            return prompt ;
        }
        public abstract Task Execute();
        public abstract string GetResult(string originalPrompt);

    }
}
