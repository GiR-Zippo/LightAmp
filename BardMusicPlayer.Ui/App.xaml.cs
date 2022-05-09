
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using BardMusicPlayer.Coffer;
using BardMusicPlayer.Grunt;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Seer;
using BardMusicPlayer.Maestro;
using System.Diagnostics;
using BardMusicPlayer.Siren;
using BardMusicPlayer.Jamboree;

namespace BardMusicPlayer.Ui
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            Globals.Globals.DataPath = @"data\";

            BmpPigeonhole.Initialize(Globals.Globals.DataPath + @"\Configuration.json");

            // var view = (MainView)View;
            // LogManager.Initialize(new(view.Log));

            BmpCoffer.Initialize(Globals.Globals.DataPath + @"\MusicCatalog.db");
            BmpSeer.Instance.SetupFirewall("BardMusicPlayer");
            BmpSeer.Instance.Start();
            BmpGrunt.Instance.Start();
            BmpMaestro.Instance.Start();
            BmpSiren.Instance.Setup();
            BmpJamboree.Instance.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            //LogManager.Shutdown();
            BmpJamboree.Instance.Stop();
            if (BmpSiren.Instance.IsReadyForPlayback)
                BmpSiren.Instance.Stop();
            BmpSiren.Instance.ShutDown();
            BmpMaestro.Instance.Stop();
            BmpGrunt.Instance.Stop();
            BmpSeer.Instance.Stop();
            BmpSeer.Instance.DestroyFirewall("BardMusicPlayer");
            BmpCoffer.Instance.Dispose();
            BmpPigeonhole.Instance.Dispose();

            //Wasabi hangs kill it with fire
            Process.GetCurrentProcess().Kill();
        }
    }
}
