/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Quotidian;
using BardMusicPlayer.Transmogrify.Song.Importers.FruityLoops.FruityStrucs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BardMusicPlayer.Transmogrify.Song.Importers.FruityLoops
{
    public static class FruityParse
    {
        public static void ParseHeader(BinaryReader reader, ref FruityProject project)
        {
            if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "FLhd")
                throw new BmpTransmogrifyException("Invalid magic number");

            // header + type
            var headerLength = reader.ReadInt32();
            if (headerLength != 6)
                throw new BmpTransmogrifyException($"Expected header length 6, not {headerLength}");

            var type = reader.ReadInt16();
            if (type != 0) throw new BmpTransmogrifyException($"Type {type} is not supported");

            // channels
            var channelCount = reader.ReadInt16();
            if (channelCount < 1 || channelCount > 1000)
                throw new BmpTransmogrifyException($"Invalid number of channels: {channelCount}");

            for (var i = 0; i < channelCount; i++)
                project.Channels.Add(new Channel { Id = i, Data = new GeneratorData() });

            // ppq
            project.Ppq = reader.ReadInt16();
            if (project.Ppq < 0) throw new Exception($"Invalid PPQ: {project.Ppq}");
        }


        public static void ParseFldt(BinaryReader reader)
        {
            string id;
            var len = 0;
            do
            {
                reader.ReadBytes(len);

                id = Encoding.ASCII.GetString(reader.ReadBytes(4));
                len = reader.ReadInt32();

                // sanity check
                if (len < 0 || len > 0x10000000)
                    throw new BmpException($"Invalid chunk length: {len}");

            } while (id != "FLdt");
        }

        public static void ParseEvent(BinaryReader reader, ref EnVars globVars, ref FruityProject project)
        {
            var startPos = reader.BaseStream.Position;
            var eventId = (FruityEnums.Event)reader.ReadByte();
            if (eventId < FruityEnums.Event.Word) ParseByteEvent(eventId, ref globVars, ref project, reader);
            else if (eventId < FruityEnums.Event.Int) ParseWordEvent(eventId, ref globVars, ref project, reader);
            else if (eventId < FruityEnums.Event.Text) ParseDwordEvent(eventId, ref globVars, ref project, reader);
            else if (eventId < FruityEnums.Event.Data) ParseTextEvent(eventId, ref globVars, ref project, reader);
            else ParseDataEvent(eventId, ref globVars, ref project, reader);
        }

        private static void ParseByteEvent(FruityEnums.Event eventId, ref EnVars globVars, ref FruityProject project, BinaryReader reader)
        {
            var data = reader.ReadByte();
            var genData = globVars.CurChannel?.Data as GeneratorData;

            switch (eventId)
            {
                case FruityEnums.Event.ByteMainVol:
                    project.MainVolume = data;
                    break;
                case FruityEnums.Event.ByteUseLoopPoints:
                    if (genData != null) genData.SampleUseLoopPoints = true;
                    break;
                case FruityEnums.Event.ByteMixSliceNum:
                    if (genData != null) genData.Insert = data;
                    break;
                case FruityEnums.Event.BytePlayTruncatedNotes:
                    project.PlayTruncatedNotes = Convert.ToBoolean(data);
                    break;
            }
        }

        private static void ParseWordEvent(FruityEnums.Event eventId, ref EnVars globVars, ref FruityProject project, BinaryReader reader)
        {
            var data = reader.ReadUInt16();
            var genData = globVars.CurChannel?.Data as GeneratorData;

            switch (eventId)
            {
                case FruityEnums.Event.WordNewChan:
                    globVars.CurChannel = project.Channels[data];
                    break;
                case FruityEnums.Event.WordNewPat:
                    while (project.Patterns.Count < data)
                        project.Patterns.Add(new Pattern { Id = project.Patterns.Count });

                    globVars.CurPattern = project.Patterns[data - 1];
                    break;
                case FruityEnums.Event.WordTempo:
                    project.Tempo = data;
                    break;
                case FruityEnums.Event.WordFadeStereo:
                    if (genData == null) break;
                    if ((data & 0x02) != 0) genData.SampleReversed = true;
                    else if ((data & 0x100) != 0) genData.SampleReverseStereo = true;
                    break;
                case FruityEnums.Event.WordPreAmp:
                    if (genData == null) break;
                    genData.SampleAmp = data;
                    break;
                case FruityEnums.Event.WordMainPitch:
                    project.MainPitch = data;
                    break;
                case FruityEnums.Event.WordInsertIcon:
                    globVars.CurInsert.Icon = data;
                    break;
                case FruityEnums.Event.WordCurrentSlotNum:
                    if (globVars.CurSlot != null) // Current slot after plugin event, now re-arranged.
                    {
                        globVars.CurInsert.Slots[data] = globVars.CurSlot;
                        globVars.CurSlot = new InsertSlot();
                    }
                    globVars.CurChannel = null;
                    break;
            }
        }

        private static void ParseDwordEvent(FruityEnums.Event eventId, ref EnVars globVars, ref FruityProject project, BinaryReader reader)
        {
            var data = reader.ReadUInt32();
            switch (eventId)
            {
                case FruityEnums.Event.DWordColor:
                    if (globVars.CurChannel != null) globVars.CurChannel.Color = data;
                    break;
                case FruityEnums.Event.DWordMiddleNote:
                    if (globVars.CurChannel != null && globVars.CurChannel.Data is GeneratorData genData) genData.BaseNote = data + 9;
                    break;
                case FruityEnums.Event.DWordInsertColor:
                    globVars.CurInsert.Color = data;
                    break;
                case FruityEnums.Event.DWordFineTempo:
                    project.Tempo = data / 1000.0;
                    break;
            }
        }

        private static void ParseTextEvent(FruityEnums.Event eventId, ref EnVars globVars, ref FruityProject project, BinaryReader reader)
        {
            var dataLen = GetBufferLen(reader);
            var dataBytes = reader.ReadBytes(dataLen);
            var unicodeString = Encoding.Unicode.GetString(dataBytes);
            var defaultEncodedString = System.Text.Encoding.Default.GetString(dataBytes);
            if (unicodeString.EndsWith("\0")) unicodeString = unicodeString.Substring(0, unicodeString.Length - 1);
            unicodeString = unicodeString.Replace('\n', ' ').Replace('\r', ' ').Replace('\'', '"');

            var genData = globVars.CurChannel?.Data as GeneratorData;
            switch (eventId)
            {
                case FruityEnums.Event.TextPluginName:
                    if (globVars.CurChannel != null)
                        globVars.CurChannel.ChannelName = unicodeString;
                    else
                    {
                        project.Channels[globVars.TrackIndex].ChannelName = unicodeString;
                        globVars.TrackIndex++;
                    }
                    break;
                case FruityEnums.Event.TextChanName:
                    if (globVars.CurChannel != null) 
                        globVars.CurChannel.ChannelName = unicodeString; 
                    break;
                case FruityEnums.Event.TextPatName:
                    if (globVars.CurPattern != null) globVars.CurPattern.Name = unicodeString;
                    break;
                case FruityEnums.Event.TextTitle:
                    project.ProjectTitle = unicodeString;
                    break;
                case FruityEnums.Event.TextAuthor:
                    project.Author = unicodeString;
                    break;
                case FruityEnums.Event.TextComment:
                    project.Comment = unicodeString;
                    break;
                case FruityEnums.Event.TextGenre:
                    project.Genre = unicodeString;
                    break;
                case FruityEnums.Event.TextSampleFileName:
                    if (genData == null) break;
                    genData.SampleFileName = unicodeString;
                    genData.GeneratorName = "Sampler";
                    break;
                case FruityEnums.Event.TextVersion:
                    project.VersionString = Encoding.UTF8.GetString(dataBytes);
                    if (project.VersionString.EndsWith("\0")) project.VersionString = project.VersionString.Substring(0, project.VersionString.Length - 1);
                    var numbers = (project.VersionString + ".0.0").Split('.');
                    globVars.VersionMajor = int.Parse(numbers[0]);
                    project.Version = (int.Parse(numbers[0]) << 8) +
                                       (int.Parse(numbers[1]) << 4) +
                                       (int.Parse(numbers[2]) << 0);
                    break;
                case FruityEnums.Event.GeneratorName:
                    if (genData != null) genData.GeneratorName = unicodeString;
                    break;
                case FruityEnums.Event.TextInsertName:
                    globVars.CurInsert.Name = unicodeString;
                    break;
                default:
                    break;
            }
        }

        private static void ParseDataEvent(FruityEnums.Event eventId, ref EnVars globVars, ref FruityProject project, BinaryReader reader)
        {
            var dataLen = GetBufferLen(reader);
            var dataStart = reader.BaseStream.Position;
            var dataEnd = dataStart + dataLen;

            var genData = globVars.CurChannel?.Data as GeneratorData;
            var autData = globVars.CurChannel?.Data as AutomationData;
            var oldAutData = globVars.CurChannel?.Data as OldAutomationData;
            var slotData = globVars.CurSlot;

            switch (eventId)
            {
                case FruityEnums.Event.DataPluginParams:
                    if (slotData != null)
                    {
                        globVars.CurSlot.PluginSettings = reader.ReadBytes(dataLen);
                        globVars.CurSlot.Plugin = ParsePluginChunk(slotData.PluginSettings);
                    }
                    else
                    {
                        if (genData == null) break;
                        if (genData.PluginSettings != null)
                            throw new Exception("Attempted to overwrite plugin");

                        genData.PluginSettings = reader.ReadBytes(dataLen);
                        genData.Plugin = ParsePluginChunk(genData.PluginSettings);
                    }
                    break;
                case FruityEnums.Event.DataChanParams:
                    {
                        if (genData == null) break;
                        var unknown1 = reader.ReadBytes(40);
                        genData.ArpDir = (FruityEnums.ArpDirection)reader.ReadInt32();
                        genData.ArpRange = reader.ReadInt32();
                        genData.ArpChord = reader.ReadInt32();
                        genData.ArpTime = reader.ReadInt32() + 1;
                        genData.ArpGate = reader.ReadInt32();
                        genData.ArpSlide = reader.ReadBoolean();
                        var unknown2 = reader.ReadBytes(31);
                        genData.ArpRepeat = reader.ReadInt32();
                        var unknown3 = reader.ReadBytes(29);
                    }
                    break;
                case FruityEnums.Event.DataBasicChanParams:
                    if (genData == null) break;
                    genData.Panning = reader.ReadInt32();
                    genData.Volume = reader.ReadInt32();
                    break;
                case FruityEnums.Event.DataOldAutomationData:
                    // Die ersten 10 Byte Header überspringen
                    var DataOldAutomationData_unknown1 = reader.ReadBytes(10);
                    while (reader.BaseStream.Position < dataEnd)
                    {
                        var unk = reader.ReadUInt16();
                        var tick = reader.ReadUInt16();
                        uint Timestamp = tick;
                        reader.BaseStream.Seek(3, SeekOrigin.Current);
                        byte Command = reader.ReadByte();
                        byte Channel = reader.ReadByte();
                        reader.BaseStream.Seek(1, SeekOrigin.Current);
                        byte InstrumentNumber = reader.ReadByte();
                        // Das allerletzte Byte des Blocks überspringen
                        reader.BaseStream.Seek(1, SeekOrigin.Current);

                        ProgramChangeData pg = new ProgramChangeData();
                        pg.Timestamp = Timestamp;
                        pg.InstrumentNumber = InstrumentNumber;
                        if (Channel > project.Channels.Count())
                            continue;
                        if (project.Channels[Channel].Data == null || !(project.Channels[Channel].Data is OldAutomationData))
                        {
                            var oa = new OldAutomationData();
                            oa.Command = Command;
                            oa.Channel = Channel;
                            project.Channels[Channel].Data = oa;
                        }
                        var d = project.Channels[Channel].Data as OldAutomationData;
                        d.Programchanges.Add(pg);
                    }
                    break;

                case FruityEnums.Event.DataPatternNotes:
                    while (reader.BaseStream.Position < dataEnd)
                    {
                        var pos = reader.ReadInt32();
                        var unknown1 = reader.ReadInt16();
                        var ch = reader.ReadByte();
                        var unknown2 = reader.ReadByte();
                        var length = reader.ReadInt32();
                        var key = reader.ReadByte();
                        var unknown3 = reader.ReadInt16();
                        var unknown4 = reader.ReadByte();
                        var finePitch = reader.ReadUInt16();
                        var release = reader.ReadUInt16();
                        var pan = reader.ReadByte();
                        var velocity = reader.ReadByte();
                        var x1 = reader.ReadByte();
                        var x2 = reader.ReadByte();

                        var channel = project.Channels[ch];
                        if (!globVars.CurPattern.Notes.ContainsKey(channel))
                            globVars.CurPattern.Notes.Add(channel, new List<Note>());
                        globVars.CurPattern.Notes[channel].Add(new Note
                        {
                            Position = pos,
                            Length = length,
                            Key = key,
                            FinePitch = finePitch,
                            Release = release,
                            Pan = pan,
                            Velocity = velocity
                        });
                    }
                    break;
                case FruityEnums.Event.DataInsertParams:
                    while (reader.BaseStream.Position < dataEnd)
                    {
                        var startPos = reader.BaseStream.Position;
                        var unknown1 = reader.ReadInt32();
                        var messageId = (FruityEnums.InsertParam)reader.ReadByte();
                        var unknown2 = reader.ReadByte();
                        var channelData = reader.ReadUInt16();
                        var messageData = reader.ReadInt32();

                        var slotId = channelData & 0x3F;
                        var insertId = (channelData >> 6) & 0x7F;
                        var insertType = channelData >> 13;

                        var insert = project.GetInsert(insertId);

                        switch (messageId)
                        {
                            case FruityEnums.InsertParam.SlotState:
                                insert.GetSlot(slotId).State = messageData;
                                break;
                            case FruityEnums.InsertParam.SlotVolume:
                                insert.GetSlot(slotId).Volume = messageData;
                                break;
                            case FruityEnums.InsertParam.Volume:
                                insert.Volume = messageData;
                                break;
                            case FruityEnums.InsertParam.Pan:
                                insert.Pan = messageData;
                                break;
                            case FruityEnums.InsertParam.StereoSep:
                                insert.StereoSep = messageData;
                                break;
                            case FruityEnums.InsertParam.LowLevel:
                                insert.LowLevel = messageData;
                                break;
                            case FruityEnums.InsertParam.BandLevel:
                                insert.BandLevel = messageData;
                                break;
                            case FruityEnums.InsertParam.HighLevel:
                                insert.HighLevel = messageData;
                                break;
                            case FruityEnums.InsertParam.LowFreq:
                                insert.LowFreq = messageData;
                                break;
                            case FruityEnums.InsertParam.BandFreq:
                                insert.BandFreq = messageData;
                                break;
                            case FruityEnums.InsertParam.HighFreq:
                                insert.HighFreq = messageData;
                                break;
                            case FruityEnums.InsertParam.LowWidth:
                                insert.LowWidth = messageData;
                                break;
                            case FruityEnums.InsertParam.BandWidth:
                                insert.BandWidth = messageData;
                                break;
                            case FruityEnums.InsertParam.HighWidth:
                                insert.HighWidth = messageData;
                                break;
                            default:
                                if ((int)messageId >= 64 && (int)messageId <= 64 + 104)  // any value 64 or above appears to be the desination insert
                                {
                                    var insertDest = (int)messageId - 64;
                                    insert.RouteVolumes[insertDest] = messageData;
                                    Console.WriteLine($"{startPos:X4} insert send from {insertId} to {insertDest} volume: {messageData:X8}");
                                }
                                else
                                {
                                    Console.WriteLine($"{startPos:X4} insert param: {messageId} {insertId}-{slotId}, data: {messageData:X8}");
                                }
                                break;
                        }
                    }
                    break;
                case FruityEnums.Event.DataAutomationChannels:
                    while (reader.BaseStream.Position < dataEnd)
                    {
                        var unknown1 = reader.ReadUInt16();
                        var automationChannel = reader.ReadByte();
                        var unknown2 = reader.ReadUInt32();
                        var unknown3 = reader.ReadByte();
                        var param = reader.ReadUInt16();
                        var paramDestination = reader.ReadInt16();
                        var unknown4 = reader.ReadUInt64();

                        var channel = project.Channels[automationChannel];

                        if ((paramDestination & 0x2000) == 0)  // Automation on channel
                        {
                            channel.Data = new AutomationData
                            {
                                Channel = project.Channels[paramDestination],
                                Parameter = param & 0x7fff,
                                VstParameter = (param & 0x8000) > 0 ? true : false      // switch determines if automation is on channel or vst
                            };
                        }
                        else
                        {
                            channel.Data = new AutomationData // automation on insert slot
                            {
                                Parameter = param & 0x7fff,
                                InsertId = (paramDestination & 0x0FF0) >> 6,  // seems to be out by one
                                SlotId = paramDestination & 0x003F
                            };
                        }
                    }
                    break;
                case FruityEnums.Event.DataPlayListItems:
                    while (reader.BaseStream.Position < dataEnd)
                    {
                        var startTime = reader.ReadInt32();
                        var patternBase = reader.ReadUInt16();
                        var patternId = reader.ReadUInt16();
                        var length = reader.ReadInt32();
                        var track = reader.ReadInt32();
                        if (globVars.VersionMajor == 20)
                            track = 501 - track;
                        else
                            track = 198 - track;
                        var unknown1 = reader.ReadUInt16();
                        var itemFlags = reader.ReadUInt16();
                        var unknown3 = reader.ReadUInt32();
                        bool muted = (itemFlags & 0x2000) > 0 ? true : false;   // flag determines if item is muted

                        // id of 0-patternBase is samples or automation, after is pattern
                        if (patternId <= patternBase)
                        {
                            var startOffset = (int)(reader.ReadSingle() * (float)project.Ppq);
                            var endOffset = (int)(reader.ReadSingle() * (float)project.Ppq);

                            project.GetTrack(track).Items.Add(new ChannelPlaylistItem
                            {
                                Position = startTime,
                                Length = length,
                                StartOffset = startOffset,
                                EndOffset = endOffset,
                                Channel = project.Channels[patternId],
                                Muted = muted
                            });
                        }
                        else
                        {
                            var startOffset = reader.ReadInt32();
                            var endOffset = reader.ReadInt32();

                            project.GetTrack(track).Items.Add(new PatternPlaylistItem
                            {
                                Position = startTime,
                                Length = length,
                                StartOffset = startOffset,
                                EndOffset = endOffset,
                                Pattern = project.Patterns[patternId - patternBase - 1],
                                Muted = muted
                            });
                        }
                    }
                    break;
                case FruityEnums.Event.DataAutomationData:
                    {
                        var unknown1 = reader.ReadUInt32(); // always 1?
                        var unknown2 = reader.ReadUInt32(); // always 64?
                        var unknown3 = reader.ReadByte();
                        var unknown4 = reader.ReadUInt16();
                        var unknown5 = reader.ReadUInt16(); // always 0?
                        var unknown6 = reader.ReadUInt32();
                        var keyCount = reader.ReadUInt32();

                        if (autData == null) break;
                        autData.Keyframes = new AutomationKeyframe[keyCount];

                        for (var i = 0; i < keyCount; i++)
                        {
                            var startPos = reader.BaseStream.Position;

                            var keyPos = reader.ReadDouble();
                            var keyVal = reader.ReadDouble();
                            var keyTension = reader.ReadSingle();
                            var unknown7 = reader.ReadUInt32(); // seems linked to tension?

                            var endPos = reader.BaseStream.Position;
                            reader.BaseStream.Position = startPos;
                            var byteData = reader.ReadBytes((int)(endPos - startPos));
                            Console.WriteLine($"Key {i} data: {string.Join(" ", byteData.Select(x => x.ToString("X2")))}");

                            autData.Keyframes[i] = new AutomationKeyframe
                            {
                                Position = (int)(keyPos * project.Ppq),
                                Tension = keyTension,
                                Value = keyVal
                            };
                        }

                        // remaining data is unknown
                    }
                    break;
                case FruityEnums.Event.DataInsertRoutes:
                    for (var i = 0; i < FruityProject.MaxInsertCount; i++)
                    {
                        if (globVars.CurInsert == null)
                            globVars.CurInsert = new();
                        globVars.CurInsert.Routes[i] = reader.ReadBoolean();
                    }

                    var newIndex = globVars.CurInsert.Id + 1;
                    if (newIndex < project.Inserts.Length) globVars.CurInsert = project.Inserts[newIndex];

                    break;
                case FruityEnums.Event.DataInsertFlags:
                    reader.ReadUInt32();
                    var flags = (FruityEnums.InsertFlags)reader.ReadUInt32();

                    if (globVars.CurInsert == null)
                        globVars.CurInsert = new();

                    globVars.CurInsert.Flags = flags;
                    globVars.CurSlot = new InsertSlot();  // New insert route, create new slot
                    break;
            }

            // make sure cursor is at end of data
            reader.BaseStream.Position = dataEnd;
        }

        private static Plugin ParsePluginChunk(byte[] chunk)
        {
            var plugin = new Plugin();

            using (var reader = new BinaryReader(new MemoryStream(chunk)))
            {
                var pluginType = (FruityEnums.PluginType)reader.ReadInt32();

                if (pluginType != FruityEnums.PluginType.Vst)
                {
                    return null;
                }

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var eventId = (FruityEnums.PluginChunkId)reader.ReadInt32();
                    var length = (int)reader.ReadInt64();

                    switch (eventId)
                    {
                        case FruityEnums.PluginChunkId.VendorName:
                            plugin.VendorName = Encoding.ASCII.GetString(reader.ReadBytes(length));
                            break;
                        case FruityEnums.PluginChunkId.Filename:
                            plugin.FileName = Encoding.ASCII.GetString(reader.ReadBytes(length));
                            break;
                        case FruityEnums.PluginChunkId.Name:
                            plugin.Name = Encoding.ASCII.GetString(reader.ReadBytes(length));
                            break;
                        case FruityEnums.PluginChunkId.State:
                            plugin.State = reader.ReadBytes(length);
                            break;
                        default:
                            Console.WriteLine($"Event {eventId}, data: {string.Join(" ", reader.ReadBytes(length).Select(x => x.ToString("X2")))}");
                            break;
                    }
                }

                return plugin;
            }
        }

        private static int GetBufferLen(BinaryReader reader)
        {
            var data = reader.ReadByte();
            var dataLen = data & 0x7F;
            var shift = 0;
            while ((data & 0x80) != 0)
            {
                data = reader.ReadByte();
                dataLen = dataLen | ((data & 0x7F) << (shift += 7));
            }
            return dataLen;
        }
    }
}
