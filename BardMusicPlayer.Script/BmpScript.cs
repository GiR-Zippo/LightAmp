/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BardMusicPlayer.Maestro;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Script.Engines;
using BardMusicPlayer.Seer;

namespace BardMusicPlayer.Script
{
    public partial class BmpScript
    {
        private static readonly Lazy<BmpScript> LazyInstance = new(static () => new BmpScript());

        /// <summary>
        /// 
        /// </summary>
        public bool Started { get; private set; }

        private BmpScript()
        {
        }
        public static BmpScript Instance => LazyInstance.Value;

        public event EventHandler<KeyValuePair<string, bool> >OnRunningStateChanged;

        private ConcurrentDictionary<string, IBmpScript> runningScripts { get; set; } = new ConcurrentDictionary<string, IBmpScript>();

        public void LoadAndRun(string filename)
        {
            if (filename.ToLower().EndsWith(".lua"))
            {
                var currentScript = new BmpLuaScript(Guid.NewGuid().ToString() + "@" + filename);
                execute(filename, currentScript);
            }
            else if (filename.ToLower().EndsWith(".bas"))
            {
                var currentScript = new BmpBASICScript(Guid.NewGuid().ToString() + "@" + filename);
                execute(filename, currentScript);
            }
        }

        private void execute(string filename, IBmpScript script)
        {
            script.OnRunningStateChanged += Script_OnRunningStateChanged;
            script.LoadAndRun(filename);
            runningScripts[script.UId]=script;
        }

        private void Script_OnRunningStateChanged(object sender, KeyValuePair<string, bool> e)
        {
            if (!e.Value)
            {
                if (runningScripts.TryRemove(e.Key, out var script))
                {
                    script.OnRunningStateChanged -= Script_OnRunningStateChanged;
                    OnRunningStateChanged?.Invoke(this, new KeyValuePair<string, bool>(e.Key, false));
                }
            }
            else
                OnRunningStateChanged?.Invoke(this, new KeyValuePair<string, bool>(e.Key, true));

            //If we have an empty list let everyone know
            if (runningScripts.Count == 0)
                OnRunningStateChanged?.Invoke(this, new KeyValuePair<string, bool>("", false));
            else
                OnRunningStateChanged?.Invoke(this, new KeyValuePair<string, bool>("", true));
        }

        public void StopExecution(string ThreadId)
        {
            if (!runningScripts.ContainsKey(ThreadId))
                return;
            runningScripts[ThreadId].StopExecution();
        }

        public void StopExecution()
        {
            foreach (var script in runningScripts)
                script.Value.StopExecution();
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
            BmpMaestro.Instance.OnPlaybackTimeChanged += Instance_PlaybackTimeChanged;
        }

        /// <summary>
        /// Stop Script.
        /// </summary>
        public void Stop()
        {
            if (!Started) return;
            Started = false;
            BmpMaestro.Instance.OnPlaybackTimeChanged -= Instance_PlaybackTimeChanged;
        }

        #region Helper
        public double PlaytimeTotalInSeconds { get; set; } = -1;
        private void Instance_PlaybackTimeChanged(object sender, Maestro.Events.CurrentPlayPositionEvent e)
        {
            PlaytimeTotalInSeconds = e.timeSpan.TotalSeconds;
        }
        #endregion


        ~BmpScript() => Dispose();
        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
