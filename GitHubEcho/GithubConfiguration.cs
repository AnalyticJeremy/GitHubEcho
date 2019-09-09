using System;
using System.Collections.Generic;

namespace GitHubEcho
{
    public class GithubConfiguration
    {
        private GithubConfiguration() { }

        public string Username { get; private set; }
        public string AuthenticationToken { get; private set; }

        public static GithubConfiguration Retrieve()
        {
            var output = new GithubConfiguration();

            // get values from the Application Settings in Azure
            output.Username = Environment.GetEnvironmentVariable("GithubUsername");
            output.AuthenticationToken = Environment.GetEnvironmentVariable("GithubAccessToken");

            return output;
        }
    }
}
