/*
 * Copyright(c) 2007-2020 Ryan Wilson syndicated.life@gmail.com (http://syndicated.life/)
 * Licensed under the MIT license. See https://github.com/FFXIVAPP/sharlayan/blob/master/LICENSE.md for full license information.
 */

#region

using System;
using System.Collections.Generic;
using BardMusicPlayer.Seer.Reader.Backend.Sharlayan.Models;

#endregion

namespace BardMusicPlayer.Seer.Reader.Backend.Sharlayan.Events
{
    internal sealed class SignaturesFoundEvent : EventArgs
    {
        public SignaturesFoundEvent(object sender, Dictionary<string, Signature> signatures, long processingTime)
        {
            Sender = sender;
            Signatures = signatures;
            ProcessingTime = processingTime;
        }

        public long ProcessingTime { get; set; }

        public object Sender { get; set; }

        public Dictionary<string, Signature> Signatures { get; }
    }
}