# DifferentialCollections
A Xamarin iOS library for keeping an UICollectionView in sync with a dynamic SQL data model.

>Install-Package Dabble.DifferentialCollections -Version 0.0.1

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
