/*
 * Copyright(c) 2023 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Ui.Classic;
using System.Windows;
using BardMusicPlayer.Pigeonhole;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Diagnostics;
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
            this.Title = "LightAmp Ver:" + Assembly.GetExecutingAssembly().GetName().Version + " - Beelzebub";
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
            try
            {
                this.AllowsTransparency = true;
                this.Height = 174;
                this.Width = 412;
                this.ResizeMode = ResizeMode.NoResize;
                var dll = Assembly.LoadFile(BmpPigeonhole.Instance.LastSkin);
                
                //get dll refs
                string loadir = Path.GetDirectoryName(BmpPigeonhole.Instance.LastSkin);
                var deps = Assembly.GetExecutingAssembly().GetReferencedAssemblies().Select(n=>n.Name);

                foreach (var n in Directory.GetFiles(loadir))
                {
                    if (!n.EndsWith(".dll"))
                        continue;
                    string file = Path.GetFileNameWithoutExtension(n);
                    if (file == "SkinnedUi")
                        continue;
                    if (file.Contains("Melanchall_DryWetMidi_Native"))
                        continue;
                    if (deps.Contains(file))
                        continue;
                    Assembly.LoadFile(loadir + "\\" + file + ".dll");
                }
                //init the skin
                var type = dll.GetType("Skin.Ui.Skinned.Skin_MainView");
                var runnable = Activator.CreateInstance(type);
                this.DataContext = runnable;
                Application.Current.MainWindow = this;
            }
            catch
            {
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
    }
}
