using System;
using System.Threading;
using System.Threading.Tasks;
using BardMusicPlayer.MidiUtil.Managers;
using BardMusicPlayer.MidiUtil.Ui;


namespace BardMusicPlayer.MidiUtil
{
    public class MidiUtil
    {
        private static MidiUtil instance = null;
        private static readonly object padlock = new object();
        MidiUtil()
        {
        }

        public static MidiUtil Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new MidiUtil();
                    }
                    return instance;
                }
            }
        }

        bool Started { get; set; } = false;

        /// <summary>
        /// Start Grunt.
        /// </summary>
        public void Start()
        {
            if (Started)
                return;
            UiManager.Instance.mainWindow = new MidiEditWindow();
            Started = true;
        }

        /// <summary>
        /// Stop Grunt.
        /// </summary>
        public void Stop()
        {
            if (!Started)
                return;
            Started = false;
        }

        ~MidiUtil() => Dispose();
        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
