using BardMusicPlayer.Maestro;
using BardMusicPlayer.Maestro.Events;
using BardMusicPlayer.Maestro.Performance;
using BardMusicPlayer.Seer;
using BardMusicPlayer.Seer.Events;
using BardMusicPlayer.Ui.Functions;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;


namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// Interaktionslogik für BardView.xaml
    /// </summary>
    public partial class BardView : UserControl
    {
        public BardView()
        {
            InitializeComponent();
            this.DataContext = this;
            Bards = new ObservableCollection<Performer>();

            BmpMaestro.Instance.OnPerformerChanged += OnPerfomerChanged;
            BmpMaestro.Instance.OnTrackNumberChanged += OnTrackNumberChanged;
            BmpMaestro.Instance.OnOctaveShiftChanged += OnOctaveShiftChanged;
            BmpMaestro.Instance.OnSongLoaded += OnSongLoaded;
            BmpSeer.Instance.PlayerNameChanged += OnPlayerNameChanged;
            BmpSeer.Instance.InstrumentHeldChanged += OnInstrumentHeldChanged;
            BmpSeer.Instance.HomeWorldChanged += OnHomeWorldChanged;
        }

        public ObservableCollection<Performer> Bards { get; private set; }

        public Performer SelectedBard { get; set; }

        private void OnPerfomerChanged(object sender, bool e)
        {
            this.Bards = new ObservableCollection<Performer>(BmpMaestro.Instance.GetAllPerformers());
            this.Dispatcher.BeginInvoke(new Action(() => this.BardsList.ItemsSource = Bards));
        }

        private void OnTrackNumberChanged(object sender, TrackNumberChangedEvent e)
        {
            UpdateList();
        }

        private void OnOctaveShiftChanged(object sender, OctaveShiftChangedEvent e)
        {
            UpdateList();
        }

        private void OnSongLoaded(object sender, SongLoadedEvent e)
        {
            UpdateList();
        }

        private void OnPlayerNameChanged(PlayerNameChanged e)
        {
            UpdateList();
        }

        private void OnHomeWorldChanged(HomeWorldChanged e)
        {
            UpdateList();
        }

        private void OnInstrumentHeldChanged(InstrumentHeldChanged e)
        {
            UpdateList();
        }

        private void UpdateList()
        {
            this.Bards = new ObservableCollection<Performer>(BmpMaestro.Instance.GetAllPerformers());
            this.Dispatcher.BeginInvoke(new Action(() => this.BardsList.ItemsSource = Bards));
        }


        private void OpenInstrumentButton_Click(object sender, RoutedEventArgs e)
        {
            BmpMaestro.Instance.EquipInstruments();
        }

        private void CloseInstrumentButton_Click(object sender, RoutedEventArgs e)
        {
            BmpMaestro.Instance.UnEquipInstruments();
        }

        private void BardsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Console.WriteLine(this.BardsList.SelectedItem);
        }

        private void BardsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectedBard = BardsList.SelectedItem as Performer;
        }

        /* Track UP/Down */
        private void TrackNumericUpDown_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TrackNumericUpDown ctl = sender as TrackNumericUpDown;
            ctl.OnValueChanged += OnValueChanged;
        }

        private void OnValueChanged(object sender, int s)
        {
            Performer game = (sender as TrackNumericUpDown).DataContext as Performer;
            BmpMaestro.Instance.SetTracknumber(game, s);
        }

        private void OctaveControl_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OctaveNumericUpDown ctl = sender as OctaveNumericUpDown;
            ctl.OnValueChanged += OnOctaveValueChanged;
        }

        private void OnOctaveValueChanged(object sender, int s)
        {
            Performer performer = (sender as OctaveNumericUpDown).DataContext as Performer;
            BmpMaestro.Instance.SetOctaveshift(performer, s);
        }

        private void HostChecker_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox ctl = sender as CheckBox;
            if (!ctl.IsChecked ?? false)
                return;

            var game = (sender as CheckBox).DataContext as Performer;
            BmpMaestro.Instance.SetHostBard(game);
        }
    }
}
