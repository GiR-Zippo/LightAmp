/*
 * Copyright(c) 2025 GiR-Zippo, 2023 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;

namespace BardMusicPlayer.Pigeonhole
{
    public sealed class BmpPigeonholeException : Exception
    {
        public BmpPigeonholeException(string message) : base(message) { }
        public BmpPigeonholeException(string message, Exception inner) : base(message, inner) { }
    }
}