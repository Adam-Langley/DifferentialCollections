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
    [Register ("CryptoCoinCell")]
    partial class CryptoCoinCell
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel lblChange { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel lblName { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel lblValue { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIView vwSeparator { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (lblChange != null) {
                lblChange.Dispose ();
                lblChange = null;
            }

            if (lblName != null) {
                lblName.Dispose ();
                lblName = null;
            }

            if (lblValue != null) {
                lblValue.Dispose ();
                lblValue = null;
            }

            if (vwSeparator != null) {
                vwSeparator.Dispose ();
                vwSeparator = null;
            }
        }
    }
}