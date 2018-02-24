using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DifferentialCollections
{
    public partial class VisibleRowManager<TIdentifier> : IEnumerable<DifferentialDataModel<TIdentifier>.RowVersion>
    {
        RowVersionDictionary _cache = new RowVersionDictionary();

        public bool TryGetIdAtRow(int row, out TIdentifier result)
        {
            //var meta
            DifferentialDataModel<TIdentifier>.RowVersion meta = null;
            if (_cache.TryGetValue(row, out meta))
            {
                result = meta.Key;
                return true;
            }
            result = default(TIdentifier);
            return false;
        }

        public VisibleRowManager()
            : this(new RowVersionDictionary())
        {
        }

        private VisibleRowManager(RowVersionDictionary cache)
        {
            _cache = cache;
        }

        public IEnumerable<TIdentifier> RowIds
        {
            get
            {
                return _cache.Select(x => x.Value.Key);
            }
        }

        public void Set(DifferentialDataModel<TIdentifier>.RowVersion rowMeta)
        {
            if (_cache.ContainsKey(rowMeta.Position) && _cache[rowMeta.Position] != rowMeta)
            {
                if (!object.Equals(_cache[rowMeta.Position].Key, rowMeta.Key))
                    throw new InvalidOperationException($"Region Manager already contains row {rowMeta.Position}");
            }
            else
                _cache[rowMeta.Position] = rowMeta;
        }

        public bool IsIdVisible(TIdentifier id)
        {
            return _cache.Any(x => object.Equals(x.Value.Key, id));
        }

        public void RemoveStaleRows(IEnumerable<int> nonStaleRows)
        {
            Queue<int> keysToRemove = new Queue<int>();
            foreach (var item in _cache)
            {
                bool isVisible = nonStaleRows.Any(x => x == item.Key);
                if (!isVisible)
                    keysToRemove.Enqueue(item.Key);
            }
            while (keysToRemove.Count > 0)
            {
                var item = keysToRemove.Dequeue();
                _cache.Remove(item);
            }
        }

        public string IdsAsCommaList
        {
            get
            {
                return String.Join(", ", _cache.Values.Select(x => x.Key.ToString()));
            }
        }

        public bool TryGetRowForId(TIdentifier id, out DifferentialDataModel<TIdentifier>.RowVersion result)
        {
            var cacheList = _cache.ToList();
            var entry = cacheList.SingleOrDefault(x => object.Equals(x.Value.Key, id));
            result = entry.Value;
            return result != null;
        }

        public int TopRow
        {
            get
            {
                if (_cache.Count == 0)
                    return -1;

                return _cache.First().Key;
            }
        }

        public int BottomRow
        {
            get
            {
                if (_cache.Count == 0)
                    return -1;

                return _cache.Last().Key;
            }
        }

        public int Count
        {
            get
            {
                return _cache.Count;
            }
        }

        public VisibleRowManagerTransaction Snapshot(int rowCountBefore, int rowCountAfter, IEnumerable<TIdentifier> retainIds)
        {
            return new VisibleRowManagerTransaction(this, rowCountBefore, rowCountAfter, retainIds);
        }

        public IEnumerator<DifferentialDataModel<TIdentifier>.RowVersion> GetEnumerator()
        {
            return _cache.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
