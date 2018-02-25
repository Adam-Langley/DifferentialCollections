using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Mono.Data.Sqlite;
using DifferentialCollections.Models;
using SQLite;
using UIKit;

namespace DifferentialCollections
{
    class CryptoCoinCollectionViewSource : DifferentialCollectionViewSource<string, CryptoCoin, CryptoCoinCriteria>
    {
        readonly string _cellIdentifier;
        readonly BatchingCache<CryptoCoin> _cache = new BatchingCache<CryptoCoin>(20);
        readonly HashSet<int> _loadingCells = new HashSet<int>();

        public CryptoCoinCollectionViewSource(UICollectionView collectionView, string nibName, string cellIdentifier = null)
            : base(collectionView, nibName, cellIdentifier)
        {
            collectionView.RegisterNibForCell(CryptoCoinCell.Nib, CryptoCoinCell.Key);

            _cellIdentifier = cellIdentifier;
        }

        public override UICollectionViewCell GetCell(UICollectionView collectionView, Foundation.NSIndexPath indexPath)
        {
            var cell = (CryptoCoinCell)collectionView.DequeueReusableCell(CryptoCoinCell.Key, indexPath);
            return cell;
        }

        public override bool ShouldSelectItem(UICollectionView collectionView, Foundation.NSIndexPath indexPath)
        {
            return true;
        }

        protected override void OnDataContextLoaded(VisibleRowManager<string> visibleRows, UICollectionViewCell rowView, int rowIndex, CryptoCoin entity)
        {
            var typedCell = rowView as CryptoCoinCell;
            typedCell.SetEntity(entity);
        }
    }
}
