namespace BardMusicPlayer.XIVMIDI.IO;

/// <summary>
/// Build the BMP API request string
/// </summary>
public class BMPAPIRequestBuilder
{
    private readonly string ApiBaseUrl = "https://bardmusicplayer.com/api";

    /// <summary>
    /// Set the performer size
    /// </summary>
    public int bandSize { get; set; } = 0;

    public string BuildRequest()
    {
        var request = ApiBaseUrl + "/midis?where";

        
        request += bandSize <= 0 || bandSize > 8 ? "" : "[ensembleSize][equals]=" + (bandSize == 2 ? "duo" : Misc.PerformerSize[bandSize].ToLower());
        request += "&limit=0";
        return request;
    }
}

/// <summary>
/// Build the XIVMIDI API request string
/// </summary>
public class XIVMIDIRequestBuilder
{
    private readonly string ApiBaseUrl = "https://api.xivmidi.com";

    public string md5 { get; set; } = "";

    /// <summary>
    /// Set the editor
    /// </summary>
    public string Editor { get; set; } = "";

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
    /// Set the instruments "Piano;Harp"
    /// </summary>
    public string Instrument { get; set; } = "";
    public int limit { get; set; } = -1;

    public string BuildRequest()
    {
        var request = ApiBaseUrl + "/public/files?";
        request += md5 == "" ? "" : "md5=" + md5 + "&";
        request += Editor == "" ? "" : "editor=" + Editor + "&";
        request += Artist == "" ? "" : "artist=" + Artist + "&";
        request += Title == "" ? "" : "title=" + Title + "&";
        request += bandSize <= 0 || bandSize > 8 ? "" : "bandSize=" + Misc.PerformerSize[bandSize] + "&";
        request += Tags == "" ? "" : "tags=" + Tags + "&";
        request += Instrument == "" ? "" : "instrument=" + Instrument;
        return request;
    }
}
