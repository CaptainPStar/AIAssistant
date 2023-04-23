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
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using System.ComponentModel;
    using System.IO;
    using System.Reflection;

    public class Config
    {
        [JsonProperty("openai_access_token")]
        [IncludeInConfig]
        [Description("Your OpenAI API access token.")]
        public string OpenAIAccessToken { get; set; }

        [JsonProperty("refine_prompts_with_gpt")]
        [IncludeInConfig]
        [Description("Whether to refine prompts using GPT before generating a response.")]
        public bool RefinePromptsWithGPT { get; set; }

        [JsonProperty("plan_ahead")]
        [IncludeInConfig]
        [Description("Whether to plan ahead before generating a response.")]
        public bool PlanAhead { get; set; }

        [JsonProperty("use_memory")]
        [IncludeInConfig]
        [Description("Whether to use memory (vector database Milvus) for generating responses. You need docker installed for this to work.")]
        public bool UseMemory { get; set; }

  
        private static readonly string ConfigFilePath = "config.json";
        [IncludeInConfig(false)]
        public static string ConfigFile { get { return Path.GetFullPath(ConfigFilePath); } } 
        public Config()
        {
            OpenAIAccessToken = "your_default_openai_access_token";
            RefinePromptsWithGPT = false;
            PlanAhead = false;
            UseMemory = false;
        }

        public static Config LoadConfig()
        {
            Config config;

            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                config = JsonConvert.DeserializeObject<Config>(StripComments(json));
                var defaultConfig = new Config();
                foreach (var property in typeof(Config).GetProperties())
                {
                    if (!property.GetCustomAttribute<IncludeInConfigAttribute>()?.IncludeInConfig ?? false)
                    {
                        // Ignore properties marked with [IncludeInConfig(false)]
                        continue;
                    }
                    if (property.GetValue(config) == null)
                    {
                        // Set null properties to their default values
                        property.SetValue(config, property.GetValue(defaultConfig));
                    }
                }
                SaveConfig(config); // Save the updated config
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
            var properties = typeof(Config).GetProperties()
               .Where(p => p.GetCustomAttribute<IncludeInConfigAttribute>()?.IncludeInConfig ?? true);

            var lines = new List<string>();
            foreach (var property in properties)
            {
                var name = property.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? property.Name;
                var value = property.GetValue(config);
                if (value == null)
                {
                    // Ignore null properties
                    continue;
                }
                var description = property.GetCustomAttribute<DescriptionAttribute>()?.Description;
                var line = "\"" + name + "\": " + JsonConvert.SerializeObject(value, Formatting.None);
                if (description != null)
                {
                    line = "// " + description + Environment.NewLine + line;
                }
                lines.Add(line);
            }

            var json = "{" + Environment.NewLine + string.Join("," + Environment.NewLine, lines) + Environment.NewLine + "}";
            File.WriteAllText(ConfigFilePath, json);
        }
        private static string StripComments(string json)
        {
            return string.Join("", json.Split(Environment.NewLine).Select(line =>
            {
                var index = line.IndexOf("//");
                return index >= 0 ? line.Substring(0, index) : line;
            }));
        }

    }
    [AttributeUsage(AttributeTargets.Property)]
    public class IncludeInConfigAttribute : Attribute
    {
        public IncludeInConfigAttribute(bool Include=true) { IncludeInConfig = Include; }
        public bool IncludeInConfig { get; set; } = true;
    }
}
