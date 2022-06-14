using System;
using Newtonsoft.Json;

namespace Chronokeep.Updates
{
    public class GithubRelease
    {
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("assets_url")]
        public string AssetsURL { get; set; }
        [JsonProperty("upload_url")]
        public string UploadURL { get; set; }
        [JsonProperty("html_url")]
        public string HTML_URL { get; set; }
        [JsonProperty("id")]
        public int ID { get; set; }
        [JsonProperty("author")]
        public Author Author { get; set; }
        [JsonProperty("node_id")]
        public string NodeID { get; set; }
        [JsonProperty("tag_name")]
        public string TagName { get; set; }
        [JsonProperty("target_commitish")]
        public string TargetComitish { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("draft")]
        public bool Draft { get; set; }
        [JsonProperty("prerelease")]
        public bool PreRelease { get; set; }
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }
        [JsonProperty("assets")]
        public Assets[] Assets { get; set; }
        [JsonProperty("tarball_url")]
        public string TarballURL { get; set; }
        [JsonProperty("zipball_url")]
        public string ZipballURL { get; set; }
        [JsonProperty("body")]
        public string Body { get; set; }
    }

    public class Author
    {
        [JsonProperty("login")]
        public string Login { get; set; }
        [JsonProperty("id")]
        public int ID { get; set; }
        [JsonProperty("node_id")]
        public string NodeID { get; set; }
        [JsonProperty("avatar_url")]
        public string AvatarURL { get; set; }
        [JsonProperty("gravatar_url")]
        public string GravatarURL { get; set; }
        [JsonProperty("url")]
        public string URL { get; set; }
        [JsonProperty("html_url")]
        public string HTMLURL { get; set; }
        [JsonProperty("followers_url")]
        public string FollowersURL { get; set; }
        [JsonProperty("following_url")]
        public string FollowingURL { get; set; }
        [JsonProperty("gists_url")]
        public string GistsURL { get; set; }
        [JsonProperty("starred_url")]
        public string StarredURL { get; set; }
        [JsonProperty("subscriptions_url")]
        public string SubscriptionsURL { get; set; }
        [JsonProperty("organizations_url")]
        public string OrganizationsURL { get; set; }
        [JsonProperty("repos_url")]
        public string ReposURL { get; set; }
        [JsonProperty("events_url")]
        public string EventsURL { get; set; }
        [JsonProperty("received_events_url")]
        public string ReceivedEventsURL { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("site_admin")]
        public bool SiteAdmin { get; set; }
    }

    public class Assets
    {
        [JsonProperty("url")]
        public string URL { get; set; }
        [JsonProperty("id")]
        public int ID { get; set; }
        [JsonProperty("node_id")]
        public string NodeID { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("label")]
        public string Label { get; set; }
        [JsonProperty("uploader")]
        public Author Uploader { get; set; }
        [JsonProperty("content_type")]
        public string ContentType { get; set; }
        [JsonProperty("state")]
        public string State { get; set; }
        [JsonProperty("size")]
        public int Size { get; set; }
        [JsonProperty("download_count")]
        public int DownloadCount { get; set; }
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [JsonProperty("browser_download_url")]
        public string BrowserDownloadURL { get; set; }
    }
}
