/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using Melanchall.DryWetMidi.Core;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BardMusicPlayer.Transmogrify.Song.Importers.MML
{
    public static class MMLConverter
    {
        public static MidiFile OpenMMLSongFile(string path)
        {
            string content = File.ReadAllText(path, Encoding.UTF8);
            return ConvertAuto(content);
        }

        // ── MML-String-API ────────────────────────────────────────────────

        public static MidiFile ConvertMml(string mml)
        {
            var result = ParseMml(mml);
            return MMLMidiWriter.BuildMidiFile(result.Key, result.Value);
        }

        // ── Internal core ─────────────────────────────────────────────────

        private static MidiFile ConvertAuto(string content)
        {
            string trimmed = content.TrimStart();

            bool isIni = trimmed.Contains("[Settings]") ||
                         Regex.IsMatch(trimmed, @"^\[Channel\d+\]", RegexOptions.Multiline);

            bool isProjectFile = isIni
                || trimmed.StartsWith("<")
                || trimmed.Contains("MML@")
                || trimmed.Contains("mml@")
                || trimmed.Contains("<Track")
                || trimmed.Contains("<track");

            if (isProjectFile)
            {
                var project = ThreeMleParser.Parse(content);
                return ConvertMml(BuildCombinedMml(project));
            }

            return ConvertMml(content);
        }

        private static KeyValuePair<List<MidiEvent>, int> ParseMml(string mml)
        {
            var tokens = new MMLLexer(mml).Tokenize();
            var parser = new MMLParser();
            var events = parser.Parse(tokens);
            return new KeyValuePair<List<MidiEvent>, int>(events, MMLParser.PPQ);
        }

        private static string BuildCombinedMml(ThreeMleFile project)
        {
            var sb = new StringBuilder("T" + project.Tempo + " ");
            foreach (var track in project.Tracks)
            {
                int ch = track.Channel > 0 ? track.Channel : 1;
                // Remove t-commands from the tracks — tempo is set globally at the start
                string mml = Regex.Replace(track.Mml, @"(?i)\bt\d+\s*", "").Trim();
                if (!string.IsNullOrWhiteSpace(mml))
                    sb.Append("CH" + ch + " " + mml + " ");
            }
            return sb.ToString().Trim();
        }
    }
}
