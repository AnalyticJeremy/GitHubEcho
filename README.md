# GitHubEcho

I :heart: GitHub.  I enjoy the social aspect of the site and seeing what my
friends and colleagues are working on.  The activity feed that GitHub shows
when I log in to the site is especially helpful.  However, I don't log in to
GitHub's main page that regularly.  I would like to see that information in a
place that I check more frequently.

I check Twitter frequently (perhaps *too* frequently).  If I could somehow view
GitHub activity events in my Twitter timeline, it would be much easier for me
to consume that information.

I solved this problem with **GitHubEcho**.  It uses an
[Azure Function](https://azure.microsoft.com/services/functions) written in C#
to regularly poll the [GitHub API](https://developer.github.com/v3/) and look
for new activity events.  Those events are then tweeted by a private Twitter
account that I follow.  These tweets are sent using the
[Twitter API](https://developer.twitter.com/).

## Setup
Here is a general overview of how you can re-create my setup.

1. Create a new Twitter account.  You will use this account to tweet your
GitHub activities.  Your main Twitter account should follow this new account.
You can set the new Twitter account to private if you don't want to share your
GitHub activity feed with the whole world.

2. In Azure, create a new Function App.  This is will host your Azure Function
code.

3. In the Azure Storage Account associated with your new Function App, create
a new blob container.  We will use this to store status information.

4. Open the Visual Studio solution from this repo.  You will need to add a
"local.settings.json" file that will hold API credential information for your
GitHub and new Twitter accounts.  The file should contain the following
information:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "AzureWebJobsDashboard": "UseDevelopmentStorage=true",
    "GithubUsername": "Your GitHub Username",
    "GithubAccessToken": "Your GitHub API Access Token",
    "TwitterConsumerKey": "Your Twitter API OAuth Consumer Key",
    "TwitterConsumerSecret": "Your Twitter API OAuth Consumer Secret",
    "TwitterAccessToken": "Your Twitter API OAuth Access Token",
    "TwitterAccessTokenSecret": "Your Twitter API OAuth Access Token Secret",
    "BlobStorageConnection": "DefaultEndpointsProtocol=https;AccountName=YOURSTORAGEACCOUNTNAME;AccountKey=YOURSTORAGEACCOUNT KEY;EndpointSuffix=core.windows.net"
  }
}
```

5. Test the solution locally using the settings above to make sure everything is
working as expected.

6. Deploy the code to your Azure Function App

7. Make sure the values from your "local.settings.json" file get copied as
Application Settings in the Azure Function app.