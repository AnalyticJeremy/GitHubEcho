using System;
using System.Security.Cryptography;
using System.Text;

namespace GitHubEcho
{
    public class TwitterConfiguration
    {
        private TwitterConfiguration() { }

        public string ConsumerKey { get; private set; }
        public string ConsumerSecret { get; private set; }
        public string AccessToken { get; private set; }
        public string AccessTokenSecret { get; private set; }

        public static TwitterConfiguration Retrieve()
        {
            var output = new TwitterConfiguration();

            // get values from the Application Settings in Azure
            output.ConsumerKey = Environment.GetEnvironmentVariable("TwitterConsumerKey");
            output.ConsumerSecret = Environment.GetEnvironmentVariable("TwitterConsumerSecret");
            output.AccessToken = Environment.GetEnvironmentVariable("TwitterAccessToken");
            output.AccessTokenSecret = Environment.GetEnvironmentVariable("TwitterAccessTokenSecret");

            return output;
        }
    }
}
