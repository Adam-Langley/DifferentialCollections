using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using Newtonsoft.Json;
using DifferentialCollections.Models;
using UIKit;

namespace DifferentialCollections
{
    public partial class ViewController : UIViewController
    {
        CryptoCoinCollectionViewSource _cryptoCoinSource;
        CryptoCoinDataSource _cryptoCoinDataSource;
        UIRefreshControl _refresh;

        protected ViewController(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.

           

        }

        private void HandleValueChanged(object sender, EventArgs e)
        {
            // start refresh
            //DownloadData();
            _cancel.Cancel();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            _refresh = new UIRefreshControl();

            collectionView.Scrolled += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine("Asdasdad");
            };

            collectionView.DraggingStarted += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine("Asdasdad");
            };

            _cryptoCoinDataSource = new CryptoCoinDataSource(activityView);
            _cryptoCoinSource = new CryptoCoinCollectionViewSource(collectionView, null, CryptoCoinCell.Key);
            _cryptoCoinSource.DataModel = _cryptoCoinDataSource;
            collectionView.RefreshControl = _refresh;

            _refresh.ValueChanged += HandleValueChanged;
            txtSearch.TextChanged += TxtSearch_TextChanged;

            // Perform any additional setup after loading the view, typically from a nib.

            var sectionInsetWidth = collectionView.ContentInset.Left + collectionView.ContentInset.Right;
            collectionView.CollectionViewLayout = new UICollectionViewFlowLayout()
            {
                ScrollDirection = UICollectionViewScrollDirection.Vertical,
                ItemSize = new CoreGraphics.CGSize(collectionView.Bounds.Width - sectionInsetWidth - 20, 68),
                MinimumInteritemSpacing = 0,
                MinimumLineSpacing = 0,
                SectionInset = UIEdgeInsets.Zero,
                SectionInsetReference = UICollectionViewFlowLayoutSectionInsetReference.LayoutMargins,
            };

            collectionView.Source = _cryptoCoinSource;

            segmentSort.AddGestureRecognizer(new UITapGestureRecognizer(SegmentSort_TouchUpInside)
            {
                CancelsTouchesInView = false
            });

            Task.Run(DownloadData);
        }

        void TxtSearch_TextChanged(object sender, UISearchBarTextChangedEventArgs e)
        {
            _cryptoCoinDataSource.RequeryWithCriteria(x =>
            {
                x.FilterString = txtSearch.Text;
            });
        }

        void SegmentSort_TouchUpInside(UITapGestureRecognizer sender)
        {
            this.txtSearch.ResignFirstResponder();
            nfloat width = 0;
            nint newSegment = -1;
            var tapPosition = sender.LocationInView(segmentSort);
            for (int i = 0; i < segmentSort.NumberOfSegments; i++)
            {
                width += !segmentSort.ApportionsSegmentWidthsByContent ? segmentSort.Bounds.Width / segmentSort.NumberOfSegments : segmentSort.SegmentWidth(i);
                if (width > tapPosition.X)
                {
                    newSegment = i;
                    break;
                }
            }

            if (newSegment == segmentSort.SelectedSegment)
            {
                var newDescending = !_cryptoCoinDataSource.Criteria.Descending;
                // change sort direction
                _cryptoCoinDataSource.RequeryWithCriteria(x =>
                {
                    x.Descending = newDescending;
                });

                var segText = segmentSort.TitleAt(newSegment);
                var lastSpace = segText.LastIndexOf(' ');
                if (lastSpace == -1)
                    lastSpace = segText.Length;
                BeginInvokeOnMainThread(() =>
                {
                    segmentSort.SetTitle(segText.Substring(0, lastSpace) + (newDescending ? " v" : " ^"), newSegment);
                });
            } else {
                var segText = segmentSort.TitleAt(newSegment);
                var lastSpace = segText.LastIndexOf(' ');
                if (lastSpace == -1)
                    lastSpace = segText.Length;
                BeginInvokeOnMainThread(() =>
                {
                    segmentSort.SetTitle(segText.Substring(0, lastSpace) + (_cryptoCoinDataSource.Criteria.Descending ? " v" : " ^"), newSegment);                
                });
            }
        }

        partial void sort_change(UISegmentedControl sender)
        {
            _cryptoCoinDataSource.RequeryWithCriteria(x => {
                x.OrderByColumnName = segmentSort.SelectedSegment == 0 ? "Name COLLATE NOCASE" : "TwentyFourHourChange";
            });
        }


        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }

        private async Task DownloadData()
        {
            try
            {
                InvokeOnMainThread(_refresh.BeginRefreshing);
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
                var request = WebRequest.Create(new Uri("https://api.coinmarketcap.com/v1/ticker/")) as HttpWebRequest;
                request.Method = "GET";
                request.ContentType = "application/json";
                WebResponse responseObject = await Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, request);
                using (var responseStream = responseObject.GetResponseStream())
                {
                    JsonSerializer ser = new JsonSerializer();
                    using (var sr = new StreamReader(responseStream))
                    {
                        var coins = ser.Deserialize<CryptoCoin[]>(new JsonTextReader(sr));

                        AppDelegate.Connection.BeginTransaction();

                        foreach (var coin in coins)
                        {
                            var backup = AppDelegate.Connection.Find<CryptoCoin>(coin.Id);

                            if (null != backup)
                            {
                                if (backup.Version != coin.Version)
                                    coin.PreviousPriceUSD = backup.PriceUSD;
                                else
                                    coin.PreviousPriceUSD = backup.PreviousPriceUSD;
                            }

                            AppDelegate.Connection.InsertOrReplace(coin);
                        }

                        AppDelegate.Connection.Commit();

                        InvokeOnMainThread(() =>
                        {
                            _cryptoCoinDataSource.Requery();
                        });
                    }
                }
            } finally
            {
                InvokeOnMainThread(() =>
                {
                    UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
                    _refresh.EndRefreshing();
                });
            }
            _cancel = new CancellationTokenSource();
            await Task.Run(async () => {
                try
                {
                    await Task.Delay(30000, _cancel.Token);
                    // wait for collection to stop scrolling...if it is.
                    await _cryptoCoinSource.WaitForScrollIdle();
                }
                catch (TaskCanceledException)
                {
                    // sleep was canceled - user forced refresh etc.
                }
                Task.Run(DownloadData);
            });
        }

        CancellationTokenSource _cancel;
    }
}
