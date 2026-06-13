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
        public List<SessionMembers> GetSessions() { return members; }
        public List<CharacterState> GetMembers() { return members.SelectMany(m => m.characters).ToList(); }

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
                    existingMember.idle = serverMember.idle;
                    var serverCharIds = serverMember.characters.Select(c => c.charId).ToList();
                    existingMember.characters.RemoveAll(c => !serverCharIds.Contains(c.charId));

                    foreach (var serverChar in serverMember.characters)
                    {
                        //update default list
                        var existingChar = existingMember.characters.FirstOrDefault(c => c.charId == serverChar.charId);
                        if (existingChar == null)
                            existingMember.characters.Add(serverChar);
                        else
                        {
                            existingChar.displayName = serverChar.displayName;
                            existingChar.world = serverChar.world;
                            existingChar.trackNumber = serverChar.trackNumber;
                            existingChar.instrument = serverChar.instrument;
                        }
                    }
                }
            }
            BmpJamboree.Instance.PublishEvent(new PartyChangedEvent());
        }

        /// <summary>
        /// Find a character by its Id
        /// </summary>
        /// <param name="charId"></param>
        /// <returns></returns>
        public (SessionMembers Session, CharacterState Character, string SessionId) FindByCharacterId(string charId)
        {
            foreach (var member in members)
            {
                var character = member.characters.FirstOrDefault(c => c.charId == charId);
                if (character != null)
                    return (member, character, member.memberId);
            }
            return (null, null, string.Empty);
        }

        /// <summary>
        /// Update the Track the user assigned for
        /// </summary>
        /// <param name="charId"></param>
        /// <param name="track"></param>
        public KeyValuePair<string, TrackAssignment> UpdateTrackForUser(string charId, int track)
        {
            var (session, character, sessionId) = FindByCharacterId(charId);
            character.trackNumber = track;
            return new KeyValuePair<string, TrackAssignment> (session.memberId, new TrackAssignment
            {
                charId = charId,
                trackNumber = track,
                instrument = character.instrument
            });
        }

        /// <summary>
        /// Update the Instrument the user assigned for
        /// </summary>
        /// <param name="charId"></param>
        /// <param name="instrument"></param>
        public KeyValuePair<string, TrackAssignment> UpdateInstrumentForUser(string charId, string instrument)
        {
            var (session, character, sessionId) = FindByCharacterId(charId);
            character.instrument = instrument;
            return new KeyValuePair<string, TrackAssignment>(session.memberId, new TrackAssignment
            {
                charId = charId,
                trackNumber = character.trackNumber,
                instrument =  instrument
            });
        }

        /// <summary>
        /// Update the Track and Instrument the user assigned for
        /// </summary>
        public KeyValuePair<string, TrackAssignment> UpdateTrackAndInstrumentForUser(string charId, int track, string instrument)
        {
            var (session, character, sessionId) = FindByCharacterId(charId);
            character.instrument = instrument;
            return new KeyValuePair<string, TrackAssignment>(session.memberId, new TrackAssignment
            {
                charId = charId,
                trackNumber = track,
                instrument = instrument
            });
        }
    }
}
