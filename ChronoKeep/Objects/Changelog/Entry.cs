using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chronokeep.Objects.Changelog
{
    public class Entry : IComparable
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }
        [JsonPropertyName("changes")]
        public List<string> ChangesList { get; set; }
        [JsonPropertyName("fixes")]
        public List<string> FixesList { get; set; }

        public string ChangesVisibility { get => ChangesList.Count > 0 ? "Visible" : "Collapsed"; }
        public string FixesVisibility { get => FixesList.Count > 0 ? "Visible" : "Collapsed"; }
        public bool IsExpanded { get; set; }

        public int CompareTo(object other)
        {
            ArgumentNullException.ThrowIfNull(other);
            if (other is not Entry) return -1;
            string[] thisSplit = Version.Replace("v", "").Split('.');
            string[] otherSplit = ((Entry)other).Version.Replace("v", "").Split('.');
            if (otherSplit.Length == 3 && thisSplit.Length == 3 &&
                int.TryParse(thisSplit[0], out int thisMajor) &&
                int.TryParse(thisSplit[1], out int thisMinor) &&
                int.TryParse(thisSplit[2], out int thisPatch) &&
                int.TryParse(otherSplit[0], out int otherMajor) &&
                int.TryParse(otherSplit[1], out int otherMinor) &&
                int.TryParse(otherSplit[2], out int otherPatch))
            {
                if (otherMajor != thisMajor) return otherMajor.CompareTo(thisMajor);
                if (otherMinor != thisMinor) return otherMinor.CompareTo(thisMinor);
                return otherPatch.CompareTo(thisPatch);
            }
            else
            {
                return Version.CompareTo(((Entry)other).Version);
            }
        }
    }
}
