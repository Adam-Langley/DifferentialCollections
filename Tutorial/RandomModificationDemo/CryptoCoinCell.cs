using System;

using Foundation;
using DifferentialCollections.Models;
using UIKit;

namespace DifferentialCollections
{
    public partial class CryptoCoinCell : UICollectionViewCell
    {
        public static readonly NSString Key = new NSString("CryptoCoinCell");
        public static readonly UINib Nib;

        static CryptoCoinCell()
        {
            Nib = UINib.FromName("CryptoCoinCell", NSBundle.MainBundle);
        }

        protected CryptoCoinCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            lblChange.Layer.CornerRadius = 4f;
        }

        public override bool Selected
        {
            get
            {
                return base.Selected;
            }
            set
            {
                base.Selected = value;

                if (Selected)
                {
                    BackgroundColor = UIColor.LightGray;
                    vwSeparator.Hidden = true;
                }
                else
                {
                    BackgroundColor = UIColor.White;
                    vwSeparator.Hidden = false;
                }

                SetLabelBackground();
            }
        }

        void SetLabelBackground()
        {
            if (null == _entity)
                return;
            
            if (_entity.PreviousPriceUSD.CompareTo(_entity.PriceUSD) == 0 && !Selected)
                lblChange.Layer.BackgroundColor = UIColor.Clear.CGColor;
            else
            {

                if (_entity.PreviousPriceUSD < _entity.PriceUSD)
                    lblChange.Layer.BackgroundColor = UIColor.FromRGB(0x4C, 0xAF, 0x50).CGColor;
                else if (_entity.PreviousPriceUSD > _entity.PriceUSD)
                    lblChange.Layer.BackgroundColor = UIColor.FromRGB(0xF4, 0x43, 0x36).CGColor;


                BeginInvokeOnMainThread(() =>
                {
                    UIView.Animate(1f, () =>
                    {
                        lblChange.Layer.BackgroundColor = UIColor.Clear.CGColor;
                    }, () => { });
                });
            }
        }

        CryptoCoin _entity;
        internal void SetEntity(CryptoCoin entity)
        {
            if (null == entity)
                return;
            
            _entity = entity;

            lblName.Text = entity.Name;
            lblValue.Text = $"US ${entity.PriceUSD.ToString()}";
            lblChange.Text = $"{entity.TwentyFourHourChange}%";

            SetLabelBackground();
        }
    }
}
