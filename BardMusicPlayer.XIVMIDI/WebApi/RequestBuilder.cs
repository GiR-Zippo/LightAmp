/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using Newtonsoft.Json;
using System;
namespace BardMusicPlayer.XIVMIDI.IO;

#region BMPApi
/// <summary>
/// Build the BMP API request string
/// </summary>
public class BMPAPIRequestBuilder
{
    private readonly string ApiBaseUrl = "https://bardmusicplayer.com/api/midi-search";

    /// <summary>
    /// Free-text search across title / artist / source / arranger.
    /// </summary>
    public string Search { get; set; } = "";

    /// <summary>
    /// Search by editor
    /// </summary>
    private string Editor { get; set; } = "";

    /// <summary>
    /// Set the performer size
    /// </summary>
    public int bandSize { get; set; } = 0;

    /// <summary>
    /// The page we request
    /// </summary>
    public int page = 1;

    public string BuildRequest()
    {
        if (Search != "")
        {
            var s = Misc.DecodeSearch(Search);
            Editor = Uri.EscapeDataString(s["editor"]);
            Search = Uri.EscapeDataString(s["search"] +" " + s["artist"]);
        }

        var request = "?limit=100";
        request += Search.Length > 1 ? "&search=" + Search : "";
        request += Editor.Length > 1 ? "&editor=" + Editor : "";
        request += bandSize <= 0 || bandSize > 8 ? "" : "&ensemble=" + (bandSize == 2 ? "duo" : Misc.PerformerSize[bandSize].ToLower());
        request += "&page=" + page.ToString();
        request = ApiBaseUrl + request.Replace(" ", "%20");
        return request;
    }
}

/// <summary>
/// The upload template to BMP Midi archive
/// </summary>
public class BMPUploadBuilder
{
    [JsonIgnore]
    public readonly string ApiBaseUrl = "https://bardmusicplayer.com/api/midis";
    [JsonIgnore]
    public string ApiKey { get; set; } = "";
    [JsonIgnore]
    public string FileName { get; set; } = "";
    [JsonIgnore]
    public byte[] MidiFile { get; set; } = new byte[0]; 

    //alles hier geht ins json
    public string title { get; set; } = "";
    public string artist { get; set; } = "";
    public string source { get; set; } = "";
    public string originalSourceUrl { get; set; } = "";
}
#endregion

#region XIVMidi
/// <summary>
/// Build the XIVMIDI API request string
/// </summary>
public class XIVMIDIRequestBuilder
{
    private readonly string ApiBaseUrl = "https://api.xivmidi.com/v2/files";

    /// <summary>
    /// Internal searchstring
    /// </summary>
    public string Search { get; set; } = "";

    /// <summary>
    /// Set the editor
    /// </summary>
    private string Credit { get; set; } = "";

    /// <summary>
    /// Set the artist
    /// </summary>
    private string Artist { get; set; } = "";

    /// <summary>
    /// Set the title
    /// </summary>
    private string Title { get; set; } = "";

    /// <summary>
    /// Set the performer size
    /// </summary>
    public int bandSize { get; set; } = 0;

    /// <summary>
    /// Set the Tags
    /// </summary>
    public string Tags { get; set; } = "";

    /// <summary>
    /// Pages are limited to 100 entries
    /// define the skip to get the next page
    /// </summary>
    public int skip { get; set; } = 0;

    /// <summary>
    /// Set the instruments "Piano;Harp"
    /// </summary>
    public string Instrument { get; set; } = "";
    public int limit { get; set; } = -1;

    public string BuildRequest()
    {
        if (Search != "")
        {
            var s = Misc.DecodeSearch(Search);
            Credit = Uri.EscapeDataString(s["editor"]);
            Artist = Uri.EscapeDataString(s["artist"]);
            Title = Uri.EscapeDataString(s["search"]);
        }

        var request = ApiBaseUrl + "?limit=100";
        request += Credit == "" ? "" : "&credit=" + Credit;
        request += Artist == "" ? "" : "&artist=" + Artist;
        request += Title == "" ? "" : "&title=" + Title;
        request += bandSize <= 0 || bandSize > 8 ? "" : "&bandsize=" + bandSize.ToString();
        request += Tags == "" ? "" : "&tags=" + Tags;
        request += Instrument == "" ? "" : "&instruments=" + Instrument;
        return request;
    }
#endregion
}
