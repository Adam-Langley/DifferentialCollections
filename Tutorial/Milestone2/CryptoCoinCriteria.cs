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

        /// <summary>
        /// Helper method to construct an SQL statement for our current criteria.
        /// </summary>
        /// <returns>The to sql.</returns>
        public string FilterAsSql(){
            return $"(Name COLLATE NOCASE LIKE '%{FilterString}%')";
        }
    }
}
