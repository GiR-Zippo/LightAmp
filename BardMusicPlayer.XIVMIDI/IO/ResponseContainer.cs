using System;
using System.Collections.Generic;

namespace BardMusicPlayer.XIVMIDI.IO
{
    /// <summary>
    /// The api responses
    /// </summary>
    public static class ResponseContainer
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
