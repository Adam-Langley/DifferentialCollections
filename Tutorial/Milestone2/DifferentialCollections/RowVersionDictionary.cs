using System;
using System.Collections.Generic;

namespace DifferentialCollections
{
    public partial class VisibleRowManager<TIdentifier>
    {
        public class RowVersionDictionary : SortedDictionary<int, DifferentialDataModel<TIdentifier>.RowVersion>
        {
            public RowVersionDictionary()
            {
            }

            public RowVersionDictionary(RowVersionDictionary parent)
                : base(parent)
            {
            }

            public DifferentialDataModel<TIdentifier>.RowVersion this[int index]
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