/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;

namespace BardMusicPlayer.XIVMIDI.IO
{
    #region BMPApi
    /// <summary>
    /// The BMP API responses
    /// </summary>
    public static class BMPResponseContainer
    {
        public record Root
        {
            public List<Doc> docs { get; set; }
            public int totalPages { get; set; }
            public int page {  get; set; }
        }

        public record Doc
        {
            public int id { get; set; }
            public string title { get; set; }
            public string artist { get; set; }
            public string source { get; set; }
            public string arranger { get; set; }
            public string ensembleSize { get; set; }
            public int trackCount { get; set; }
            public string duration { get; set; }
            public string notes { get; set; }
            public string downloads { get; set; }
            public int hearts { get; set; }
            public string filename { get; set; }
            public string mimeType { get; set; }
            public long filesize { get; set; }
            public long songDurationMs { get; set; }
            public List<Track> tracks { get; set; }
            public string importedFrom { get; set; }
            public string originalSourceUrl { get; set; }
            public int uploadedBy { get; set; }
            public string md5 { get; set; }
            public string titleSort { get; set; }
            public DateTime createdAt { get; set; }
            public DateTime updatedAt { get; set; }
            public string uploadedFrom { get; set; }
            public DateTime? originalCreatedAt { get; set; }
            public string url { get; set; }
            public List<Instrument> instruments { get; set; }
//            public List<string> genres { get; set; }
        }

        public record Track
        {
            public string name { get; set; }
            public int order { get; set; }
            public string modifier { get; set; }
            public string instrument { get; set; }
        }

        public record Instrument
        { 
            public int id { get; set; }
            public string name { get; set; }
        }


    }
    #endregion

    #region XIVMidiApi
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
            public bool sucess { get; set; }
            public MetaData meta { get; set; }
            public List<File> data { get; set;}
        }

        /// <summary>
        /// The meta data block for pagination
        /// </summary>
        public record MetaData
        {
            public int total { get; set; }
            public int skip { get; set; }
            public int limit { get; set; }
        }

        /// <summary>
        /// The file structure
        /// </summary>
        public record File
        {
            public string id { get; set; }
            public string artist { get; set; }
            public string title { get; set; }
            public string credit { get; set; }
            public string download_url { get; set; }
        }
    }
    #endregion
}
