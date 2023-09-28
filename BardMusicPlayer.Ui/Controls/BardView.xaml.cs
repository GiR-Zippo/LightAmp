﻿/*
 * Copyright(c) 2023 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Maestro;
using BardMusicPlayer.Maestro.Events;
using BardMusicPlayer.Maestro.Performance;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Seer;
using BardMusicPlayer.Seer.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using BardMusicPlayer.DalamudBridge;
using System.Windows.Input;
using BardMusicPlayer.Ui.Functions;
using System.ComponentModel;

namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// Interaktionslogik für BardView.xaml
    /// </summary>
    public sealed partial class BardView : UserControl
    {
        MidiBardConverterWindow _QuickEdit { get; set; } = null;

        public BardView()
        {
            InitializeComponent();
            StartDelay_CheckBox.IsChecked = BmpPigeonhole.Instance.EnsemblePlayDelay;

            this.DataContext = this;

            BmpMaestro.Instance.OnPerformerChanged      += OnPerfomerChanged;
            BmpMaestro.Instance.OnTrackNumberChanged    += OnTrackNumberChanged;
            BmpMaestro.Instance.OnOctaveShiftChanged    += OnOctaveShiftChanged;
            BmpMaestro.Instance.OnSongLoaded            += OnSongLoaded;
            BmpMaestro.Instance.OnPerformerUpdate       += OnPerformerUpdate;
            BmpSeer.Instance.PlayerNameChanged          += OnPlayerNameChanged;
            BmpSeer.Instance.InstrumentHeldChanged      += OnInstrumentHeldChanged;
            BmpSeer.Instance.HomeWorldChanged           += OnHomeWorldChanged;
            Globals.Globals.OnConfigReload              += Globals_OnConfigReload;
            Globals_OnConfigReload(null, null);
        }

        private void Globals_OnConfigReload(object sender, EventArgs e)
        {
            Autoequip_CheckBox.IsChecked = BmpPigeonhole.Instance.AutoEquipBards;
        }

        public Performer SelectedBard { get; set; }

        private void OnPerfomerChanged(object sender, bool e) { UpdateList(); }
        private void OnTrackNumberChanged(object sender, TrackNumberChangedEvent e) { UpdateView(); }
        private void OnOctaveShiftChanged(object sender, OctaveShiftChangedEvent e) { UpdateView(); }
        private void OnSongLoaded(object sender, SongLoadedEvent e)
        { 
            UpdateView();
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_QuickEdit != null)
                    _QuickEdit.MidiFromSong(PlaybackFunctions.CurrentSong);
            }));
        }
        private void OnPerformerUpdate(object sender, PerformerUpdate e) { UpdateView(); }
        private void OnPlayerNameChanged(PlayerNameChanged e) { UpdateView(); }
        private void OnHomeWorldChanged(HomeWorldChanged e) { UpdateView(); }

        private void OnInstrumentHeldChanged(InstrumentHeldChanged e)
        {
            UpdateView();
            if (e.InstrumentHeld.Index == 0)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (CloseInstrument_Button.Visibility != Visibility.Visible)
                        return;
                    OpenInstrument_Button.Visibility = Visibility.Visible;
                    CloseInstrument_Button.Visibility = Visibility.Hidden;
                }
                ));
            }
            else
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (OpenInstrument_Button.Visibility != Visibility.Visible)
                        return;
                    OpenInstrument_Button.Visibility = Visibility.Hidden;
                    CloseInstrument_Button.Visibility = Visibility.Visible;
                }
                ));
            }
                
        }

        /// <summary>
        /// Update the contents of the item only
        /// </summary>
        private void UpdateView()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.BardsList.Items.Refresh();
            }));
        }

        /// <summary>
        /// Add/Remove an element from the list
        /// </summary>
        private void UpdateList()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                List<Performer> tempPerf = BardsList.Items.OfType<Performer>().ToList();
                var comparator = BmpMaestro.Instance.GetAllPerformers().Except(tempPerf).ToList();
                foreach (var p in comparator)
                    BardsList.Items.Add(p);

                comparator = tempPerf.Except(BmpMaestro.Instance.GetAllPerformers()).ToList();
                foreach (var p in comparator)
                    BardsList.Items.Remove(p);

                this.BardsList.Items.Refresh();
            }));
        }

        private void RdyCheck_Click(object sender, RoutedEventArgs e)
        {
            BmpMaestro.Instance.StartEnsCheck();
        }

        private void OpenInstrumentButton_Click(object sender, RoutedEventArgs e)
        {
            BmpMaestro.Instance.EquipInstruments();
        }

        private void CloseInstrumentButton_Click(object sender, RoutedEventArgs e)
        {
            BmpMaestro.Instance.StopLocalPerformer();
            BmpMaestro.Instance.UnEquipInstruments();
        }

        #region Drag&Drop
        /// <summary>
        /// Hier geht mitm Drag&Drop los
        /// </summary>
        private Point startPoint = new Point();

        private void BardsListItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }

        private void BardsListItem_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = startPoint - mousePos;
            if (e.LeftButton == MouseButtonState.Pressed &&
                    (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                           Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                if (sender is ListViewItem celltext)
                {
                    DragDrop.DoDragDrop(BardsList, celltext, DragDropEffects.Move);
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Called when there is a drop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BardsListItem_Drop(object sender, DragEventArgs e)
        {
            ListViewItem draggedObject = e.Data.GetData(typeof(ListViewItem)) as ListViewItem;
            ListViewItem targetObject = ((ListViewItem)(sender));

            var drag = draggedObject.Content as Performer;
            var drop = targetObject.Content as Performer;
            int dragIdx = BardsList.Items.IndexOf(drag);
            int dropIdx = BardsList.Items.IndexOf(drop);

            if (drag == drop)
                return;

            SortedDictionary<int, Performer> newBardsList = new SortedDictionary<int, Performer>();
            int index = 0;
            foreach (var p in BardsList.Items)
            {
                if ((Performer)p == drag)
                    continue;

                if ((Performer)p == drop)
                {
                    if (dropIdx < dragIdx)
                    {
                        newBardsList.Add(index, drag); index++;
                        newBardsList.Add(index, drop); index++;
                    }
                    else if (dropIdx > dragIdx)
                    {
                        newBardsList.Add(index, drop); index++;
                        newBardsList.Add(index, drag); index++;
                    }
                }
                else
                {
                    newBardsList.Add(index, p as Performer);
                    index++;
                }
            }

            BardsList.ItemsSource = null;
            BardsList.Items.Clear();

            foreach (var p in newBardsList)
                BardsList.Items.Add(p.Value);

            newBardsList.Clear();
        }
        #endregion

        /* Track UP/Down */
        private void TrackNumericUpDown_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TrackNumericUpDown ctl = sender as TrackNumericUpDown;
            ctl.OnValueChanged += OnValueChanged;
        }

        private static void OnValueChanged(object sender, int s)
        {
            Performer game = (sender as TrackNumericUpDown).DataContext as Performer;
            BmpMaestro.Instance.SetTracknumber(game, s);

            TrackNumericUpDown ctl = sender as TrackNumericUpDown;
            ctl.OnValueChanged -= OnValueChanged;
        }

        /* Octave UP/Down */
        private void OctaveControl_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OctaveNumericUpDown ctl = sender as OctaveNumericUpDown;
            ctl.OnValueChanged += OnOctaveValueChanged;
        }

        private static void OnOctaveValueChanged(object sender, int s)
        {
            Performer performer = (sender as OctaveNumericUpDown).DataContext as Performer;
            BmpMaestro.Instance.SetOctaveshift(performer, s);

            OctaveNumericUpDown ctl = sender as OctaveNumericUpDown;
            ctl.OnValueChanged -= OnOctaveValueChanged;
        }

        private void HostChecker_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox ctl = sender as CheckBox;
            if (!ctl.IsChecked ?? false)
                return;

            var game = (sender as CheckBox).DataContext as Performer;
            BmpMaestro.Instance.SetHostBard(game);
        }

        private void PerfomerEnabledChecker_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox ctl = sender as CheckBox;
            var game = (sender as CheckBox).DataContext as Performer;
            game.PerformerEnabled = ctl.IsChecked ?? false;
        }

        private void StartDelay_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.EnsemblePlayDelay = StartDelay_CheckBox.IsChecked ?? true;
        }

        private void Bard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
                return;
            if (SelectedBard == null)
                return;

            var bardExtSettings = new BardExtSettingsWindow(SelectedBard);
            bardExtSettings.Activate();
            bardExtSettings.Visibility = Visibility.Visible;
        }

        private void Autoequip_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.AutoEquipBards = Autoequip_CheckBox.IsChecked ?? false;
            Globals.Globals.ReloadConfig();
        }

        /// <summary>
        /// load the performer config file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Load_Performer_Settings(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Performer Config | *.cfg",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            MemoryStream memoryStream = new MemoryStream();
            FileStream fileStream = File.Open(openFileDialog.FileName, FileMode.Open);
            fileStream.CopyTo(memoryStream);
            fileStream.Close();

            var data = memoryStream.ToArray();
            List<PerformerSettingData> pdatalist = JsonConvert.DeserializeObject<List<PerformerSettingData>>(new UTF8Encoding(true).GetString(data));
            foreach (var pconfig in pdatalist)
            {
                var p = BardsList.Items.OfType<Performer>().ToList().Where(perf => perf.game.ConfigId.Equals(pconfig.CID));
                if (p.Count() == 0)
                {
                    p = BardsList.Items.OfType<Performer>().ToList().Where(perf => perf.game.PlayerName.Equals(pconfig.Name));
                    if (p.Count() == 0)
                        continue;
                }

                p.First().TrackNumber = pconfig.Track;
                if (pconfig.AffinityMask != 0)
                    p.First().game.SetAffinity(pconfig.AffinityMask);
            
                if (GameExtensions.SetSoundOnOff(p.First().game, pconfig.IsInGameSoundEnabled).Result)
                    p.First().game.SoundOn = pconfig.IsInGameSoundEnabled;
            }

            List<Performer> tempPerf = new List<Performer>(BardsList.Items.OfType<Performer>().ToList());
            
            //Reorder if there is no -1 OrderNum
            if (pdatalist.Where(p => p.OrderNum == -1).Count() == 0)
            {
                pdatalist.Sort((x, y) => x.OrderNum.CompareTo(y.OrderNum));
                BardsList.Items.Clear();
                foreach (var pconfig in pdatalist)
                {
                    var p = tempPerf.Where(perf => perf.game.ConfigId.Equals(pconfig.CID));
                    if (p.Count() != 0)
                        BardsList.Items.Add(p.First());
                }
            }
            
            //Set the host performer
            /*var host_perf = pdatalist.Where(p => p.IsHost);
            if (host_perf.Count() != 0)
            {
                BmpMaestro.Instance.SetHostBard(tempPerf.Find(p => p.game.ConfigId == host_perf.First().CID));
                BmpMaestro.Instance.SetTracknumber(tempPerf.Find(p => p.game.ConfigId == host_perf.First().CID), host_perf.First().Track);
            }*/
            tempPerf.Clear();
            pdatalist.Clear();

            //Set Thymms box, cuz if u use this function, you know what you are doing
            if (BmpPigeonhole.Instance.EnsembleKeepTrackSetting)
                return;

            BmpPigeonhole.Instance.EnsembleKeepTrackSetting = true;
            Globals.Globals.ReloadConfig();

            tempPerf = BardsList.Items.OfType<Performer>().ToList();
            foreach (var perf in tempPerf)
            {
                if (!perf.game.SoundOn && !perf.HostProcess)
                    Mute_CheckBox.IsChecked = true;
            }
        }

        /// <summary>
        /// save the performer config file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Performer_Settings(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Performer Config | *.cfg"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            List<PerformerSettingData> pdatalist = new List<PerformerSettingData>();
            foreach (var performer in BardsList.Items.OfType<Performer>().ToList())
            {
                PerformerSettingData pdata = new PerformerSettingData();
                pdata.OrderNum = BardsList.Items.IndexOf(performer);
                pdata.CID = performer.game.ConfigId;
                pdata.Name = performer.game.PlayerName;
                pdata.Track = performer.TrackNumber;
                pdata.AffinityMask = (long)performer.game.GetAffinity();
                pdata.IsHost = performer.HostProcess;
                pdata.IsInGameSoundEnabled = performer.game.SoundOn;
                pdatalist.Add(pdata);
            }
            var t = JsonConvert.SerializeObject(pdatalist);
            byte[] content = new UTF8Encoding(true).GetBytes(t);

            FileStream fileStream = File.Create(openFileDialog.FileName);
            fileStream.Write(content, 0, content.Length);
            fileStream.Close();
            pdatalist.Clear();
        }


        /// <summary>
        /// Opens the QEdit with the current loaded song
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenQuickEdit_Button(object sender, RoutedEventArgs e)
        {
            if (_QuickEdit == null)
            {
                _QuickEdit = new MidiBardConverterWindow();
                _QuickEdit.Closing += new CancelEventHandler(QuickEditWindow_Closing);

            }
            _QuickEdit.Visibility = Visibility.Visible;
            _QuickEdit.MidiFromSong(PlaybackFunctions.CurrentSong);
        }

        void QuickEditWindow_Closing(object sender, CancelEventArgs e)
        {
            _QuickEdit = null;
        }

        private void GfxLow_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var p in BardsList.Items.OfType<Performer>().ToList().Where(p => p.game.GfxSettingsLow != GfxLow_CheckBox.IsChecked))
            {
                p.game.GfxSettingsLow = GfxLow_CheckBox.IsChecked ?? false;
                p.game.GfxSetLow(GfxLow_CheckBox.IsChecked ?? false);
            }
        }

        private void Mute_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var p in BardsList.Items.OfType<Performer>().ToList().Where(p => !p.HostProcess))
            {
                p.game.SoundOn = !Mute_CheckBox.IsChecked ?? false;
                p.game.SetSoundOnOff(!Mute_CheckBox.IsChecked ?? false);
            }
        }

        /// <summary>
        /// Window pos load button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ArrangeWindow_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "WindowLayout | *.txt",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            ArrangeWindows(openFileDialog.FileName);
        }

        /// <summary>
        /// Arrange the window position and size
        /// </summary>
        private void ArrangeWindows(string filename)
        {
            if (BardsList.Items.OfType<Performer>().ToList().Count == 0)
                return;
            int x = 0;
            int y = 0;
            int size_x = 0;
            int size_y = 0;
            StreamReader reader = new StreamReader(filename);
            string input = reader.ReadLine();
            if (input.Split(':')[0].Contains("Size"))
            {
                size_x = Convert.ToInt32(input.Split(':')[1].Split('x')[0]);
                size_y = Convert.ToInt32(input.Split(':')[1].Split('x')[1]);
            }

            String line;
            while ((line = reader.ReadLine()) != null)
            {
                x = 0;
                for (int i = 0; i < line.Length;)
                {
                    String value = line[i].ToString() + line[i + 1].ToString();
                    i = i + 2;
                    if (value != "--")
                    {
                        var bard = BardsList.Items.OfType<Performer>().ToList().Where(p => p.TrackNumber == Convert.ToInt32(value)).FirstOrDefault();
                        if (bard == null)
                            continue;
                        bard.game.SetWindowPosAndSize(x, y, size_x, size_y, true);
                    }
                    x = x + size_x;
                }
                y = y + size_y;
            }
            reader.Close();
        }

        /// <summary>
        /// Button context menu routine
        /// </summary>
        private void MenuButton_PreviewMouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            Button rectangle = sender as Button;
            ContextMenu contextMenu = rectangle.ContextMenu;
            contextMenu.PlacementTarget = rectangle;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }
    }

    /// <summary>
    /// Helperclass
    /// </summary>
    public sealed class PerformerSettingData
    {
        public string CID { get; set; } = "None";
        public int OrderNum { get; set; } = -1;
        public string Name { get; set; } = "";
        public int Track { get; set; } = 0;
        public long AffinityMask { get; set; } = 0;
        public bool IsHost { get; set; } = false;
        public bool IsInGameSoundEnabled { get; set; } = true;
    }
}
