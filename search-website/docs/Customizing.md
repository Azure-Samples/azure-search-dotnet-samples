# Customizing this template

After deploying this sample, I'd encourage you to try it out with your own search index.

Up until now, you've been working with an existing index, but creating and loading an index with your own data is straightforward. You can create an index in the [Azure portal](https://docs.microsoft.com/azure/search/search-import-data-portal), use the [REST APIs](https://docs.microsoft.com/rest/api/searchservice/create-index), or use any of the Azure SDKs such as the new [Azure SDK for Javascript/TypeScript](https://github.com/Azure/azure-sdk-for-js/tree/master/sdk/search/search-documents/) that we used in this sample. Please see the Azure Cognitive Search [documentation](https://docs.microsoft.com/azure/search/search-get-started-portal) for more information on how to get started.

There are two main changes you'd need to make to use your own index:

## 1. Edit application settings in the portal

Navigate to the Azure portal -> find your Azure Static Web App -> select configuration -> edit the application settings.

![Azure Static Web Apps Configuration Screenshot](../images/config.png)

## 2. Update Result and Detail components

Much of the UI won't require customization, however, if you integrate a new index with this template, you'll likely need to update the `Results` component and the `Details` component to reflect the fields in your index.

### Results

To edit the results view, first navigate to `src/components/Results/Result/Result.js`. This file represents a single result on the UI. In contrast, `src/components/Results/Results.js` controls the entire results view.

The JSX for the basic result looks like this:

```javascript
<div className="card result">
    <a href={`/details/${props.document.id}`}>
        <img className="card-img-top" src={props.document.image_url} alt="book cover"></img>
        <div className="card-body">
            <h6 className="title-style">{props.document.original_title}</h6>
        </div>
    </a>
</div>
```

The property `props.document` will contain all of the fields returned by your query. The properties `id`, `image_url`, and `original_title` all correspond to fields in the good-books index and should be updated to reflect the fields in your own search index.

Beyond that, you can customize this component as much as you want depending on the needs of your application.

### Details

The other component that needs to be customized is the Details view. This file can be found at `src/pages/Details/details.js`.

The component is currently designed as a card with multiple tabs to make it easy to present additional information on the screen.

To edit the main view, simply edit the JSX used to define the body variable. All fields within your search index will be a part of the `document` variable. These should be edited to match the properties in your index.

```javascript
  var body;
  if (isLoading) {
    body = (<CircularProgress />);
  } else {
    body = (<div class="card-body">
      <h5 class="card-title">{document.original_title}</h5>
      <img style={imageStyle} src={document.image_url} alt="Book cover"></img>
      <p class="card-text">{document.authors?.join('; ')} - {document.original_publication_year}</p>
      <p class="card-text">ISBN {document.isbn}</p>
      <Rating name="half-rating-read" value={parseInt(document.average_rating)} precision={0.1} readOnly></Rating>
      <p class="card-text">{document.ratings_count} Ratings</p>
    </div>)
  }
```

The raw data tab simply prints out the entire `document` variable and doesn't need to be edited unless you'd like to change it.

You can also add additional tabs to show more content to your users.

```javascript
<li class="nav-item">
    <button class="nav-link active" onClick={() => setTab(2)}>New Tab</button>
</li>
```
