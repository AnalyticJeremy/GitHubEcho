using System;
using System.Collections.Generic;

namespace GitHubEcho
{
    public class GithubFollowee
    {
        public GithubFollowee()
        {
            this.Followees = new List<string>();
        }

        public string Username { get; set; }
        public string Etag { get; set; }
        public bool Modified { get; set; }
        public List<string> Followees { get; set; }

        public override string ToString()
        {
            return $"{this.Username} is following {this.Followees.Count} people";
        }
    }
}
