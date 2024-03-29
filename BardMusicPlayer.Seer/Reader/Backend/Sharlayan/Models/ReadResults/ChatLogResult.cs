/*
 * Copyright(c) 2007-2020 Ryan Wilson syndicated.life@gmail.com (http://syndicated.life/)
 * Licensed under the MIT license. See https://github.com/FFXIVAPP/sharlayan/blob/master/LICENSE.md for full license information.
 */

#region

using System.Collections.Generic;
using BardMusicPlayer.Seer.Reader.Backend.Sharlayan.Core;

#endregion

namespace BardMusicPlayer.Seer.Reader.Backend.Sharlayan.Models.ReadResults
{
    internal sealed class ChatLogResult
    {
        public List<ChatLogItem> ChatLogItems { get; } = new();

        public int PreviousArrayIndex { get; set; }

        public int PreviousOffset { get; set; }
    }
}