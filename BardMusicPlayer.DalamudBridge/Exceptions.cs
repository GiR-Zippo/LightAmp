/*
 * Copyright(c) 2025 GiR-Zippo, 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Quotidian;

namespace BardMusicPlayer.DalamudBridge
{
    public sealed class DalamudBridgeException : BmpException
    {
        internal DalamudBridgeException() : base()
        {
        }
        internal DalamudBridgeException(string message) : base(message)
        {
        }
    }
}
