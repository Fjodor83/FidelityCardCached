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
}
