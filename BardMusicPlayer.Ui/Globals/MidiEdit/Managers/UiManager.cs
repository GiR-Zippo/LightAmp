using System;
using System.Windows;
using BardMusicPlayer.Ui.MidiEdit.Ui;

namespace BardMusicPlayer.Ui.MidiEdit.Managers
{
    public partial class UiManager : IDisposable
    {
        private static UiManager instance = null;
        private static readonly object padlock = new object();
        UiManager()
        {
        }

        ~UiManager()
        {
            Dispose();
        }

        public static UiManager Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new UiManager();
                    }
                    return instance;
                }
            }
        }

        public void Dispose()
        {
            mainWindow = null;
        }

        public MidiEditWindow mainWindow { get; set; } = null;

        public void ThrowError(string message)
        {
            MessageBox.Show(
                message,
                "Error",
                MessageBoxButton.OK
            );
        }

        // track config
        public int TrackHeightDefault { get; set; } = 100;
        public int TrackHeightMin { get; set; } = 100;
        public int TrackHeightMax { get; set; } = 500;

        // input config
        public double noteLengthDivider { get; set; } = 4;
        public double plotDivider { get; set; } = 4;
        public int plotVelocity { get; set; } = 100;

    }

}
