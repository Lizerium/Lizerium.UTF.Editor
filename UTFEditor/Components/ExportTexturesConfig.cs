using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UTFEditor.Components
{
    [Serializable]
    public class ExportTexturesConfig
    {
        [JsonPropertyName("exports")]
        public List<ExportItem> ExportItems { get; set; } = new List<ExportItem>();
    }
}
