/*
 * FLP -> MidiFile Converter
 * Ziel: Melanchall.DryWetMidi.MidiFile
 *
 * Reverse-engineered aus FL Studio 20/21 FLP-Format:
 *
 * Header: "FLhd" + size(u32=6) + format(i16) + nChannels(u16) + ppq(u16)
 * Data:   "FLdt" + size(u32) + Events...
 *
 * Event-Typen:
 *   ID 0x00-0x3F  -> 1 Byte  Data
 *   ID 0x40-0x7F  -> 2 Bytes Data
 *   ID 0x80-0xBF  -> 4 Bytes Data
 *   ID 0xC0-0xFF  -> varint Length + Data
 *
 * Relevante Events:
 *   0x9C (DWORD)  -> Tempo in Millibeats (val/1000 = BPM)
 *   0xCB (TEXT)   -> Channel name (UTF-16LE)
 *   0xE0 (STRUCT) -> Piano Roll notes, 24 Bytes pro Note:
 *                    [0]  u32 position   (Playlist-Ticks)
 *                    [4]  u16 flags      (immer 0x4000)
 *                    [6]  u8  ???
 *                    [7]  u8  ???
 *                    [8]  u32 inner_pos  (Clip-interne Position)
 *                    [12] u32 key        (MIDI-Note 0-127)
 *                    [16] u8  velocity   (0-127)
 *                    [17] u8  ???
 *                    [18] u8  ???
 *                    [19] u8  rack_channel
 *                    [20] u8  length     (Ticks)
 *                    [21] u8  release
 *                    [22] u8  0x80
 *                    [23] u8  0x80
 *
 * Hinweis: Program-Change-Daten sind im FLP nicht gespeichert (delegiert an
 * externes MIDI-Gerät/VST). Der Converter nutzt Name-basiertes Heuristic-Mapping
 * mit optionalem Override via channelProgramMap.
 */

using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace BardMusicPlayer.Transmogrify.Song.Importers
{
    public static class FlpToMidi
    {
        // Event IDs
        private const byte EV_TEMPO = 0x9C;
        private const byte EV_CHAN_NAME = 0xCB;
        private const byte EV_NOTES = 0xE0;
        private const byte EV_PROGRAM = 0xDF;

        private const int NOTE_STRUCT_SIZE = 24;

        // -------------------------------------------------------------------------
        // Öffentliche API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Konvertiert eine FLP-Datei in eine MidiFile.
        /// </summary>
        /// <param name="flpPath">Pfad zur .flp Datei</param>
        /// <param name="channelProgramMap">
        /// Optionales Mapping von Kanal-Name -> GM-Program (0-127).
        /// Hat Vorrang vor dem automatischen Name-Heuristik-Mapping.
        /// Beispiel: { "Saxophone", 65 }, { "BassDrum", 117 }
        /// </param>
        public static MidiFile Convert(string flpPath, Dictionary<string, byte> channelProgramMap = null)
        {
            using var fs = File.OpenRead(flpPath);
            using var br = new BinaryReader(fs, Encoding.GetEncoding(1252));
            return Convert(br, channelProgramMap);
        }

        public static MidiFile Convert(Stream stream, Dictionary<string, byte> channelProgramMap = null)
        {
            using var br = new BinaryReader(stream, Encoding.GetEncoding(1252), leaveOpen: true);
            return Convert(br, channelProgramMap);
        }

        public static MidiFile Convert(BinaryReader br, Dictionary<string, byte> channelProgramMap = null)
        {
            // --- Header ---
            var magic = new string(br.ReadChars(4));
            if (magic != "FLhd")
                throw new InvalidDataException("Keine gültige FLP-Datei (FLhd erwartet)");

            br.ReadUInt32();               // Header-Size (immer 6)
            br.ReadInt16();                // Format
            br.ReadUInt16();               // nChannels
            ushort ppq = br.ReadUInt16();  // Pulses Per Quarter

            // --- Data Chunk ---
            var dataMagic = new string(br.ReadChars(4));
            if (dataMagic != "FLdt")
                throw new InvalidDataException("Keine gültige FLP-Datei (FLdt erwartet)");

            uint dataSize = br.ReadUInt32();
            long dataEnd = br.BaseStream.Position + dataSize;

            double tempoBpm = 125.0;
            int chanIdx = -1;
            var chanNames = new Dictionary<int, string>();
            var notes = new List<FlpNote>();
            var programs = new List<FlpProgram>();

            // --- Events ---
            while (br.BaseStream.Position < dataEnd)
            {
                byte evId = br.ReadByte();

                if (evId < 0x40)
                {
                    br.ReadByte();
                }
                else if (evId < 0x80)
                {
                    br.ReadUInt16();
                }
                else if (evId < 0xC0)
                {
                    uint val = br.ReadUInt32();
                    if (evId == EV_TEMPO)
                    {
                        tempoBpm = val / 1000.0;
                        if (tempoBpm <= 0) tempoBpm = 125.0;
                    }
                }
                else
                {
                    uint len = ReadVarInt(br);
                    long blockStart = br.BaseStream.Position;

                    if (evId == EV_CHAN_NAME)
                    {
                        chanIdx++;
                        byte[] raw = br.ReadBytes((int)len);
                        string name = len >= 2 && raw[1] == 0
                            ? Encoding.Unicode.GetString(raw).TrimEnd('\0')
                            : Encoding.GetEncoding(1252).GetString(raw).TrimEnd('\0');
                        chanNames[chanIdx] = name;
                    }
                    else if (evId == EV_NOTES)
                    {
                        byte[] block = br.ReadBytes((int)len);
                        ParseNotes(block, notes);
                    }
                    else if (evId == EV_PROGRAM)
                    {
                        byte[] block = br.ReadBytes((int)len);
                        ParsePrograms(block, programs);
                    }
                    br.BaseStream.Position = blockStart + len;
                }
            }
            return BuildMidiFile(ppq, tempoBpm, notes, programs, chanNames, channelProgramMap ?? new Dictionary<string, byte>());
        }

        // -------------------------------------------------------------------------
        // Note-Parser
        // -------------------------------------------------------------------------
        private static void ParseNotes(byte[] block, List<FlpNote> notes)
        {
            int count = block.Length / NOTE_STRUCT_SIZE;
            for (int i = 0; i < count; i++)
            {
                int o = i * NOTE_STRUCT_SIZE;

                uint position = BitConverter.ToUInt32(block, o + 0);
                uint key = BitConverter.ToUInt32(block, o + 12);
                byte velocity = block[o + 16];
                byte rackChan = block[o + 19];
                byte lengthTicks = block[o + 20];

                if (key > 127) continue;
                if (velocity == 0 || velocity > 127) velocity = 100;

                notes.Add(new FlpNote
                {
                    Position = position,
                    LengthTicks = lengthTicks == 0 ? (byte)48 : lengthTicks,
                    Key = (byte)key,
                    Velocity = velocity,
                    RackChannel = rackChan,
                });
            }
        }

        private static void ParsePrograms(byte[] block, List<FlpProgram> program)
        {
            using (MemoryStream ms = new MemoryStream(block))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                // 1. Die ersten 10 Byte Header überspringen
                if (ms.Length > 10)
                    ms.Seek(10, SeekOrigin.Begin);

                while (ms.Position + 12 <= ms.Length)
                {
                    FlpProgram ev = new FlpProgram();
                    // Byte 0-3: Zeitstempel (4 Byte)
                    var unk = reader.ReadUInt16();
                    var lengthTicks = reader.ReadUInt16();
                    ev.Timestamp = lengthTicks == 0 ? (byte)48 : lengthTicks;
                    // Byte 4-6: Die 3 Bytes VOR dem Command überspringen (z.B. 00 00 00)
                    reader.BaseStream.Seek(3, SeekOrigin.Current);
                    // Byte 7: Command (1 Byte -> 0x80)
                    ev.Command = reader.ReadByte();
                    // Byte 8: Channel (1 Byte -> 0x00, 0x01...)
                    ev.Channel = reader.ReadByte();
                    // Byte 9: Das Byte zwischen Channel und Instrument überspringen (0x00)
                    reader.BaseStream.Seek(1, SeekOrigin.Current);
                    // Byte 10: Instrumenten-Nummer (1 Byte -> 0x42, 0x2C...)
                    ev.InstrumentNumber = reader.ReadByte();
                    // Byte 11: Das allerletzte Byte des Blocks überspringen
                    reader.BaseStream.Seek(1, SeekOrigin.Current);
                    program.Add(ev);
                }
            }
        }

        // -------------------------------------------------------------------------
        // MIDI-Builder
        // -------------------------------------------------------------------------
        private static MidiFile BuildMidiFile(
            ushort ppq, double tempoBpm,
            List<FlpNote> notes,
            List<FlpProgram> programs,
            Dictionary<int, string> chanNames,
            Dictionary<string, byte> programOverrides)
        {
            var midiFile = new MidiFile();
            midiFile.TimeDivision = new TicksPerQuarterNoteTimeDivision((short)ppq);

            // Tempo-Track
            var tempoTrack = new TrackChunk();
            int tempoMicro = (int)(60_000_000.0 / tempoBpm);
            tempoTrack.Events.Add(new SetTempoEvent(tempoMicro) { DeltaTime = 0 });
            midiFile.Chunks.Add(tempoTrack);

            // Noten nach Rack-Channel gruppieren
            var byChannel = notes
                .GroupBy(n => n.RackChannel)
                .OrderBy(g => g.Key);

            foreach (var group in byChannel)
            {
                int rackCh = group.Key;
                byte midiChan = (byte)(rackCh % 16);
                string trackName = chanNames.TryGetValue(rackCh, out var n) ? n : $"Channel {rackCh + 1}";
                byte program = ResolveProgram(trackName, programOverrides);

                var evList = new List<(long tick, MidiEvent ev)>();

                // Program Change am Anfang
                evList.Add((0, new ProgramChangeEvent((SevenBitNumber)program)
                { Channel = (FourBitNumber)midiChan }));

                foreach (var note in group)
                {
                    long startTick = note.Position;
                    long endTick = startTick + note.LengthTicks;

                    evList.Add((startTick, new NoteOnEvent((SevenBitNumber)note.Key, (SevenBitNumber)note.Velocity)
                    { Channel = (FourBitNumber)midiChan }));
                    evList.Add((endTick, new NoteOffEvent((SevenBitNumber)note.Key, (SevenBitNumber)0)
                    { Channel = (FourBitNumber)midiChan }));
                }

                foreach (var prog in programs)
                {
                    if (prog.Channel != group.Key)
                        continue;

                    long startTick = prog.Timestamp;
                    evList.Add((startTick, new ProgramChangeEvent((SevenBitNumber)(prog.InstrumentNumber-1))
                    { Channel = (FourBitNumber)midiChan }));
                }

                evList.Sort((a, b) =>
                {
                    if (a.tick != b.tick) return a.tick.CompareTo(b.tick);
                    // NoteOff und ProgramChange vor NoteOn bei gleichem Tick
                    int pri(MidiEvent e) => e is NoteOnEvent ? 1 : 0;
                    return pri(a.ev).CompareTo(pri(b.ev));
                });

                var track = new TrackChunk();
                track.Events.Add(new SequenceTrackNameEvent(trackName) { DeltaTime = 0 });

                long lastTick = 0;
                foreach (var (tick, ev) in evList)
                {
                    ev.DeltaTime = Math.Max(0, tick - lastTick);
                    lastTick = Math.Max(lastTick, tick);
                    track.Events.Add(ev);
                }

                midiFile.Chunks.Add(track);
            }
            return midiFile;
        }

        // -------------------------------------------------------------------------
        // Program-Auflösung: Override -> Heuristik -> Default
        // -------------------------------------------------------------------------
        private static byte ResolveProgram(string channelName,
            Dictionary<string, byte> overrides)
        {
            // 1. Exakter Override-Match (case-insensitive)
            foreach (var kv in overrides)
                if (string.Equals(kv.Key, channelName, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;

            // 2. Partial-Match Override (z.B. "Guitar" trifft "ElectricGuitarClean")
            var lower = channelName.ToLowerInvariant();
            foreach (var kv in overrides)
                if (lower.Contains(kv.Key.ToLowerInvariant()))
                    return kv.Value;

            // 3. Name-Heuristik
            return GuessProgram(lower);
        }

        private static byte GuessProgram(string nameLower)
        {
            // Drums / Percussion
            if (nameLower.Contains("kick") || nameLower.Contains("bassdrum")) return 117;
            if (nameLower.Contains("snare")) return 115;
            if (nameLower.Contains("cymbal")) return 127;
            if (nameLower.Contains("hihat") || nameLower.Contains("hi-hat")) return 127;
            if (nameLower.Contains("drum")) return 118;
            if (nameLower.Contains("perc")) return 116;

            // Bass
            if (nameLower.Contains("doublebass") || nameLower.Contains("contrabass")) return 43;
            if (nameLower.Contains("bass")) return 33;

            // Gitarre
            if (nameLower.Contains("guitarspecial") || nameLower.Contains("overdrive")) return 30;
            if (nameLower.Contains("guitar")) return 29;

            // Bläser
            if (nameLower.Contains("sax")) return 65;
            if (nameLower.Contains("trumpet")) return 56;
            if (nameLower.Contains("trombone")) return 57;
            if (nameLower.Contains("flute")) return 73;
            if (nameLower.Contains("oboe")) return 68;
            if (nameLower.Contains("clarinet")) return 71;

            // Streicher
            if (nameLower.Contains("violin") || nameLower.Contains("fiddle")) return 40;
            if (nameLower.Contains("cello")) return 42;
            if (nameLower.Contains("string")) return 48;

            // Keys
            if (nameLower.Contains("piano")) return 0;
            if (nameLower.Contains("organ")) return 16;
            if (nameLower.Contains("harp")) return 46;
            if (nameLower.Contains("marimba")) return 12;

            // Synth
            if (nameLower.Contains("lead")) return 80;
            if (nameLower.Contains("pad")) return 88;
            if (nameLower.Contains("synth")) return 81;

            // Default
            return 0; // Acoustic Grand Piano
        }

        // -------------------------------------------------------------------------
        // Hilfsmethoden
        // -------------------------------------------------------------------------
        private static uint ReadVarInt(BinaryReader br)
        {
            uint result = 0;
            int shift = 0;
            byte b;
            do
            {
                b = br.ReadByte();
                result |= (uint)(b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return result;
        }

        private struct FlpNote
        {
            public uint Position;
            public byte LengthTicks;
            public byte Key;
            public byte Velocity;
            public byte RackChannel;
        }
        private struct FlpProgram
        {
            // 4 Byte: Zeitstempel (Little-Endian uint, z.B. 6072 Ticks)
            public uint Timestamp;

            // 1 Byte: Padding / Status vor dem Command
            public byte UnknownPadding1;

            // 1 Byte: Der Command (Sollte immer 0x80 sein)
            public byte Command;

            // 1 Byte: Der MIDI-Kanal (0-15)
            public byte Channel;

            // 1 Byte: Padding zwischen Kanal und Instrument
            public byte UnknownPadding2;

            // 1 Byte: Die MIDI-Instrumenten-Nummer (0-127 / 128)
            public byte InstrumentNumber;

            // 3 Byte: Padding am Ende, um das 12-Byte-Alignment vollzumachen
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] EndPadding;
        }
    }
}