/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System.Threading.Tasks;
using BardMusicPlayer.Maestro.Sequencing;
using BardMusicPlayer.Seer;
using BardMusicPlayer.Quotidian.Structs;

namespace BardMusicPlayer.Maestro.Performance
{
    public interface IPerformer
    {
        // --- Properties ---
        int SingerTrackNr { get; set; }
        int OctaveShift { get; set; }
        bool OctaveShiftEnabled { get; }
        int TrackNumber { get; set; }
        bool PerformerEnabled { get; set; }
        bool UsesDalamud { get; }
        bool UsesDalamudForKeys { get; set; }
        bool HostProcess { get; set; }
        int PId { get; set; }
        Game game { get; set; }
        string PlayerName { get; }
        string HomeWorld { get; }
        string SongName { get; }
        string TrackInstrument { get; }
        Sequencer Sequencer { get; set; }
        long LyricsOffsetTime { get; set; }

        // --- Performer.cs ---
        void Close();
        void SetProgress(int progress);
        void Play(bool play, int delay = 0);
        void Stop();

        // --- Performer.GameActions.cs ---
        void OpenInstrument();
        Task<int> ReplaceInstrument();
        void CloseInstrument();

        // --- Performer.GameActions.cs ---
        void DoReadyCheck();
        void EnsembleAccept();
        void YesNoBoxAccept();
        void EnterHouse();

        // --- Performer.GameActions.cs ---
        void SendTextCopyPasta(string text);
        void SendText(string text);
        void SendText(ChatMessageChannelType type, string text);
        void TapKey(string modifier, string character);

        // --- Performer.Lyrics.cs ---
        void StartLyricsTimer();
        void StopLyricsTimer();
    }
}