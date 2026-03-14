/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Script;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace BardMusicPlayer.Ui.Windows
{
    public sealed class Macro
    {
        public string DisplayedText { get; set; } = "";
        public string File { get; set; } = "";
        public bool Running { get; set; } = false;
        public string Uid { get; set; } = "";
    }

    public sealed partial class MacroLaunchpad : Window
    {
        public List<Macro> Macros { get; private set; } = new List<Macro>();

        public Macro SelectedMacro { get; set; }

        public MacroLaunchpad()
        {
            InitializeComponent();
            BmpScript.Instance.OnRunningStateChanged += Instance_OnRunningStateChanged;

            this.DataContext = this;
        }

        private void Instance_OnRunningStateChanged(object sender, KeyValuePair<string, bool> e)
        {
            if (e.Key.Length == 0)
            {
                Dispatcher.BeginInvoke(e.Value
                    ? new Action(() => { StopIndicator.Content = "Stop"; })
                    : () => StopIndicator.Content = "Idle");
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() => {
                    var macro = Macros.Find(s => s.File == e.Key.Split('@')[1]);
                    if (macro == null)
                        return;
                    macro.Running = e.Value;
                    macro.Uid = e.Value ? e.Key : "";
                    this.MacroList.Items.Refresh();
                }));
            }
        }

        private void Macros_CollectionChanged()
        {
            this.MacroList.Items.Refresh();
        }

        private void MacroList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            SelectedMacro = MacroList.SelectedItem as Macro;
        }

        private void MacroList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SelectedMacro == null)
                return;

            SelectedMacro = MacroList.SelectedItem as Macro;
            if (!File.Exists(SelectedMacro.File))
                return;
            if (SelectedMacro.Running)
                return;
            BmpScript.Instance.LoadAndRun(SelectedMacro.File);
        }

        private void TextBlock_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SelectedMacro == null)
                return;

            MacroEditWindow macroEdit = new MacroEditWindow(SelectedMacro);
            macroEdit.Visibility = Visibility.Visible;
            macroEdit.Closed += MacroEdit_Closed;
        }

        private void MacroRunning_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SelectedMacro == null)
                return;

            SelectedMacro = MacroList.SelectedItem as Macro;
            BmpScript.Instance.StopExecution(SelectedMacro.Uid);
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var newMacro = new Macro();

            MacroEditWindow macroEdit = new MacroEditWindow(newMacro);
            macroEdit.Visibility = Visibility.Visible;
            macroEdit.Closed += MacroEdit_Closed;
            Macros.Add(newMacro);
            Macros_CollectionChanged();
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedMacro == null)
                return;
            Macros.Remove(SelectedMacro);
            SelectedMacro = null;
            Macros_CollectionChanged();
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Macro List | *.cfg",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            MemoryStream memoryStream = new MemoryStream();
            FileStream fileStream = File.Open(openFileDialog.FileName, FileMode.Open);
            fileStream.CopyTo(memoryStream);
            fileStream.Close();

            var data = memoryStream.ToArray();
            Macros.Clear();
            var x = JsonConvert.DeserializeObject<List<Macro>>(new UTF8Encoding(true).GetString(data));
            foreach (var m in x)
                Macros.Add(m);

            Macros_CollectionChanged();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (Macros.Count <= 0)
                return;

            var openFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Macro List | *.cfg"
            };

            if (openFileDialog.ShowDialog() != true)
                return;


            string json = JsonConvert.SerializeObject(MacroList.Items.OfType<Macro>()
                .Select(macro => new Macro
                {
                    DisplayedText = macro.DisplayedText,
                    File = macro.File,
                    Uid = "",
                    Running = false
                }).ToList());

            byte[] content = new UTF8Encoding(true).GetBytes(json);

            FileStream fileStream = File.Create(openFileDialog.FileName);
            fileStream.Write(content, 0, content.Length);
            fileStream.Close();

            Macros_CollectionChanged();
        }

        private void MacroEdit_Closed(object sender, System.EventArgs e)
        {
            Macros_CollectionChanged();
        }

        private void StopIndicator_Click(object sender, RoutedEventArgs e)
        {
            BmpScript.Instance.StopExecution();
        }
    }
}
