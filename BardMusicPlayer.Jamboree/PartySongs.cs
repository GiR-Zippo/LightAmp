/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Jamboree.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BardMusicPlayer.Jamboree
{
    public class PartySongs
    {
        private Dictionary<PlaylistItem, byte[]> _SongList { get; set; } = new();
        private BMPApi _Api { get; } = null;
        private int _PlaylistVersion { get; set; } = 0;
        public int GetPlaylistVersion() => _PlaylistVersion;

        public PartySongs(BMPApi api)
        {
            _Api = api;
        }

        /// <summary>
        /// Update the current playlist
        /// </summary>
        /// <param name="manifest"></param>
        public async void UpdateSongs(SessionManifest manifest)
        {
            if (manifest == null) return;
            if (manifest.items == null) return;

            _PlaylistVersion = manifest.playlistVersion;

            var currentIds = manifest.items.Select(m => m.itemId);
            _SongList.Where(m => !currentIds.Contains(m.Key.itemId)).ToList().ForEach(m => _SongList.Remove(m.Key));

            foreach (var playlistItem in manifest.items)
            {
                var existingItem = _SongList.FirstOrDefault(m => m.Key.itemId == playlistItem.itemId);
                if (existingItem.Key == default)
                {
                    _SongList.Add(playlistItem, null);
                    await _Api.DownloadMidiFile(playlistItem.itemId);
                }
            }
            // Publish the new list
            BmpJamboree.Instance.PublishEvent(new PartyPlaylistChangeEvent(_SongList.Keys.ToList()));
        }

        /// <summary>
        /// Adds the midi-data to the entry with Id
        /// </summary>
        /// <param name="itemId">The unique ID of the playlist item</param>
        /// <param name="data">The MIDI file bytes to append</param>
        /// <returns>true if the item was found and updated; otherwise, false</returns>
        public bool AddMidiFile(string itemId, byte[] data)
        {
            var key = _SongList.Keys.FirstOrDefault(k => k.itemId == itemId);
            if (key == null) return false;

            _SongList[key] = data;
            return true;
        }

        /// <summary>
        /// Get the Midi by its itemId as byte[]
        /// </summary>
        /// <param name="itemId">The unique ID of the song</param>
        /// <returns>The MIDI file bytes, or null if not found/loaded</returns>
        public (string, byte[]) GetMidiData(string itemId)
        {
            var existingItem = _SongList.FirstOrDefault(m => m.Key.itemId == itemId);
            if (existingItem.Key == null)
                return ("", null);
            return (existingItem.Key.filename, existingItem.Value);
        }
    }
}