﻿using System;
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
using System.Text;

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

            this.btnInsert.TouchUpInside += (sender, e) => OnInsert((UIButton)sender);
            this.btnDelete.TouchUpInside += (sender, e) => OnDelete((UIButton)sender);
            this.btnMove.TouchUpInside += (sender, e) => OnMove((UIButton)sender);
            Task.Run(DownloadData);
        }

        private static Random Random = new Random((int)DateTime.Now.Ticks);
        private static string RandomString(int length)
        {
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
            var builder = new StringBuilder();

            for (var i = 0; i < length; i++)
            {
                var c = pool[Random.Next(0, pool.Length)];
                builder.Append(c);
            }

            return builder.ToString();
        }

        void OnInsert(UIButton sender)
        {
            // insert a random record into the database
            var newCoin = new CryptoCoin
            {
                Name = "Bitcoin " + RandomString(8),
                PriceUSD = Random.NextDouble() * 2,
                TwentyFourHourChange = Random.NextDouble(),
            };
            newCoin.Id = newCoin.Name;

            AppDelegate.Connection.BeginTransaction();

            AppDelegate.Connection.InsertOrReplace(newCoin);

            AppDelegate.Connection.Commit();

            _cryptoCoinDataSource.Requery();
        }

        void OnMove(UIButton sender)
        {
            // pick 2 random rows, and swap their 24 hour change values
            var count = _cryptoCoinDataSource.Count;
            if (count < 2)
                return;
            var randomIndexA = Random.Next(count - 1);
            var randomIndexB = 0;
            do
            {
                randomIndexB = Random.Next(count - 1);
            } while (randomIndexB == randomIndexA);

            var randomA = _cryptoCoinDataSource.GetPage(randomIndexA, 1).FirstOrDefault();
            var randomB = _cryptoCoinDataSource.GetPage(randomIndexB, 1).FirstOrDefault();
            if (null != randomA && null != randomB)
            {
                AppDelegate.Connection.BeginTransaction();

                var temp = randomA.TwentyFourHourChange;
                randomA.TwentyFourHourChange = randomB.TwentyFourHourChange;
                randomB.TwentyFourHourChange = temp;

                // bump version field so UI can locate changed rows.
                randomA.Version++;
                randomB.Version++;

                // commit to database
                AppDelegate.Connection.UpdateAll(new []{
                    randomA, randomB
                });

                AppDelegate.Connection.Commit();

                // This is the magic line, request re-sync of the view
                _cryptoCoinDataSource.Requery();
            }
        }

        void OnDelete(UIButton sender)
        {
            AppDelegate.Connection.BeginTransaction();

            var count = _cryptoCoinDataSource.Count;
            var random = _cryptoCoinDataSource.GetPage(Random.Next(count - 1), 1).FirstOrDefault();
            if (null != random)
            {
                AppDelegate.Connection.Delete(random);

                AppDelegate.Connection.Commit();

                _cryptoCoinDataSource.Requery();
            }
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
                    await Task.Delay(int.MaxValue, _cancel.Token);
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
