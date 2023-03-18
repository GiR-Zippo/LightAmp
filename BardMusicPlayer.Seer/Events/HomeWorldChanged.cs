/*
 * Copyright(c) 2023 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/BardMusicPlayer/BardMusicPlayer/blob/develop/LICENSE for full license information.
 */

#region

using System.Text.RegularExpressions;

#endregion

namespace BardMusicPlayer.Seer.Events
{
    public sealed class HomeWorldChanged : SeerEvent
    {
        internal HomeWorldChanged(EventSource readerBackendType, string homeWorld) : base(readerBackendType)
        {
            EventType = GetType();
            HomeWorld = homeWorld;
        }

        public string HomeWorld { get; }

        public override bool IsValid()
        {
            return !string.IsNullOrEmpty(HomeWorld) && Regex.IsMatch(HomeWorld, @"^[a-zA-Z]+$");
        }
    }
}