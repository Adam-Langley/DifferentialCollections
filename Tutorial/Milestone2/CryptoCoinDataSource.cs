using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DifferentialCollections.Models;
using SQLite;
using UIKit;

namespace DifferentialCollections
{
    public class CryptoCoinDataSource : DifferentialDataModel<string, CryptoCoin, CryptoCoinCriteria>
    {
        readonly SQLiteConnection _conn;
        readonly UIView _activityView;

        static SQLiteConnection CreateConnection()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "test.sqlite");
            return new SQLiteConnection(path)
            {
                Trace = true
            };
        }

        string CriteriaToSql()
        {
            return $"(Name COLLATE NOCASE LIKE '%{Criteria.FilterString}%')";
        }

        // changes the underlying query, and forces update in one transaction

        public override DifferentialDataModel<string, CryptoCoin, CryptoCoinCriteria> FromCriteria(CryptoCoinCriteria criteria)
        {
            return new CryptoCoinDataSource(this, _conn, criteria);
        }


        protected CryptoCoinDataSource(DifferentialDataModel<string, CryptoCoin, CryptoCoinCriteria> parent, SQLiteConnection conn, CryptoCoinCriteria criteria)
            : base(x => new RowVersion { Key = x.Id, Version = x.Version })
        {
            _parent = parent;
            _conn = conn;
            Criteria = criteria;
        }

        public CryptoCoinDataSource(UIView activityView)
            : this(null, CreateConnection(), new CryptoCoinCriteria()
            {
                OrderByColumnName = nameof(CryptoCoin.Name),
                Descending = false
            })
        {
            _activityView = activityView;
        }

        public override int GetCount()
        {
            var sql = $"SELECT COUNT(*) FROM {nameof(CryptoCoin)} WHERE {CriteriaToSql()}";
            return _conn.ExecuteScalar<int>(sql);
        }



        public override IEnumerable<DifferentialDataModel<string>.RowVersion> GetRowPositions(IEnumerable<string> identifiers)
        {
            var ids = string.Join(", ", identifiers.Select(x => $"'{x}'").ToArray());

            var orderComparison = $"((v2.{Criteria.OrderByColumnName} < v.{Criteria.OrderByColumnName}) OR (v2.{Criteria.OrderByColumnName} = v.{Criteria.OrderByColumnName} AND v2.rowid < v.rowid))";
            if (Criteria.Descending)
            {
                orderComparison = $"((v2.{Criteria.OrderByColumnName} > v.{Criteria.OrderByColumnName}) OR (v2.{Criteria.OrderByColumnName} = v.{Criteria.OrderByColumnName} AND v2.rowid > v.rowid))";
            }

            var sqlOffset =
                $"SELECT Id as Key, Version, (select COUNT(*) FROM {nameof(CryptoCoin)} v2 WHERE ({CriteriaToSql()}) AND {orderComparison}) as Position FROM {nameof(CryptoCoin)} v"
                + $" WHERE Id IN ({ids}) AND {CriteriaToSql()}"
                + $"  ORDER BY Position {(Criteria.Descending ? "DESC" : "ASC")}, Id {(Criteria.Descending ? "DESC" : "ASC")}";

            return _conn.Query<RowVersion>(sqlOffset);
        }

        public override IEnumerable<string> GetIds(int skip, int take)
        {
            var sqlIds = $"SELECT Id as Key FROM {nameof(CryptoCoin)} WHERE {CriteriaToSql()} ORDER BY {Criteria.OrderByColumnName} {(Criteria.Descending ? "DESC" : "ASC")}, rowid {(Criteria.Descending ? "DESC" : "ASC")} LIMIT ? OFFSET ?";
            var idList = _conn.Query<RowVersion>(sqlIds, take, skip).Select(x => x.Key);
            return idList;
        }

        public override IEnumerable<CryptoCoin> GetPage(int skip, int take)
        {
            UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
            {
                _activityView.Alpha = 1f;
                UIView.Animate(1f, () => {
                    _activityView.Alpha = 0f;
                });
            });

            var result = _conn.Query<CryptoCoin>($"SELECT * FROM {nameof(CryptoCoin)} WHERE {CriteriaToSql()} ORDER BY {Criteria.OrderByColumnName} {(Criteria.Descending ? "DESC" : "ASC")}, rowid {(Criteria.Descending ? "DESC" : "ASC")} LIMIT ? OFFSET ?", take, skip);
            return result;
        }
    }

}
