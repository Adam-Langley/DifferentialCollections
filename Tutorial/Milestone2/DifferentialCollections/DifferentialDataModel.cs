using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

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

        public class RowVersion
        {
            public TIdentifier Key { get; set; }
            public long Version { get; set; }
            public int Position { get; set; }

            public override string ToString()
            {
                return string.Format("[RowVersion: Key={0}, Version={1}, Position={2}]", Key, Version, Position);
            }
        }
    }

    public abstract class DifferentialDataModel<TIdentifier, TModel, TCriteria> : DifferentialDataModel<TIdentifier> where TCriteria : class
    {
        public class CriteriaEventArgs : EventArgs
        {
            public TCriteria Criteria { get; set; }
        }

        protected DifferentialDataModel<TIdentifier, TModel, TCriteria> _parent;

        public event EventHandler<CriteriaEventArgs> DataSourceChanged;

        public Expression<Func<TModel, RowVersion>> IdentifierExpression { get; private set; }

        public abstract IEnumerable<TModel> GetPage(int skip, int take);

        public abstract IEnumerable<RowVersion> GetRowPositions(IEnumerable<TIdentifier> identifiers);

        public abstract IEnumerable<TIdentifier> GetIds(int skip, int take);

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
            var t = typeof(TCriteria);
            var fields = t.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToArray();
            var copy = Activator.CreateInstance(t) as TCriteria;
            for (int i = 0; i < fields.Length; i++)
                fields[i].SetValue(copy, fields[i].GetValue(Criteria));

            mutator(copy);
            this.OnDataSourceChanged(copy);
        }

        public abstract DifferentialDataModel<TIdentifier, TModel, TCriteria> FromCriteria(TCriteria criteria);

        readonly SynchronizationContext _syncContext;

        public DifferentialDataModel(Expression<Func<TModel, RowVersion>> idExpression)
        {
            _syncContext = SynchronizationContext.Current;
            IdentifierExpression = idExpression;
        }

        public void OnDataSourceChanged(TCriteria criteria)
        {
            if (SynchronizationContext.Current != _syncContext)
                throw new InvalidOperationException("Call reload from the same thread that owns this instance.");

            if (null != DataSourceChanged)
            {
                DataSourceChanged.Invoke(this, new CriteriaEventArgs()
                {
                    Criteria = criteria
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
