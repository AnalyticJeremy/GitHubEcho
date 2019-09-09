using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;

namespace GitHubEcho
{
    public class GithubActor
    {
        private GithubActor() { }

        public string Login { get; private set; }
        public string Name { get; private set; }

        public string DisplayName
        {
            get
            {
                string output = this.Login;

                if (string.IsNullOrWhiteSpace(this.Name) == false)
                {
                    output += $" ({this.Name})";
                }

                return output;
            }
        }

        public override string ToString()
        {
            return $"actor {this.DisplayName}";
        }

        public static Dictionary<string, GithubActor> GetDistinct(GithubApi githubApi, List<dynamic> githubEvents)
        {
            var output = new Dictionary<string, GithubActor>();
            var logins = githubEvents.Select(e => e.actor.login.Value.ToString()).Distinct().ToList();

            foreach (string login in logins)
            {
                var apiResult = githubApi.GetUser(login);
                var actor = new GithubActor() { Login = login, Name = apiResult.Data.name.Value };
                output.Add(login, actor);
            }

            return output;
        }
    }
}
