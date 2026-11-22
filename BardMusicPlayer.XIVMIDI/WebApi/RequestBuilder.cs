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
    public string Editor { get; set; } = "";

    /// <summary>
    /// Set the performer size
    /// </summary>
    public int bandSize { get; set; } = 0;

    /// <summary>
    /// The page we request
    /// </summary>
    public int page = 1;

    public SortOrder Sort { get; set; } = SortOrder.Newest;

    public string BuildRequest()
    {
        var search = Search;
        var editor = Editor;

        var sort = Sort switch
        {
            SortOrder.Newest => null,
            SortOrder.Oldest => "createdAt",
            SortOrder.TitleAsc => "titleSort",
            SortOrder.TitleDesc => "-titleSort",
            SortOrder.MostDownloaded => "-downloads",
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = Misc.DecodeSearch(search);

            search = (s["search"] +" " + s["artist"]).Trim();

            if (string.IsNullOrWhiteSpace(editor))
                editor = s["editor"];
        }

        var request = "?limit=100";
        request += !string.IsNullOrWhiteSpace(search) ? "&search=" + Uri.EscapeDataString(search) : "";
        request += !string.IsNullOrWhiteSpace(editor) ? "&editor=" + Uri.EscapeDataString(editor) : "";
        request += bandSize <= 0 || bandSize > 8 ? "" : "&ensemble=" + (bandSize == 2 ? "duo" : Misc.PerformerSize[bandSize].ToLower());
        request += string.IsNullOrWhiteSpace(sort) ? "" : "&sort=" + sort;
        request += "&page=" + page.ToString();
        request = ApiBaseUrl + request;
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
    //public string originalSourceUrl { get; set; } = "";
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
    public string Credit { get; set; } = "";

    /// <summary>
    /// Set the artist
    /// </summary>
    public string Artist { get; set; } = "";

    /// <summary>
    /// Set the title
    /// </summary>
    public string Title { get; set; } = "";

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

    public SortOrder Sort { get; set; } = SortOrder.Newest;

    public string BuildRequest()
    {
        var artist = Artist;
        var title = Title;
        var credit = Credit;

        var sort = Sort switch
        {
            SortOrder.Newest => null,
            SortOrder.Oldest => "date_asc",
            SortOrder.TitleAsc => "title_asc",
            SortOrder.TitleDesc => "title_desc",
            SortOrder.MostDownloaded => "downloads_desc",
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Misc.DecodeSearch(Search);

            if (string.IsNullOrWhiteSpace(credit))
                credit = s["editor"];

            if (string.IsNullOrWhiteSpace(artist))
                artist = s["artist"];

            if (string.IsNullOrWhiteSpace(title))
                title = s["search"];
        }

        var request = ApiBaseUrl + "?limit=100";
        request += skip == 0 ? "" : "&skip="+skip;
        request += string.IsNullOrWhiteSpace(credit) ? "" : "&credit=" + Uri.EscapeDataString(credit);
        request += string.IsNullOrWhiteSpace(artist) ? "" : "&artist=" + Uri.EscapeDataString(artist);
        request += string.IsNullOrWhiteSpace(title) ? "" : "&title=" + Uri.EscapeDataString(title);
        request += bandSize <= 0 || bandSize > 8 ? "" : "&bandsize=" + bandSize.ToString();
        request += Tags == "" ? "" : "&tags=" + Tags;
        request += Instrument == "" ? "" : "&instruments=" + Instrument;
        request += string.IsNullOrWhiteSpace(sort) ? "" : "&sort=" + sort;
        return request;
    }
}

#endregion
