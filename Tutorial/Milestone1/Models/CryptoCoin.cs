using System;
using Newtonsoft.Json;
using SQLite;

namespace DifferentialCollections.Models
{
    public class CryptoCoin
    {
        [PrimaryKey, AutoIncrement]
        [JsonIgnore]
        public int ID
        {
            get; set;
        }

        [JsonIgnore]
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

        [JsonProperty("price_usd")]
        public double PriceUSD { get; internal set; }
    }
}
