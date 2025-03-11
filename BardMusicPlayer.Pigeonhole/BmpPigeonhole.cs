/*
 * Copyright(c) 2025 GiR-Zippo, 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Pigeonhole.JsonSettings.Autosave;
using BardMusicPlayer.Quotidian;

namespace BardMusicPlayer.Pigeonhole
{
    public class BmpPigeonhole : JsonSettings.JsonSettings
    {
        private static BmpPigeonhole _instance;

        /// <summary>
        /// Initializes the pigeonhole file
        /// </summary>
        /// <param name="filename">full path to the json pigeonhole file</param>
        public static void Initialize(string filename)
        {
            if (Initialized) return;
            _instance = Load<BmpPigeonhole>(filename).EnableAutosave();
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool Initialized => _instance != null;

        /// <summary>
        /// Gets this pigeonhole instance
        /// </summary>
        public static BmpPigeonhole Instance => _instance ?? throw new BmpException("This pigeonhole must be initialized first.");

        #region Playlist Settings

        /// <summary>
        /// Sets PlayAllTracks
        /// </summary>
        public virtual bool PlayAllTracks { get; set; } = false;

        /// <summary>
        /// Sets PlaylistDelay
        /// </summary>
        public virtual float PlaylistDelay { get; set; } = 1;

        /// <summary>
        /// Sets PlayAllTracks
        /// </summary>
        public virtual bool PlaylistAutoPlay { get; set; } = false;

        /// <summary>
        /// last loaded song
        /// </summary>
        public virtual string LastLoadedCatalog { get; set; } = "";

        #endregion

        /// <summary>
        /// sets/gets if the host should be switches according to the group lead
        /// </summary>
        public virtual bool AutoselectHost { get; set; } = true;

        /// <summary>
        /// last loaded song
        /// </summary>
        public virtual string SongDirectory { get; set; } = "songs/";

        /// <summary>
        /// hold long notes
        /// </summary>
        public virtual bool HoldNotes { get; set; } = true;

        /// <summary>
        /// save the chatlog
        /// </summary>
        public virtual bool SaveChatLog { get; set; } = false;

        /// <summary>
        /// Sets the autostart method
        /// </summary>
        public virtual int AutostartMethod { get; set; } = 2;

        /// <summary>
        /// Sets UnequipPause
        /// </summary>
        public virtual bool UnequipPause { get; set; } = true;

        /// <summary>
        /// last selected midi input device
        /// </summary>
        public virtual int MidiInputDev { get; set; } = -1;

        /// <summary>
        /// are we using the LA for live midi input play
        /// </summary>
        public virtual bool LiveMidiPlayDelay { get; set; } = false;

        /// <summary>
        /// force the playback
        /// </summary>
        public virtual bool ForcePlayback { get; set; } = false;

        /// <summary>
        /// brings the bmp to front
        /// </summary>
        public virtual bool BringBMPtoFront { get; set; } = false;

        /// <summary>
        /// Enables the multibox feature
        /// </summary>
        public virtual bool EnableMultibox { get; set; } = true;

        /// <summary>
        /// BMP window location
        /// </summary>
        public virtual global::System.Drawing.Point BmpLocation { get; set; } = System.Drawing.Point.Empty;

        public virtual global::System.Drawing.Size BmpSize { get; set; } = System.Drawing.Size.Empty;

        /// <summary>
        /// The Ui version which should be used
        /// </summary>
        public virtual bool ClassicUi { get; set; } = true;

        /// <summary>
        /// Sets/Gets last used skin
        /// </summary>
        public virtual string LastSkin { get; set; } = "";

        /// <summary>
        /// open local orchestra after hooking new proc
        /// </summary>
        public virtual bool LocalOrchestra { get; set; } = true;

        /// <summary>
        /// Enable the 16 voice limit in Synthesizer
        /// </summary>
        public virtual bool EnableSynthVoiceLimiter { get; set; } = false;

        /// <summary>
        /// milliseconds till ready check confirmation.
        /// </summary>
        public virtual int EnsembleReadyDelay { get; set; } = 500;

        /// <summary>
        /// playback delay enabled
        /// </summary>
        public virtual bool EnsemblePlayDelay { get; set; } = false;

        /// <summary>
        /// autoequip bards after song loaded
        /// </summary>
        public virtual bool AutoEquipBards { get; set; } = false;

        /// <summary>
        /// keep the ensmble track settings
        /// </summary>
        public virtual bool EnsembleKeepTrackSetting { get; set; } = true;

        /// <summary>
        /// ignores the progchange
        /// </summary>
        public virtual bool IgnoreProgChange { get; set; } = false;

        /// <summary>
        /// start the performer by it's own ready signal
        /// </summary>
        public virtual bool EnsembleStartIndividual { get; set; } = true;

        /// <summary>
        /// milliseconds between game process scans / seer scanner startups.
        /// </summary>
        public virtual int SeerGameScanCooldown { get; set; } = 20;

        /// <summary>
        /// Contains the last path of an opened midi file
        /// </summary>
        public virtual string LastOpenedMidiPath { get; set; } = "";

        /// <summary>
        /// Compatmode for MidiBard
        /// </summary>
        public virtual bool MidiBardCompatMode { get; set; } = false;

        /// <summary>
        /// unkown but used
        /// </summary>
        public virtual bool UsePluginForKeyOutput { get; set; } = false;

        /// <summary>
        /// Use the Hypnotoad for instruemtn eq
        /// </summary>
        public virtual bool UsePluginForInstrumentOpen { get; set; } = false;

        /// <summary>
        /// Defaults to log level Info
        /// </summary>
        public virtual BmpLog.Verbosity DefaultLogLevel { get; set; } = BmpLog.Verbosity.Info;

        /// <summary>
        /// Use the NoteOffset instead the instrument offset
        /// </summary>
        public virtual bool UseNoteOffset { get; set; } = false;

        /// <summary>
        /// Use the LyricsOffset to keep lyrics in sync with the ensemble
        /// </summary>
        public virtual bool UseLyricsOffset { get; set; } = false;

        /// <summary>
        /// Player HomeWorld cache
        /// </summary>
        public virtual string PlayerHomeWorldCache { get; set; } = "";

        /// <summary>
        /// Autoaccepts the party invite from local account
        /// </summary>
        public virtual bool AutoAcceptPartyInvite { get; set; } = false;

        /// <summary>
        /// Songhistory in use or not
        /// </summary>
        public virtual bool EnableSongHistory { get; set; } = false;
    }
}
