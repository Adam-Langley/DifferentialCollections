using System.IO;
using Foundation;
using DifferentialCollections.Models;
using SQLite;
using UIKit;

namespace DifferentialCollections
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        public static SQLiteConnection Connection { get; set; }

        static AppDelegate(){
            var path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            path = Path.Combine(path, "test.sqlite");

            Connection = new SQLite.SQLiteConnection(path);
        }
        // class-level declarations

        public override UIWindow Window
        {
            get;
            set;
        }

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {

            Connection.CreateTable<CryptoCoin>();
            //conn.Execute($"DELETE from {nameof(CryptoCoin)}");

            //var rnd = new Random(DateTime.Now.Millisecond);
            //conn.BeginTransaction();
            //int limit = 100;
            //for (int i = 0; i <= limit; i++)
            //{
            //    var coin = new CryptoCoin
            //    {
            //        Name = $"Coin {i}",
            //        TwentyFourHourChange = (long)rnd.Next() / 100
            //    };
            //    conn.Insert(coin);
            //}
            Connection.Commit();

            return true;
        }

        public override void OnResignActivation(UIApplication application)
        {
            // Invoked when the application is about to move from active to inactive state.
            // This can occur for certain types of temporary interruptions (such as an incoming phone call or SMS message) 
            // or when the user quits the application and it begins the transition to the background state.
            // Games should use this method to pause the game.
        }

        public override void DidEnterBackground(UIApplication application)
        {
            // Use this method to release shared resources, save user data, invalidate timers and store the application state.
            // If your application supports background exection this method is called instead of WillTerminate when the user quits.
        }

        public override void WillEnterForeground(UIApplication application)
        {
            // Called as part of the transiton from background to active state.
            // Here you can undo many of the changes made on entering the background.
        }

        public override void OnActivated(UIApplication application)
        {
            // Restart any tasks that were paused (or not yet started) while the application was inactive. 
            // If the application was previously in the background, optionally refresh the user interface.
        }

        public override void WillTerminate(UIApplication application)
        {
            // Called when the application is about to terminate. Save data, if needed. See also DidEnterBackground.
        }
    }
}

