using System;

namespace GitHubEcho
{    public class Tweet
    {
        private Tweet() { }

        public GithubActor Actor { get; private set; }
        public string Verb { get; private set; }
        public string Object { get; private set; }
        public string Url { get; private set; }
        public string EventType { get; private set; }
        public string ParsingErrorMessage { get; private set; }

        #region CreateBody
        public string CreateBody()
        {
            string output = "";
            bool hasUrl = !string.IsNullOrWhiteSpace(this.Url);
            bool hasError = !string.IsNullOrWhiteSpace(this.ParsingErrorMessage);

            int maxTweetLength = 280;

            if (hasUrl == true)
            {
                maxTweetLength -= 30;
            }

            if (hasError == true)
            {
                output = CreateErrorBody(maxTweetLength);
            }
            else
            {
                output = CreateNormalBody(maxTweetLength);
            }

            if (hasUrl == true)
            {
                output += "\n" + this.Url;
            }

            return output;
        }

        private string CreateErrorBody(int maxTweetLength)
        {
            string output = $"Error parsing {this.EventType}: {this.ParsingErrorMessage}";
            return ConformStringToLength(output, maxTweetLength);
        }

        private string CreateNormalBody(int maxTweetLength)
        {
            string actor = this.Actor.DisplayName;
            string verb = this.Verb;
            string obj = this.Object;

            // a local function for forming the body
            string AssembleBody()
            {
                return $"{actor} {verb} {obj}";
            }

            string output = AssembleBody();

            // Make a series of edits until we get the tweet size below the character limit
            if (output.Length > maxTweetLength)
            {
                actor = this.Actor.Login;
                output = AssembleBody();

                if (output.Length > maxTweetLength)
                {
                    obj = obj.Replace("repository", "repo");
                    output = AssembleBody();
                }
            }

            return ConformStringToLength(output, maxTweetLength);
        }

        private string ConformStringToLength(string input, int maxLength)
        {
            if (input.Length > maxLength)
            {
                bool hasQuoteEnding = input.EndsWith("\"");
                maxLength -= 3;

                if (hasQuoteEnding == true)
                {
                    maxLength--;
                }

                input = input.Substring(0, maxLength);
                input += "...";

                if (hasQuoteEnding == true)
                {
                    input += "\"";
                }
            }

            return input;
        }
        #endregion

        public static Tweet Create(GithubActor actor, dynamic item)
        {
            var output = new Tweet();

            output.Actor = actor;
            output.EventType = item.type.Value;

            try
            {
                output.Url = "https://github.com/" + item.repo.name.Value;
                ConvertItemToTweet(ref output, item);
            }
            catch (Exception exception)
            {
                output.ParsingErrorMessage = exception.Message;
            }

            return output;
        }

        private static void ConvertItemToTweet(ref Tweet tweet, dynamic item)
        {
            switch (tweet.EventType)
            {
                case "CreateEvent":
                    tweet.Verb = "created";
                    tweet.Object = item.payload["ref_type"].Value + " \"" + (item.payload["ref"].Value ?? item.repo.name.Value) + "\"";
                    tweet.Url = "https://github.com/" + item.repo.name.Value;
                    break;

                case "DeleteEvent":
                    tweet.Verb = "created";
                    tweet.Object = item.payload["ref_type"].Value + " \"" + (item.payload["ref"].Value ?? item.repo.name.Value) + "\"";
                    break;

                case "DownloadEvent":       //Events of this type are no longer delivered
                    tweet.Verb = "created";
                    tweet.Object = "download \"" + item.payload.download.name.Value + "\"";
                    tweet.Url = item.payload.download.html_url.Value;
                    break;

                case "FollowEvent":         //Events of this type are no longer delivered (but we are adding fake ones to the stream... so we still have to handle them)
                    tweet.Verb = item.payload.action.Value + " following";
                    tweet.Object = item.payload.target.login.Value;
                    tweet.Url = item.payload.target.html_url.Value;
                    break;

                case "ForkEvent":
                    tweet.Verb = "forked";
                    tweet.Object = $"repository \"{item.repo.name.Value}\"";
                    tweet.Url = item.payload.forkee.html_url.Value;
                    break;

                case "GollumEvent":
                    int pageCount = item.payload.pages.Count;
                    tweet.Verb = "created";
                    tweet.Object = $"{pageCount} Wiki page{(pageCount == 1 ? "" : "s")}";
                    tweet.Url = item.payload.pages[0].html_url.Value;
                    break;

                case "InstallationEvent":
                    tweet.Verb = item.payload.action.Value;
                    tweet.Object = "a GitHub App";
                    tweet.Url = item.payload.installation.html_url.Value;
                    break;

                case "InstallationRepositoriesEvent":
                    tweet.Verb = item.payload.action.Value;
                    tweet.Object = "a repository from an installation";
                    tweet.Url = item.payload.installation.html_url.Value;
                    break;

                case "IssueCommentEvent":
                    tweet.Verb = item.payload.action.Value;
                    tweet.Object = $"a comment in issue \"{item.payload.issue.title.Value}\"";
                    tweet.Url = item.payload.issue.html_url.Value;
                    break;

                case "IssuesEvent":
                    tweet.Verb = item.payload.action.Value;
                    tweet.Object = $"issue \"{item.payload.issue.title.Value}\"";

                    if (item.payload.assignee != null && string.IsNullOrWhiteSpace(item.payload.assignee.login.Value) == false)
                    {
                        tweet.Object += " user " + item.payload.assignee.login.Value;
                    }

                    if (item.payload.label != null && string.IsNullOrWhiteSpace(item.payload.label.name.Value) == false)
                    {
                        tweet.Object += $" as \"{item.payload.label.name.Value}\"";
                    }

                    tweet.Url = item.payload.issue.html_url.Value;
                    break;

                case "LabelEvent":
                    tweet.Verb = item.payload.action.Value;
                    tweet.Object = $"label \"{item.payload.label.name.Value}\" for repository \"{item.repo.name.Value}\"";
                    tweet.Url = "https://github.com/" + item.repo.name.Value;
                    break;

                case "MemberEvent":
                    tweet.Verb = item.payload.action.Value;
                    tweet.Object = $"collaborator \"{item.payload.member.login.Value}\" in repository \"{item.repo.name.Value}\"";
                    tweet.Url = "https://github.com/" + item.repo.name.Value;
                    break;

                case "PublicEvent":
                    tweet.Verb = "went public";
                    tweet.Object = $"with repository \"{item.repo.name.Value}\"";
                    tweet.Url = "https://github.com/" + item.repo.name.Value;
                    break;

                case "PullRequestEvent":
                    tweet.Verb = item.payload.action.Value;
                    tweet.Object = $"pull request \"{item.payload.pull_request.title.Value}\" for repository \"{item.repo.name.Value}\"";
                    tweet.Url = item.payload.pull_request.html_url.Value;
                    break;

                case "PullRequestReviewCommentEvent":
                    tweet.Verb = item.payload.action.Value;
                    tweet.Object = $"a review comment for pull request \"{item.payload.pull_request.title.Value}\" for repository \"{item.repo.name.Value}\"";
                    tweet.Url = item.payload.review_comment_url.Value;
                    break;

                case "PushEvent":
                    tweet.Verb = "pushed";
                    tweet.Object = $"to repository \"{item.repo.name.Value}\"";
                    tweet.Url = "https://github.com/" + item.repo.name.Value;
                    break;

                case "ReleaseEvent":
                    tweet.Verb = "released";
                    tweet.Object = $"\"{item.payload.release.name.Value}\"";
                    tweet.Url = item.payload.release.html_url.Value;
                    break;

                case "WatchEvent":
                    tweet.Verb = "starred";
                    tweet.Object = $"repository \"{item.repo.name.Value}\"";
                    tweet.Url = "https://github.com/" + item.repo.name.Value;
                    break;

                case "CheckRunEvent":
                case "CheckSuiteEvent":
                case "CommitCommentEvent":
                case "DeploymentEvent":
                case "DeploymentStatusEvent":
                case "ForkApplyEvent":         //Events of this type are no longer delivered
                case "GistEvent":              //Events of this type are no longer delivered
                case "MarketplacePurchaseEvent":
                case "MembershipEvent":
                case "MilestoneEvent":
                case "OrganizationEvent":
                case "OrgBlockEvent":
                case "PageBuildEvent":
                case "ProjectCardEvent":
                case "ProjectColumnEvent":
                case "ProjectEvent":
                case "PullRequestReviewEvent":
                case "RepositoryEvent":
                case "RepositoryVulnerabilityAlertEvent":
                case "StatusEvent":
                case "TeamEvent":
                case "TeamAddEvent":
                default:
                    string verb = item.type.Value;

                    if (verb.EndsWith("Event") == true)
                    {
                        verb = verb.Substring(0, verb.Length - 5);
                        verb = verb.ToLower();
                    }

                    if (item.payload.action != null && string.IsNullOrWhiteSpace(item.payload.action.Value) == false)
                    {
                        verb = item.payload.action.Value + " " + verb;
                    }

                    tweet.Verb = verb;
                    tweet.Object = "(this is an unknown event)";
                    tweet.Url = "https://github.com/" + item.repo.name.Value;
                    break;
            }
        }
    }
}
