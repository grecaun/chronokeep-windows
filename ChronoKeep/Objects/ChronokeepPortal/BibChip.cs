using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Chronokeep.Objects.ChronokeepPortal
{
    public class BibChip
    {
        [JsonPropertyName("bib")]
        public string Bib { get; set; }
        [JsonPropertyName("chip")]
        public string Chip { get; set; }
    }
}
