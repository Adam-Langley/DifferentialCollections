using System;
using System.Collections.Generic;

namespace DifferentialCollections
{
    public class ViewInstructions
    {
        readonly HashSet<int> _insertedVisible = new HashSet<int>();
        readonly HashSet<int> _insertedInvisible = new HashSet<int>();
        readonly HashSet<int> _deletedVisible = new HashSet<int>();
        readonly HashSet<int> _deletedInvisible = new HashSet<int>();
        readonly Dictionary<int, int> _moved = new Dictionary<int, int>();

        public IEnumerable<int> InsertedVisible { get { return _insertedVisible; } }
        public IEnumerable<int> InsertedInvisible { get { return _insertedInvisible; } }
        public IEnumerable<int> DeletedVisible { get { return _deletedVisible; } }
        public IEnumerable<int> DeletedInvisible { get { return _deletedInvisible; } }
        public IDictionary<int, int> Moved { get { return _moved; } }

        public bool HasChanges
        {
            get
            {
                return _insertedVisible.Count > 0
                       || _insertedInvisible.Count > 0
                       || _deletedVisible.Count > 0
                       || _deletedInvisible.Count > 0
                       || _moved.Count > 0;
            }
        }

        public void Insert(int index, bool visible)
        {
            if (visible)
                _insertedVisible.Add(index);
            else
                _insertedInvisible.Add(index);

        }

        public void Delete(int index, bool visible)
        {
            if (visible)
                _deletedVisible.Add(index);
            else
                _deletedInvisible.Add(index);
        }

        public void Delete(IEnumerable<int> indices, bool visible)
        {
            foreach (var index in indices)
                this.Delete(index, visible);
        }

        public void Move(int from, int to)
        {
            _moved[from] = to;
        }

        public bool CanInsertAt(int index)
        {
            return !(_insertedVisible.Contains(index)
                ||
                _insertedInvisible.Contains(index)
                ||
                _moved.ContainsValue(index));
        }

        public bool CanDeleteFrom(int index)
        {
            return !(_deletedVisible.Contains(index)
                ||
                _deletedInvisible.Contains(index)
                ||
                 _moved.ContainsKey(index));
        }

        public int NetChange
        {
            get
            {
                return (_insertedVisible.Count + _insertedInvisible.Count)
                    -
                    (_deletedVisible.Count + _deletedInvisible.Count);
            }
        }

    }

}
