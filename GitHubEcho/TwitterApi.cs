using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace GitHubEcho
{
    // this code was inspired by:  https://blog.dantup.com/2016/07/simplest-csharp-code-to-post-a-tweet-using-oauth/
    public class TwitterApi
    {
        private TwitterApi() { }

        public TwitterApi(TwitterConfiguration twitterConfig)
        {
            this.twitterConfig = twitterConfig;

            string signature = $"{twitterConfig.ConsumerSecret}&{twitterConfig.AccessTokenSecret }";
            signatureHasher = new HMACSHA1(new ASCIIEncoding().GetBytes(signature));
        }

        public string SendTweet(string message)
        {
            string twitterApiUrl = "https://api.twitter.com/1.1/statuses/update.json";

            int timestamp = (int)(DateTime.UtcNow - this.epochUtc).TotalSeconds;

            var data = new Dictionary<string, string>();
            data.Add("status", message);
            data.Add("oauth_consumer_key", twitterConfig.ConsumerKey);
            data.Add("oauth_signature_method", "HMAC-SHA1");
            data.Add("oauth_timestamp", timestamp.ToString());
            data.Add("oauth_nonce", "a");    // Required, but Twitter doesn't appear to use it, so "a" will do.
            data.Add("oauth_token", twitterConfig.AccessToken);
            data.Add("oauth_version", "1.0");
            data.Add("oauth_signature", GenerateSignature(twitterApiUrl, data));    // Generate the OAuth signature and add it to our payload.

            // Build the OAuth HTTP Header from the data.
            string oauthHeader = GenerateOAuthHeader(data);

            // Build the form data (exclude OAuth stuff that's already in the header).
            var formData = new FormUrlEncodedContent(data.Where(kvp => !kvp.Key.StartsWith("oauth_")));

            return SendRequest(twitterApiUrl, oauthHeader, formData);
        }

        private string GenerateSignature(string url, Dictionary<string, string> data)
        {
            string sigString = string.Join(
                "&",
                data
                    .Union(data)
                    .Select(kvp => string.Format("{0}={1}", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value)))
                    .OrderBy(s => s)
            );

            string fullSignature = $"POST&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(sigString)}";

            return Convert.ToBase64String(signatureHasher.ComputeHash(new ASCIIEncoding().GetBytes(fullSignature)));
        }

        private string GenerateOAuthHeader(Dictionary<string, string> data)
        {
            return "OAuth " + string.Join(
                ", ",
                data
                    .Where(kvp => kvp.Key.StartsWith("oauth_"))
                    .Select(kvp => string.Format("{0}=\"{1}\"", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value)))
                    .OrderBy(s => s)
            );
        }

        private string SendRequest(string url, string oauthHeader, FormUrlEncodedContent formData)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", oauthHeader);

                var httpResponse = httpClient.PostAsync(url, formData).Result;
                string responseBody = httpResponse.Content.ReadAsStringAsync().Result;

                return responseBody;
            }
        }

        private readonly DateTime epochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private HMACSHA1 signatureHasher = null;
        private TwitterConfiguration twitterConfig = null;
    }
}
