/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System.Collections.Generic;
using System.Text;

namespace BardMusicPlayer.Transmogrify.Song.Importers.MML
{
    // ═══════════════════════════════════════════════════════════════════════
    //  Token-Types
    // ═══════════════════════════════════════════════════════════════════════
    public enum TokenType
    {
        Note,           // C D E F G A B
        Rest,           // R
        Octave,         // O4
        OctaveUp,       // >
        OctaveDown,     // <
        Length,         // L8
        Tempo,          // T120
        Volume,         // V12
        Channel,        // CH1
        ChordStart,     // [
        ChordEnd,       // ]
        Dot,            // .  (Länge × 1,5)
        Tie,            // &
        Pan,            // P
        Instrument,     // @3  (GM-Program)
        Eof
    }

    public class Token
    {
        public TokenType Type;
        public string Raw;
        public int IntValue;    // Value (Octave, Tempo …)
        public char NoteChar;   // 'C'..'B'
        public int Accidental;  // -1 = b, 0 = nat, +1 = #
        public int Length;      // Note-length (1=full, 2=half, ...), 0 = default
        public bool Dotted;     // A full stop immediately after the note
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Lexer
    // ═══════════════════════════════════════════════════════════════════════
    public class MMLLexer
    {
        private readonly string _src;
        private int _pos;

        public MMLLexer(string source)
        {
            // Remove comments
            _src = StripComments(source.ToUpperInvariant());
            _pos = 0;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            while (_pos < _src.Length)
            {
                SkipWhitespace();
                if (_pos >= _src.Length) break;

                char c = _src[_pos];
                Token t = null;

                if (c == 'C' && Peek(1) == 'H') t = ReadChannel();
                else if ("CDEFGAB".IndexOf(c) >= 0) t = ReadNote();
                else if (c == 'R') t = ReadRest();
                else if (c == 'O') t = ReadOctave();
                else if (c == '>') t = Simple(TokenType.OctaveUp);
                else if (c == '<') t = Simple(TokenType.OctaveDown);
                else if (c == 'L') t = ReadLength();
                else if (c == 'T') t = ReadIntCommand(TokenType.Tempo, 20, 600);
                else if (c == 'V') t = ReadIntCommand(TokenType.Volume, 0, 15);
                else if (c == 'P') t = ReadIntCommand(TokenType.Pan, 0, 127);
                else if (c == '@') t = ReadIntCommand(TokenType.Instrument, 0, 127, skip: '@');
                else if (c == '[') t = Simple(TokenType.ChordStart);
                else if (c == ']') t = Simple(TokenType.ChordEnd);
                else if (c == '.') t = Simple(TokenType.Dot);
                else if (c == '&') t = Simple(TokenType.Tie);
                else { _pos++; continue; }  // skip unkown chars

                if (t != null) tokens.Add(t);
            }
            tokens.Add(new Token { Type = TokenType.Eof });
            return tokens;
        }

        #region Helper
        private Token ReadNote()
        {
            var t = new Token { Type = TokenType.Note, NoteChar = _src[_pos++] };
            t.Accidental = ReadAccidental();
            t.Length = ReadOptionalInt();
            t.Dotted = ConsumeDot();
            return t;
        }

        private Token ReadRest()
        {
            _pos++; // 'R'
            var t = new Token { Type = TokenType.Rest };
            t.Length = ReadOptionalInt();
            t.Dotted = ConsumeDot();
            return t;
        }

        private Token ReadOctave()
        {
            _pos++; // 'O'
            int val = ReadRequiredInt(0, 8);
            return new Token { Type = TokenType.Octave, IntValue = val };
        }

        private Token ReadLength()
        {
            _pos++; // 'L'
            int val = ReadRequiredInt(1, 64);
            bool dotted = ConsumeDot();
            return new Token { Type = TokenType.Length, IntValue = val, Dotted = dotted };
        }

        private Token ReadChannel()
        {
            _pos += 2; // 'CH'
            int val = ReadRequiredInt(1, 16);
            return new Token { Type = TokenType.Channel, IntValue = val };
        }

        private Token ReadIntCommand(TokenType type, int min, int max, char skip = '\0')
        {
            _pos++; // Command-Character
            if (skip != '\0' && _pos < _src.Length && _src[_pos] == skip) _pos++;
            int val = ReadRequiredInt(min, max);
            return new Token { Type = type, IntValue = val };
        }

        private Token Simple(TokenType type)
        {
            _pos++;
            return new Token { Type = type };
        }

        private int ReadAccidental()
        {
            if (_pos >= _src.Length) return 0;
            if (_src[_pos] == '#' || _src[_pos] == '+') { _pos++; return 1; }
            if (_src[_pos] == '-' || _src[_pos] == 'B') { _pos++; return -1; }
            return 0;
        }

        private int ReadOptionalInt()
        {
            if (_pos >= _src.Length || !char.IsDigit(_src[_pos])) return 0;
            return ReadInt();
        }

        private int ReadRequiredInt(int min, int max)
        {
            int v = ReadInt();
            if (v < min) v = min;
            if (v > max) v = max;
            return v;
        }

        private int ReadInt()
        {
            int start = _pos;
            while (_pos < _src.Length && char.IsDigit(_src[_pos])) _pos++;
            return _pos == start ? 0 : int.Parse(_src.Substring(start, _pos - start));
        }

        private bool ConsumeDot()
        {
            if (_pos < _src.Length && _src[_pos] == '.')
            {
                _pos++;
                return true;
            }
            return false;
        }

        private void SkipWhitespace()
        {
            while (_pos < _src.Length && char.IsWhiteSpace(_src[_pos])) _pos++;
        }

        private char Peek(int offset) =>
            (_pos + offset < _src.Length) ? _src[_pos + offset] : '\0';

        private static string StripComments(string src)
        {
            // One line comments
            var sb = new StringBuilder();
            int i = 0;
            while (i < src.Length)
            {
                if (i + 1 < src.Length && src[i] == '/' && src[i + 1] == '/')
                {
                    while (i < src.Length && src[i] != '\n') i++;
                }
                else if (i + 1 < src.Length && src[i] == '/' && src[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < src.Length && !(src[i] == '*' && src[i + 1] == '/')) i++;
                    i += 2;
                }
                else sb.Append(src[i++]);
            }
            return sb.ToString();
        }
        #endregion
    }
}
