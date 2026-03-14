/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace BardMusicPlayer.Transmogrify.Song.Importers.MML
{
    public class ThreeMleTrack
    {
        public string Name { get; set; } = "";
        public int Channel { get; set; } = 1;    // 1-based
        public int Instrument { get; set; } = 0; // GM-Programm
        public string Mml { get; set; } = "";
    }

    public class ThreeMleFile
    {
        public string Title { get; set; } = "";
        public int Tempo { get; set; } = 120;
        public List<ThreeMleTrack> Tracks { get; set; } = new List<ThreeMleTrack>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ThreeMleParser – statisch
    //
    //  Unterstützte Formate:
    //
    //  A) 3MLE-Projektdatei (INI-Stil) — das native Speicherformat von 3MLE
    //  ──────────────────────────────────────────────────────────────────────
    //  [Settings]
    //  Encoding=iso-8859-1
    //  Title=Aldi Gaga - Pokerface
    //
    //  [Channel1]
    //  //#using_extension
    //  //#using_channel = 0
    //  // title : Songname
    //  /*M 0  */  t119
    //  /*M 1  */  l8o4cdefg
    //
    //  [Channel2]
    //  //#using_channel = 1
    //  // Flute
    //  /*M 0  */  y121,0@73r1
    //
    //  Mabinogi-MML-Besonderheiten (werden alle korrekt behandelt):
    //    n<0-127>   MIDI-Notennummer direkt
    //    y<cc>,<v>  Controller  (CC7/11 -> Volume, CC10 -> Pan, Rest ignoriert)
    //    @<prog>    Instrument-Change (GM-Programmnummer)
    //    t<bpm>     Tempo (wirkt in Mabinogi global, wird auf Spur 1 gesetzt)
    //    v<0-15>    Lautstärke (Mabinogi-Skala, wird auf MIDI-Velocity gemappt)
    //    /*M n */   Takt-Marker (Kommentar, wird ignoriert)
    //    //#using_channel = n   MIDI-Kanal (0-basiert)
    //    b als Vorzeichen: In Mabinogi ist '-' das b-Vorzeichen (nicht 'b',
    //    da 'B' eine Note ist). Der Lexer versteht '-' bereits korrekt.
    //
    //  B) XML-Format (<MusicData><Track>…</Track></MusicData>)
    //  C) Mabinogi-Clipboard  MML@track1,track2,track3;
    //  D) Raw-MML / Komma-getrennter Multitrack
    // ═══════════════════════════════════════════════════════════════════════
    public static class ThreeMleParser
    {
        public static ThreeMleFile ParseFile(string path)
        {
            // 3MLE-INI files are often in ISO-8859-1 (it’s  specified in the [Settings])
            string raw = File.ReadAllBytes(path)
                            .Pipe(b => TryDetectEncoding(b, out var enc)
                                       ? new StreamReader(new MemoryStream(b), enc).ReadToEnd()
                                       : Encoding.UTF8.GetString(b));
            return Parse(raw);
        }

        public static ThreeMleFile Parse(string content)
        {
            content = content.Trim();

            if (IsIniFormat(content)) return ParseIni(content);
            if (IsXmlFormat(content)) return ParseXml(content);
            if (IsMabiFormat(content)) return ParseMabiClipboard(content);
            return ParseRawMultiTrack(content);
        }

        // ── Format-Detection ─────────────────────────────────────────────

        private static bool IsIniFormat(string s) =>
            s.Contains("[Settings]") ||
            Regex.IsMatch(s, @"^\[Channel\d+\]", RegexOptions.Multiline);

        private static bool IsXmlFormat(string s) =>
            s.StartsWith("<") || s.StartsWith("<?");

        private static bool IsMabiFormat(string s) =>
            s.ToUpper().Contains("MML@");

        // ══════════════════════════════════════════════════════════════════
        //  A) INI-Parser – das real 3MLE
        // ══════════════════════════════════════════════════════════════════
        private static ThreeMleFile ParseIni(string content)
        {
            var file = new ThreeMleFile();
            var sections = SplitIntoSections(content);

            // [Settings]
            if (sections.TryGetValue("Settings", out var settingLines))
                ReadSettings(settingLines, file);

            // [Channel1], [Channel2], ..., sorted
            var channelKeys = sections.Keys
                .Where(k => Regex.IsMatch(k, @"^Channel\d+$", RegexOptions.IgnoreCase))
                .OrderBy(k => int.Parse(Regex.Match(k, @"\d+").Value));

            int fallbackChannel = 1;
            foreach (var key in channelKeys)
            {
                var track = ParseChannelSection(key, sections[key], file, fallbackChannel);
                if (track != null)
                {
                    file.Tracks.Add(track);
                    fallbackChannel++;
                }
            }

            // Use the tempo from the MML of the first track if [Settings] does not have one
            if (file.Tempo == 120)
                ExtractTempoFromTracks(file);

            return file;
        }

        private static Dictionary<string, List<string>> SplitIntoSections(string content)
        {
            var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            string current = "";
            sections[current] = new List<string>();

            foreach (var rawLine in content.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                var m = Regex.Match(line, @"^\[(.+?)\]");
                if (m.Success)
                {
                    current = m.Groups[1].Value.Trim();
                    if (!sections.ContainsKey(current))
                        sections[current] = new List<string>();
                }
                else
                    sections[current].Add(line);
            }

            return sections;
        }

        private static void ReadSettings(List<string> lines, ThreeMleFile file)
        {
            foreach (var line in lines)
            {
                var m = Regex.Match(line, @"^Title\s*=\s*(.+)", RegexOptions.IgnoreCase);
                if (m.Success) { file.Title = m.Groups[1].Value.Trim(); continue; }

                m = Regex.Match(line, @"^Tempo\s*=\s*(\d+)", RegexOptions.IgnoreCase);
                if (m.Success) file.Tempo = int.Parse(m.Groups[1].Value);
            }
        }

        private static ThreeMleTrack ParseChannelSection(
            string sectionKey, List<string> lines, ThreeMleFile file, int fallbackChannel)
        {
            // Derive the standard channel from the section number (Channel 1 -> MIDI 0, Channel 2 -> MIDI 1 …)
            int sectionNum = int.Parse(Regex.Match(sectionKey, @"\d+").Value);
            int midiChannel = sectionNum - 1;  // 0-base intern, 1-base in Track-Model
            string trackName = sectionKey;
            var mmlParts = new List<string>();

            foreach (var rawLine in lines)
            {
                string line = Regex.Replace(rawLine, @"/\*M\s*\d+\s*\*/", "").Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var chMatch = Regex.Match(line, @"//\s*#using_channel\s*=\s*(\d+)");
                if (chMatch.Success)
                {
                    midiChannel = int.Parse(chMatch.Groups[1].Value);
                    continue;
                }

                var titleMatch = Regex.Match(line,
                    @"//\s*(?:title|name)\s*[:=]\s*(.+)", RegexOptions.IgnoreCase);
                if (titleMatch.Success)
                {
                    trackName = titleMatch.Groups[1].Value.Trim();
                    if (string.IsNullOrEmpty(file.Title)) file.Title = trackName;
                    continue;
                }

                if (line.TrimStart().StartsWith("//")) continue;

                line = Regex.Replace(line, @"/\*.*?\*/", "", RegexOptions.Singleline).Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                line = NormalizeMabiMml(line);
                if (!string.IsNullOrWhiteSpace(line))
                    mmlParts.Add(line);
            }

            string mml = string.Join(" ", mmlParts).Trim();

            // If the track contains only tempo commands (e.g. Channel1 in Mabinogi files)
            // Set the tempo globally and do not output the track
            string mmlWithoutTempo = Regex.Replace(mml, @"(?i)\bt\d+\s*", "").Trim();
            if (string.IsNullOrWhiteSpace(mmlWithoutTempo))
            {
                var tempoMatch = Regex.Match(mml, @"(?i)\bt(\d+)");
                if (tempoMatch.Success && file.Tempo == 120)
                    file.Tempo = int.Parse(tempoMatch.Groups[1].Value);
                return null;
            }

            return new ThreeMleTrack
            {
                Name = trackName,
                Channel = midiChannel + 1,   // intern 1-basiert
                Mml = mml
            };
        }

        // ── Mabinogi MML Normalisation ───────────────────────────────────
        //
        // The 3MLE format contains some extensions that the lexer does not
        // recognise directly. These are converted here into compatible equivalents.
        private static string NormalizeMabiMml(string mml)
        {
            // y<cc>,<val> -> Controller-Befehle auflösen
            //   CC7  / CC11 = Volume        -> V0-V15 (Mabinogi-Scale)
            //   CC10         = Pan          -> P0-127
            //   CC0, CC32    = Bank Select  -> ignore
            //   CC121        = Reset All    -> ignore
            //   Rest                        -> ignore
            mml = Regex.Replace(mml, @"(?i)y(\d+)\s*,\s*(\d+)", m =>
            {
                int cc = int.Parse(m.Groups[1].Value);
                int val = int.Parse(m.Groups[2].Value);
                if (cc == 7 || cc == 11)
                    return "V" + (int)Math.Round(val / 127.0 * 15);
                if (cc == 10)
                    return "P" + val;
                return "";
            });

            // n<0-127> -> direct MIDI note number
            // Lexer does not recognise this -> convert to octave+note
            // n60 = C4, n61 = C#4, n62 = D4, …
            mml = Regex.Replace(mml, @"(?i)\bn(\d+)", m =>
            {
                int midi = Math.Max(0, Math.Min(127, int.Parse(m.Groups[1].Value)));
                int octave = (midi / 12) - 1;
                int semi = midi % 12;
                // Chromatic scale -> Note + accident
                string[] notes = { "C", "C+", "D", "D+", "E", "F", "F+", "G", "G+", "A", "A+", "B" };
                return $"O{octave}{notes[semi]}";
            });

            // Normalise spaces
            return Regex.Replace(mml, @"\s+", " ").Trim();
        }

        private static void ExtractTempoFromTracks(ThreeMleFile file)
        {
            foreach (var track in file.Tracks)
            {
                var m = Regex.Match(track.Mml, @"(?i)\bt(\d+)");
                if (m.Success)
                {
                    file.Tempo = int.Parse(m.Groups[1].Value);
                    return;
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  B) XML-Format
        // ══════════════════════════════════════════════════════════════════
        private static ThreeMleFile ParseXml(string xml)
        {
            XDocument doc;
            try { doc = XDocument.Parse(xml); }
            catch (Exception ex)
            {
                throw new FormatException($"Ungültiges XML: {ex.Message}");
            }

            var root = doc.Root ?? throw new FormatException("Leeres XML-Dokument.");

            var file = new ThreeMleFile
            {
                Title = AttrStr(root, "title", AttrStr(root, "name", "")),
                Tempo = AttrInt(root, "tempo", 120),
            };

            var trackEls = root.Descendants()
                .Where(e => IsTrackElement(e.Name.LocalName))
                .ToList();

            if (trackEls.Count == 0)
            {
                string rootMml = root.Value.Trim();
                if (!string.IsNullOrEmpty(rootMml))
                    file.Tracks.Add(new ThreeMleTrack { Name = "Track 1", Channel = 1, Mml = rootMml });
                return file;
            }

            int autoCh = 1;
            foreach (var el in trackEls)
            {
                int ch = AttrInt(el, "channel", AttrInt(el, "ch", AttrInt(el, "part", autoCh)));
                var track = new ThreeMleTrack
                {
                    Name = AttrStr(el, "name", AttrStr(el, "title", $"Track {autoCh}")),
                    Channel = ch,
                    Instrument = AttrInt(el, "instrument", AttrInt(el, "program", AttrInt(el, "inst", 0))),
                    Mml = NormalizeXmlMml(el.Value)
                };

                var mmlNode = el.Element("MML") ?? el.Element("mml") ?? el.Element("Data");
                if (mmlNode != null) track.Mml = NormalizeXmlMml(mmlNode.Value);

                if (!string.IsNullOrWhiteSpace(track.Mml))
                {
                    if (track.Instrument != 0 && !track.Mml.TrimStart().StartsWith("@"))
                        track.Mml = $"@{track.Instrument} {track.Mml}";
                    file.Tracks.Add(track);
                }
                autoCh++;
            }

            return file;
        }

        private static string NormalizeXmlMml(string raw) =>
            string.Join(" ", raw.Split(new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim())).Trim();

        // ══════════════════════════════════════════════════════════════════
        //  C) Mabinogi-Clipboard  MML@t1,t2,t3;
        // ══════════════════════════════════════════════════════════════════
        private static ThreeMleFile ParseMabiClipboard(string text)
        {
            var file = new ThreeMleFile();

            foreach (var line in text.Split('\n'))
            {
                var t = line.Trim();
                if (t.StartsWith("//title=", StringComparison.OrdinalIgnoreCase))
                    file.Title = t.Substring(8).Trim();
                else if (t.StartsWith("//tempo=", StringComparison.OrdinalIgnoreCase) &&
                         int.TryParse(t.Substring(8).Trim(), out int bpm))
                    file.Tempo = bpm;
            }

            int start = text.IndexOf("MML@", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return file;
            start += 4;

            int end = text.IndexOf(';', start);
            string body = end >= 0 ? text.Substring(start, end - start) : text.Substring(start);
            var parts = body.Split(',');

            for (int i = 0; i < parts.Length; i++)
            {
                string mml = parts[i].Trim();
                if (!string.IsNullOrEmpty(mml))
                    file.Tracks.Add(new ThreeMleTrack
                    {
                        Name = $"Track {i + 1}",
                        Channel = i + 1,
                        Mml = mml
                    });
            }

            return file;
        }

        // ══════════════════════════════════════════════════════════════════
        //  D) Raw-Multitrack (separated by comma)
        // ══════════════════════════════════════════════════════════════════
        private static ThreeMleFile ParseRawMultiTrack(string text)
        {
            var file = new ThreeMleFile();
            var parts = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string mml = parts[i].Trim();
                if (!string.IsNullOrEmpty(mml))
                    file.Tracks.Add(new ThreeMleTrack
                    {
                        Name = $"Track {i + 1}",
                        Channel = i + 1,
                        Mml = mml
                    });
            }
            return file;
        }

        // ── Encoding-Detection ────────────────────────────────────────────

        private static bool TryDetectEncoding(byte[] bytes, out Encoding encoding)
        {
            // BOM-Detection
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            { encoding = Encoding.UTF8; return true; }

            // 3MLE sets Encoding= in [Settings] — readable as ASCII
            string ascii = Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 512));
            var m = Regex.Match(ascii, @"Encoding\s*=\s*([^\r\n]+)", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                try
                {
                    encoding = Encoding.GetEncoding(m.Groups[1].Value.Trim());
                    return true;
                }
                catch { /* unknown Encoding -> UTF-8 */ }
            }

            encoding = Encoding.UTF8;
            return false;
        }

        // ── Hilfsmethoden ─────────────────────────────────────────────────

        private static bool IsTrackElement(string name) =>
            new[] { "track", "part", "channel", "voice", "melody", "harmony" }
                .Contains(name, StringComparer.OrdinalIgnoreCase);

        private static string AttrStr(XElement el, string name, string fallback)
        {
            var attr = el.Attribute(name)
                    ?? el.Attribute(name.ToLowerInvariant())
                    ?? el.Attribute(name.ToUpperInvariant());
            return attr?.Value ?? fallback;
        }

        private static int AttrInt(XElement el, string name, int fallback)
        {
            string val = AttrStr(el, name, null);
            return val != null && int.TryParse(val, out int r) ? r : fallback;
        }
    }

    // ── Pipeline-Helper ───────────────────────────────────────────────────
    internal static class PipeExtension
    {
        public static TOut Pipe<TIn, TOut>(this TIn v, Func<TIn, TOut> f) => f(v);
    }
}
