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
    public class Party : IDisposable
    {
        private List<SessionMembers> members { get; set; } = new();
        public List<SessionMembers> GetMembers() { return members; }

        public void Dispose()
        {
            members.Clear();
        }

        /// <summary>
        /// UpdateMembers called from Api
        /// </summary>
        /// <param name="manifest"></param>
        public void UpdateMembers(SessionManifest manifest)
        {
            if (manifest == null || manifest.members == null)
                return;

            // sichern ist sicher
            if (members.SequenceEqual(manifest.members))
                return;

            var currentIds = manifest.members.Select(m => m.memberId);
            members.Where(m => !currentIds.Contains(m.memberId)).ToList().ForEach(m => members.Remove(m));

            foreach (var serverMember in manifest.members)
            {
                var existingMember = members.FirstOrDefault(m => m.memberId == serverMember.memberId);
                if (existingMember == null)
                    members.Add(serverMember);
                else
                {
                    existingMember.displayName = serverMember.displayName;
                    existingMember.trackNumber = serverMember.trackNumber;
                    existingMember.instrument = serverMember.instrument;
                    existingMember.idle = serverMember.idle;
                }
            }
            BmpJamboree.Instance.PublishEvent(new PartyChangedEvent());
        }

        /// <summary>
        /// Update the Track the user assigned for
        /// </summary>
        /// <param name="id"></param>
        /// <param name="track"></param>
        public TrackAssignment UpdateTrackForUser(string id, int track)
        {
            var member = members.FirstOrDefault(m => m.memberId == id);
            member.trackNumber = track;
            return new TrackAssignment
            {
                trackNumber = track,
                instrument = member.instrument
            };
        }

        /// <summary>
        /// Update the Instrument the user assigned for
        /// </summary>
        /// <param name="id"></param>
        /// <param name="track"></param>
        public TrackAssignment UpdateInstrumentForUser(string id, string instrument)
        {
            var member = members.FirstOrDefault(m => m.memberId == id);
            member.instrument = instrument;
            return new TrackAssignment
            {
                trackNumber = member.trackNumber,
                instrument = instrument
            };
        }
    }
}
