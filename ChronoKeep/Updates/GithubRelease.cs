using System;
using Newtonsoft.Json;

namespace ChronoKeep.Updates
{
    internal class GithubRelease
    {
        [JsonProperty("url")]
        public string Url;
        [JsonProperty("assets_url")]
        public string AssetsURL;
        [JsonProperty("upload_url")]
        public string UploadURL;
        [JsonProperty("html_url")]
        public string HTML_URL;
        [JsonProperty("id")]
        public int ID;
        [JsonProperty("author")]
        public Author Author;
        [JsonProperty("node_id")]
        public string NodeID;
        [JsonProperty("tag_name")]
        public string TagName;
        [JsonProperty("target_commitish")]
        public string TargetComitish;
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("draft")]
        public bool Draft;
        [JsonProperty("prerelease")]
        public bool PreRelease;
        [JsonProperty("created_at")]
        public DateTime CreatedAt;
        [JsonProperty("published_at")]
        public DateTime PublishedAt;
        [JsonProperty("assets")]
        public Assets[] Assets;
        [JsonProperty("tarball_url")]
        public string TarballURL;
        [JsonProperty("zipball_url")]
        public string ZipballURL;
        [JsonProperty("body")]
        public string Body;
    }

    internal class Author
    {
        [JsonProperty("login")]
        public string Login;
        [JsonProperty("id")]
        public int ID;
        [JsonProperty("node_id")]
        public string NodeID;
        [JsonProperty("avatar_url")]
        public string AvatarURL;
        [JsonProperty("gravatar_url")]
        public string GravatarURL;
        [JsonProperty("url")]
        public string URL;
        [JsonProperty("html_url")]
        public string HTMLURL;
        [JsonProperty("followers_url")]
        public string FollowersURL;
        [JsonProperty("following_url")]
        public string FollowingURL;
        [JsonProperty("gists_url")]
        public string GistsURL;
        [JsonProperty("starred_url")]
        public string StarredURL;
        [JsonProperty("subscriptions_url")]
        public string SubscriptionsURL;
        [JsonProperty("organizations_url")]
        public string OrganizationsURL;
        [JsonProperty("repos_url")]
        public string ReposURL;
        [JsonProperty("events_url")]
        public string EventsURL;
        [JsonProperty("received_events_url")]
        public string ReceivedEventsURL;
        [JsonProperty("type")]
        public string Type;
        [JsonProperty("site_admin")]
        public bool SiteAdmin;
    }

    internal class Assets
    {
        [JsonProperty("url")]
        public string URL;
        [JsonProperty("id")]
        public int ID;
        [JsonProperty("node_id")]
        public string NodeID;
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("label")]
        public string Label;
        [JsonProperty("uploader")]
        public Author Uploader;
        [JsonProperty("content_type")]
        public string ContentType;
        [JsonProperty("state")]
        public string State;
        [JsonProperty("size")]
        public int Size;
        [JsonProperty("download_count")]
        public int DownloadCount;
        [JsonProperty("created_at")]
        public DateTime CreatedAt;
        [JsonProperty("updated_at")]
        public DateTime UpdatedAt;
        [JsonProperty("browser_download_url")]
        public string BrowserDownloadURL;
    }
}
