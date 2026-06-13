using BardMusicPlayer.Jamboree;
using BardMusicPlayer.Maestro.Performance;
using BardMusicPlayer.Maestro.Sequencing;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Seer;
using System.Threading.Tasks;

namespace BardMusicPlayer.Ui.Functions.Network;

/// <summary>
/// Networkperformer / wrapper for CharacterState
/// </summary>
public class NetworkPerformer : IPerformer
{
    public NetworkPerformer(CharacterState character)
    {
        _characterState = character;
    }

    private CharacterState _characterState { get; set; } = null;
    public string CharId() { return _characterState.charId; }
    public string PlayerName { get {  return _characterState.displayName; } }
    public string HomeWorld { get {  return _characterState.world; } }
    public int TrackNumber 
    { 
        get { return _characterState.trackNumber ?? 1; } 
        set 
        {
            if (value == _characterState.trackNumber)
                return;
            if (value <= 0)
                _characterState.trackNumber = 1;
            _characterState.trackNumber = value; 
        }
    }
    public string TrackInstrument
    { 
        get { return _characterState.instrument; }
        set { _characterState.instrument = value; }
    }
    public bool PerformerEnabled { get; set; } = true;
    public bool UsesDalamud { get; } = false;

    // not in use now
    public int SingerTrackNr { get; set; } = 0;
    public int OctaveShift { get; set; } = 0;
    public bool OctaveShiftEnabled { get; } = false;
    public bool UsesDalamudForKeys { get; set; }
    public bool HostProcess { get; set; }
    public int PId { get; set; }
    public Game game { get; set; }
    public string SongName { get; set; }
    public Sequencer Sequencer { get; set; }
    public long LyricsOffsetTime { get; set; }

    public void Close() { }

    public void CloseInstrument() { }

    public void DoReadyCheck() { }

    public void EnsembleAccept() { }

    public void EnterHouse() { }

    public void OpenInstrument() { }

    public void Play(bool play, int delay = 0) { }

    public Task<int> ReplaceInstrument() { return null; }

    public void SendText(string text) { }

    public void SendText(ChatMessageChannelType type, string text) { }

    public void SendTextCopyPasta(string text) { }

    public void SetProgress(int progress) { }

    public void StartLyricsTimer() { }

    public void Stop() {}

    public void StopLyricsTimer() {}

    public void TapKey(string modifier, string character) {}

    public void YesNoBoxAccept() {}
}
