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

namespace EVA
{
    using Newtonsoft.Json;
    using System.IO;

    public class Config
    {
        public string OpenAIAccessToken { get; set; }
        public bool RefinePromptsWithGPT { get; set; }

        private static readonly string ConfigFilePath = "config.json";

        public Config()
        {
            OpenAIAccessToken = "your_default_openai_access_token";
            RefinePromptsWithGPT = false;
        }

        public static Config LoadConfig()
        {
            Config config;

            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                config = JsonConvert.DeserializeObject<Config>(json);
            }
            else
            {
                config = new Config();
                SaveConfig(config);
            }

            return config;
        }

        public static void SaveConfig(Config config)
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
        }
    }
}
