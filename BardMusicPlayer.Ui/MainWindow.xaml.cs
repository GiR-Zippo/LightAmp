/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Ui.Classic;
using System.Windows;
using BardMusicPlayer.Pigeonhole;
using System.Reflection;
using System;
using System.IO;
using System.Linq;

namespace BardMusicPlayer.Ui
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Title = "LightAmp Ver:" + Assembly.GetExecutingAssembly().GetName().Version + " - Ami Tsuruga";
            if (BmpPigeonhole.Instance.ClassicUi)
                SwitchClassicStyle();
            else
                SwitchSkinnedStyle();
        }

        public void SwitchClassicStyle()
        {
            this.DataContext = new Classic_MainView();
            this.AllowsTransparency = false;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.Height = 500;
            this.Width = 830;
            this.ResizeMode = ResizeMode.CanResizeWithGrip;
        }

        public void SwitchSkinnedStyle()
        {
            if (BmpPigeonhole.Instance.LastSkin == "")
            {
                SwitchClassicStyle();
                return;
            }

            try
            {
                this.AllowsTransparency = true;
                this.Height = 174;
                this.Width = 412;
                this.ResizeMode = ResizeMode.NoResize;

                var dll = Assembly.LoadFile(BmpPigeonhole.Instance.LastSkin);
                if (!loadSkinDLLDepencies())
                {
                    SwitchClassicStyle();
                    return;
                }
                //init the skin
                var type = dll.GetType("Skin.Ui.Skin_MainView");
                var runnable = Activator.CreateInstance(type);
                this.DataContext = runnable;
                Application.Current.MainWindow = this;
            }
            catch (FileNotFoundException)
            {
                var retval = MessageBox.Show("Skin not found.\r\nUsing default Ui.", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes, MessageBoxOptions.DefaultDesktopOnly);
                if (retval == MessageBoxResult.OK)
                    SwitchClassicStyle();
            }
            catch(TargetInvocationException e)
            {
                var retval = MessageBox.Show("Skin error:\r\n"
                                             + e.Message + "\r\n"
                                             + e.InnerException + "\r\n"
                                             + e.HelpLink+ "\r\n", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes, MessageBoxOptions.DefaultDesktopOnly);
                if (retval == MessageBoxResult.OK)
                    SwitchClassicStyle();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            for (int intCounter = App.Current.Windows.Count - 1; intCounter >= 0; intCounter--)
            {
                try { App.Current.Windows[intCounter].Close(); }
                catch { }
            }
            base.OnClosing(e);
        }

        private bool loadSkinDLLDepencies()
        {
            //get dll refs, ignore the ones we already have
            string loadir = Path.GetDirectoryName(BmpPigeonhole.Instance.LastSkin);
            var deps = Assembly.GetExecutingAssembly().GetReferencedAssemblies().Select(n => n.Name);

            foreach (var n in Directory.GetFiles(loadir))
            {
                if (!n.EndsWith(".dll"))
                    continue;
                string file = Path.GetFileNameWithoutExtension(n);
                if (file == "Skin")
                    continue;
                if (deps.Contains(file))
                    continue;
                try
                {
                    Assembly.LoadFile(loadir + "\\" + file + ".dll");
                }
                catch (BadImageFormatException)
                { }
                catch (Exception e)
                {
                    var retval = MessageBox.Show("Error loading skin depency DLL:\r\n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes, MessageBoxOptions.DefaultDesktopOnly);
                    if (retval == MessageBoxResult.OK)               
                        return false;
                }
            }
            return true;
        }
    }
}
