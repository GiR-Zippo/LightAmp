using BardMusicPlayer.Maestro;
using BardMusicPlayer.Maestro.Performance;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// Interaktionslogik für BardExtSettings.xaml
    /// </summary>
    public partial class BardExtSettings : Window
    {
        private Performer _performer = null;
        private List<CheckBox> _cpuBoxes = new List<CheckBox>();

        public BardExtSettings(Performer performer)
        {
            _performer = performer;
            InitializeComponent();
            Title = "Settings for: " + _performer.PlayerName;

            if (BmpMaestro.Instance.GetSongTitleParsingBard() == null)
                PostSongTitle.IsChecked = false;
            else
            {
                if (BmpMaestro.Instance.GetSongTitleParsingBard().PId == _performer.PId)
                    PostSongTitle.IsChecked = true;
                else
                    PostSongTitle.IsChecked = false;
            }

            this.Singer.IsChecked = performer.IsSinger;

            PopulateCPUTab();

        }

        private void ChatInputText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ComboBoxItem t = Chat_Type.SelectedValue as ComboBoxItem;
                string chattype = "";
                if (t != null)
                    chattype = (string)t.Content;

                _performer.SendText("/"+chattype+" "+ ChatInputText.Text);
            }
        }

        private void PostSongTitle_Checked(object sender, RoutedEventArgs e)
        {
            ComboBoxItem t = Chat_Type.SelectedValue as ComboBoxItem;
            string chattype = "";
            if (t != null)
                chattype = (string)t.Content;

            if ((bool)PostSongTitle.IsChecked)
                BmpMaestro.Instance.SetSongTitleParsingBard("/"+chattype, _performer);
            else
                BmpMaestro.Instance.SetSongTitleParsingBard("", null);
        }

        private void Singer_Checked(object sender, RoutedEventArgs e)
        {
            _performer.IsSinger = (bool)Singer.IsChecked;
        }

        #region CPU-Tab
        private void PopulateCPUTab()
        {
            //Get the our application's process.
            Process process = _performer.game.Process;

            //Get the processor count of our machine.
            int cpuCount = Environment.ProcessorCount;
            long AffinityMask = (long)_performer.game.GetAffinity();

            int res = (int)Math.Ceiling((double)cpuCount / (double)3);
            int idx = 1;
            for (int col = 0; col != 3; col++)
            {
                CPUDisplay.ColumnDefinitions.Add(new ColumnDefinition());
                
                for (int i = 0; i != res + 1; i++)
                {
                    if (idx == cpuCount+1)
                        break;
                    if (CPUDisplay.RowDefinitions.Count < res +1)
                        CPUDisplay.RowDefinitions.Add(new RowDefinition());
                    var uc = new CheckBox();
                    uc.Name = "CPU" + idx;
                    uc.Content = "CPU" + idx;
                    if ((AffinityMask & (1 << idx-1)) > 0) //-1 since we count at 1
                        uc.IsChecked = true;
                    _cpuBoxes.Add(uc);
                    CPUDisplay.Children.Add(uc);
                    Grid.SetRow(uc, i);
                    Grid.SetColumn(uc, CPUDisplay.ColumnDefinitions.Count - 1);
                    idx++;
                }
            }
        }
        #endregion

        private void Save_CPU_Click(object sender, RoutedEventArgs e)
        {
            long mask = 0;
            int idx = 0;
            foreach (CheckBox box in _cpuBoxes)
            {
                if ((bool)box.IsChecked)
                    mask += 0b1 << idx;
                else
                    mask += 0b0 << idx;
                idx++;
            }
            _performer.game.SetAffinity(mask);
        }

        private void Clear_CPU_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox box in _cpuBoxes)
            {
                box.IsChecked = false;
            }
        }

        private void Reset_CPU_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox box in _cpuBoxes)
            {
                box.IsChecked = true;
            }
        }
    }
}
