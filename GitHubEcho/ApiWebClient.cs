using System;
using System.Net;

namespace GitHubEcho
{
    // A modified version of the "WebClient" class that allows you to set the "If-Modified-Since"
    // header in an HTTP request.
    // from https://stackoverflow.com/questions/29464720/cant-i-set-if-modified-since-on-a-webclient
    public class ApiWebClient : WebClient
    {
        public DateTime? IfModifiedSince { get; set; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var webRequest = base.GetWebRequest(address);
            var httpWebRequest = webRequest as HttpWebRequest;

            if (httpWebRequest != null)
            {
                // GitHub API requires User Agent to be set (use the OAuth application name)
                httpWebRequest.UserAgent = "Activity Feed Echo (Azure Function)";

                if (IfModifiedSince != null)
                {
                    httpWebRequest.IfModifiedSince = IfModifiedSince.Value;
                    IfModifiedSince = null;
                }
            }

            return webRequest;
        }
    }
}
