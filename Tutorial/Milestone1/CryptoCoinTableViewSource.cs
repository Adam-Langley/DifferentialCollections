using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DifferentialCollections.Models;
using Foundation;
using SQLite;
using UIKit;

namespace DifferentialCollections
{
    public class CryptoCoinTableViewSource : UITableViewSource
    {
        readonly string _cellIdentifier;
        readonly BatchingCache<CryptoCoin> _cache = new BatchingCache<CryptoCoin>(20);
        readonly HashSet<int> _loadingCells = new HashSet<int>();
        readonly SQLiteConnection _conn;
        readonly UIView _activityView;

        public CryptoCoinTableViewSource(UITableView tableView, UIView activityView, string nibName, string cellIdentifier = null)
            : base()
        {
            _activityView = activityView;
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "test.sqlite");
            _conn = new SQLiteConnection(path);

            _conn.Trace = true;

            _cellIdentifier = cellIdentifier;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            return tableView.DequeueReusableCell(_cellIdentifier, indexPath);
        }

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            var sql = $"SELECT COUNT(*) FROM {nameof(CryptoCoin)}";
            return _conn.ExecuteScalar<int>(sql);
        }

        public override void WillDisplay(UITableView tableView, UITableViewCell cell, NSIndexPath indexPath)
        {
            _loadingCells.Add(indexPath.Row);

            // this is when we should load it
            _cache.Get(indexPath.Row, GetPage)
                  .ContinueWith(x => OnDataContextLoaded(cell, indexPath.Row, x.Result), TaskScheduler.FromCurrentSynchronizationContext());
        }

        public override void CellDisplayingEnded(UITableView tableView, UITableViewCell cell, NSIndexPath indexPath)
        {
            _loadingCells.Remove(indexPath.Row);
        }

        public IEnumerable<CryptoCoin> GetPage(int skip, int take)
        {
            InvokeOnMainThread(() =>
            {
                _activityView.Alpha = 1f;
                UIView.Animate(1f, () =>{
                    _activityView.Alpha = 0f;
                 });
            });
            var result = _conn.Query<CryptoCoin>($"SELECT * FROM {nameof(CryptoCoin)} LIMIT ? OFFSET ?", take, skip);
            return result;
        }

        protected void OnDataContextLoaded(UITableViewCell rowView, int rowIndex, CryptoCoin entity)
        {
            // OPTIMIZATION: if the is not 'loading', and it has not become visible... then it must have passed out of visibility
            if (!_loadingCells.Contains(rowIndex))
            {
                System.Diagnostics.Debug.WriteLine($"Skipping row load {rowIndex}");
                return;
            }
            
            rowView.TextLabel.Text = $"{entity.Name} {entity.TwentyFourHourChange}% ${entity.PriceUSD}";
        }
    }
}
