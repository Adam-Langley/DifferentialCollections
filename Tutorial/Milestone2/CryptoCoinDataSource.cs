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
        readonly UIView _activityView;

        string CriteriaToSql()
        {
            return $"(Name COLLATE NOCASE LIKE '%{Criteria.FilterString}%')";
        }

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

        public override int GetCount()
        {
            var sql = $"SELECT COUNT(*) FROM {nameof(CryptoCoin)} WHERE {CriteriaToSql()}";
            return AppDelegate.Connection.ExecuteScalar<int>(sql);
        }



        public override IEnumerable<RowMeta> GetRowMeta(IEnumerable<string> identifiers)
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

            return AppDelegate.Connection.Query<RowMeta>(sqlOffset);
        }

        public override IEnumerable<string> GetIds(int skip, int take)
        {
            var sqlIds = $"SELECT Id as Key FROM {nameof(CryptoCoin)} WHERE {CriteriaToSql()} ORDER BY {Criteria.OrderByColumnName} {(Criteria.Descending ? "DESC" : "ASC")}, rowid {(Criteria.Descending ? "DESC" : "ASC")} LIMIT ? OFFSET ?";
            return AppDelegate.Connection.Query<RowMeta>(sqlIds, take, skip).Select(x => x.Key);
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

            return AppDelegate.Connection.Query<CryptoCoin>($"SELECT * FROM {nameof(CryptoCoin)} WHERE {CriteriaToSql()} ORDER BY {Criteria.OrderByColumnName} {(Criteria.Descending ? "DESC" : "ASC")}, rowid {(Criteria.Descending ? "DESC" : "ASC")} LIMIT ? OFFSET ?", take, skip);
        }
    }

}
