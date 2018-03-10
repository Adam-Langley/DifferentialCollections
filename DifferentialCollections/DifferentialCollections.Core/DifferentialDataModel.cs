using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DifferentialCollections
{
    public abstract class DifferentialDataModel<TIdentifier> : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RecalculateCount()
        {
            var oldCount = _count;
            _count = GetCount();
            if (oldCount != _count)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        }

        public abstract int GetCount();

        protected int? _count;
        public int Count
        {
            get
            {
                if (null == _count)
                    RecalculateCount();
                return _count.Value;
            }
        }

        public class RowMeta
        {
            public TIdentifier Key { get; set; }
            public long Version { get; set; }
            public int Position { get; set; }

            public override string ToString()
            {
                return string.Format("[RowMeta: Key={0}, Version={1}, Position={2}]", Key, Version, Position);
            }
        }
    }

    public abstract class DifferentialDataModel<TIdentifier, TModel, TCriteria> : DifferentialDataModel<TIdentifier> where TCriteria : class
    {
        public static Action<DifferentialDataModel<TIdentifier,TModel,TCriteria>, Action<TCriteria>> PlatformRequeryWithCriteria { get; set; }

        public class CriteriaEventArgs : EventArgs
        {
            public TCriteria Criteria { get; set; }
            public Action Completed { get; set; }
        }

        protected DifferentialDataModel<TIdentifier, TModel, TCriteria> _parent;

        public event EventHandler<CriteriaEventArgs> DataSourceChanged;

        public Expression<Func<TModel, RowMeta>> IdentifierExpression { get; private set; }

        public abstract IEnumerable<TModel> GetPage(int skip, int take);

        public abstract IEnumerable<RowMeta> GetRowMeta(IEnumerable<TIdentifier> identifiers);

        public abstract IEnumerable<TIdentifier> GetIds(int skip, int take);

        private TaskCompletionSource<bool> _tcs;
        public Task BusyWaiter
        {
            get
            {
                return _tcs.Task;
            }
        }

        public VisibleRowManager<TIdentifier> VisibleRows
        {
            get;
            private set;
        } = new VisibleRowManager<TIdentifier>();

        private TCriteria _criteria;
        public TCriteria Criteria
        {
            get
            {
                return _criteria;
            }
            protected set
            {
                _criteria = value;
                RecalculateCount();
            }
        }

        public void Requery()
        {
            RequeryWithCriteria(x => {});
        }

        public void RequeryWithCriteria(Action<TCriteria> mutator)
        {
            PlatformRequeryWithCriteria(this, mutator);
        }

        // remove the row from the cache, causing it to be reloaded at next refresh
        public void Evict(TIdentifier id)
        {
            RowMeta rowInfo;
            if (VisibleRows.TryGetRowForId(id, out rowInfo))
            {
                rowInfo.Version = 0;
            }
        }

        public abstract DifferentialDataModel<TIdentifier, TModel, TCriteria> FromCriteria(TCriteria criteria);

        readonly SynchronizationContext _syncContext;

        public DifferentialDataModel(Expression<Func<TModel, RowMeta>> idExpression)
        {
            _syncContext = SynchronizationContext.Current;
            IdentifierExpression = idExpression;
            _tcs = new TaskCompletionSource<bool>();
        }

        public void OnDataSourceChanged(TCriteria criteria)
        {
            if (SynchronizationContext.Current != _syncContext)
                throw new InvalidOperationException("Call reload from the same thread that owns this instance.");

            if (null != DataSourceChanged)
            {
                _tcs = new TaskCompletionSource<bool>();
                DataSourceChanged.Invoke(this, new CriteriaEventArgs()
                {
                    Criteria = criteria,
                    Completed = () => {
                        _tcs.TrySetResult(true);
                    }
                });
            }
            else
                Criteria = criteria;
        }

        public void Commit()
        {
            _parent.Criteria = Criteria;
        }
    }
}
