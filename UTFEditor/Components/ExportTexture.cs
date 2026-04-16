using System;
using System.Text.Json.Serialization;

namespace UTFEditor.Components
{
    [Serializable]
    public class ExportTexture
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("format")]
        public string Format { get; set; }
    }
}