using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace GitHubEcho
{
    public class GithubApi
    {
        private GithubApi() { }

        public GithubApi(CloudBlockBlob githubStatusBlob)
        {
            this.Configuration = GithubConfiguration.Retrieve();
            this.Status = GithubStatus.Retrieve(githubStatusBlob);
        }

        public GithubConfiguration Configuration { get; private set; }
        public GithubStatus Status { get; private set; }

        public List<GithubApiResult<List<dynamic>>> GetReceivedEvents()
        {
            string x = $"https://api.github.com/notifications";
            var z = GetDataFromGithubApi<List<dynamic>>(x, null);

            string apiUrl = $"https://api.github.com/users/{this.Configuration.Username}/received_events";
            return GetPagedDataFromGithubApi(apiUrl, ConditionalRequestHeader.CreateLastModified(this.Status.EventsLastModified));
        }

        public List<GithubApiResult<List<dynamic>>> GetNotifications()
        {
            // THIS CODE IS NOT YET USED AND NOT YET TESTED!
            string apiUrl = $"https://api.github.com/notifications";
            return GetPagedDataFromGithubApi(apiUrl, ConditionalRequestHeader.CreateLastModified(this.Status.EventsLastModified));
        }

        public List<GithubApiResult<List<dynamic>>> GetUserFollowees(string username, string etag)
        {
            string apiUrl = $"https://api.github.com/users/{username}/following";
            return GetPagedDataFromGithubApi(apiUrl, ConditionalRequestHeader.CreateEtag(etag));
        }

        public GithubApiResult<dynamic> GetUser(string username)
        {
            string apiUrl = $"https://api.github.com/users/{username}";
            return GetDataFromGithubApi<dynamic>(apiUrl, null);
        }

        private List<GithubApiResult<List<dynamic>>> GetPagedDataFromGithubApi(string apiUrl, ConditionalRequestHeader conditionalRequestHeader)
        {
            int pageNumber = 1;
            var output = new List<GithubApiResult<List<dynamic>>>();
            bool doesPageHaveData = true;

            while (doesPageHaveData == true)
            {
                string callUrl = $"{apiUrl}?page={pageNumber++}";
                var apiResult = GetDataFromGithubApi<List<dynamic>>(callUrl, conditionalRequestHeader);

                if (apiResult.Data.Count > 0)
                {
                    output.Add(apiResult);
                }
                else
                {
                    doesPageHaveData = false;
                }
            }

            return output;
        }

        private GithubApiResult<T> GetDataFromGithubApi<T>(string apiUrl, ConditionalRequestHeader conditionalRequestHeader)
        {
            var output = new GithubApiResult<T>();

            using (var webClient = new ApiWebClient())
            {
                webClient.Headers.Add("Authorization", $"token {this.Configuration.AuthenticationToken}");

                if (conditionalRequestHeader != null)
                {
                    if (conditionalRequestHeader.Key == LastModifiedKey)
                    {
                        webClient.IfModifiedSince = DateTime.Parse(conditionalRequestHeader.Value);
                    }
                    else if (string.IsNullOrWhiteSpace(conditionalRequestHeader.Value) == false)
                    {
                        webClient.Headers.Add(conditionalRequestHeader.Key, conditionalRequestHeader.Value);
                    }
                }

                try
                {
                    output.Json = webClient.DownloadString(apiUrl);
                    output.LastModifiedDate = webClient.ResponseHeaders.Get(LastModifiedKey);
                    output.Etag = webClient.ResponseHeaders.Get("ETag");
                    output.Modified = true;

                    foreach (string key in webClient.ResponseHeaders.Keys)
                    {
                        output.ResponseHeaders.Add(key, webClient.ResponseHeaders.Get(key));
                    }
                }
                catch (WebException webException)
                {
                    if (((HttpWebResponse)webException.Response).StatusCode == HttpStatusCode.NotModified)
                    {
                        output.Modified = false;
                        if (typeof(T).GetInterface("IEnumerable") != null)
                        {
                            output.Json = "[ ]";
                        }
                        else
                        {
                            output.Json = "{ }";
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            output.Data = JsonConvert.DeserializeObject<T>(output.Json);

            return output;
        }

        private const string LastModifiedKey = "Last-Modified";

        private class ConditionalRequestHeader
        {
            private ConditionalRequestHeader() { }

            public string Key { get; private set; }
            public string Value { get; private set; }

            public static ConditionalRequestHeader CreateLastModified(DateTime lastModified)
            {
                return new ConditionalRequestHeader() { Key = LastModifiedKey, Value = lastModified.ToString() };
            }

            public static ConditionalRequestHeader CreateEtag(string etag)
            {
                return new ConditionalRequestHeader() { Key = "If-None-Match", Value = etag };
            }
        }
    }

    public class GithubApiResult<T>
    {
        public GithubApiResult()
        {
            this.ResponseHeaders = new Dictionary<string, string>();
        }

        public string Json { get; set; }
        public string LastModifiedDate { get; set; }
        public string Etag { get; set; }
        public bool Modified { get; set; }
        public Dictionary<string, string> ResponseHeaders { get; private set; }
        public T Data { get; set; }
    }
}
