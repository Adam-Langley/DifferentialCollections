using System;
using Newtonsoft.Json;
using SQLite;

namespace DifferentialCollections.Models
{
    [Table("CryptoCoin")]
    public class CryptoCoin
    {
        [PrimaryKey]
        public string Id
        {
            get; set;
        }

        [JsonProperty("last_updated")]
        public long Version { get; set; }

        [JsonProperty("name")]
        public string Name
        {
            get; set;
        }

        [JsonProperty("percent_change_24h")]
        public double TwentyFourHourChange
        {
            get; set;
        }

        [JsonIgnore]
        public double PreviousPriceUSD { get; internal set; }

        [JsonProperty("price_usd")]
        public double PriceUSD { get; internal set; }

        [JsonIgnore]
        [Ignore]
        public double Change { get; set; }
    }
}
