/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System.Windows;
using BardMusicPlayer.Coffer;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Seer;
using BardMusicPlayer.Maestro;
using System.Diagnostics;
using BardMusicPlayer.Siren;
using BardMusicPlayer.Jamboree;
using BardMusicPlayer.Script;
using System.Globalization;
using System;

namespace BardMusicPlayer.Ui
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public sealed partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            SplashScreen splashScreen = new SplashScreen("/Resources/Images/splash.jpg");
            splashScreen.Show(true);

            Globals.Globals.DataPath = @"data\";

            //init pigeon at first
            BmpPigeonhole.Initialize(Globals.Globals.DataPath + @"\Configuration.json");

            // LogManager.Initialize(new(view.Log));

            //Load the last used catalog
            string CatalogFile = BmpPigeonhole.Instance.LastLoadedCatalog;
            if (System.IO.File.Exists(CatalogFile))
                BmpCoffer.Initialize(CatalogFile);
            else
                BmpCoffer.Initialize(Globals.Globals.DataPath + @"\MusicCatalog.db");

            //Setup seer
            BmpSeer.Instance.SetupFirewall("BardMusicPlayer");
            //Start meastro before seer, else we'll not get all the players
            BmpMaestro.Instance.Start();
            //Start seer
            BmpSeer.Instance.Start();

            DalamudBridge.DalamudBridge.Instance.Start();

            //Start the scripting
            BmpScript.Instance.Start();

            BmpSiren.Instance.Setup();
            //BmpJamboree.Instance.Start();
            ConfigureLanguage(System.Threading.Thread.CurrentThread.CurrentUICulture.ToString());
        }

        protected override void OnExit(ExitEventArgs e)
        {
            //LogManager.Shutdown();
            BmpJamboree.Instance.Stop();
            if (BmpSiren.Instance.IsReadyForPlayback)
                BmpSiren.Instance.Stop();
            BmpSiren.Instance.ShutDown();
            BmpMaestro.Instance.Stop();

            BmpScript.Instance.Stop();

            DalamudBridge.DalamudBridge.Instance.Stop();
            BmpSeer.Instance.Stop();
            BmpSeer.Instance.DestroyFirewall("BardMusicPlayer");
            BmpCoffer.Instance.Dispose();
            BmpPigeonhole.Instance.Dispose();

            //Wasabi hangs kill it with fire
            Process.GetCurrentProcess().Kill();
        }
        internal static void ConfigureLanguage(string langCode = null)
        {
            try
            {
                Locales.Language.Culture = new CultureInfo(langCode);
            }
            catch (Exception)
            {
                Locales.Language.Culture = CultureInfo.DefaultThreadCurrentUICulture;
            }
        }
    }
}
