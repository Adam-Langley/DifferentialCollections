using System;
namespace DifferentialCollections
{
    /// <summary>
    /// Elements required for Crypto coin query criteria.
    /// </summary>
    public class CryptoCoinCriteria
    {
        public string FilterString { get; set; }

        public string OrderByColumnName { get; set; }

        public bool Descending { get; set; }
    }
}
