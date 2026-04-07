using System;
using System.Collections.Generic;

namespace BardMusicPlayer.XIVMIDI.IO
{
    /// <summary>
    /// The BMP API responses
    /// </summary>
    public static class BMPResponseContainer
    {
        public class Root
        {
            public List<Doc> docs { get; set; }
            public bool hasNextPage { get; set; }
            public bool hasPrevPage { get; set; }
            public int limit { get; set; }
            public int? nextPage { get; set; }
            public int page { get; set; }
            public int pagingCounter { get; set; }
            public int? prevPage { get; set; }
            public int totalDocs { get; set; }
            public int totalPages { get; set; }
        }

        public class Doc
        {
            public int id { get; set; }
            public string title { get; set; }
            public string titleSort { get; set; }
            public string artist { get; set; }
            public string source { get; set; }
            public string arranger { get; set; }
            public List<Tag> tags { get; set; }
            public string ensembleSize { get; set; }
            public int trackCount { get; set; }
            public string duration { get; set; }
            public string notes { get; set; }
            public int downloads { get; set; }
            public string originalSourceUrl { get; set; }
            public string importedFrom { get; set; }
            public int uploadedBy { get; set; }
            public string originalEditorDiscordId { get; set; }
            public string md5 { get; set; }
            public long songDurationMs { get; set; }
            public List<Track> tracks { get; set; }
            public List<string> discord_choice { get; set; }
            public string uploadedFrom { get; set; }
            public object discordLinks { get; set; }
            public DateTime? originalCreatedAt { get; set; }
            public DateTime updatedAt { get; set; }
            public DateTime createdAt { get; set; }
            public string url { get; set; }
            public string thumbnailURL { get; set; }
            public string filename { get; set; }
            public string mimeType { get; set; }
            public long filesize { get; set; }
            public int? width { get; set; }
            public int? height { get; set; }
            public double? focalX { get; set; }
            public double? focalY { get; set; }
        }

        public class Tag
        {
            public int id { get; set; }
            public string name { get; set; }
            public DateTime updatedAt { get; set; }
            public DateTime createdAt { get; set; }
        }

        public class Track
        {
            public string name { get; set; }
            public int order { get; set; }
            public string modifier { get; set; }
            public string instrument { get; set; }
        }

    }

    /// <summary>
    /// The XIVMIDI API responses
    /// </summary>
    public static class XIVMIDIResponseContainer
    {
        /// <summary>
        /// The container for downloaded midi file
        /// </summary>
        public record MidiFile
        {
            /// <summary>
            /// Filename of the downloaded midi
            /// </summary>
            public string Filename { get; set; } = "";

            /// <summary>
            /// Binary data reqdy to use via File.WriteAllBytes
            /// </summary>
            public byte[] data { get; set; } = null;
        }

        /// <summary>
        /// The API response
        /// </summary>
        public record ApiResponse
        {
            public int status { get; set; }
            public string message { get; set; }
            public JsonData data {get; set;}
        }

        /// <summary>
        /// The inner data
        /// </summary>
        public record JsonData
        {
            public List<File> files { get; set; }
            public int totalFiles { get; set; }
        }

        /// <summary>
        /// The file structure
        /// </summary>
        public record File
        {
            public string md5 { get; set; }
            public string editorDiscordId { get; set; }
            public string editor { get; set; }
            public string artist { get; set; }
            public string title { get; set; }
            public string bandSize { get; set; }
            public string source { get; set; }  
            public List<string> tags { get; set; }
            public int songDuration { get; set; }
            public List<Track> tracks { get; set; }
            public bool BMLPublished { get; set; }
            public bool BMLEditorChannelPublished { get; set; }
            public bool BMPPublished { get; set; }
            public bool BardMetalPublished { get; set; }
            public bool websitePublished { get; set; }
            public bool publicAPIPublished { get; set; }
            public string uploadedFrom { get; set; }
            public DateTime createdAt { get; set; }
            public DateTime updatedAt { get; set; }
            public string BMLDiscordLink { get; set; }
            public string BMLDiscordMessageId { get; set; }
            public string BMLEditorChannelLink { get; set; }
            public string BMLEditorChannelMessageId { get; set; }
            public string BMPDiscordLink { get; set; }
            public string BMPDiscordMessageId { get; set; }
            public string BardMetalDiscordLink { get; set; }
            public string BardMetalDiscordMessageId { get; set; }
            public string websiteFilePath { get; set; }
            public string websiteLink { get; set; }
        }

        /// <summary>
        /// And the track structure
        /// </summary>
        public record Track
        {
            public int order { get; set; }
            public string name { get; set; }
            public string instrument { get; set; }
            public int modifier { get; set; }
        }
    }
}
