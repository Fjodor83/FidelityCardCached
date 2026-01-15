using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FidelityCard.Lib.Models
{
    public class RequestSede
    {
        [JsonPropertyName("request")]
        public Request Request { get; set; } = default!;
        [JsonPropertyName("parameters")]
        public ParamElement[] Parameters { get; set; } = [];
    }

    public class Request
    {
        [JsonPropertyName("db_name")]
        public string DbName { get; set; } = string.Empty;
        [JsonPropertyName("sp_name")]
        public string SpName { get; set; } = string.Empty;
        [JsonPropertyName("called_from")]
        public string CalledFrom { get; set; } = string.Empty;
        [JsonPropertyName("called_operator")]
        public string CalledOperator { get; set; } = string.Empty;
    }

    public class ParamElement
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("type")]
        public string? Type { get; set; } = null;
        [JsonPropertyName("value")]
        public string? Value { get; set; } = null;
        [JsonPropertyName("sequence")]
        public string? Sequence { get; set; } = null;
    }
}
