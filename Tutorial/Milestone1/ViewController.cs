using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using DifferentialCollections.Models;
using Newtonsoft.Json;
using UIKit;

namespace DifferentialCollections
{
    public partial class ViewController : UIViewController
    {
        UITableViewController _tableController;
        CryptoCoinTableViewSource _source;

        protected ViewController(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            table.RegisterClassForCellReuse(typeof(UITableViewCell), "cell");
            _source = new CryptoCoinTableViewSource(table, activityView, null, "cell");

            _tableController = new UITableViewController();
            _tableController.TableView = table;

            AppDelegate.Connection.CreateTable<CryptoCoin>();
            AppDelegate.Connection.Execute($"DELETE from {nameof(CryptoCoin)}");

            // grab data
            Task.Run(DownloadData);
        }

        private async Task DownloadData()
        {
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

                    var rnd = new Random(DateTime.Now.Millisecond);
                    AppDelegate.Connection.BeginTransaction();
                    for (var i = 0; i < 1000 / coins.Length; i++) // multiply the number of records by 100 for stress-test purposes.
                        foreach (var coin in coins)
                        {
                            AppDelegate.Connection.Insert(coin);
                        }
                    AppDelegate.Connection.Commit();

                    InvokeOnMainThread(() =>
                    {
                        table.Source = _source;
                        table.ReloadData();
                    });
                }
            }
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }
    }
}
