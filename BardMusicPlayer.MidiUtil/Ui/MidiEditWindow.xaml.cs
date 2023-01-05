using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

//using UI.Resources;

using BardMusicPlayer.MidiUtil.Managers;
using BardMusicPlayer.MidiUtil.Ui.TrackLine;

namespace BardMusicPlayer.MidiUtil.Ui
{
    public partial class MidiEditWindow : Window
    {
        #region CTOR

        public Control Ctrl { get; set; }
        public Model Model { get; set; }
        public List<MidiLineView> MidiLines = new List<MidiLineView>();

        public MidiEditWindow()
        {
            InitializeComponent();
            Model = new Model();
            DataContext = Model;
            Ctrl = new Control(Model, this);
            Ctrl.InitView();
            Model.TracksPanel = TracksPanel;
            MidiManager.Instance.Init();
            UiManager.Instance.mainWindow = this;
            Show();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Model.TracksPanel.Children.Clear();
            Model.TracksPanel.RowDefinitions.Clear();
            MidiManager.Instance.Dispose();
            UiManager.Instance.mainWindow = null;

            MidiUtil.Instance.Stop();
        }

        #endregion

        public void HandleWheel(object sender, MouseWheelEventArgs e)
        {
            if (!this.IsActive)
                return;

            int value = e.Delta / 120;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                Ctrl.TranslateTracks(e.Delta / 5);
            }
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                Ctrl.ZoomTracksX(value);
            }
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {
                Ctrl.ZoomTracksY(value);
            }
        }
        
        #region MENU

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openMidiFileDialog = new OpenFileDialog();
            if (openMidiFileDialog.ShowDialog() == true)
            {
                Ctrl.Open(openMidiFileDialog.FileName);
            }
        }

        private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new SaveFileDialog
            {
                Filter = "Midi file | *.mid"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            Ctrl.Save(openFileDialog.FileName);
        }

        #region Track Menu

        private void AddTrackMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Ctrl.AddTrack();
        }

        private void DeleteTrackMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Ctrl.RemoveTrack();
        }

        private void RemoveAllEventsTrackMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Ctrl.RemoveAllEventsFromTrack();
        }

        private void DrummappingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Ctrl.Drummapping();
        }

        private void TransposeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            TextInputWindow textInput = new TextInputWindow("Transpose in halftones", 4);
            if (textInput.ShowDialog() == true)
            {
                if (textInput.ResponseText.Length < 1)
                    return;

                int n;
                if (int.TryParse(textInput.ResponseText, out n))
                    Ctrl.TransposeTrack(n);
            }
        }
        #endregion

        #region SongMenu
        private void CleanUpSong_Click(object sender, RoutedEventArgs e)
        {
            Ctrl.CleanUpSong();
        }

        private void AutochannelSong_Click(object sender, RoutedEventArgs e)
        {
            Ctrl.AutochannelSong();
        }

        #endregion

        private void SirenEnabled_Click(object sender, RoutedEventArgs e)
        {
            MidiManager.Instance.UseSiren = !MidiManager.Instance.UseSiren;
            SirenEnabled.IsChecked = MidiManager.Instance.UseSiren;
        }

        #endregion

        #region UPDATE

        /// <summary>
        /// Update horizontal scrolling
        /// </summary>
        public void UpdateHorizontalScrolling()
        {
            if (Model.TracksPanel.Children.Count <= 0)
                return;
            Model.XOffset = MasterScroller.Value;
            UpdateLayout();
        }

        /// called when played
        public void TimeUpdate()
        {
            if (UiManager.Instance.mainWindow == null)
                return;

            if (Model.TracksPanel.Children.Count < 0) 
                return;

            //Normal playback or siren
            if (!SirenEnabled.IsChecked)
            {
                if (MidiManager.Instance.playbackPos != null)
                {
                    UiManager.Instance.mainWindow.TimeBar.SetValue(System.Windows.Controls.Canvas.LeftProperty,
                        ((Model.relativeTimePosition * Model.XZoom) + Model.touchOffset) + ((MidiManager.Instance.playbackPos.TotalMicroseconds / 1000) * Model.XZoom) - Model.XOffset);
                }
            }
            else
            {
                UiManager.Instance.mainWindow.TimeBar.SetValue(System.Windows.Controls.Canvas.LeftProperty,
                    ((Model.relativeTimePosition * Model.XZoom) + Model.touchOffset) + ((MidiManager.Instance.playbackPosSiren) * Model.XZoom) - Model.XOffset);
                VoiceCount.Text = MidiManager.Instance.SirenVoiceCount.ToString();
            }
            UiManager.Instance.mainWindow.Model.Tempo = MidiManager.Instance.Tempo;
        }

        #region Private

        private double HandleTrackSlide()
        {
            System.Windows.Controls.Canvas canvas = (
                (MidiLineView)(
                    (System.Windows.Controls.Frame)TracksPanel.Children[0]
                ).Content
            ).TrackBody;


            double width = canvas.ActualWidth + canvas.Margin.Left;
            double margin = width * Model.marginPercent;
            double relativeTimePosition = Model.absoluteTimePosition - Model.XOffset;

            Console.WriteLine(": " + width + " : " + margin);

            #region Slide

            //left blocked
            if (Model.absoluteTimePosition < margin)
            {
                Model.XOffset = 0;
                return Model.absoluteTimePosition;
            }

            // right to left slide
            if (relativeTimePosition < margin)
            {
                double delta = margin - relativeTimePosition;
                Model.XOffset -= delta;
                relativeTimePosition =  margin;
            }

            // left to right slide
            if (relativeTimePosition > width - margin)
            {
                double delta = relativeTimePosition - (width - margin);
                Model.XOffset += delta;
                relativeTimePosition = width - margin;
            }

            #endregion

            return relativeTimePosition;
        }

        public void HandleTimeBar()
        {
            TimeBar.SetValue(System.Windows.Controls.Canvas.LeftProperty, (Model.relativeTimePosition*Model.XZoom)+Model.touchOffset );
        }

        // TODO
        private void HandleTimeScroller()
        {
            /*TimeScroller.Value = Math.Min(
                Model.absoluteTimePosition,
                TimeScroller.Maximum
            );*/
        }

        #endregion

        #endregion

        #region PLAY Palette

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            MidiManager.Instance.Start();
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            MidiManager.Instance.Continue();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            MidiManager.Instance.Stop();
        }

        #endregion

        #region User Interractions ON/OFF

        public void EnableUserInterractions()
        {
            Cursor = System.Windows.Input.Cursors.Arrow;
            startButton.IsEnabled = true;
            continueButton.IsEnabled = true;
            stopButton.IsEnabled = true;
            OpenMenuItem.IsEnabled = true;
        }

        public void DisableUserInterractions()
        {
            Cursor = System.Windows.Input.Cursors.Wait;
            startButton.IsEnabled = false;
            continueButton.IsEnabled = false;
            stopButton.IsEnabled = false;
            OpenMenuItem.IsEnabled = false;
        }

        #endregion
    }
}
