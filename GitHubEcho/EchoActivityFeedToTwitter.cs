using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace GitHubEcho
{
    public static class EchoActivityFeedToTwitter
    {
        [FunctionName("EchoActivityFeedToTwitter")]
        public static void Run(
            [TimerTrigger("0 */10 * * * *")]TimerInfo myTimer,
            [Blob("data-storage/github.status.json", System.IO.FileAccess.ReadWrite, Connection = "BlobStorageConnection")] CloudBlockBlob githubStatusBlob,
            TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            // Initialize out API helper class.
            // As part of the initialization process, it gets GitHub credentials from app settings and "Last Modified" date from blob storage
            var githubApi = new GithubApi(githubStatusBlob);

            // Call the GitHub "Received Events" API for the current user and deserialize the JSON that the API sends back
            var githubEventsApiResults = githubApi.GetReceivedEvents();
            var githubEvents = githubEventsApiResults.SelectMany(r => r.Data).ToList();



            // The GitHub "Received Events" API no longer includes follow notices (e.g. Frank is now following Susan)
            // However, it would be nice to have that in our feed... so we have to build it ourselves.
            // We'll keep track of everyone we follow and who they are following.  Then we'll compare the current list of followings
            // to what we're tracking.  If something has changed (i.e. a following is the list that wasn't there before), we can
            // report that as a new event.

            // First, get a list of everyone that I am following
            var me = GetUserFollowees(githubApi.Configuration.Username, githubApi, githubApi.Status.FolloweesEtag);

            var previousFollowees = githubApi.Status.Followees.Select(f => f.Username);
            if (me.Modified == false)
            {
                // My list of followers has not changed since the last run.  So just use the old list from the config
                me.Followees.AddRange(previousFollowees);
            }
            else
            {
                // If my list of followers has changed, it could be because I stopped following someone.
                // In that case, we need to remove them from the config so we don't keep tracking them.
                // (No need to worry about new additions here.  They get handled in the regular processing loop.
                var drops = previousFollowees.Except(me.Followees);
                githubApi.Status.Followees.RemoveAll(f => drops.Contains(f.Username));
            }

            // Now loop through all of the I'm following people and get a list of everyone they are following
            var followees = new List<GithubFollowee>();
            foreach (string username in me.Followees)
            {
                string etag = "";
                var followee = githubApi.Status.Followees.FirstOrDefault(f => f.Username == username);

                if (followee != null)
                {
                    etag = followee.Etag;
                }

                followees.Add(GetUserFollowees(username, githubApi, etag));
            }

            foreach (var followee in followees)
            {
                if (githubApi.Status.Followees.Any(f => f.Username == followee.Username) == false)
                {
                    // This is a new person that I'm following.  Don't tweet about all of their existings follows...
                    // Just add them to the list so we can start watching them.
                    githubApi.Status.Followees.Add(followee);
                }
                else
                {
                    // For everyone that I've been following, compare their new list of followees to the list that we stored
                    // Tweet about any changes in the list.
                    if (followee.Modified == true)
                    {
                        var oldFollowee = githubApi.Status.Followees.First(f => f.Username == followee.Username);
                        var adds = followee.Followees.Except(oldFollowee.Followees).ToList();
                        var drops = oldFollowee.Followees.Except(followee.Followees).ToList();

                        // For any changes that we discover, create a "fake" event in our event list
                        adds.ForEach(i => githubEvents.Add(CreateFakeFollowEvent(followee.Username, "started", i)));
                        drops.ForEach(i => githubEvents.Add(CreateFakeFollowEvent(followee.Username, "stopped", i)));

                        // For any changes that we discover, update the status with the changes
                        oldFollowee.Followees.AddRange(adds);
                        oldFollowee.Followees.RemoveAll(f => drops.Contains(f));
                        oldFollowee.Etag = followee.Etag;
                    }
                }
            }



            // Now that we have followees sorted out, let's continue on with our tweeting...
            // Build a hash table of all of the actors mentioned in the "Received Events" feed
            // (Note that it makes an API call for each actor to get their details
            var actors = GithubActor.GetDistinct(githubApi, githubEvents);

            // Loop through all of the items in the data we got from the API.  If there's a new item we havent seen before,
            // create a new tweet for it.
            var tweets = new List<Tweet>();
            foreach (var item in githubEvents)
            {
                if (item.created_at.Value > githubApi.Status.EventsLastModified)
                {
                    var actor = actors[item.actor.login.Value];
                    var tweet = Tweet.Create(actor, item);

                    tweets.Add(tweet);
                }
            }



            // Now it's time to send out our tweets!
            var twitterConfig = TwitterConfiguration.Retrieve();
            var twitterApi = new TwitterApi(twitterConfig);

            var tweetResults = new List<dynamic>();
            foreach (var tweet in tweets)
            {
                string resultJson = twitterApi.SendTweet(tweet.CreateBody());
                tweetResults.Add(JsonConvert.DeserializeObject<dynamic>(resultJson));
            }



            // Save the updated status back to blob storage
            githubApi.Status.StoreInBlob(githubStatusBlob, githubEventsApiResults, me);

            // Report back our results
            log.Info($"C# Timer trigger function completed at: {DateTime.Now}  - attempted {tweets.Count} tweets");
            foreach (var tweetResult in tweetResults)
            {
                if (tweetResult.errors == null)
                {
                    log.Info($"--- SUCCESS! {tweetResult.text.Value}");
                }
                else
                {
                    var error = tweetResult.errors.First;
                    log.Info($"--- FAILED! Code {error.code.Value}: {error.message.Value}");
                }
            }
        }

        private static GithubFollowee GetUserFollowees(string username, GithubApi githubApi, string etag)
        {
            var output = new GithubFollowee();
            output.Username = username;

            var followeesApiResults = githubApi.GetUserFollowees(username, etag);
            if (followeesApiResults.Any() == true) {
                output.Etag = followeesApiResults.First().Etag;
                output.Modified = followeesApiResults.First().Modified;

                var followees = followeesApiResults.SelectMany(r => r.Data);
                output.Followees = followees.Select(u => (string)u.login.Value).Distinct().ToList();
            }

            return output;
        }

        private static dynamic CreateFakeFollowEvent(string actorUsername, string actionName, string followeeUsername)
        {
            var fakeEvent = new
            {
                id = "-1",
                type = "FollowEvent",
                actor = new
                {
                    id = -1,
                    login = actorUsername,
                    display_login = actorUsername
                },
                payload = new
                {
                    action = actionName,
                    target = new
                    {
                        login = followeeUsername,
                        html_url = $"https://github.com/{followeeUsername}"
                    }
                },
                repo = new {
                    name = "FAKE EVENT"
                },
                created_at = DateTime.UtcNow.AddDays(1)     // Set this event in the future so it will always be seen as a "new" event
            };

            return Newtonsoft.Json.Linq.JObject.FromObject(fakeEvent);
        }
    }
}
