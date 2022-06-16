using System;
using System.Text.Json.Serialization;

namespace Chronokeep.Updates
{
    public class GithubRelease
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("assets_url")]
        public string AssetsURL { get; set; }
        [JsonPropertyName("upload_url")]
        public string UploadURL { get; set; }
        [JsonPropertyName("html_url")]
        public string HTML_URL { get; set; }
        [JsonPropertyName("id")]
        public int ID { get; set; }
        [JsonPropertyName("author")]
        public Author Author { get; set; }
        [JsonPropertyName("node_id")]
        public string NodeID { get; set; }
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }
        [JsonPropertyName("target_commitish")]
        public string TargetComitish { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("draft")]
        public bool Draft { get; set; }
        [JsonPropertyName("prerelease")]
        public bool PreRelease { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }
        [JsonPropertyName("assets")]
        public Assets[] Assets { get; set; }
        [JsonPropertyName("tarball_url")]
        public string TarballURL { get; set; }
        [JsonPropertyName("zipball_url")]
        public string ZipballURL { get; set; }
        [JsonPropertyName("body")]
        public string Body { get; set; }
    }

    public class Author
    {
        [JsonPropertyName("login")]
        public string Login { get; set; }
        [JsonPropertyName("id")]
        public int ID { get; set; }
        [JsonPropertyName("node_id")]
        public string NodeID { get; set; }
        [JsonPropertyName("avatar_url")]
        public string AvatarURL { get; set; }
        [JsonPropertyName("gravatar_url")]
        public string GravatarURL { get; set; }
        [JsonPropertyName("url")]
        public string URL { get; set; }
        [JsonPropertyName("html_url")]
        public string HTMLURL { get; set; }
        [JsonPropertyName("followers_url")]
        public string FollowersURL { get; set; }
        [JsonPropertyName("following_url")]
        public string FollowingURL { get; set; }
        [JsonPropertyName("gists_url")]
        public string GistsURL { get; set; }
        [JsonPropertyName("starred_url")]
        public string StarredURL { get; set; }
        [JsonPropertyName("subscriptions_url")]
        public string SubscriptionsURL { get; set; }
        [JsonPropertyName("organizations_url")]
        public string OrganizationsURL { get; set; }
        [JsonPropertyName("repos_url")]
        public string ReposURL { get; set; }
        [JsonPropertyName("events_url")]
        public string EventsURL { get; set; }
        [JsonPropertyName("received_events_url")]
        public string ReceivedEventsURL { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("site_admin")]
        public bool SiteAdmin { get; set; }
    }

    public class Assets
    {
        [JsonPropertyName("url")]
        public string URL { get; set; }
        [JsonPropertyName("id")]
        public int ID { get; set; }
        [JsonPropertyName("node_id")]
        public string NodeID { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("label")]
        public string Label { get; set; }
        [JsonPropertyName("uploader")]
        public Author Uploader { get; set; }
        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }
        [JsonPropertyName("state")]
        public string State { get; set; }
        [JsonPropertyName("size")]
        public int Size { get; set; }
        [JsonPropertyName("download_count")]
        public int DownloadCount { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadURL { get; set; }
    }
}
