/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;

namespace BardMusicPlayer.Script.Engines
{
    internal interface IBmpScript
    {
        public event EventHandler<KeyValuePair<string, bool> > OnRunningStateChanged;
        public string UId { get; set; }
        public void LoadAndRun(string filename);
        public void StopExecution();
    }
}