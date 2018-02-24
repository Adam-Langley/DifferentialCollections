using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreAnimation;
using CoreFoundation;
using Foundation;
using UIKit;

namespace DifferentialCollections
{
    public abstract class DifferentialCollectionViewSource<TKey, TEntity, TCriteria> : UICollectionViewSource where TCriteria : class
    {
        const int CACHE_SIZE = 20;
        readonly UICollectionView _collectionView;
        readonly string _cellIdentifier;

        VisibleRowManager<TKey> _visibleRows = new VisibleRowManager<TKey>();
        BatchingCache<TEntity> _cache = new BatchingCache<TEntity>(CACHE_SIZE);
        HashSet<int> _loadingCells = new HashSet<int>();
        Func<TEntity, DifferentialDataModel<TKey>.RowVersion> _idGetter;
        DifferentialDataModel<TKey, TEntity, TCriteria> _dataModel;
        TaskCompletionSource<bool> _tcs;

        public DifferentialCollectionViewSource(UICollectionView collectionView, string nibName, string cellIdentifier = null)
        {
            _collectionView = collectionView;
            _cellIdentifier = cellIdentifier;
        }

        public DifferentialDataModel<TKey, TEntity, TCriteria> DataModel
        {
            get
            {
                return _dataModel;
            }
            set
            {
                if (null != _dataModel)
                    _dataModel.DataSourceChanged -= OnDataSourceChanged;

                var wasNull = _dataModel == null;
                _dataModel = value;

                if (null != _dataModel)
                {
                    _dataModel.DataSourceChanged += OnDataSourceChanged;
                    _idGetter = _dataModel.IdentifierExpression.Compile();

                    if (wasNull)
                        QueueAction(x => TableViewReloadData(x));
                    else
                        DataModel.RequeryWithCriteria(x => { });

                }
            }
        }

        protected void OnDataSourceChanged(object sender, DifferentialDataModel<TKey, TEntity, TCriteria>.CriteriaEventArgs e)
        {
            DispatchQueue.MainQueue.DispatchAsync(() => QueueAction(x => Requery(x, e.Criteria)));
        }

        Task _nextTask;

        protected void QueueAction(Action<TaskCompletionSource<bool>> action)
        {
            var newSource = new TaskCompletionSource<bool>();
            var oldSource = Interlocked.Exchange(ref _tcs, newSource);

            if (null == oldSource)
            {
                action(_tcs);
                _tcs = null;
            }
            else
            {
                var newTask = new Task(() =>
                {
                    _tcs = null;
                    QueueAction(action);
                });

                if (_nextTask == null)
                {
                    oldSource.Task.ContinueWith(x => _nextTask, TaskScheduler.FromCurrentSynchronizationContext());
                }

                _nextTask = newTask;
            }
        }

        protected void TableViewReloadData(TaskCompletionSource<bool> tcs)
        {
            CATransaction.Begin();
            CATransaction.CompletionBlock = () => tcs.TrySetResult(true);
            this._collectionView.ReloadData();
            CATransaction.Commit();
        }

        protected void Requery(TaskCompletionSource<bool> tcs, TCriteria criteria)
        {
            //if (_collectionView.RowHeight < 0)
                //throw new ArgumentException("Table must have a fixed row height.");


            //if (_loadingCells.Count > 0)
            //{
            //    System.Diagnostics.Debug.WriteLine("canceling load");
            //    _tcs.TrySetResult(true);
            //    return;
            //}
            var contentOffset = _collectionView.ContentOffset;
            var rowCountBefore = DataModel.Count;
            var dataModelSnapshot = DataModel.FromCriteria(criteria);
            //_visibleRows.RemoveStaleRows(_collectionView.IndexPathsForVisibleItems.Select(x => x.Row).Union((_collectionView.IndexPathsForVisibleItems ?? new NSIndexPath[0]).Select(x => x.Row)));

            var previousRows = _visibleRows;

            var selectedIds = previousRows.Where(x => _collectionView.IndexPathsForVisibleItems != null && _collectionView.GetIndexPathsForSelectedItems().Any(y => y.Row == x.Position)).Select(x => x.Key).ToList();
            // 1. pull new 'currently visible' id list from database

            var rowCountAfter = dataModelSnapshot.Count;
            var snapshot = previousRows.Snapshot(rowCountBefore, rowCountAfter, selectedIds);
            var rowHeight = (_collectionView.CollectionViewLayout as UICollectionViewFlowLayout).ItemSize.Height;
            var maxRowCount = (int)Math.Ceiling(_collectionView.Bounds.Height / rowHeight);

            var newTopRow = (int)Math.Floor(_collectionView.ContentOffset.Y / rowHeight);
            //var newTopRow = _collectionView.IndexPathsForVisibleItems.Select(x => x.Row).FirstOrDefault();


            var newBottomRow = newTopRow + maxRowCount - 1;

            var rowIdsInViewport = dataModelSnapshot.GetIds(newTopRow, (int)maxRowCount);

            var newRows = dataModelSnapshot.GetRowPositions(rowIdsInViewport.Union(previousRows.RowIds).Union(selectedIds)).ToList();

            var invalidResults = newRows.GroupBy(x => x.Position).Where(x => x.Count() > 1).Select(x => new { Position = x, Count = x.Count() });
            foreach (var item in invalidResults)
            {
                System.Diagnostics.Debug.WriteLine($"RowPosition results are incorrect: Row position {item.Position} appears {item.Count} times.");
            }

            List<int> rowsToRefresh = new List<int>();

            var q = previousRows.FullOuterJoin(newRows, a => a.Key, b => b.Key, (a, b, id) => new { Before = a, After = b });

            foreach (var joined in q)
            {
                if (joined.After == null)
                {
                    snapshot.Removed(joined.Before.Position);
                }
                else if (joined.Before == null)
                {
                    // was not previously available, meaning it must have become visible
                    snapshot.Appeared(joined.After);
                }
                else
                {
                    // row was previously available too, so it has moved, or held
                    if (joined.Before.Position != joined.After.Position)
                        snapshot.Moved(joined.Before.Position, joined.After.Position);
                    else
                        snapshot.Held(joined.Before.Position);

                    if (joined.Before.Version != joined.After.Version)
                    {
                        rowsToRefresh.Add(joined.After.Position);
                        joined.Before.Version = joined.After.Version;
                    }
                }
            }

            // calculate new legal position
            var top = Math.Min(contentOffset.Y, Math.Max(0, rowCountAfter * rowHeight - _collectionView.Bounds.Height));

            var tableInstructions = snapshot.Commit(newTopRow, newBottomRow);

            if (tableInstructions.HasChanges)
            {
                _collectionView.PerformBatchUpdates(() =>
                {
                    foreach (var row in tableInstructions.Moved)
                        _collectionView.MoveItem(NSIndexPath.FromRowSection(row.Key, 0), NSIndexPath.FromRowSection(row.Value, 0));

                    _collectionView.InsertItems(tableInstructions.InsertedVisible.Union(tableInstructions.InsertedInvisible).Select(x => NSIndexPath.FromRowSection(x, 0)).ToArray());

                    _collectionView.DeleteItems(tableInstructions.DeletedVisible.Union(tableInstructions.DeletedInvisible).Select(x => NSIndexPath.FromRowSection(x, 0)).ToArray());

                    // the new source has the new filter
                    dataModelSnapshot.Commit();

                    // ok, now commit the change
                    // blow away the item cache
                    _cache = new BatchingCache<TEntity>(CACHE_SIZE);

                    tcs.TrySetResult(true);
                }, (finished) =>
                {
                    // reloads must happen after the collection has been rearranged, to avoid cell multiple-animation conflicts.
                    _collectionView.ReloadItems(rowsToRefresh.Select(x => NSIndexPath.FromRowSection(x, 0)).ToArray());
                });
            }
            else
            {
                dataModelSnapshot.Commit();

                if (rowsToRefresh.Count > 0)
                {
                    foreach (var item in rowsToRefresh)
                        _cache.RemovePageForRow(item);
                    _collectionView.ReloadItems(rowsToRefresh.Select(x => NSIndexPath.FromRowSection(x, 0)).ToArray());
                }
                tcs.TrySetResult(true);
            }
        }

        public override nint GetItemsCount(UICollectionView collectionView, nint section)
        {
            return DataModel?.Count ?? 0;
        }

        public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var cell = (UICollectionViewCell)collectionView.DequeueReusableCell(_cellIdentifier, indexPath);
            //var cell = new UITableViewCell();

            return cell;
        }

        public override void WillDisplayCell(UICollectionView collectionView, UICollectionViewCell cell, NSIndexPath indexPath)
        {
            var cache = _cache;
            _loadingCells.Add(indexPath.Row);

            // this is when we should load it
            _cache.Get(indexPath.Row, DataModel.GetPage)
                  .ContinueWith(x => OnDataContextLoadedInternal(cache, cell, indexPath.Row, x.Result), TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void OnDataContextLoadedInternal(BatchingCache<TEntity> cache, UICollectionViewCell cell, int rowIndex, TEntity entity)
        {
            if (_cache != cache)
            {
                if (_collectionView.IndexPathsForVisibleItems.Any(x => x.Row == rowIndex))
                    _cache.Get(rowIndex, DataModel.GetPage)
                          .ContinueWith(x => OnDataContextLoadedInternal(_cache, cell, rowIndex, x.Result), TaskScheduler.FromCurrentSynchronizationContext());

                // cache has been invalidated
                return;
            }
            try
            {
                // if the is not 'loading', and it has not become visible... then it must have passed out of visibility
                if (!_loadingCells.Contains(rowIndex))
                    return;

                _loadingCells.Remove(rowIndex);

                var rowMeta = _idGetter(entity);
                rowMeta.Position = rowIndex;
                _visibleRows.Set(rowMeta);

                DispatchQueue.MainQueue.DispatchAsync(() =>
                {
                    //cell.TextLabel.Text = null;
                    OnDataContextLoaded(_visibleRows, cell, rowIndex, entity);
                });
            }
            catch (Exception ex)
            {
            }
        }

        protected abstract void OnDataContextLoaded(VisibleRowManager<TKey> visibleRows, UICollectionViewCell rowView, int rowIndex, TEntity entity);

        public override void CellDisplayingEnded(UICollectionView tableView, UICollectionViewCell cell, NSIndexPath indexPath)
        {
            if (!tableView.IndexPathsForVisibleItems.Contains(indexPath)) // sometimes CellDisplayingEnded fires when row was not actually removed... not sure why.
                _loadingCells.Remove(indexPath.Row);
        }
    }
}
