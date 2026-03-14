/*
 * Copyright(c) 2025 GiR-Zippo, 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;

namespace BardMusicPlayer.Quotidian
{
    public class BmpException : Exception
    {
        public BmpException() : base()
        {
        }

        public BmpException(string message) : base(message)
        {
        }

        public BmpException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}