/*
 * Copyright(c) 2026 GiR-Zippo, 
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System.Collections.Generic;

namespace BardMusicPlayer.Jamboree
{
    #region HostState
    public record SessionCreated
    {
        public string code { get; set; } = "";
        public string hostToken { get; set; } = "";
        public string sessionId { get; set; } = "";
        public string expiresAt { get; set; } = "";
    }
    #endregion

    #region MemberState
    public record MemberStateResponse
    {
        public string code { get; set; } = "";
        public string memberId { get; set; } = "";
        public string memberToken { get; set; } = "";
        public int? stateVersion { get; set; } = 0;
        public int? playlistVersion { get; set; } = 0;
        public string nowPlaying { get; set; } = "";
        public string playbackState { get; set; } = "";
        public TrackAssignment assignment { get; set; } = null;
    }
    #endregion

    #region SessionManifest
    public record SessionManifest
    {
        public string status { get; set; } = string.Empty;
        public int stateVersion { get; set; } = 0;
        public int playlistVersion { get; set; } = 0;
        public string nowPlaying { get; set; } = string.Empty;
        public string playbackState { get; set; } = string.Empty;
        public List<PlaylistItem> items { get; set; } = new();
        public List<SessionMembers> members { get; set; } = new();
    }

    public record SessionMembers
    {
        public string memberId { get; set; } = "";
        public bool? idle { get; set; } = true;
        public List<CharacterState> characters { get; set; } = new();
    }

    public record CharacterState
    {
        public string charId { get; set; } = "";
        public string displayName { get; set; } = "";
        public string world { get; set; } = "";
        public int? trackNumber { get; set; } = 0;
        public string instrument { get; set; } = "";
    }

    #endregion

    #region Playlist
    public record PlaylistResponse
    {
        public int playlistVersion { get; set; } = 0;
        public List<PlaylistItem>? items { get; set; } = new();
    }

    public record PlaylistItem
    {
        public string itemId { get; set; } = "";
        public int? position { get; set; } = 0;
        public string filename { get; set; } = "";
        public string md5 { get; set; } = "";
        public long size { get; set; } = 0;
        public string source { get; set; } = "";
        public string title { get; set; } = "";
        public string artist { get; set; } = "";
        public string fileUrl { get; set; } = "";
    }
    #endregion

    #region NowPlaying
    public record NowPlayingRequest
    {
        public string itemId { get; set; } = "";
        public string playbackState { get; set; } = "";
    }

    public record NowPlayingResponse
    {
        public string nowPlaying { get; set; } = "";
        public string playbackState { get; set; } = "";
        public int stateVersion { get; set; } = 0;
    }
    #endregion

    #region Heartbeat
    public record Heartbeat
    {
        public bool wait { get; set; } = false;
        public int since { get; set; } = 0;
        public int knownPlaylistVersion { get; set; } = 0;
    }

    public record HeartbeatResponse
    {
        public string sessionStatus { get; set; } = "";
        public int stateVersion { get; set; } = 0;
        public int playlistVersion { get; set; } = 0;
        public bool playlistStale { get; set; } = true;
        public string nowPlaying { get; set; } = "";
        public string playbackState { get; set; } = "stopped";
        public TrackAssignment assignment { get; set; } = null;
    }
    #endregion

    #region Shared
    public record TrackAssignment
    {
        public string charId { get; set; } = "";
        public int? trackNumber { get; set; } = 0;
        public string instrument { get; set; } = "";
    }

    public record TrackAssignmentResponse
    {
        public bool ok { get; set; } = true;
        public int? trackNumber { get; set; } = 0;
        public string? instrument { get; set; } = "";
}
    #endregion
}
