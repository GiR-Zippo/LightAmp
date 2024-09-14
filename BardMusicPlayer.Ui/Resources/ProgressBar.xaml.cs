using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace BardMusicPlayer.Ui.Resources
{
    /// <summary>
    /// Interaktionslogik für MessageWindow.xaml
    /// </summary>
    public partial class ProgressBar : Window
    {
        public static Dispatcher WindowDispatcher { get; set; } = null;

        public static void Show(string content = "", string Htitle = "")
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                new ProgressBar(content, Htitle);
            });
        }

        public static float GetPercent(float val, float max)
        {
            return ((val+1) / max) * (float)100;
        }

        public static ProgressBar win;

        public ProgressBar(string content, string Htitle)
        {
            InitializeComponent();
            WindowDispatcher = this.Dispatcher;
            win = this;
            this.Visibility = Visibility.Visible;
            this.Title = Htitle;
            tContent.Text = content;
        }

        public static void Update(double val)
        {
            if (WindowDispatcher == null)
                return;
            WindowDispatcher.Invoke(new Action(() =>
            {
                win.pgProcessing.Value = val;
            }));
        }

        public static void WndClose()
        {
            if (WindowDispatcher == null)
                return;
            WindowDispatcher.Invoke(new Action(() =>
            {
                win.Close();
            }));
        }

        #region WindowEvents
        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion
    }
}
