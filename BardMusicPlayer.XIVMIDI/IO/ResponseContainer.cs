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
        public class MidiFile
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
        public class ApiResponse
        {
            public List<File> files { get; set; }
            public int totalPages { get; set; }
            public int totalRecords { get; set; }
        }

        public class File
        {
            public string artist { get; set; }
            public string title { get; set; }
            public string editor { get; set; }
            public string performer { get; set; }
            public string sources { get; set; }
            public string comments { get; set; }
            public List<object> tags { get; set; }
            public int song_duration { get; set; }
            public List<Track> tracks { get; set; }
            public bool discord { get; set; }
            public bool website { get; set; }
            public bool editor_channel { get; set; }
            public object discord_message_id { get; set; }
            public object discord_link { get; set; }
            public string website_file_path { get; set; }
            public string website_link { get; set; }
            public object editor_channel_id { get; set; }
            public object editor_channel_link { get; set; }
            public DateTime createdAt { get; set; }
            public DateTime updatedAt { get; set; }
        }

        public class Track
        {
            public int order { get; set; }
            public string name { get; set; }
            public string instrument { get; set; }
            public int modifier { get; set; }
        }
    }
}
