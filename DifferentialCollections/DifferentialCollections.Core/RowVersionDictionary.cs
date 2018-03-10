using System;
using System.Collections.Generic;

namespace DifferentialCollections
{
    public partial class VisibleRowManager<TIdentifier>
    {
        public class RowVersionDictionary : SortedDictionary<int, DifferentialDataModel<TIdentifier>.RowMeta>
        {
            public RowVersionDictionary()
            {
            }

            public RowVersionDictionary(RowVersionDictionary parent)
                : base(parent)
            {
            }

            public DifferentialDataModel<TIdentifier>.RowMeta this[int index]
            {
                get
                {
                    return base[index];
                }
                set
                {
                    base[index] = value;
                    value.Position = index;
                }
            }
        }
    }
}