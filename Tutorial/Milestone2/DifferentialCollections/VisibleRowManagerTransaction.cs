using System;
using System.Collections.Generic;
using System.Linq;

namespace DifferentialCollections
{
    public partial class VisibleRowManager<TIdentifier>
    {
        public class VisibleRowManagerTransaction
        {
            readonly VisibleRowManager<TIdentifier> _owner;
            readonly Dictionary<int, int> _movedRows = new Dictionary<int, int>();
            readonly HashSet<int> _removed = new HashSet<int>();
            readonly List<int> _held = new List<int>();
            readonly Dictionary<int, DifferentialDataModel<TIdentifier>.RowVersion> _appeared = new Dictionary<int, DifferentialDataModel<TIdentifier>.RowVersion>();

            readonly int _rowCountBefore;
            readonly int _rowCountAfter;
            readonly IEnumerable<TIdentifier> _retainIds;

            public VisibleRowManagerTransaction(VisibleRowManager<TIdentifier> owner, int rowCountBefore, int rowCountAfter, IEnumerable<TIdentifier> retainIds)
            {
                _owner = owner;
                _rowCountBefore = rowCountBefore;
                _rowCountAfter = rowCountAfter;
                _retainIds = retainIds;
            }

            public void Appeared(DifferentialDataModel<TIdentifier>.RowVersion row)
            {
                if (_appeared.ContainsKey(row.Position))
                    throw new ArgumentException($"Appeared {row.Position}: Already have row at position.");

                _appeared[row.Position] = row;
            }

            public void Removed(int position)
            {
                if (_removed.Contains(position))
                    throw new ArgumentException($"Remove {position}: Already removed row at position.");

                _removed.Add(position);
            }

            public void Moved(int from, int to)
            {
                if (_movedRows.ContainsValue(to))
                    throw new ArgumentException($"Move {from}->{to}: Destination row already target by a move.");

                if (_movedRows.ContainsKey(from))
                    throw new ArgumentException($"Move {from}->{to}: Source row already moved.");

                _movedRows[from] = to;
            }

            public void Held(int position)
            {
                _held.Add(position);
            }

            public TableInstructions Commit(int topRow, int bottomRow)
            {
                var tableInstructions = new TableInstructions();
                var newCache = new RowVersionDictionary();
                var rowCountDifference = _rowCountAfter - _rowCountBefore;
                var topDeficit = 0;

                foreach (var item in _held)
                {
                    // row was previously visible
                    if (_owner._cache.ContainsKey(item))
                    {
                        // lock it in at new position
                        newCache[item] = _owner._cache[item];
                    }
                    else
                    {
                        throw new Exception("This should not happen!");
                    }
                }

                foreach (var item in _movedRows)
                {
                    // otherwise we need to add a new row.
                    if (item.Value >= _rowCountAfter)
                    {
                        throw new ArgumentException("Cannot move to nonexistent row.");
                    }

                    tableInstructions.Move(item.Key, item.Value);

                    if (item.Key >= topRow && item.Value < topRow)
                        topDeficit++; // too many above top position

                    if (item.Value >= topRow && item.Key < topRow)
                        topDeficit--; // not enough above top position

                    // row was previously visible
                    if (_owner._cache.ContainsKey(item.Key))
                    {
                        // lock it in at new position
                        newCache[item.Value] = _owner._cache[item.Key];
                    }
                }

                tableInstructions.Delete(_removed, true);

                var rowChangeByInstructions = _appeared.Count - _removed.Count;

                // item either appeared because it was added to the database,
                // OR it was simply moved into view.
                // if it was added to the database, BUT th net-change stayed the same, another row (non-visible) may have
                // been removed, allowing count to stay the same - in which case we can treat it like a move.
                int deletionPoint = _rowCountBefore - 1;
                foreach (var item in _appeared)
                {
                    if (tableInstructions.NetChange < rowCountDifference)
                    {
                        // otherwise we need to add a new row.
                        tableInstructions.Insert(item.Key, true);
                    }
                    else
                    {
                        if (topDeficit > 0)
                        {
                            deletionPoint = 0;
                            while (_appeared.ContainsKey(deletionPoint) || !tableInstructions.CanDeleteFrom(deletionPoint))
                                deletionPoint++;

                            if (deletionPoint >= topRow)
                            {
                                // WARNING
                                System.Diagnostics.Debug.WriteLine("WAS EXPECTING TO BE ABLE TO MOVE FROM ABOVE TOP ROW!");
                            }

                            topDeficit--;

                            if (item.Key >= _rowCountAfter)
                            {
                                throw new ArgumentException("Cannot move to nonexistent row.");
                            }

                            // this is not a true move, we have picked a cell essentially at random, so use a del/add pair to avoid it
                            // animating across the screen.
                            //tableInstructions.Move(deletionPoint, item.Key);
                            tableInstructions.Delete(deletionPoint, false);
                            tableInstructions.Insert(item.Key, false);
                        }
                        else
                        {
                            deletionPoint = _rowCountBefore - 1;

                            while (_appeared.ContainsKey(deletionPoint) || !tableInstructions.CanDeleteFrom(deletionPoint))
                                deletionPoint--;

                            if (item.Key >= _rowCountAfter)
                            {
                                throw new ArgumentException("Cannot move to nonexistent row.");
                            }

                            // this is not a true move, we have picked a cell essentially at random, so use a del/add pair to avoid it
                            // animating across the screen.
                            //tableInstructions.Move(deletionPoint, item.Key);
                            tableInstructions.Delete(deletionPoint, false);
                            tableInstructions.Insert(item.Key, false);

                            deletionPoint--;
                        }
                    }

                    newCache[item.Key] = item.Value;
                }

                if (tableInstructions.NetChange < rowCountDifference)
                {
                    // we need to compensate by adding some rows to our UITable
                    // always add to the end.
                    int insertionPoint = _rowCountAfter - 1;
                    var numberToAdd = rowCountDifference - tableInstructions.NetChange;
                    for (int i = 0; i < numberToAdd; i++)
                    {
                        while (newCache.ContainsKey(insertionPoint) || !tableInstructions.CanInsertAt(insertionPoint))
                            insertionPoint--;

                        tableInstructions.Insert(insertionPoint, false);

                        insertionPoint--;
                    }
                }
                else if (tableInstructions.NetChange > rowCountDifference)
                {
                    // we need to compensate by removing some rows from our UITable
                    deletionPoint = _rowCountBefore - 1;
                    var numberToRemove = tableInstructions.NetChange - rowCountDifference;
                    for (int i = 0; i < numberToRemove; i++)
                    {
                        while (newCache.ContainsKey(deletionPoint) || !tableInstructions.CanDeleteFrom(deletionPoint))
                            deletionPoint--;

                        tableInstructions.Delete(deletionPoint, false);

                        deletionPoint--;
                    }
                }
                var toRemove = new Stack<int>();
                foreach(var item in newCache){
                    if ((item.Key < topRow || item.Key > bottomRow) && !_retainIds.Contains(item.Value.Key))
                        toRemove.Push(item.Key);
                }

                int rowToRemove = 0;
                while(toRemove.TryPop(out rowToRemove))
                    newCache.Remove(rowToRemove);
                _owner._cache = newCache;
                return tableInstructions;
            }
        }
    }
}
