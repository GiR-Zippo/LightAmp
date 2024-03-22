/*
 * Copyright(c) 2024 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Threading;
using BardMusicPlayer.Maestro;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Seer;
using BasicSharp;
using Neo.IronLua;

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

        public event EventHandler<bool> OnRunningStateChanged;

        private Thread thread = null;
        private Interpreter basic = null;
        private Lua lua = null;

        #region accessors
        public void StopExecution()
        {
            if (thread == null)
                return;
            
            if (basic is not null)
                basic.StopExec();

            if (thread.ThreadState != ThreadState.Stopped)
            {
                if (lua is not null)
                    lua.Dispose();
                thread.Abort();
            }
        }
        #endregion

        public void LoadAndRun(string filename)
        {
            if (filename.ToLower().EndsWith(".lua"))
                LoadLua(filename);
            else if (filename.ToLower().EndsWith(".bas"))
                LoadBasic(filename);
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

        ~BmpScript() => Dispose();
        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
