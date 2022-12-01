using BardMusicPlayer.Script;
using BardMusicPlayer.Ui.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace BardMusicPlayer.Ui.Classic
{
    public class Macro
    {
        public string DisplayedText { get; set; } = "";
        public string File { get; set; } = "";
    }

    public partial class MacroLaunchpad : Window
    {
        public List<Macro> _Macros { get; private set; }
        public Macro SelectedMacro { get; set; }

        public MacroLaunchpad()
        {
            InitializeComponent();
            BmpScript.Instance.OnRunningStateChanged += Instance_OnRunningStateChanged;


            this.DataContext = this;
            _Macros = new List<Macro>();
            MacroList.ItemsSource = _Macros;
        }

        private void Instance_OnRunningStateChanged(object sender, bool e)
        {
            if (e)
                this.Dispatcher.BeginInvoke(new Action(() => StopIndicator.Content = "Stop" ));
            else
                this.Dispatcher.BeginInvoke(new Action(() => StopIndicator.Content = "Idle"));
        }

        private void Macros_CollectionChanged()
        {
            MacroList.ItemsSource = _Macros;
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
            
            Script.BmpScript.Instance.LoadAndRun(SelectedMacro.File);
        }

        private void TextBlock_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SelectedMacro == null)
                return;

            MacroEditWindow macroEdit = new MacroEditWindow(SelectedMacro);
            macroEdit.Visibility = Visibility.Visible;
            macroEdit.Closed += MacroEdit_Closed;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var newMacro = new Macro();

            MacroEditWindow macroEdit = new MacroEditWindow(newMacro);
            macroEdit.Visibility = Visibility.Visible;
            macroEdit.Closed += MacroEdit_Closed;
            _Macros.Add(newMacro);
            Macros_CollectionChanged();
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedMacro == null)
                return;
            _Macros.Remove(SelectedMacro);
            SelectedMacro = null;
            Macros_CollectionChanged();
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Macrolist | *.cfg",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            MemoryStream memoryStream = new MemoryStream();
            FileStream fileStream = File.Open(openFileDialog.FileName, FileMode.Open);
            fileStream.CopyTo(memoryStream);
            fileStream.Close();

            var data = memoryStream.ToArray();
            _Macros.Clear();
            var x = JsonConvert.DeserializeObject<List<Macro>>(new UTF8Encoding(true).GetString(data));
            foreach (var m in x)
                _Macros.Add(m);

            Macros_CollectionChanged();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_Macros.Count <= 0)
                return;

            var openFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Macrolist | *.cfg"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            var t = JsonConvert.SerializeObject(_Macros);
            byte[] content = new UTF8Encoding(true).GetBytes(t);

            FileStream fileStream = File.Create(openFileDialog.FileName);
            fileStream.Write(content, 0, content.Length);
            fileStream.Close();

            Macros_CollectionChanged();
        }

        private void MacroEdit_Closed(object sender, System.EventArgs e)
        {
            this.MacroList.Items.Refresh();
        }

        private void StopIndicator_Click(object sender, RoutedEventArgs e)
        {
            BmpScript.Instance.StopExecution();
        }
    }
}
