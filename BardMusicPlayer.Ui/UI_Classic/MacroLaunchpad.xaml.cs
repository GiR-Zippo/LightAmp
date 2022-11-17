using BardMusicPlayer.Transmogrify.Song.Importers;
using BardMusicPlayer.Ui.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Windows;
using static BasicSharp.Interpreter;

namespace BardMusicPlayer.Ui.Classic
{
    public class Macro
    {
        public string DisplayedText { get; set; } = "";
        public string File { get; set; } = "";
    }

    public partial class MacroLaunchpad : Window
    {
        public ObservableCollection<Macro> _Macros { get; private set; }
        public Macro SelectedMacro { get; set; }

        public MacroLaunchpad()
        {
            InitializeComponent();

            this.DataContext = this;
            _Macros = new ObservableCollection<Macro>();
            _Macros.CollectionChanged += Macros_CollectionChanged;
        }

        private void Macros_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.MacroList.ItemsSource = _Macros;
            this.MacroList.Items.Refresh();
        }

        private void MacroList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            SelectedMacro = MacroList.SelectedItem as Macro;
        }

        private void MacroList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectedMacro = MacroList.SelectedItem as Macro;
            Debug.WriteLine(SelectedMacro.File);

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
            if (SelectedMacro == null)
                SelectedMacro = new Macro();

            MacroEditWindow macroEdit = new MacroEditWindow(SelectedMacro);
            macroEdit.Visibility = Visibility.Visible;
            macroEdit.Closed += MacroEdit_Closed;
            _Macros.Add(SelectedMacro);
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            _Macros.Remove(SelectedMacro);
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
        }


        private void MacroEdit_Closed(object sender, System.EventArgs e)
        {
            this.MacroList.Items.Refresh();
        }


    }
}
