/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Quotidian;

namespace BardMusicPlayer.Script
{
    public sealed class BmpScriptException : BmpException
    {
        internal BmpScriptException() : base()
        {
        }
        internal BmpScriptException(string message) : base(message)
        {
        }
    }
}
