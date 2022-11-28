/*
 * Copyright(c) 2022 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/BardMusicPlayer/BardMusicPlayer/blob/develop/LICENSE for full license information.
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BardMusicPlayer.Maestro;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Seer;
using BasicSharp;

namespace BardMusicPlayer.Script
{
    public class BmpScript
    {
        private static readonly Lazy<BmpScript> LazyInstance = new(() => new BmpScript());

        /// <summary>
        /// 
        /// </summary>
        public bool Started { get; private set; }

        private BmpScript()
        {
        }
        public static BmpScript Instance => LazyInstance.Value;

        public event EventHandler<bool> OnRunningStateChanged;

        private Thread thread = null;
        private Interpreter basic = null;

        private int selectedBard        { get; set; } = 0;
        private string selectedBardName { get; set; } = "";

#region Routine Handlers

        public void SetSelectedBard(int num)
        {
            selectedBardName = "";
            selectedBard = num;
        }

        public void SetSelectedBardName(string name)
        {
            selectedBard = -1;
            selectedBardName = name;
        }

        public void Print(Quotidian.Structs.ChatMessageChannelType type, string text)
        {
            if (selectedBard != -1)
                BmpMaestro.Instance.SendText(selectedBard, type, text);
            else
                BmpMaestro.Instance.SendText(selectedBardName, type, text);
        }

#endregion

#region accessors
        public void StopExecution()
        {
            if (thread == null)
                return;
            if (basic == null)
                return;

            basic.StopExec();

            if (thread.ThreadState == ThreadState.Running)
                thread.Abort();
        }

#endregion 

        public void LoadAndRun(string basicfile)
        {
            Task task = Task.Run(() =>
            {
                thread = Thread.CurrentThread;
                if (OnRunningStateChanged != null)
                    OnRunningStateChanged(this, true);
                basic = new Interpreter(File.ReadAllText(basicfile));
                basic.printHandler += Print;
                basic.selectedBardHandler += SetSelectedBard;
                basic.selectedBardAsStringHandler += SetSelectedBardName;
                try
                {
                    basic.Exec();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error");
                }
                if (OnRunningStateChanged != null)
                    OnRunningStateChanged(this, false);

                basic.printHandler -= Print;
                basic.selectedBardHandler -= SetSelectedBard;
                basic.selectedBardAsStringHandler -= SetSelectedBardName;
                basic = null;
            });
        }

        /// <summary>
        /// Start Script.
        /// </summary>
        public void Start()
        {
            if (Started) return;
            if (!BmpPigeonhole.Initialized) throw new BmpScriptException("Script requires Pigeonhole to be initialized.");
            if (!BmpSeer.Instance.Started) throw new BmpScriptException("Script requires Seer to be running.");
            Started = true;
        }

        /// <summary>
        /// Stop Script.
        /// </summary>
        public void Stop()
        {
            if (!Started) return;
            Started = false;
        }

        ~BmpScript() => Dispose();
        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
