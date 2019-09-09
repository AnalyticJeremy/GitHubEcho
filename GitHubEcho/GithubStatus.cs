using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace GitHubEcho
{
    public class GithubStatus
    {
        private GithubStatus() { }

        public DateTime EventsLastModified { get; private set; }
        public string FolloweesEtag { get; private set; }
        public List<GithubFollowee> Followees { get; private set; }

        public dynamic Raw { get; private set; }

        public void StoreInBlob(CloudBlockBlob githubStatusBlob, List<GithubApiResult<List<dynamic>>> githubEventsApiResults, GithubFollowee meFollowee)
        {
            string newEventsLastModified = "";
            string previousEventsLastModified = this.Raw.events_last_modified.Value;

            if (githubEventsApiResults != null && githubEventsApiResults.Any() == true)
            {
                newEventsLastModified = githubEventsApiResults.First().LastModifiedDate;
            }

            if (string.IsNullOrWhiteSpace(newEventsLastModified) == true)
            {
                newEventsLastModified = previousEventsLastModified;
            }

            var newStatus = new
            {
                events_last_modified = newEventsLastModified,
                followees_etag = meFollowee.Etag,
                followees = this.Followees
            };


            string statusJson = JsonConvert.SerializeObject(newStatus, Formatting.Indented);
            githubStatusBlob.Properties.ContentType = "application/json";
            githubStatusBlob.UploadText(statusJson);
        }

        public static GithubStatus Retrieve(CloudBlockBlob githubStatusBlob)
        {
            var output = new GithubStatus();

            // get the "Last Modified" date value from the config file stored in blob storage
            string json = "";
            if (githubStatusBlob.Exists() == true)
            {
                json = githubStatusBlob.DownloadText();
            }
            else
            {
                json = "{ events_last_modified: \"" + DateTime.UtcNow.AddDays(-3).ToString("ddd, dd MMM yyyy hh:mm:ss 'GMT'") + "\", followees_etag: null }";
            }

            output.Raw = JsonConvert.DeserializeObject<dynamic>(json);
            output.EventsLastModified = DateTime.Parse(output.Raw.events_last_modified.Value);
            output.FolloweesEtag = output.Raw.followees_etag.Value;

            if (output.Raw.followees == null)
            {
                output.Followees = new List<GithubFollowee>();
            }
            else
            {
                output.Followees = output.Raw.followees.ToObject<List<GithubFollowee>>();
            }

            return output;
        }
    }
}
