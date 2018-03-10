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
| ![Move Demo](/Tutorial/Images/Demo_RandomMove.gif)  | Modification of 2 random SQLite table rows to demonstrate them exchanging positions<br><pre>// pick 2 random rows, and swap their 24 hour change values<br>var count = _cryptoCoinDataSource.Count;<br>if (count < 2)<br>    return;<br>var randomIndexA = Random.Next(count - 1);<br>var randomIndexB = 0;<br>do<br>{<br>    randomIndexB = Random.Next(count - 1);<br>} while (randomIndexB == randomIndexA);<br><br>var randomA = _cryptoCoinDataSource.GetPage(randomIndexA, 1).FirstOrDefault();<br>var randomB = _cryptoCoinDataSource.GetPage(randomIndexB, 1).FirstOrDefault();<br>if (null != randomA && null != randomB)<br>{<br>    AppDelegate.Connection.BeginTransaction();<br><br>    var temp = randomA.TwentyFourHourChange;<br>    randomA.TwentyFourHourChange = randomB.TwentyFourHourChange;<br>    randomB.TwentyFourHourChange = temp;<br><br>    // bump version field so UI can locate changed rows.<br>    randomA.Version++;<br>    randomB.Version++;<br><br>    // commit to database<br>    AppDelegate.Connection.UpdateAll(new []{<br>        randomA, randomB<br>    });<br><br>    AppDelegate.Connection.Commit();<br><br>    // This is the magic line, request re-sync of the view<br>    _cryptoCoinDataSource.Requery();<br>}</pre> |

# How do I use it?

Follow these steps - the example class names here are referencing the 'Milestone 2' example project.

1. Derive `DifferentialDataModel<TPrimaryKey, TTableModel, TTableCriteria>`.<br>
_This class executes the appropriate queries to supply paginated results, and row meta-data to the `DifferentialCollectionViewSource`._ <br>
_`CryptoCoinDataSource` in the example._
    * Implement `GetCount` to count the number of table records matching your criteria.
    * Implement `GetRowMeta` to return the row position and version of a set of records matching the supplied list of primary keys.
    * Implement `GetIds` to return a list of primary keys for a page of records.
    * Implement `GetPage` to return a page of fully-populate records that will be passed into your cells for display.

2. Implement `TTableCriteria`.<br>
_This class encapsulates all query variation that you will want to perform, if the user will 'search' for example, this class needs needs a property to hold the search string._<br>
_`CryptoCoinCriteria` in the example._

3. Derive `DifferentialCollectionViewSource<TPrimaryKey, TTableModel, TTableCriteria>`.<br>
_This class receives the message that a model object has been pulled from the database, and lets you bind it into the widgets in a cell via `OnDataContextLoaded`_<br>
_`CryptoCoinCollectionViewSource` in the example._

4. Wire it up in your `ViewController`.<br>
    * Instantiate your `DifferentialCollectionViewDataSource`
    * Instantiate your `DifferentialCollectionViewSource`
    * Assign the `DifferentialCollectionViewDataSource` to the `DataModel` property of your `DifferentialCollectionViewSource`
    * Assign the `DifferentialCollectionViewSource` to the `Source` property of your `UICollectionView`
