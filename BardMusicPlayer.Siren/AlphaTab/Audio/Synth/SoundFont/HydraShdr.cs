﻿/*
 * Copyright(c) 2021 Daniel Kuschny
 * Licensed under the MPL-2.0 license. See https://github.com/CoderLine/alphaTab/blob/develop/LICENSE for full license information.
 */

// The SoundFont loading and Audio Synthesis is based on TinySoundFont, licensed under MIT,
// developed by Bernhard Schelling (https://github.com/schellingb/TinySoundFont)

// C# port for alphaTab: (C) 2019 by Daniel Kuschny
// Licensed under: MPL-2.0

/*
 * LICENSE (MIT)
 *
 * Copyright (C) 2017, 2018 Bernhard Schelling
 * Based on SFZero, Copyright (C) 2012 Steve Folta (https://github.com/stevefolta/SFZero)
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this
 * software and associated documentation files (the "Software"), to deal in the Software
 * without restriction, including without limitation the rights to use, copy, modify, merge,
 * publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
 * to whom the Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
 * PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using BardMusicPlayer.Siren.AlphaTab.IO;

namespace BardMusicPlayer.Siren.AlphaTab.Audio.Synth.SoundFont
{
    internal sealed class HydraShdr
    {
        public const int SizeInFile = 46;

        public string SampleName { get; set; }
        public uint Start { get; set; }
        public uint End { get; set; }
        public uint StartLoop { get; set; }
        public uint EndLoop { get; set; }
        public uint SampleRate { get; set; }
        public byte OriginalPitch { get; set; }
        public sbyte PitchCorrection { get; set; }
        public ushort SampleLink { get; set; }
        public ushort SampleType { get; set; }

        public static HydraShdr Load(IReadable reader)
        {
            var shdr = new HydraShdr
            {
                SampleName = reader.Read8BitStringLength(20),
                Start = reader.ReadUInt32LE(),
                End = reader.ReadUInt32LE(),
                StartLoop = reader.ReadUInt32LE(),
                EndLoop = reader.ReadUInt32LE(),
                SampleRate = reader.ReadUInt32LE(),
                OriginalPitch = (byte)reader.ReadByte(),
                PitchCorrection = reader.ReadSignedByte(),
                SampleLink = reader.ReadUInt16LE(),
                SampleType = reader.ReadUInt16LE()
            };
            return shdr;
        }
    }
}