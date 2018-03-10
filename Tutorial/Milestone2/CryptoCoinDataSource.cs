using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DifferentialCollections.Models;
using SQLite;
using UIKit;

namespace DifferentialCollections
{
    /// <summary>
    /// A Crypto-Coin data source.
    /// </summary>
    /// <remarks>
    /// This is where we specialize our DifferentialDataModel to our CryptoCoin schema.
    /// </remarks>
    public class CryptoCoinDataSource : DifferentialDataModel<string, CryptoCoin, CryptoCoinCriteria>
    {
        readonly UIView _activityView;

        /// <summary>
        /// Return a new instance of our DifferentialDataModel, given a new criteria object.
        /// </summary>
        /// <returns>The criteria.</returns>
        /// <param name="criteria">Criteria.</param>
        public override DifferentialDataModel<string, CryptoCoin, CryptoCoinCriteria> FromCriteria(CryptoCoinCriteria criteria)
        {
            return new CryptoCoinDataSource(this, criteria);
        }

        protected CryptoCoinDataSource(DifferentialDataModel<string, CryptoCoin, CryptoCoinCriteria> parent, CryptoCoinCriteria criteria)
            : base(x => new RowMeta { Key = x.Id, Version = x.Version })
        {
            _parent = parent;
            Criteria = criteria;
        }

        public CryptoCoinDataSource(UIView activityView)
            : this(null, new CryptoCoinCriteria()
            {
                OrderByColumnName = nameof(CryptoCoin.Name),
                Descending = false
            })
        {
            _activityView = activityView;
        }

        /// <summary>
        /// Execute a simple count on the table with our SQL criteria.
        /// </summary>
        /// <returns>The count.</returns>
        public override int GetCount()
        {
            var sql = $"SELECT COUNT(*) FROM {nameof(CryptoCoin)} WHERE {Criteria.FilterAsSql()}";
            return AppDelegate.Connection.ExecuteScalar<int>(sql);
        }

        /// <summary>
        /// Get meta-data for all records matching given primary keys.
        /// </summary>
        /// <returns>The row meta.</returns>
        /// <param name="identifiers">Identifiers.</param>
        public override IEnumerable<RowMeta> GetRowMeta(IEnumerable<string> identifiers)
        {
            // join the list of identifiers into a string to be appended to SQL criteria
            var ids = string.Join(", ", identifiers.Select(x => $"'{x}'").ToArray());

            string op = "<";
            string order = "ASC";
            if(Criteria.Descending)
            {
                op = ">";
                order = "DESC";
            }
                
            // craft sub-select criteria which will count the rows considered to come 'before' 
            // a given row based on current ordering.
            var subSelectPositionalCriteria = $"((v2.{Criteria.OrderByColumnName} {op} v.{Criteria.OrderByColumnName}) OR (v2.{Criteria.OrderByColumnName} = v.{Criteria.OrderByColumnName} AND v2.rowid {op} v.rowid))";

            var sql =
                $"SELECT Id AS Key, Version, " +
                    $"(SELECT COUNT(*) FROM {nameof(CryptoCoin)} v2 " +
                    $"WHERE ({Criteria.FilterAsSql()}) AND {subSelectPositionalCriteria}) AS Position " +
                $"FROM {nameof(CryptoCoin)} v " +
                $"WHERE Id IN ({ids}) AND {Criteria.FilterAsSql()}" +
                $"ORDER BY Position {order}, Id {order}";

            return AppDelegate.Connection.Query<RowMeta>(sql);
        }

        /// <summary>
        /// Return a paginated list of primary keys.
        /// </summary>
        /// <returns>The identifiers.</returns>
        /// <param name="skip">Skip.</param>
        /// <param name="take">Take.</param>
        public override IEnumerable<string> GetIds(int skip, int take)
        {
            string order = "ASC";
            if (Criteria.Descending)
                order = "DESC";
            
            var sqlIds = 
                $"SELECT Id as Key FROM {nameof(CryptoCoin)} " +
                $"WHERE {Criteria.FilterAsSql()} " +
                $"ORDER BY {Criteria.OrderByColumnName} {order}, rowid {order} LIMIT ? OFFSET ?";
            return AppDelegate.Connection.Query<RowMeta>(sqlIds, take, skip).Select(x => x.Key);
        }

        /// <summary>
        /// Returns a paginated set of results.
        /// </summary>
        /// <returns>The page.</returns>
        /// <param name="skip">Skip.</param>
        /// <param name="take">Take.</param>
        public override IEnumerable<CryptoCoin> GetPage(int skip, int take)
        {
            UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
            {
                _activityView.Alpha = 1f;
                UIView.Animate(1f, () => {
                    _activityView.Alpha = 0f;
                });
            });

            string order = "ASC";
            if (Criteria.Descending)
                order = "DESC";

            var sql =
                $"SELECT * FROM {nameof(CryptoCoin)} " +
                $"WHERE {Criteria.FilterAsSql()} " +
                $"ORDER BY {Criteria.OrderByColumnName} {order}, rowid {order} LIMIT ? OFFSET ?";

            return AppDelegate.Connection.Query<CryptoCoin>(sql, take, skip);
        }
    }
}
