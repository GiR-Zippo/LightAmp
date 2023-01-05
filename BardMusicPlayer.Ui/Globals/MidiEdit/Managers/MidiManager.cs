using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;

using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;

using BardMusicPlayer.Quotidian;
using BardMusicPlayer.Transmogrify.Song;

namespace BardMusicPlayer.Ui.MidiEdit.Managers
{

    public partial class MidiManager : IDisposable
    {
        #region Const/Dest
        private static MidiManager instance = null;
        private static readonly object padlock = new object();
        private bool isDisposing = false;
        MidiManager()
        {
        }

        ~MidiManager()
        {
            Dispose();
        }

        public static MidiManager Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new MidiManager();
                    }
                    return instance;
                }
            }
        }

        public void Dispose()
        {
            if (isDisposing)
                return;

            isDisposing = true;

            DeInitSequencer();

            if (playback != null)
                playback.Stop();
            
            if (outDevice != null)
                outDevice.Dispose();

            if (playback != null)
                playback.Dispose();

            GC.SuppressFinalize(this);
            currentSong = null;
            instance = null;
        }
        #endregion

        #region ATRB
        // IO
        private Melanchall.DryWetMidi.Multimedia.OutputDevice outDevice;

        // Midi sequencing
        private MidiFile currentSong { get; set; } = new MidiFile();
        private Playback playback;
        
        public MetricTimeSpan playbackPos { get; set; }
        public IEnumerable<TrackChunk> GetTrackChunks() { return currentSong.GetTrackChunks(); }
        public TempoMap GetTempoMap() { return currentSong.GetTempoMap(); }

        #endregion

        #region CTOR

        public void Init()
        {
            InitSequencer();
            if (CheckMidiOutput())
                InitOutputDevice();
        }

        private bool CheckMidiOutput()
        {
            if (OutputDevice.GetAll().Count < 0)
            {
                UiManager.Instance.ThrowError("No MIDI output devices available.");
                return false;
            }
            else return true;
        }

        private void InitOutputDevice()
        {
            try
            {
                outDevice = OutputDevice.GetByName("Microsoft GS Wavetable Synth");
            }
            catch (Exception ex)
            {
                UiManager.Instance.ThrowError(ex.Message);
            }
        }
        
        /// <summary>
        /// init the sequencer
        /// </summary>
        private void InitSequencer()
        {
            //Create
            if (outDevice != null)
            {
                playback = currentSong.GetPlayback(outDevice);
                playback.InterruptNotesOnStop = true;

                PlaybackCurrentTimeWatcher.Instance.AddPlayback(playback, TimeSpanType.Midi);
                PlaybackCurrentTimeWatcher.Instance.CurrentTimeChanged += OnCurrentTimeChanged;
                PlaybackCurrentTimeWatcher.Instance.PollingInterval = TimeSpan.FromMilliseconds(50);
                PlaybackCurrentTimeWatcher.Instance.Start();

                playback.Finished += Playback_Finished;
            }
        }

        /// <summary>
        /// deinit the sequencer
        /// </summary>
        private void DeInitSequencer()
        {
            Stop();
            if ((outDevice != null) && (playback != null))
            {
                PlaybackCurrentTimeWatcher.Instance.RemovePlayback(playback);
                PlaybackCurrentTimeWatcher.Instance.CurrentTimeChanged -= OnCurrentTimeChanged;
                PlaybackCurrentTimeWatcher.Instance.PollingInterval = TimeSpan.FromMilliseconds(50);
                PlaybackCurrentTimeWatcher.Instance.Stop();

                playback.Finished -= Playback_Finished;
            }
        }

        #endregion

        #region MIDI EVENTS

        private static void OnCurrentTimeChanged(object sender, PlaybackCurrentTimeChangedEventArgs e)
        {
            if (!MidiManager.Instance.playback.IsRunning)
                return;

            var ti = MidiManager.Instance.playback.GetCurrentTime(TimeSpanType.Metric);
            if (ti is MetricTimeSpan mts)
            {
                MidiManager.Instance.playbackPos = mts;
                UiManager.Instance.mainWindow.Ctrl.Update(null, null);
            }
        }

        private void Playback_Finished(object sender, EventArgs e)
        {
            Stop();
        }

        #endregion

        #region PLAY/PAUSE GESTION

        public bool IsPlaying { get; set; } = false;

        public int Tempo
        {
            get
            {
                if (currentSong != null)
                    return (int)currentSong.GetTempoMap().GetTempoAtTime((MidiTimeSpan)0).BeatsPerMinute;
                return 0;
            }
            set
            {
                if (value < 1) value = 1;
            }
        }

        internal void Start()
        {
            try
            {
                IsPlaying = true;
                if (playback is null)
                    return;
                if (outDevice != null)
                {
                    playback.Finished -= Playback_Finished;
                    playback.Dispose();
                    playback = currentSong.GetPlayback(outDevice);
                    playback.InterruptNotesOnStop = true;
                    playback.Finished += Playback_Finished;
                }
                playback.Start();
            }
            catch (Exception ex)
            {
                UiManager.Instance.ThrowError(ex.Message);
            }
        }

        internal void Stop()
        {
            try
            {
                IsPlaying = false;
                if (playback is null)
                    return;

                playback.Stop();
                playback.MoveToStart();
            }
            catch (Exception ex)
            {
                UiManager.Instance.ThrowError(ex.Message);
            }
        }

        internal void Continue()
        {
            try
            {
                IsPlaying = true;
                if (playback is null)
                    return;

                playback.Stop();
            }
            catch (Exception ex)
            {
                UiManager.Instance.ThrowError(ex.Message);
            }
        }

        internal void Playback(bool v, int noteID)
        {
            if (!IsPlaying)
            {
                try
                {
                    if (outDevice == null)
                        return;
                    if (v)
                        outDevice.SendEvent(new NoteOnEvent((SevenBitNumber)noteID, (SevenBitNumber)127));
                    else
                        outDevice.SendEvent(new NoteOffEvent((SevenBitNumber)noteID, (SevenBitNumber)127));
                }
                catch (Exception e)
                {
                    Console.WriteLine("MidiRollDebug : " + e);
                }
            }
        }

        #endregion

        #region PLOT GESTION



        #endregion

        #region DATA

        /// <summary>
        /// Get the song length in milliseconds
        /// </summary>
        /// <returns></returns>
        internal long GetLength()
        {
            var e = playback.GetDuration(TimeSpanType.Metric);
            if (e is MetricTimeSpan mts)
                return mts.TotalMicroseconds / 1000;
            return 0;
        }

        /// <summary>
        /// Save the midifile
        /// </summary>
        /// <param name="fileName"></param>
        internal void SaveFile(string fileName)
        {
            Stop();
            try
            {
                FileStream myStream = new FileStream(fileName, FileMode.Create);
                currentSong.Write(myStream, MidiFileFormat.MultiTrack, new WritingSettings { });
                myStream.Close();
            }
            catch (Exception ex)
            {
                UiManager.Instance.ThrowError(ex.Message);
            }
        }

        /// <summary>
        /// open a midifile
        /// </summary>
        /// <param name="fileName"></param>
        internal void OpenFile(string fileName)
        {
            DeInitSequencer();
            BmpSong bmpSong = BmpSong.OpenFile(fileName).Result;
            OpenFile(bmpSong.GetExportMidi());
        }

        /// <summary>
        /// open the midi from memorystream
        /// </summary>
        /// <param name="midiFile"></param>
        internal void OpenFile(MemoryStream midiFile)
        {
            DeInitSequencer();
            UiManager.Instance.mainWindow.DisableUserInterractions();
            try
            {
                currentSong = MidiFile.Read(midiFile);
                midiFile.Close();
                midiFile.Dispose();
                
            }
            catch (Exception ex)
            {
                UiManager.Instance.ThrowError(ex.Message);
            }
            UiManager.Instance.mainWindow.EnableUserInterractions();
            InitSequencer();

            UiManager.Instance.mainWindow.ProgressionBar.Value = 0;
            UiManager.Instance.mainWindow.MasterScroller.Value = 0;
            UiManager.Instance.mainWindow.MasterScroller.Maximum = ((playback.GetDuration(TimeSpanType.Metric) as MetricTimeSpan).TotalMicroseconds /1000) * UiManager.Instance.mainWindow.Model.XZoom;
            UiManager.Instance.mainWindow.Model.Tempo = (int)currentSong.GetTempoMap().GetTempoAtTime((MidiTimeSpan)0).BeatsPerMinute;
            UiManager.Instance.mainWindow.Ctrl.InitTracks();
        }

        public void HandleLoadProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            UiManager.Instance.mainWindow.ProgressionBar.Value = e.ProgressPercentage;
        }

        #endregion
    }
}
