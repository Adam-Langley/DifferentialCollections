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
        UIKit.UITableView table { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (activityView != null) {
                activityView.Dispose ();
                activityView = null;
            }

            if (table != null) {
                table.Dispose ();
                table = null;
            }
        }
    }
}