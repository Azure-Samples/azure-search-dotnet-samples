# Authentication

In many scenarios, you will want to add authentication to your application to limit who can use it. Fortunately, Azure Static Web Apps makes it really easy to [add authentication and authorization](https://docs.microsoft.com/azure/static-web-apps/authentication-authorization) to the application.

This template is already configured to support authentication. However, it's worth noting that the application needs to be deployed to take advantage of this functionality.

The only file you need to edit is `public/routes.json` which controls the routes for the Static Web App.

By default, the routes don't require authentication.

```json
{
    "route": "/search"
},
{
    "route": "/api/search"
},
...
```

To require users to be authenticated, simply specified the allowedRoles parameter as shown below:

```json
{
    "route": "/search",
    "allowedRoles": ["authenticated"]
},
{
    "route": "/api/search",
    "allowedRoles": ["authenticated"]
},
...
```

Be sure to secure both the page and the api to keep your data secure. You can learn more about securing routes [here](https://docs.microsoft.com/azure/static-web-apps/routes#securing-routes-with-roles).
