/*
 * Copyright(c) 2022 MoogleTroupe, 2018-2020 parulina
 * Licensed under the GPL v3 license. See https://github.com/BardMusicPlayer/BardMusicPlayer/blob/develop/LICENSE for full license information.
 */

#region

using System;
using System.Collections.Generic;

#endregion

namespace BardMusicPlayer.Seer.Reader.Backend.DatFile.Objects
{
    internal sealed class HotbarRow : IDisposable
    {
        public Dictionary<int, HotbarJobSlot> Slots = new();

        public HotbarJobSlot this[int i]
        {
            get
            {
                if (!Slots.ContainsKey(i)) Slots[i] = new HotbarJobSlot();

                return Slots[i];
            }
            set => Slots[i] = value;
        }

        public void Dispose()
        {
            if (Slots == null) return;

            foreach (var slot in Slots.Values) slot?.Dispose();

            Slots.Clear();
        }

        ~HotbarRow()
        {
            Dispose();
        }
    }
}