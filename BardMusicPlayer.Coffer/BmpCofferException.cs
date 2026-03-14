/*
 * Copyright(c) 2025 GiR-Zippo, 2021 MoogleTroupe, isaki
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using BardMusicPlayer.Quotidian;

namespace BardMusicPlayer.Coffer
{
    public sealed class BmpCofferException : BmpException
    {
        public BmpCofferException(string message) : base(message) { }

        public BmpCofferException(string message, Exception inner) : base(message, inner) { }
    }
}
