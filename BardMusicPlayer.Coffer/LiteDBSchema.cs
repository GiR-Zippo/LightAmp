/*
 * Copyright(c) 2025 GiR-Zippo, 2021 MoogleTroupe, isaki
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using LiteDB;

namespace BardMusicPlayer.Coffer
{
    public sealed class LiteDBSchema
    {
        [BsonId]
        public int Id { get; set; } = Constants.SCHEMA_DOCUMENT_ID;
        public static byte Version => Constants.SCHEMA_VERSION;
    }
}
