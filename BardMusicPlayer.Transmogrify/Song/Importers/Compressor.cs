/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace BardMusicPlayer.Transmogrify.Song.Importers;

public class BitStreamWriter
{
    private readonly MemoryStream _stream = new MemoryStream();
    private byte _currentByte = 0;
    private int _bitCount = 0;

    public void WriteBits(int value, int numBits)
    {
        for (int i = numBits - 1; i >= 0; i--)
        {
            int bit = (value >> i) & 1;
            _currentByte = (byte)((_currentByte << 1) | bit);
            _bitCount++;

            if (_bitCount == 8)
            {
                _stream.WriteByte(_currentByte);
                _currentByte = 0;
                _bitCount = 0;
            }
        }
    }

    public byte[] Flush()
    {
        if (_bitCount > 0)
        {
            _currentByte <<= (8 - _bitCount);
            _stream.WriteByte(_currentByte);
            _currentByte = 0;
            _bitCount = 0;
        }
        return _stream.ToArray();
    }
}

public class BitStreamReader
{
    private readonly byte[] _data;
    private int _byteIndex = 0;
    private int _bitIndex = 7;

    public BitStreamReader(byte[] data)
    {
        _data = data;
    }

    public bool HasMore => _byteIndex < _data.Length;

    public int ReadBit()
    {
        if (!HasMore) return 0;
        int bit = (_data[_byteIndex] >> _bitIndex) & 1;
        _bitIndex--;
        if (_bitIndex < 0)
        {
            _bitIndex = 7;
            _byteIndex++;
        }
        return bit;
    }

    public int ReadBits(int numBits)
    {
        int value = 0;
        for (int i = 0; i < numBits; i++)
        {
            value = (value << 1) | ReadBit();
        }
        return value;
    }
}


//Version 6k
public static class MidiRangeCompressor
{
    private const long TICK_MS = 50;
    private const int MIN_NOTE = 48; // C3
    private const int MAX_NOTE = 84; // C6

    public static string CompressMidiToBase64(string midiFilePath)
    {
        var midiFile = MidiFile.Read(midiFilePath);
        TempoMap tempoMap = midiFile.GetTempoMap();

        var allNotes = new List<CompressedNoteEvent>();
        var allProgramChanges = new List<CompressedPCEvent>();

        var tracks = midiFile.GetTrackChunks().ToList();
        for (int trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
        {
            var track = tracks[trackIndex];
            var trackNotes = track.GetNotes().Select(n => {
                double startMs = TimeConverter.ConvertTo<MetricTimeSpan>(n.Time, tempoMap).TotalMilliseconds;
                double durationMs = LengthConverter.ConvertTo<MetricTimeSpan>(n.Length, n.Time, tempoMap).TotalMilliseconds;
                return new CompressedNoteEvent
                {
                    StartTick = (long)Math.Round(startMs) / TICK_MS,
                    DurationTicks = Math.Max(1, (long)Math.Round(durationMs) / TICK_MS),
                    ChannelOrTrack = trackIndex,
                    NoteNumber = Math.Max(MIN_NOTE, Math.Min(MAX_NOTE, (int)n.NoteNumber))
                };
            });
            allNotes.AddRange(trackNotes);

            var trackPCs = track.GetTimedEvents().Where(e => e.Event is ProgramChangeEvent).Select(e => {
                double ms = TimeConverter.ConvertTo<MetricTimeSpan>(e.Time, tempoMap).TotalMilliseconds;
                return new CompressedPCEvent
                {
                    Tick = (long)Math.Round(ms) / TICK_MS,
                    ChannelOrTrack = trackIndex,
                    ProgramNumber = (int)((ProgramChangeEvent)e.Event).ProgramNumber
                };
            });
            allProgramChanges.AddRange(trackPCs);
        }

        long maxTick = 0;
        if (allNotes.Any()) maxTick = Math.Max(maxTick, allNotes.Max(n => n.StartTick));
        if (allProgramChanges.Any()) maxTick = Math.Max(maxTick, allProgramChanges.Max(p => p.Tick));

        if (maxTick == 0 && !allNotes.Any()) return "";

        var writer = new BitStreamWriter();

        for (long currentTick = 0; currentTick <= maxTick; currentTick++)
        {
            var currentPC = allProgramChanges.Where(p => p.Tick == currentTick);
            foreach (var pc in currentPC)
            {
                writer.WriteBits(3, 2); // Header '11'
                writer.WriteBits(pc.ChannelOrTrack & 0x0F, 4);
                writer.WriteBits(pc.ProgramNumber, 7);
            }

            var currentNotes = allNotes.Where(n => n.StartTick == currentTick);
            foreach (var note in currentNotes)
            {
                writer.WriteBits(2, 2); // Header '10'
                writer.WriteBits(note.ChannelOrTrack & 0x0F, 4);
                writer.WriteBits(note.NoteNumber - MIN_NOTE, 6);
                int durBits = (int)Math.Min(31, note.DurationTicks - 1);
                writer.WriteBits(durBits, 5);
            }

            writer.WriteBits(0, 1); // Zeitschritt
        }

        byte[] rawBytes = writer.Flush();
        using (var outputStream = new MemoryStream())
        {
            using (var compressor = new GZipStream(outputStream, CompressionLevel.Optimal))
            {
                compressor.Write(rawBytes, 0, rawBytes.Length);
            }
            File.WriteAllBytes(midiFilePath + ".raw", outputStream.ToArray());

            string finalCrunchedText = PackTextRepeats(EncodeBase85(outputStream.ToArray()));
            return finalCrunchedText;
        }
    }

    private static string EncodeBase85(byte[] data)
    {
        var sb = new System.Text.StringBuilder((data.Length * 5 / 4) + 5);
        int count = 0;
        uint value = 0;

        foreach (byte b in data)
        {
            value = (value << 8) | b;
            count++;

            if (count == 4)
            {
                // Ein 32-Bit-Wort in 5 Base85-Zeichen zerlegen
                for (int i = 4; i >= 0; i--)
                {
                    sb.Append((char)((value / (uint)Math.Pow(85, i)) % 85 + 33));
                }
                value = 0;
                count = 0;
            }
        }

        // Reste (Padding) verarbeiten, falls die Bytes nicht durch 4 teilbar sind
        if (count > 0)
        {
            int padding = 4 - count;
            value <<= (padding * 8);
            for (int i = 4; i >= padding; i--)
            {
                sb.Append((char)((value / (uint)Math.Pow(85, i)) % 85 + 33));
            }
        }

        return sb.ToString();
    }

    public static string PackTextRepeats(string base85Text)
    {
        if (string.IsNullOrEmpty(base85Text)) return "";

        var sb = new System.Text.StringBuilder();
        int i = 0;

        while (i < base85Text.Length)
        {
            char currentChar = base85Text[i];
            int runLength = 1;

            // Zählen, wie oft sich das Zeichen direkt wiederholt
            while (i + runLength < base85Text.Length &&
                   base85Text[i + runLength] == currentChar &&
                   runLength < 10) // Maximal 10er-Blöcke pro Marker
            {
                runLength++;
            }

            // Wenn sich ein Zeichen 3-mal oder öfter wiederholt, crunchen wir es
            if (runLength >= 3)
            {
                // Wir nutzen die ungenutzten ASCII-Zeichen ab 'v' (ASCII 118) als Marker
                // Ein 'v' bedeutet: Das vorherige Zeichen wird noch 2-mal wiederholt (insg. 3)
                // Ein 'w' bedeutet: 3-mal wiederholt (insg. 4), etc.
                char marker = (char)('v' + (runLength - 3));
                sb.Append(currentChar);
                sb.Append(marker);
                i += runLength;
            }
            else
            {
                sb.Append(currentChar);
                i++;
            }
        }

        return sb.ToString();
    }

    private class CompressedNoteEvent
    {
        public long StartTick { get; set; }
        public long DurationTicks { get; set; }
        public int ChannelOrTrack { get; set; }
        public int NoteNumber { get; set; }
    }

    private class CompressedPCEvent
    {
        public long Tick { get; set; }
        public int ChannelOrTrack { get; set; }
        public int ProgramNumber { get; set; }
    }
}

public static class MidiRangeDecompressor
{
    private const int TICKS_PER_QUARTER_NOTE = 480;
    private const int MIDI_DELTA_PER_TICK = 48;
    private const int MIN_NOTE = 48;

    public static void DecompressBase64ToMidi(string base64String, string outputFilePath)
    {
        byte[] crunchedData = DecodeBase85(UnpackTextRepeats(base64String));
        byte[] decompressedBytes;

        // Sicher entpacken im RAM
        using (var inputStream = new MemoryStream(crunchedData))
        using (var decompressor = new GZipStream(inputStream, CompressionMode.Decompress))
        using (var outputStream = new MemoryStream())
        {
            decompressor.CopyTo(outputStream);
            decompressedBytes = outputStream.ToArray();
        }

        var reader = new BitStreamReader(decompressedBytes);
        var midiFile = new MidiFile
        {
            TimeDivision = new TicksPerQuarterNoteTimeDivision(TICKS_PER_QUARTER_NOTE)
        };

        var tracks = new TrackChunk[16];
        for (int i = 0; i < 16; i++)
        {
            tracks[i] = new TrackChunk();
            midiFile.Chunks.Add(tracks[i]);
        }

        long[] trackCurrentDeltas = new long[16];
        var tempoEvent = new SetTempoEvent(500000) { DeltaTime = 0 };
        tracks[0].Events.Add(tempoEvent);

        var pendingNoteOffs = new List<(long targetAbsTick, int track, NoteOffEvent ev)>();
        long currentGlobalAbsTick = 0;

        // Sichere Abbruchbedingung: Solange echte Bits im Stream vorhanden sind
        while (reader.HasMore || pendingNoteOffs.Any(n => n.targetAbsTick <= currentGlobalAbsTick))
        {
            var readyNoteOffs = pendingNoteOffs.Where(n => n.targetAbsTick <= currentGlobalAbsTick).ToList();
            foreach (var noteOff in readyNoteOffs)
            {
                int t = noteOff.track;
                noteOff.ev.DeltaTime = currentGlobalAbsTick - trackCurrentDeltas[t];
                tracks[t].Events.Add(noteOff.ev);
                trackCurrentDeltas[t] = currentGlobalAbsTick;
                pendingNoteOffs.Remove(noteOff);
            }

            if (!reader.HasMore)
            {
                if (pendingNoteOffs.Any())
                {
                    currentGlobalAbsTick = pendingNoteOffs.Min(n => n.targetAbsTick);
                }
                continue;
            }

            int firstBit = reader.ReadBit();

            if (firstBit == 0)
            {
                currentGlobalAbsTick += MIDI_DELTA_PER_TICK;
            }
            else
            {
                int nextBit = reader.ReadBit();
                int typeId = (firstBit << 1) | nextBit;

                int trackIndex = reader.ReadBits(4) & 0x0F;

                if (typeId == 2) // Note On
                {
                    int mappedNote = reader.ReadBits(6);
                    int realNoteNumber = mappedNote + MIN_NOTE;

                    int durationBits = reader.ReadBits(5);
                    int durationTicks = durationBits + 1;
                    long noteOffAbsTick = currentGlobalAbsTick + (durationTicks * MIDI_DELTA_PER_TICK);

                    var noteOnEvent = new NoteOnEvent((SevenBitNumber)realNoteNumber, (SevenBitNumber)64)
                    {
                        Channel = (FourBitNumber)trackIndex,
                        DeltaTime = currentGlobalAbsTick - trackCurrentDeltas[trackIndex]
                    };
                    tracks[trackIndex].Events.Add(noteOnEvent);
                    trackCurrentDeltas[trackIndex] = currentGlobalAbsTick;

                    var noteOffEvent = new NoteOffEvent((SevenBitNumber)realNoteNumber, (SevenBitNumber)0)
                    {
                        Channel = (FourBitNumber)trackIndex
                    };
                    pendingNoteOffs.Add((noteOffAbsTick, trackIndex, noteOffEvent));
                }
                else if (typeId == 3) // Program Change
                {
                    int programVal = reader.ReadBits(7);
                    var pcEvent = new ProgramChangeEvent((SevenBitNumber)programVal)
                    {
                        Channel = (FourBitNumber)trackIndex,
                        DeltaTime = currentGlobalAbsTick - trackCurrentDeltas[trackIndex]
                    };
                    tracks[trackIndex].Events.Add(pcEvent);
                    trackCurrentDeltas[trackIndex] = currentGlobalAbsTick;
                }
            }
        }
        midiFile.Write(outputFilePath, true);
    }

    private static byte[] DecodeBase85(string text)
    {
        var ms = new MemoryStream();
        int count = 0;
        uint value = 0;

        foreach (char c in text)
        {
            value = value * 85 + (uint)(c - 33);
            count++;

            if (count == 5)
            {
                ms.WriteByte((byte)((value >> 24) & 0xFF));
                ms.WriteByte((byte)((value >> 16) & 0xFF));
                ms.WriteByte((byte)((value >> 8) & 0xFF));
                ms.WriteByte((byte)(value & 0xFF));
                value = 0;
                count = 0;
            }
        }

        // Reste auswerten
        if (count > 0)
        {
            int padding = 5 - count;
            for (int i = 0; i < padding; i++) value = value * 85 + 84;

            if (count > 1) ms.WriteByte((byte)((value >> 24) & 0xFF));
            if (count > 2) ms.WriteByte((byte)((value >> 16) & 0xFF));
            if (count > 3) ms.WriteByte((byte)((value >> 8) & 0xFF));
        }

        return ms.ToArray();
    }

    public static string UnpackTextRepeats(string crunchedText)
    {
        if (string.IsNullOrEmpty(crunchedText)) return "";

        var sb = new System.Text.StringBuilder();
        int i = 0;

        while (i < crunchedText.Length)
        {
            char c = crunchedText[i];

            // Prüfen, ob das nächste Zeichen ein RLE-Marker ist (>= 'v')
            if (i + 1 < crunchedText.Length && crunchedText[i + 1] >= 'v' && crunchedText[i + 1] <= 'z')
            {
                char marker = crunchedText[i + 1];
                int extraRepeats = marker - 'v' + 2; // Berechnen, wie viele dazu gehören

                for (int r = 0; r <= extraRepeats; r++)
                {
                    sb.Append(c);
                }
                i += 2; // Zeichen und Marker überspringen
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }

        return sb.ToString();
    }
}
