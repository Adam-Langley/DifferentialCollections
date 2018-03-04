# DifferentialCollections
A Xamarin iOS library for keeping an UICollectionView in sync with a dynamic SQL data model.

> Install-Package Dabble.DifferentialCollections

With a single line of code, initiate the synchronization between a UICollectionView and a highly dynamic data source of 
thousands of records, avoiding any complex, application-specific messaging - while supporting request pagination, 
animations, and persistent cell-selection.

```csharp
void TxtSearch_TextChanged(object sender, UISearchBarTextChangedEventArgs e)
{
    // Tell the Differential Collection to requery the SQL data-source, calculate and push all the changes
    // to the collection view, in a non-destructive manner.
    _cryptoCoinDataSource.RequeryWithCriteria(x =>
    {
        x.FilterString = txtSearch.Text;
    });
}
```

The result (see Milestone2 project in the Tutorials folder), is a collection-view update that
1. Removes rows that have disappeared from the result-set.
2. Adds rows that have appeared in the result-set.
3. Moves the positions of rows that now fall in a different location in the result-set.
4. Reloads rows that have changed in the database since they were last displayed.
5. Retains selection as rows move about.
6. Avoids any 'janky' visual experience!

![Final Demo App](/Tutorial/Images/UICollectionViewFinalDemo.gif)

# Demos

| Video                                                     | Explanation |
| --------------------------------------------------------- | ----------------------------------------------------------------- |
| ![Deletion Demo](/Tutorial/Images/Demo_RandomDelete.gif)  | Deletion of a random row from SQLite table. <br><pre>var count = _cryptoCoinDataSource.Count; <br />var random = _cryptoCoinDataSource.GetPage(Random.Next(count - 1), 1).FirstOrDefault();<br />if (null != random)<br>{ <br>  AppDelegate.Connection.BeginTransaction(); <br>  AppDelegate.Connection.Delete(random);<br>  AppDelegate.Connection.Commit();<br>  // This is the magic line, request re-sync of the view<br>  _cryptoCoinDataSource.Requery();<br>}</pre> |
| ![Insertion Demo](/Tutorial/Images/Demo_RandomInsert.gif)  | Insertion of a random row to SQLite table<br><pre>var newName = "Bitcoin " + RandomString(8);<br>var newCoin = new CryptoCoin<br>{<br>  Id = newName,<br>  Name = newName,<br>  PriceUSD = Random.NextDouble() * 2,<br>  TwentyFourHourChange = Random.NextDouble(),<br>};<br>AppDelegate.Connection.BeginTransaction();<br>AppDelegate.Connection.InsertOrReplace(newCoin);<br>AppDelegate.Connection.Commit();<br>// This is the magic line, request re-sync of the view<br>_cryptoCoinDataSource.Requery();</pre>  |
| ![Move Demo](/Tutorial/Images/Demo_RandomMove.gif)  | Modification of 2 random SQLite table rows to demonstrate them exchanging positions |
