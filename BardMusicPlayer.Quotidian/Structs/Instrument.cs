/*
 * Copyright(c) 2025 GiR-Zippo, 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BardMusicPlayer.Quotidian.Enums;

namespace BardMusicPlayer.Quotidian.Structs
{
    /// <summary>
    /// Represents available instruments in game.
    /// </summary>
    public readonly struct Instrument : IComparable, IConvertible, IComparable<Instrument>, IEquatable<Instrument>
    {
        public static readonly Instrument None = new("None", 0, 122, OctaveRange.Invalid, false, 100, 0, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE0, new ReadOnlyCollection<string>(new List<string> { }));

        public static readonly Instrument Harp = new("Harp", 1, 46, OctaveRange.C3toC6, false, 50, 1, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE0, new ReadOnlyCollection<string>(new List<string> { "OrchestralHarp", "orchestralharps", "harps" }));
        public static readonly Instrument Piano = new("Piano", 2, 0, OctaveRange.C4toC7, false, 50, 1, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE1, new ReadOnlyCollection<string>(new List<string> { "AcousticGrandPiano", "acousticgrandpianos", "pianos" }));
        public static readonly Instrument Lute = new("Lute", 3, 24, OctaveRange.C2toC5, false, 50, 1, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE2, new ReadOnlyCollection<string>(new List<string> { "guitar", "guitars", "lutes" }));
        public static readonly Instrument Fiddle = new("Fiddle", 4, 45, OctaveRange.C2toC5, false, 50, 1, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE3, new ReadOnlyCollection<string>(new List<string> { "PizzicatoStrings", "fiddles" }));
        public static readonly IReadOnlyList<Instrument> Strummed = new ReadOnlyCollection<Instrument>(new List<Instrument> { Harp, Piano, Lute, Fiddle });

        public static readonly Instrument Flute = new("Flute", 5, 73, OctaveRange.C4toC7, true, 50, 2, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE0, new ReadOnlyCollection<string>(new List<string> { "flutes" }));
        public static readonly Instrument Oboe = new("Oboe", 6, 68, OctaveRange.C4toC7, true, 50, 2, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE1, new ReadOnlyCollection<string>(new List<string> { "oboes" }));
        public static readonly Instrument Clarinet = new("Clarinet", 7, 71, OctaveRange.C3toC6, true, 50, 2, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE2, new ReadOnlyCollection<string>(new List<string> { "clarinets" }));
        public static readonly Instrument Fife = new("Fife", 8, 72, OctaveRange.C5toC8, true, 50, 2, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE3, new ReadOnlyCollection<string>(new List<string> { "fifes", "Piccolo", "piccolos", "ocarina", "ocarinas" }));
        public static readonly Instrument Panpipes = new("Panpipes", 9, 75, OctaveRange.C4toC7, true, 50, 2, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE4, new ReadOnlyCollection<string>(new List<string> { "panpipe", "Panflute", "panflutes" }));
        public static readonly IReadOnlyList<Instrument> Wind = new ReadOnlyCollection<Instrument>(new List<Instrument> { Flute, Oboe, Clarinet, Fife, Panpipes });

        public static readonly Instrument Timpani = new("Timpani", 10, 47, OctaveRange.C2toC5, false, 50, 3, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE0, new ReadOnlyCollection<string>(new List<string> { "timpanis" }));
        public static readonly Instrument Bongo = new("Bongo", 11, 116, OctaveRange.C3toC6, false, 50, 3, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE1, new ReadOnlyCollection<string>(new List<string> { "bongos" }));
        public static readonly Instrument BassDrum = new("BassDrum", 12, 117, OctaveRange.C2toC5, false, 50, 3, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE2, new ReadOnlyCollection<string>(new List<string> { "bassdrums", "bass_drum", "bass_drums", "kick" }));
        public static readonly Instrument SnareDrum = new("SnareDrum", 13, 115, OctaveRange.C3toC6, false, 50, 3, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE3, new ReadOnlyCollection<string>(new List<string> { "snaredrums", "Snare", "snare_drum", "RiceDrum" }));
        public static readonly Instrument Cymbal = new("Cymbal", 14, 127, OctaveRange.C3toC6, false, 100, 3, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE4, new ReadOnlyCollection<string>(new List<string> { "cymbals" }));
        public static readonly IReadOnlyList<Instrument> Drums = new ReadOnlyCollection<Instrument>(new List<Instrument> { Timpani, Bongo, BassDrum, SnareDrum, Cymbal });

        public static readonly Instrument Trumpet = new("Trumpet", 15, 56, OctaveRange.C3toC6, true, 100, 4, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE0, new ReadOnlyCollection<string>(new List<string> { "trumpets", "Humpet" }));
        public static readonly Instrument Trombone = new("Trombone", 16, 57, OctaveRange.C2toC5, true, 100, 4, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE1, new ReadOnlyCollection<string>(new List<string> { "trombones", "Tromboner" }));
        public static readonly Instrument Tuba = new("Tuba", 17, 58, OctaveRange.C1toC4, true, 100, 4, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE2, new ReadOnlyCollection<string>(new List<string> { "tubas", "Booba" }));
        public static readonly Instrument Horn = new("Horn", 18, 60, OctaveRange.C2toC5, true, 100, 4, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE3, new ReadOnlyCollection<string>(new List<string> { "FrenchHorn", "frenchhorns", "horns", "Horny" }));
        public static readonly Instrument Saxophone = new("Saxophone", 19, 65, OctaveRange.C3toC6, true, 100, 4, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE4, new ReadOnlyCollection<string>(new List<string> { "Sax", "AltoSaxophone", "AltoSax", "Sexophone" }));
        public static readonly IReadOnlyList<Instrument> Brass = new ReadOnlyCollection<Instrument>(new List<Instrument> { Trumpet, Trombone, Tuba, Horn, Saxophone });

        public static readonly Instrument Violin = new("Violin", 20, 40, OctaveRange.C3toC6, true, 100, 5, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE0, new ReadOnlyCollection<string>(new List<string> { }));
        public static readonly Instrument Viola = new("Viola", 21, 41, OctaveRange.C3toC6, true, 100, 5, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE1, new ReadOnlyCollection<string>(new List<string> { }));
        public static readonly Instrument Cello = new("Cello", 22, 42, OctaveRange.C2toC5, true, 100, 5, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE2, new ReadOnlyCollection<string>(new List<string> { }));
        public static readonly Instrument DoubleBass = new("DoubleBass", 23, 43, OctaveRange.C1toC4, true, 100, 5, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE3, new ReadOnlyCollection<string>(new List<string> { "ContraBass", "bass" }));
        public static readonly IReadOnlyList<Instrument> Strings = new ReadOnlyCollection<Instrument>(new List<Instrument> { Violin, Viola, Cello, DoubleBass });

        public static readonly Instrument ElectricGuitarOverdriven = new("ElectricGuitarOverdriven", 24, 29, OctaveRange.C2toC5, true, 100, 6, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE0, new ReadOnlyCollection<string>(new List<string> { "Program:ElectricGuitar", "guitaroverdriven", "overdrivenguitar", "overdriven" }));
        public static readonly Instrument ElectricGuitarClean = new("ElectricGuitarClean", 25, 27, OctaveRange.C2toC5, true, 100, 6, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE1, new ReadOnlyCollection<string>(new List<string> { "guitarclean", "cleanguitar", "clean" }));
        public static readonly Instrument ElectricGuitarMuted = new("ElectricGuitarMuted", 26, 28, OctaveRange.C2toC5, false, 50, 6, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE2, new ReadOnlyCollection<string>(new List<string> { "guitarmuted", "mutedguitar", "muted" }));
        public static readonly Instrument ElectricGuitarPowerChords = new("ElectricGuitarPowerChords", 27, 30, OctaveRange.C1toC4, true, 100, 6, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE3, new ReadOnlyCollection<string>(new List<string> { "ElectricGuitarPowerChord", "guitarpowerchords", "powerchords" }));
        public static readonly Instrument ElectricGuitarSpecial = new("ElectricGuitarSpecial", 28, 31, OctaveRange.C3toC6, false, 100, 6, InstrumentToneMenuKey.PERFORMANCE_MODE_EX_TONE4, new ReadOnlyCollection<string>(new List<string> { "guitarspecial", "special" }));
        public static readonly IReadOnlyList<Instrument> ElectricGuitar = new ReadOnlyCollection<Instrument>(new List<Instrument> { ElectricGuitarOverdriven, ElectricGuitarClean, ElectricGuitarMuted, ElectricGuitarPowerChords, ElectricGuitarSpecial });

        public static readonly IReadOnlyList<Instrument> All = new ReadOnlyCollection<Instrument>(new List<Instrument>().Concat(Strummed).Concat(Wind).Concat(Drums).Concat(Brass).Concat(Strings).Concat(ElectricGuitar).ToList());

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the index.
        /// </summary>
        /// <value>The index.</value>
        public int Index { get; }

        /// <summary>
        /// Gets the midi program change code.
        /// </summary>
        /// <value>The midi program change code.</value>
        public int MidiProgramChangeCode { get; }

        /// <summary>
        /// Gets the default octave range.
        /// </summary>
        /// <value>The default octave range.</value>
        public OctaveRange DefaultOctaveRange { get; }

        /// <summary>
        /// Returns true if this instrument supports being sustained.
        /// </summary>
        public bool IsSustained { get; }

        /// <summary>
        /// Gets the sample offset
        /// </summary>
        public int SampleOffset { get; }

        /// <summary>
        /// 
        /// </summary>
        public InstrumentTone InstrumentTone => InstrumentTone.Parse(_instrumentToneNumber);
        private readonly int _instrumentToneNumber;

        /// <summary>
        /// 
        /// </summary>
        public InstrumentToneMenuKey InstrumentToneMenuKey { get; }

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<string> AlternativeNames { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Instrument"/> struct.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="index">Index.</param>
        /// <param name="midiProgramChangeCode">MidiProgramChangeCode.</param>
        /// <param name="defaultOctaveRange">DefaultOctaveRange</param>
        /// <param name="isSustained">IsSustained</param>
        /// <param name="sampleOffset">SampleOffset</param>
        /// <param name="instrumentToneNumber"></param>
        /// <param name="instrumentToneMenuKey"></param>
        /// <param name="alternativeNames"></param>
        private Instrument(string name, int index, int midiProgramChangeCode, OctaveRange defaultOctaveRange, bool isSustained, int sampleOffset, int instrumentToneNumber, InstrumentToneMenuKey instrumentToneMenuKey, IReadOnlyList<string> alternativeNames)
        {
            Name = name;
            Index = index;
            MidiProgramChangeCode = midiProgramChangeCode;
            DefaultOctaveRange = defaultOctaveRange;
            IsSustained = isSustained;
            SampleOffset = sampleOffset;
            _instrumentToneNumber = instrumentToneNumber;
            InstrumentToneMenuKey = instrumentToneMenuKey;
            AlternativeNames = alternativeNames;
        }

        /// <summary>
        /// Determines whether the specified <see cref="Instrument"/> is equal to the
        /// current <see cref="Instrument"/>.
        /// </summary>
        /// <param name="other">The <see cref="Instrument"/> to compare with the current <see cref="Instrument"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="Instrument"/> is equal to the current
        /// <see cref="Instrument"/>; otherwise, <c>false</c>.</returns>
        public bool Equals(Instrument other) => Index == other;

        bool IEquatable<Instrument>.Equals(Instrument other) => Equals(other);

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="Instrument"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="Instrument"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="object"/> is equal to the current
        /// <see cref="Instrument"/>; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj) => obj is Instrument instrument && Equals(instrument);

        /// <summary>
        /// Serves as a hash function for a <see cref="Instrument"/> object.
        /// </summary>
        /// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a
        /// hash table.</returns>
        public override int GetHashCode() => (Name, Index, MidiProgramChangeCode, DefaultOctaveRange).GetHashCode();

        public static implicit operator string(Instrument instrument) => instrument.Name;
        public static implicit operator Instrument(string name) => Parse(name);
        public static implicit operator int(Instrument instrument) => instrument.Index;
        public static implicit operator Instrument(int index) => Parse(index);

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            if (obj is not Instrument instrument) 
                throw new ArgumentException("This is not an Instrument");

            return Index - instrument.Index;
        }

        public int CompareTo(Instrument other) => Index - other.Index;
        public TypeCode GetTypeCode() => TypeCode.Int32;
        public bool ToBoolean(IFormatProvider provider) => throw new InvalidCastException("Invalid cast from Instrument to Boolean");
        public char ToChar(IFormatProvider provider) => throw new InvalidCastException("Invalid cast from Instrument to Char");
        public sbyte ToSByte(IFormatProvider provider) => Convert.ToSByte(Index);
        public byte ToByte(IFormatProvider provider) => Convert.ToByte(Index);
        public short ToInt16(IFormatProvider provider) => Convert.ToInt16(Index);
        public ushort ToUInt16(IFormatProvider provider) => Convert.ToUInt16(Index);
        public int ToInt32(IFormatProvider provider) => Convert.ToInt32(Index);
        public uint ToUInt32(IFormatProvider provider) => Convert.ToUInt32(Index);
        public long ToInt64(IFormatProvider provider) => Convert.ToInt64(Index);
        public ulong ToUInt64(IFormatProvider provider) => Convert.ToUInt64(Index);
        public float ToSingle(IFormatProvider provider) => throw new InvalidCastException("Invalid cast from Instrument to Single");
        public double ToDouble(IFormatProvider provider) => throw new InvalidCastException("Invalid cast from Instrument to Double");
        public decimal ToDecimal(IFormatProvider provider) => throw new InvalidCastException("Invalid cast from Instrument to Decimal");
        public DateTime ToDateTime(IFormatProvider provider) => throw new InvalidCastException("Invalid cast from Instrument to DateTime");
        public string ToString(IFormatProvider provider) => Index.ToString();
        public override string ToString() => Index.ToString();
        public object ToType(Type conversionType, IFormatProvider provider) => throw new InvalidCastException("Invalid cast from Instrument to " + conversionType);

        /// <summary>
        /// Gets the Default Track name of this instrument.
        /// </summary>
        /// <returns></returns>
        public string GetDefaultTrackName() => Name + DefaultOctaveRange.TrackNameOffset;

        /// <summary>
        /// Gets to get the instrument from the program change number.
        /// </summary>
        /// <param name="prognumber"></param>
        /// <returns></returns>
        public static Instrument ParseByProgramChange(int prognumber)
        {
            TryParseByProgramChange(prognumber, out var result);
            return result;
        }

        /// <summary>
        /// Tries to get the instrument from the program change number.
        /// </summary>
        /// <param name="prognumber"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryParseByProgramChange(int prognumber, out Instrument result)
        {
            if (All.Any(x => x.MidiProgramChangeCode.Equals(prognumber)))
            {
                result = All.First(x => x.MidiProgramChangeCode.Equals(prognumber));
                return true;
            }
            result = None;
            return false;
        }

        /// <summary>
        /// Gets to get the instrument from the instrument number.
        /// </summary>
        /// <param name="instrument"></param>
        /// <returns></returns>
        public static Instrument Parse(int instrument)
        {
            TryParse(instrument, out var result);
            return result;
        }

        /// <summary>
        /// Tries to get the instrument from the instrument number.
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryParse(int instrument, out Instrument result)
        {
            if (All.Any(x => x.Index.Equals(instrument)))
            {
                result = All.First(x => x.Index.Equals(instrument));
                return true;
            }
            result = None;
            return false;
        }

        /// <summary>
        /// Gets the instrument from a string.
        /// </summary>
        /// <param name="instrument">The string with the name of the instrument</param>
        /// <returns>The <see cref="Instrument"/>, or <see cref="None"/> if invalid.</returns>
        public static Instrument Parse(string instrument)
        {
            TryParse(instrument, out var result);
            return result;
        }

        /// <summary>
        /// Tries to get the instrument from a string.
        /// </summary>
        /// <param name="instrument">The string with the name of the instrument</param>
        /// <param name="result">The <see cref="Instrument"/>, or <see cref="None"/> if invalid.</param>
        /// <returns>true if the <see cref="Instrument"/> is anything besides <see cref="None"/></returns>
        public static bool TryParse(string instrument, out Instrument result)
        {
            if (instrument is null)
            {
                result = None;
                return false;
            }
            instrument = instrument.Replace(" ", "").Replace("_", "");
            if (int.TryParse(instrument, out var number)) return TryParse(number, out result);
            if (All.Any(x => x.Name.Equals(instrument, StringComparison.CurrentCultureIgnoreCase)))
            {
                result = All.First(x => x.Name.Equals(instrument, StringComparison.CurrentCultureIgnoreCase));
                return true;
            }
            foreach (var instr in All.Where(instr =>
                         instr.AlternativeNames.Any(x => x.Equals(instrument, StringComparison.CurrentCultureIgnoreCase))))
            {
                result = instr;
                return true;
            }
            result = None;
            return false;
        }

        /// <summary>
        /// Gets the per note sound sample offset for a given instrument. Should be combined with the SampleOffest.
        /// </summary>
        /// <param name="note">The in game note in this Instrument's default range</param>
        /// <returns>The millisecond offset</returns>
        public long NoteSampleOffset(int note)
        {
            // TODO: Double check Lute in-game instead of sample offset measurement.
            /*if (Equals(Lute) && note < 15) return 0;
            if (Equals(Lute) && note > 14) return 50;*/
            if (Equals(Clarinet) && note <= 7) return -50;

            return 0;
        }

        /// <summary>
        /// Gets the per note sound sample offset for a given instrument. Should be combined with the SampleOffest.
        /// </summary>
        /// <param name="note">The in game note in this Instrument's default range</param>
        /// <returns>The millisecond offset</returns>
        public long NoteSampleOffsetOrDefault(int note, bool mb2CompatMode = false, bool toadcompensation = false)
        {
            int max = InstrumentOffset.GetMaxOffset();
            if (mb2CompatMode)
            {
                //if we are using the toad compensate by 25ms
                max = toadcompensation ? InstrumentOffset.MidiBard2CompatOffset.Max() - 25: InstrumentOffset.MidiBard2CompatOffset.Max();
                if (this.Name.Equals(Instrument.Clarinet.Name) && (note - 48 >= 0) && (note - 48 <= 9))
                    return max - (toadcompensation ? InstrumentOffset.MidiBard2CompatOffset[note - 48] - 25 : InstrumentOffset.MidiBard2CompatOffset[note - 48]);
            }

            //in case we have an invalid note number, get the "default" offset
            if ((note - 48 < 0) || (note - 48 > 36))
                return max - InstrumentOffset.NoteInstrumentsampleOffset[Index].Min();
            return max - InstrumentOffset.NoteInstrumentsampleOffset[Index][note-48];
        }

        /// <summary>
        /// Gets the new note value for a note that needs to move to a different base octave.
        /// </summary>
        /// <param name="currentOctaveRange">The current octave range this note is in</param>
        /// <param name="note">The note number</param>
        /// <returns>True, if this note was in range to be moved, else false.</returns>
        public bool TryShiftNoteToDefaultOctave(OctaveRange currentOctaveRange, ref int note)
        {
            if (Equals(None))
                throw new BmpException(Name + " is not a valid instrument for this function.");

            return DefaultOctaveRange.TryShiftNoteToOctave(currentOctaveRange, ref note);
        }

        /// <summary>
        /// Validates this note is in this Instrument's octave range.
        /// </summary>
        /// <param name="note">The note</param>
        /// <returns></returns>
        public bool ValidateNoteRange(int note) => DefaultOctaveRange.ValidateNoteRange(note);
    }

    public static class InstrumentOffset
    {
        public static int GetMaxOffset() { return NoteInstrumentsampleOffset.Max((byte[] val) => val.Max()); }
        public static readonly byte[] MidiBard2CompatOffset = new byte[]
        {
            135, 135, 135, 135, 135, 135, 135, 135, 135, 135
        };

        //public const byte NoteInstrumentsampleOffset[28][37] ={{}};
        public static readonly byte[][] NoteInstrumentsampleOffset = new byte[][]
        {
            //  01  02  03  04  05  06  07  08  09  10  11  12  13  14  15  16  17
            //00 None
            new byte[]
            {
                00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00,
                00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00,
                00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00
            },
			//01 Harp - 4 samples
			new byte[]
            {
                66, 66, 66, 66, 66, 66, 66, 66, 66, 66,
                66, 65, 65, 65, 65, 65, 65, 65, 65, 65,
                66, 66, 66, 66, 66, 66, 66, 66, 66, 65,
                65, 65, 65, 65, 65, 65, 65
            },
			//02 Piano - 5 samples
			new byte[]
            {
                70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
                70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
                70, 70, 70, 70, 70, 70, 69, 69, 69, 69,
                70, 70, 70, 70, 70, 70, 70
            },
			//03 Lute - 4 samples
			new byte[]
            {
                81, 81, 81, 81, 81, 81, 81, 81, 81, 81,
                81, 81, 81, 81, 81, 78, 78, 78, 78, 78,
                78, 78, 79, 79, 79, 79, 79, 79, 79, 79,
                79, 79, 79, 79, 79, 79, 79
            },
			//04 Fiddle - 8 samples
			new byte[]
            {
                78, 78, 78, 68, 68, 68, 68, 79, 79, 79,
                79, 68, 68, 68, 67, 67, 67, 67, 67, 72,
                72, 72, 69, 69, 69, 69, 69, 69, 71, 71,
                71, 71, 71, 71, 71, 71, 71
            },
			//05 Flute - 6 samples
			new byte[]
            {
                70, 70, 70, 70, 70, 70, 70, 70, 70, 70,
                79, 79, 79, 79, 79, 72, 72, 72, 72, 77,
                77, 77, 77, 80, 80, 80, 80, 80, 82, 82,
                82, 82, 82, 82, 82, 82, 82
            },
			//06 Oboe - 5 samples
			new byte[]
            {
                75, 75, 75, 75, 75, 75, 75, 75, 75, 75,
                74, 74, 74, 74, 74, 73, 73, 73, 70, 70,
                70, 70, 68, 68, 68, 68, 68, 68, 68, 68,
                68, 68, 68, 68, 68, 68, 68
            },
			//07 Clarinet - 5 samples
			new byte[]
            {
                147, 147, 147, 147, 147, 147, 147, 147, 147, 147,
                147, 79, 79, 79, 79, 79, 79, 73, 73, 73,
                73, 73, 78, 78, 78, 78, 78, 78, 78, 69,
                69, 69, 69, 69, 69, 69, 69
            },
			//08 Fife - 5 samples
			new byte[]
            {
                70, 70, 70, 70, 70, 70, 70, 70, 73, 73,
                73, 73, 73, 73, 83, 83, 83, 88, 88, 88,
                88, 88, 88, 88, 85, 85, 85, 85, 85, 85,
                85, 85, 85, 85, 85, 85, 85
            },
			//09 Panpipe - 4 samples
			new byte[]
            {
                69, 69, 69, 69, 69, 69, 69, 69, 69, 69,
                69, 69, 69, 69, 69, 69, 71, 71, 71, 71,
                71, 71, 71, 71, 70, 70, 70, 70, 70, 70,
                70, 70, 70, 70, 70, 70, 70
            },
			//10 Timpani - 3 samples
			new byte[]
            {
                67, 67, 67, 67, 67, 67, 67, 67, 67, 67,
                67, 67, 67, 67, 67, 67, 65, 65, 65, 65,
                65, 65, 65, 65, 67, 67, 67, 67, 67, 67,
                67, 67, 67, 67, 67, 67, 67
            },
			//11 Bongo - 3 samples
			new byte[]
            {
                69, 69, 69, 69, 69, 69, 69, 69, 65, 65,
                65, 65, 65, 65, 65, 65, 65, 65, 65, 65,
                65, 65, 54, 54, 54, 54, 54, 54, 54, 54,
                54, 54, 54, 54, 54, 54, 54
            },
			//12 Bassdrum - 4 samples
			new byte[]
            {
                71, 71, 71, 71, 71, 71, 71, 65, 65, 65,
                65, 65, 65, 65, 65, 65, 65, 65, 55, 55,
                55, 55, 55, 55, 46, 46, 46, 46, 46, 46,
                46, 46, 46, 46, 46, 46, 46
            },
			//13 SnareDrum - 4 samples
			new byte[]
            {
                71, 71, 71, 71, 71, 71, 71, 71, 71, 71,
                71, 71, 63, 63, 63, 63, 63, 63, 63, 63,
                62, 62, 62, 62, 55, 55, 55, 55, 55, 55,
                55, 55, 55, 55, 55, 55, 55
            },
			//14 Cymbals - 7 samples
			new byte[]
            {
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5
            },
			//15 Trumpet - 6 samples
			new byte[]
            {
                17, 17, 17, 17, 17, 17, 17, 17, 17, 17,
                17, 17, 17, 17, 17, 23, 23, 23, 23, 8,
                8, 8, 8, 8, 6, 6, 6, 4, 4, 4,
                4, 20, 20, 20, 20, 20, 20
            },
			//16 Trombone - 5 samples
			new byte[]
            {
                9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
                9, 9, 9, 9, 9, 8, 8, 8, 8, 8,
                9, 9, 9, 9, 5, 5, 5, 9, 9, 9,
                9, 9, 9, 9, 9, 9, 9
            },
			//17 Tuba - 6 samples
			new byte[]
            {
                15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
                15, 7, 7, 7, 13, 13, 13, 13, 13, 4,
                4, 4, 4, 4, 7, 7, 7, 7, 7, 27,
                27, 27, 27, 27, 27, 27, 27
            },
			//18 Horn - 5 samples
			new byte[]
            {
                5, 5, 5, 5, 5, 5, 5, 5, 5, 10,
                10, 10, 10, 10, 10, 10, 10, 10, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6
            },
			//19 Saxophone - 4 samples
			new byte[]
            {
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 6, 6, 6, 6, 6, 6, 7, 7,
                7, 7, 7, 7, 7, 7, 8, 8, 8, 8,
                8, 8, 8, 8, 8, 8, 8
            },
			//20 Violin - 5 samples
			new byte[]
            {
                23, 23, 23, 23, 23, 23, 23, 23, 23, 23,
                18, 18, 18, 18, 18, 18, 18, 18, 18, 18,
                18, 18, 18, 18, 18, 18, 14, 14, 14, 14,
                11, 11, 11, 11, 11, 11, 11
            },
			//21 Viola - 5 samples
			new byte[]
            {
                16, 16, 16, 16, 16, 16, 16, 16, 16, 15,
                15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
                15, 15, 15, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13
            },
			//22 Cello - 5 samples
			new byte[]
            {
                21, 21, 21, 21, 21, 21, 9, 9, 9, 9,
                9, 9, 9, 9, 17, 17, 17, 17, 17, 17,
                17, 17, 13, 13, 13, 13, 13, 13, 17, 17,
                17, 17, 17, 17, 17, 17, 17
            },
			//23 Doublebass - 5 samples
			new byte[]
            {
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                10, 10, 10, 10, 10, 11, 11, 11, 11, 11,
                11, 11, 8, 8, 8, 8, 8, 8, 12, 12,
                12, 12, 12, 12, 12, 12, 12
            },
			//24 ElectricGuitarOverdriven - 5 samples
			new byte[]
            {
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 1, 1, 1, 1, 1, 1, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3
            },
			//25 ElectricGuitarClean - 5 samples
			new byte[]
            {
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 6, 6, 6, 6, 6, 6,
                6, 4, 4, 4, 4, 4, 4
            },
			//26 ElectricGuitarMuted - 10 samples
			new byte[]
            {
                65, 65, 65, 60, 60, 60, 68, 68, 68, 68,
                63, 63, 63, 63, 74, 74, 74, 74, 74, 74,
                74, 74, 68, 68, 68, 68, 69, 69, 69, 69,
                70, 70, 70, 70, 68, 68, 68
            },
			//27 ElectricGuitarPowerChords - 5 samples
			new byte[]
            {
                8, 8, 8, 8, 8, 8, 8, 8, 8, 1,
                1, 1, 1, 1, 1, 1, 1, 6, 6, 6,
                6, 6, 0, 0, 0, 0, 0, 0, 0, 6,
                6, 6, 6, 6, 6, 6, 6
            },
			//27 ElectricGuitarSpecial - 5 samples
			new byte[]
            {
                11, 11, 11, 11, 11, 11, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 0,
                0, 0, 0, 0, 0, 0, 0
            }
        };
    }
}