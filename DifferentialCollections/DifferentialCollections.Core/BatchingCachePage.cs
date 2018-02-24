using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DifferentialCollections
{
    public partial class BatchingCache<T>
    {
        class BatchingCachePage
        {
            public TaskCompletionSource<IEnumerable<T>> Source { get; set; }
            public int StartRow { get; private set; }
            public int EndRow { get; private set; }

            public BatchingCachePage(int startRow, int endRow)
            {
                StartRow = startRow;
                EndRow = endRow;

                Source = new TaskCompletionSource<IEnumerable<T>>();
            }
        }
    }
}
