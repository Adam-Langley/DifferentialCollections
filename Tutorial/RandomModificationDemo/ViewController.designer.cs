// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace DifferentialCollections
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIView activityView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton btnDelete { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton btnInsert { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton btnMove { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UICollectionView collectionView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISegmentedControl segmentSort { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISearchBar txtSearch { get; set; }

        [Action ("sort_change:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void sort_change (UIKit.UISegmentedControl sender);

        void ReleaseDesignerOutlets ()
        {
            if (activityView != null) {
                activityView.Dispose ();
                activityView = null;
            }

            if (btnDelete != null) {
                btnDelete.Dispose ();
                btnDelete = null;
            }

            if (btnInsert != null) {
                btnInsert.Dispose ();
                btnInsert = null;
            }

            if (btnMove != null) {
                btnMove.Dispose ();
                btnMove = null;
            }

            if (collectionView != null) {
                collectionView.Dispose ();
                collectionView = null;
            }

            if (segmentSort != null) {
                segmentSort.Dispose ();
                segmentSort = null;
            }

            if (txtSearch != null) {
                txtSearch.Dispose ();
                txtSearch = null;
            }
        }
    }
}