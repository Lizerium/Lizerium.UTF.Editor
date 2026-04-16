using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UTFEditor.Components
{
    [Serializable]
    public class ExportItem
    {
        [JsonPropertyName("name")]
        public string NameFile { get; set; }
        [JsonPropertyName("textures")]
        public List<ExportTexture> ExportTextures { get; set; } = new List<ExportTexture>();
    }
}