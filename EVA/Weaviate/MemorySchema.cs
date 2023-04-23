using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EVA.Weaviate
{
    public class Memory
    {

        [JsonProperty("summary")]
        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonProperty("text")]
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonProperty("keywords")]
        [JsonPropertyName("keywords")]
        public string[] Keywords { get; set; }

        [JsonProperty("vector")]
        public float[] Vector { get; set; }
    }
}
