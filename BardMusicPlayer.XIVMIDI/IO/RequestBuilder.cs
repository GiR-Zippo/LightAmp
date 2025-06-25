namespace BardMusicPlayer.XIVMIDI.IO;

/// <summary>
/// Build the API request string
/// </summary>
public class RequestBuilder
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
    public int Performers { get; set; } = 0;

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
        var request = ApiBaseUrl + "/files?";
        request += md5 == "" ? "" : "md5=" + md5 + "&";
        request += Editor == "" ? "" : "editor=" + Editor + "&";
        request += Artist == "" ? "" : "artist=" + Artist + "&";
        request += Title == "" ? "" : "title=" + Title + "&";
        request += Performers <= 0 || Performers > 8 ? "" : "performer=" + Misc.PerformerSize[Performers] + "&";
        request += Tags == "" ? "" : "tags=" + Tags + "&";
        request += Instrument == "" ? "" : "instrument=" + Instrument + "&";
        request += limit == -1 ? "" : "limit=" + limit.ToString() + "&";
        request += "website=true";
        return request;
    }
}
