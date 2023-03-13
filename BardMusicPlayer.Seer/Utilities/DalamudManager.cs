/*
 * Copyright(c) 2023 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BardMusicPlayer.Quotidian.UtcMilliTime;
using Machina.FFXIV;
using Machina.FFXIV.Oodle;
using Machina.Infrastructure;
using static BardMusicPlayer.Seer.BmpSeer;

#endregion

namespace BardMusicPlayer.Seer.Utilities
{
    public sealed class DalamudManager 
    {
        private static readonly Lazy<DalamudManager> LazyInstance = new(static () => new DalamudManager());

        private readonly object _lock;

        private DalamudManager()
        {
            _lock = new object();

            Trace.UseGlobalLock = false;
            Trace.Listeners.Add(new MachinaLogger());

        }

        public static DalamudManager Instance => LazyInstance.Value;

        public void Dispose()
        {
            lock (_lock)
            {

            }
        }

        #region Accessors

        internal event EnsembleStartHandler EnsembleStart;
        internal delegate void EnsembleStartHandler(int processId, int code);
        public void EnsembleStartEventHandler(int processId, int code)
        {
            EnsembleStart?.Invoke(processId, code);
        }

        #endregion

        ~DalamudManager()
        {
            Dispose();
        }
    }
}