using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DifferentialCollections
{
    /// <summary>
    /// An asynchronous cache which populates in batches of size <paramref name="pageSize"/>
    /// </summary>
    public partial class BatchingCache<T>
    {
        // the underlying thread-safe storage structure.
        readonly ConcurrentDictionary<int, BatchingCachePage> _persistence = new ConcurrentDictionary<int, BatchingCachePage>();
        // contents will be requested and stored in pages of this size
        readonly int _pageSize = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:PositionAndVersionTracking.BatchingCache`1"/> class.
        /// </summary>
        /// <param name="pageSize">The size of batches.</param>
        public BatchingCache(int pageSize)
        {
            if (pageSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero. Ideally, larger than the number of rows that are likely to fit on-screen.");
            
            _pageSize = pageSize;
        }

        /// <summary>
        /// Removes the page for a given row position.
        /// </summary>
        /// <returns><c>true</c>, if a page was removed, <c>false</c> otherwise.</returns>
        /// <param name="row">The row position for which the enclosing page will be removed.</param>
        public bool RemovePageForRow(int row)
        {
            if (row < 0)
                throw new ArgumentOutOfRangeException(nameof(row), "Row must be a positive integer.");

            var pageIndex = (row / _pageSize) * _pageSize;

            BatchingCachePage page = null;

            return _persistence.TryRemove(pageIndex, out page);
        }

        public bool TryGet(int row, out T result)
        {
            // calculate the position of the enclosing page.
            var offsetOfPage = (row / _pageSize) * _pageSize;
            // calculate the offset into the page that the row resides
            var offset = row - offsetOfPage;

            BatchingCachePage page;
            if (!_persistence.TryGetValue(offsetOfPage, out page))
            {
                result = default(T);
                return false;
            }

            result = page.Source.Task.Result.Skip(row).FirstOrDefault();
            return true;
        }

        /// <summary>
        /// Get the specified row, invoke the getter method if the row is not currently available in the cache.
        /// </summary>
        /// <returns>A Task that will complete when the row has been loaded.</returns>
        /// <param name="row">The position of the row to return.</param>
        /// <param name="getter">The callback function used to load the page that contains the given row index.</param>
        public Task<T> Get(int row, Func<int, int, IEnumerable<T>> getter)
        {
            // calculate the position of the enclosing page.
            var offsetOfPage = (row / _pageSize) * _pageSize;
            // calculate the offset into the page that the row resides
            var offset = row - offsetOfPage;

            // Attempt to retrieve the page from storage.
            var cacheSource = _persistence.GetOrAdd(offsetOfPage, x =>
            {
                // The page was not in storage, so let's create a page to hold
                // the data we're about to request.
                var page = new BatchingCachePage(offsetOfPage, offsetOfPage + _pageSize);

                // Spawn a task to pull a page of data.
                // The Task Result property doubles as a storage mechanism going forward.
                Task.Run(() => PopulateCachePage(page.Source, offsetOfPage, getter));
                return page;
            });

            var cachedTask = cacheSource.Source.Task;
            // Our results will not be furnished to the caller until the Task has completed...
            TaskCompletionSource<T> rowSource = new TaskCompletionSource<T>();
            // ... which may be immediately
            if (cachedTask.IsCompleted)
                OnQueryCompleted(cacheSource, rowSource, cachedTask.Result, offset);
            else
                // or may be once the database call has completed.
                cachedTask.ContinueWith(x => OnQueryCompleted(cacheSource, rowSource, x.Result, offset));

            return rowSource.Task;
        }

        void PopulateCachePage(TaskCompletionSource<IEnumerable<T>> cacheSource, int start, Func<int, int, IEnumerable<T>> getter)
        {
            // call to the delegate to pull an IEnumerable from the database given a skip/take
            var cache = getter(start, _pageSize);
            // store the results, completing our Task.
            cacheSource.TrySetResult(cache.ToList());
        }

        void OnQueryCompleted(BatchingCachePage page, TaskCompletionSource<T> rowSource, IEnumerable<T> result, int offsetIntoPage)
        {
            // The database results have returned, skip and take the results to grab a single row...
            var datacontext = result.Skip(offsetIntoPage).Take(1).SingleOrDefault();
            // ...and return the row to our caller.
            rowSource.TrySetResult(datacontext);
        }
    }
}