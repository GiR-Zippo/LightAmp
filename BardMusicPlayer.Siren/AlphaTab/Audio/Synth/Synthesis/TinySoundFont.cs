﻿/*
 * Copyright(c) 2021 Daniel Kuschny
 * Licensed under the MPL-2.0 license. See https://github.com/CoderLine/alphaTab/blob/develop/LICENSE for full license information.
 */

// The SoundFont loading and Audio Synthesis is based on TinySoundFont, licensed under MIT,
// developed by Bernhard Schelling (https://github.com/schellingb/TinySoundFont)

// C# port for alphaTab: (C) 2019 by Daniel Kuschny
// Licensed under: MPL-2.0

/*
 * LICENSE (MIT)
 *
 * Copyright (C) 2017, 2018 Bernhard Schelling
 * Based on SFZero, Copyright (C) 2012 Steve Folta (https://github.com/stevefolta/SFZero)
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this
 * software and associated documentation files (the "Software"), to deal in the Software
 * without restriction, including without limitation the rights to use, copy, modify, merge,
 * publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
 * to whom the Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
 * PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Siren.AlphaTab.Audio.Synth.SoundFont;
using BardMusicPlayer.Siren.AlphaTab.Audio.Synth.Util;
using BardMusicPlayer.Siren.AlphaTab.Collections;

namespace BardMusicPlayer.Siren.AlphaTab.Audio.Synth.Synthesis
{
    /// <summary>
    ///     This is a tiny soundfont based synthesizer.
    /// </summary>
    /// <remarks>
    ///     NOT YET IMPLEMENTED
    ///     - Support for ChorusEffectsSend and ReverbEffectsSend generators
    ///     - Better low-pass filter without lowering performance too much
    ///     - Support for modulators
    /// </remarks>
    internal sealed partial class TinySoundFont
    {
        private readonly List<Voice> _voices;
        private Channels _channels;
        private ulong _noteCounter;
        private uint _voicePlayIndex;

        /// <summary>
        ///     Initializes a new instance of the <seealso cref="TinySoundFont" /> class.
        /// </summary>
        public TinySoundFont(float sampleRate)
        {
            OutSampleRate = sampleRate;
            OutputMode = OutputMode.StereoInterleaved;
            _voices = new List<Voice>();
        }

        internal float[] FontSamples { get; set; }

        /// <summary>
        ///     Returns the number of presets in the loaded SoundFont
        /// </summary>
        public int PresetCount => Presets.Count;

        public List<Preset> Presets { get; private set; }

        /// <summary>
        ///     Gets the currently configured output mode.
        /// </summary>
        /// <seealso cref="SetOutput" />
        public OutputMode OutputMode { get; private set; }

        /// <summary>
        ///     Gets the currently configured sample rate.
        /// </summary>
        /// <seealso cref="SetOutput" />
        public float OutSampleRate { get; private set; }

        /// <summary>
        ///     Gets the currently configured global gain in DB.
        /// </summary>
        public float GlobalGainDb { get; set; }

        /// <summary>
        ///     Returns the number of active voices
        /// </summary>
        public int ActiveVoiceCount
        {
            get { return _voices.Count(static v => v.PlayingPreset != -1 && v.Stopped != true); }
        }

        /// <summary>
        ///     Stop all playing notes immediatly and reset all channel parameters
        /// </summary>
        public void Reset()
        {
            foreach (var v in _voices.Where(static v => v.PlayingPreset != -1 &&
                                                        (v.AmpEnv.Segment < VoiceEnvelopeSegment.Release ||
                                                         v.AmpEnv.Parameters.Release != 0)))
                v.EndQuick(OutSampleRate);

            _channels = null;
        }

        /// <summary>
        ///     Setup the parameters for the voice render methods
        /// </summary>
        /// <param name="outputMode">if mono or stereo and how stereo channel data is ordered</param>
        /// <param name="sampleRate">the number of samples per second (output frequency)</param>
        /// <param name="globalGainDb">volume gain in decibels (&gt;0 means higher, &lt;0 means lower)</param>
        public void SetOutput(OutputMode outputMode, int sampleRate, float globalGainDb)
        {
            OutputMode = outputMode;
            OutSampleRate = sampleRate >= 1 ? sampleRate : 48000;
            GlobalGainDb = globalGainDb;
        }

        /// <summary>
        ///     Start playing a note
        /// </summary>
        /// <param name="presetIndex">preset index &gt;= 0 and &lt; <see cref="PresetCount" /></param>
        /// <param name="key">note value between 0 and 127 (60 being middle C)</param>
        /// <param name="vel">velocity as a float between 0.0 (equal to note off) and 1.0 (full)</param>
        public void NoteOn(int presetIndex, int key, float vel)
        {
            var midiVelocity = (int)(vel * 127);

            if (presetIndex < 0 || presetIndex >= Presets.Count) return;

            if (vel <= 0.0f)
            {
                NoteOff(presetIndex, key);
                return;
            }

            // Play all matching regions.
            var voicePlayIndex = _voicePlayIndex++;
            foreach (var region in Presets[presetIndex].Regions)
            {
                if (_noteCounter == ulong.MaxValue) _noteCounter = 0;

                _noteCounter++;
                if (key < region.LoKey || key > region.HiKey || midiVelocity < region.LoVel ||
                    midiVelocity > region.HiVel)
                    continue;

                Voice voice = null;
                if (region.Group != 0)
                {
                    foreach (var v in _voices)
                        if (v.PlayingPreset == presetIndex && v.Region.Group == region.Group)
                            v.EndQuick(OutSampleRate);
                        else if (v.PlayingPreset == -1 && voice == null) voice = v;
                }
                else
                {
                    foreach (var v in _voices.Where(static v => v.PlayingPreset == -1)) voice = v;
                }

                if (voice == null)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        var newVoice = new Voice
                        {
                            PlayingPreset = -1
                        };
                        _voices.Add(newVoice);
                    }

                    voice = _voices[_voices.Count - 4];
                }

                voice.Region = region;
                voice.PlayingPreset = presetIndex;
                voice.PlayingKey = key;
                voice.PlayIndex = voicePlayIndex;
                voice.NoteGainDb = GlobalGainDb - region.Attenuation - SynthHelper.GainToDecibels(1.0f / vel);

                voice.Counter = _noteCounter;
                voice.Stopped = false;

                if (_channels != null)
                {
                    _channels.SetupVoice(this, voice);
                }
                else
                {
                    voice.CalcPitchRatio(0, OutSampleRate);
                    // The SFZ spec is silent about the pan curve, but a 3dB pan law seems common. This sqrt() curve matches what Dimension LE does; Alchemy Free seems closer to sin(adjustedPan * pi/2).
                    voice.PanFactorLeft = (float)Math.Sqrt(0.5f - region.Pan);
                    voice.PanFactorRight = (float)Math.Sqrt(0.5f + region.Pan);
                }

                // Offset/end.
                voice.SourceSamplePosition = region.Offset;

                // Loop.
                var doLoop = region.LoopMode != LoopMode.None && region.LoopStart < region.LoopEnd;
                voice.LoopStart = doLoop ? region.LoopStart : 0;
                voice.LoopEnd = doLoop ? region.LoopEnd : 0;

                // Setup envelopes.
                voice.AmpEnv.Setup(region.AmpEnv, key, midiVelocity, true, OutSampleRate);
                voice.ModEnv.Setup(region.ModEnv, key, midiVelocity, false, OutSampleRate);

                // Setup lowpass filter.
                var filterQDB = region.InitialFilterQ / 10.0f;
                voice.LowPass.QInv = 1.0 / Math.Pow(10.0, filterQDB / 20.0);
                voice.LowPass.Z1 = voice.LowPass.Z2 = 0;
                voice.LowPass.Active = region.InitialFilterFc <= 13500;
                if (voice.LowPass.Active)
                    voice.LowPass.Setup(SynthHelper.Cents2Hertz(region.InitialFilterFc) / OutSampleRate);

                // Setup LFO filters.
                voice.ModLfo.Setup(region.DelayModLFO, region.FreqModLFO, OutSampleRate);
                voice.VibLfo.Setup(region.DelayVibLFO, region.FreqVibLFO, OutSampleRate);
            }
        }

        /// <summary>
        ///     Start playing a note
        /// </summary>
        /// <param name="bank">instrument bank number (alternative to preset_index)</param>
        /// <param name="presetNumber">preset number (alternative to preset_index)</param>
        /// <param name="key">note value between 0 and 127 (60 being middle C)</param>
        /// <param name="vel">velocity as a float between 0.0 (equal to note off) and 1.0 (full)</param>
        /// <returns>returns false if preset does not exist, otherwise true</returns>
        public bool BankNoteOn(int bank, int presetNumber, int key, float vel)
        {
            var presetIndex = GetPresetIndex(bank, presetNumber);
            if (presetIndex == -1) return false;

            NoteOn(presetIndex, key, vel);
            return true;
        }

        /// <summary>
        ///     Stop playing a note
        /// </summary>
        /// <param name="presetIndex"></param>
        /// <param name="key"></param>
        public void NoteOff(int presetIndex, int key)
        {
            Voice matchFirst = null;
            Voice matchLast = null;
            var matches = new FastList<Voice>();
            foreach (var v in _voices)
                if (v.PlayingPreset != presetIndex || v.PlayingKey != key ||
                    v.AmpEnv.Segment >= VoiceEnvelopeSegment.Release)
                {
                }
                else if (matchFirst == null || v.PlayIndex < matchFirst.PlayIndex)
                {
                    matchFirst = v;
                    matchLast = v;
                    matches.Add(v);
                }
                else if (v.PlayIndex == matchFirst.PlayIndex)
                {
                    matchLast = v;
                    matches.Add(v);
                }

            if (matchFirst == null) return;

            foreach (var v in matches.Where(v => v == matchFirst || v == matchLast ||
                                                 (v.PlayIndex == matchFirst.PlayIndex &&
                                                  v.PlayingPreset == presetIndex && v.PlayingKey == key &&
                                                  v.AmpEnv.Segment < VoiceEnvelopeSegment.Release)))
                v.End(OutSampleRate);
        }

        /// <summary>
        ///     Stop playing a note
        /// </summary>
        /// <param name="bank"></param>
        /// <param name="presetNumber"></param>
        /// <param name="key"></param>
        /// <returns>returns false if preset does not exist, otherwise true</returns>
        public bool BankNoteOff(int bank, int presetNumber, int key)
        {
            var presetIndex = GetPresetIndex(bank, presetNumber);
            if (presetIndex == -1) return false;

            NoteOff(presetIndex, key);
            return true;
        }

        /// <summary>
        ///     Stop playing all notes (end with sustain and release)
        /// </summary>
        public void NoteOffAll(bool immediate)
        {
            foreach (var voice in _voices.Where(static voice =>
                         voice.PlayingPreset != -1 && voice.AmpEnv.Segment < VoiceEnvelopeSegment.Release))
                if (immediate)
                    voice.EndQuick(OutSampleRate);
                else
                    voice.End(OutSampleRate);
        }

        private Channel ChannelInit(int channel)
        {
            if (_channels != null && channel < _channels.ChannelList.Count) return _channels.ChannelList[channel];

            _channels ??= new Channels();

            for (var i = _channels.ChannelList.Count; i <= channel; i++)
            {
                var c = new Channel();
                c.PresetIndex = c.Bank = 0;
                c.PitchWheel = c.MidiPan = 8192;
                c.MidiVolume = c.MidiExpression = 16383;
                c.MidiRpn = 0xFFFF;
                c.MidiData = 0;
                c.PanOffset = 0.0f;
                c.GainDb = 0.0f;
                c.PitchRange = 2.0f;
                c.Tuning = 0.0f;
                c.MixVolume = 1;
                _channels.ChannelList.Add(c);
            }

            return _channels.ChannelList[channel];
        }

        /// <summary>
        ///     Returns the preset index from a bank and preset number, or -1 if it does not exist in the loaded SoundFont
        /// </summary>
        /// <param name="bank"></param>
        /// <param name="presetNumber"></param>
        /// <returns></returns>
        private int GetPresetIndex(int bank, int presetNumber)
        {
            for (var i = 0; i < Presets.Count; i++)
            {
                var preset = Presets[i];
                if (preset.PresetNumber == presetNumber && preset.Bank == bank) return i;
            }

            return -1;
        }

        /// <summary>
        ///     Returns the name of a preset index &gt;= 0 and &lt; GetPresetName()
        /// </summary>
        /// <param name="presetIndex"></param>
        /// <returns></returns>
        public string GetPresetName(int presetIndex)
        {
            return presetIndex < 0 || presetIndex >= Presets.Count ? null : Presets[presetIndex].Name;
        }

        /// <summary>
        ///     Returns the name of a preset by bank and preset number
        /// </summary>
        /// <param name="bank"></param>
        /// <param name="presetNumber"></param>
        /// <returns></returns>
        public string BankGetPresetName(int bank, int presetNumber)
        {
            return GetPresetName(GetPresetIndex(bank, presetNumber));
        }

        #region Loading

        public void LoadPresets(Hydra hydra, bool append)
        {
            List<Preset> newPresets = new(new Preset[hydra.Phdrs.Count - 1]);
            for (double phdrIndex = 0; phdrIndex < hydra.Phdrs.Count - 1; phdrIndex++)
            {
                var phdr = hydra.Phdrs[(int)phdrIndex];
                double regionIndex = 0;
                var preset = newPresets[(int)phdrIndex] = new Preset();
                preset.Name = phdr.PresetName;
                preset.Bank = phdr.Bank;
                preset.PresetNumber = phdr.Preset;
                preset.FontSamples = hydra.FontSamples;
                double regionNum = 0;
                for (double pbagIndex = phdr.PresetBagNdx;
                     pbagIndex < hydra.Phdrs[(int)(phdrIndex + 1)].PresetBagNdx;
                     pbagIndex++)
                {
                    var pbag = hydra.Pbags[(int)pbagIndex];
                    double plokey = 0;
                    double phikey = 127;
                    double plovel = 0;
                    double phivel = 127;
                    for (double pgenIndex = pbag.GenNdx;
                         pgenIndex < hydra.Pbags[(int)(pbagIndex + 1)].GenNdx;
                         pgenIndex++)
                    {
                        var pgen = hydra.Pgens[(int)pgenIndex];
                        switch (pgen.GenOper)
                        {
                            case HydraPgen.GenKeyRange:
                                plokey = pgen.GenAmount.LowByteAmount;
                                phikey = pgen.GenAmount.HighByteAmount;
                                continue;
                            case HydraPgen.GenVelRange:
                                plovel = pgen.GenAmount.LowByteAmount;
                                phivel = pgen.GenAmount.HighByteAmount;
                                continue;
                        }

                        if (pgen.GenOper != HydraPgen.GenInstrument) continue;
                        if (pgen.GenAmount.WordAmount >= hydra.Insts.Count) continue;

                        var pinst = hydra.Insts[pgen.GenAmount.WordAmount];
                        for (double ibagIndex = pinst.InstBagNdx;
                             ibagIndex < hydra.Insts[pgen.GenAmount.WordAmount + 1].InstBagNdx;
                             ibagIndex++)
                        {
                            var ibag = hydra.Ibags[(int)ibagIndex];
                            double ilokey = 0;
                            double ihikey = 127;
                            double ilovel = 0;
                            double ihivel = 127;
                            for (double igenIndex = ibag.InstGenNdx;
                                 igenIndex < hydra.Ibags[(int)(ibagIndex + 1)].InstGenNdx;
                                 igenIndex++)
                            {
                                var igen = hydra.Igens[(int)igenIndex];
                                switch (igen.GenOper)
                                {
                                    case HydraPgen.GenKeyRange:
                                        ilokey = igen.GenAmount.LowByteAmount;
                                        ihikey = igen.GenAmount.HighByteAmount;
                                        continue;
                                    case HydraPgen.GenVelRange:
                                        ilovel = igen.GenAmount.LowByteAmount;
                                        ihivel = igen.GenAmount.HighByteAmount;
                                        continue;
                                    case 53 when ihikey >= plokey && ilokey <= phikey && ihivel >= plovel &&
                                                 ilovel <= phivel:
                                        regionNum++;
                                        break;
                                }
                            }
                        }
                    }
                }

                preset.Regions = new Region[(int)regionNum];
                var globalRegion = new Region();
                globalRegion.Clear(true);
                for (double pbagIndex = phdr.PresetBagNdx;
                     pbagIndex < hydra.Phdrs[(int)(phdrIndex + 1)].PresetBagNdx;
                     pbagIndex++)
                {
                    var pbag = hydra.Pbags[(int)pbagIndex];
                    var presetRegion = new Region(globalRegion);
                    var hadGenInstrument = false;
                    for (double pgenIndex = pbag.GenNdx;
                         pgenIndex < hydra.Pbags[(int)(pbagIndex + 1)].GenNdx;
                         pgenIndex++)
                    {
                        var pgen = hydra.Pgens[(int)pgenIndex];
                        if (pgen.GenOper == HydraPgen.GenInstrument)
                        {
                            double whichInst = pgen.GenAmount.WordAmount;
                            if (whichInst >= hydra.Insts.Count) continue;

                            var instRegion = new Region();
                            instRegion.Clear(false);
                            var inst = hydra.Insts[(int)whichInst];
                            for (double ibagIndex = inst.InstBagNdx;
                                 ibagIndex < hydra.Insts[(int)(whichInst + 1)].InstBagNdx;
                                 ibagIndex++)
                            {
                                var ibag = hydra.Ibags[(int)ibagIndex];
                                var zoneRegion = new Region(instRegion);
                                var hadSampleId = false;
                                for (double igenIndex = ibag.InstGenNdx;
                                     igenIndex < hydra.Ibags[(int)(ibagIndex + 1)].InstGenNdx;
                                     igenIndex++)
                                {
                                    var igen = hydra.Igens[(int)igenIndex];
                                    if (igen.GenOper == HydraPgen.GenSampleId)
                                    {
                                        if (zoneRegion.HiKey < presetRegion.LoKey ||
                                            zoneRegion.LoKey > presetRegion.HiKey) continue;
                                        if (zoneRegion.HiVel < presetRegion.LoVel ||
                                            zoneRegion.LoVel > presetRegion.HiVel) continue;

                                        if (presetRegion.LoKey > zoneRegion.LoKey)
                                            zoneRegion.LoKey = presetRegion.LoKey;

                                        if (presetRegion.HiKey < zoneRegion.HiKey)
                                            zoneRegion.HiKey = presetRegion.HiKey;

                                        if (presetRegion.LoVel > zoneRegion.LoVel)
                                            zoneRegion.LoVel = presetRegion.LoVel;

                                        if (presetRegion.HiVel < zoneRegion.HiVel)
                                            zoneRegion.HiVel = presetRegion.HiVel;

                                        zoneRegion.Offset += presetRegion.Offset;
                                        zoneRegion.End += presetRegion.End;
                                        zoneRegion.LoopStart += presetRegion.LoopStart;
                                        zoneRegion.LoopEnd += presetRegion.LoopEnd;
                                        zoneRegion.Transpose += presetRegion.Transpose;
                                        zoneRegion.Tune += presetRegion.Tune;
                                        zoneRegion.PitchKeyTrack += presetRegion.PitchKeyTrack;
                                        zoneRegion.Attenuation += presetRegion.Attenuation;
                                        zoneRegion.Pan += presetRegion.Pan;
                                        zoneRegion.AmpEnv.Delay += presetRegion.AmpEnv.Delay;
                                        zoneRegion.AmpEnv.Attack += presetRegion.AmpEnv.Attack;
                                        zoneRegion.AmpEnv.Hold += presetRegion.AmpEnv.Hold;
                                        zoneRegion.AmpEnv.Decay += presetRegion.AmpEnv.Decay;
                                        zoneRegion.AmpEnv.Sustain += presetRegion.AmpEnv.Sustain;
                                        zoneRegion.AmpEnv.Release += presetRegion.AmpEnv.Release;
                                        zoneRegion.ModEnv.Delay += presetRegion.ModEnv.Delay;
                                        zoneRegion.ModEnv.Attack += presetRegion.ModEnv.Attack;
                                        zoneRegion.ModEnv.Hold += presetRegion.ModEnv.Hold;
                                        zoneRegion.ModEnv.Decay += presetRegion.ModEnv.Decay;
                                        zoneRegion.ModEnv.Sustain += presetRegion.ModEnv.Sustain;
                                        zoneRegion.ModEnv.Release += presetRegion.ModEnv.Release;
                                        zoneRegion.InitialFilterQ += presetRegion.InitialFilterQ;
                                        zoneRegion.InitialFilterFc += presetRegion.InitialFilterFc;
                                        zoneRegion.ModEnvToPitch += presetRegion.ModEnvToPitch;
                                        zoneRegion.ModEnvToFilterFc += presetRegion.ModEnvToFilterFc;
                                        zoneRegion.DelayModLFO += presetRegion.DelayModLFO;
                                        zoneRegion.FreqModLFO += presetRegion.FreqModLFO;
                                        zoneRegion.ModLfoToPitch += presetRegion.ModLfoToPitch;
                                        zoneRegion.ModLfoToFilterFc += presetRegion.ModLfoToFilterFc;
                                        zoneRegion.ModLfoToVolume += presetRegion.ModLfoToVolume;
                                        zoneRegion.DelayVibLFO += presetRegion.DelayVibLFO;
                                        zoneRegion.FreqVibLFO += presetRegion.FreqVibLFO;
                                        zoneRegion.VibLfoToPitch += presetRegion.VibLfoToPitch;
                                        zoneRegion.AmpEnv.EnvToSecs(true);
                                        zoneRegion.ModEnv.EnvToSecs(false);
                                        zoneRegion.DelayModLFO = zoneRegion.DelayModLFO < -11950
                                            ? 0
                                            : SynthHelper.Timecents2Secs(zoneRegion.DelayModLFO);
                                        zoneRegion.DelayVibLFO = zoneRegion.DelayVibLFO < -11950
                                            ? 0
                                            : SynthHelper.Timecents2Secs(zoneRegion.DelayVibLFO);
                                        if (zoneRegion.Pan < -0.5)
                                            zoneRegion.Pan = (float)-0.5;
                                        else if (zoneRegion.Pan > 0.5) zoneRegion.Pan = (float)0.5;

                                        if (zoneRegion.InitialFilterQ is < 1500 or > 13500)
                                            zoneRegion.InitialFilterQ = 0;

                                        var shdr = hydra.SHdrs[igen.GenAmount.WordAmount];
                                        zoneRegion.Offset += shdr.Start;
                                        zoneRegion.End += shdr.End;
                                        zoneRegion.LoopStart += shdr.StartLoop;
                                        zoneRegion.LoopEnd += shdr.EndLoop;
                                        if (shdr.EndLoop > 0) zoneRegion.LoopEnd -= 1;

                                        if (zoneRegion.PitchKeyCenter == -1)
                                            zoneRegion.PitchKeyCenter = shdr.OriginalPitch;

                                        zoneRegion.Tune += shdr.PitchCorrection;
                                        zoneRegion.SampleRate = shdr.SampleRate;
                                        if (zoneRegion.End != 0 && zoneRegion.End < preset.FontSamples.Length)
                                            zoneRegion.End++;
                                        else
                                            zoneRegion.End = (uint)preset.FontSamples.Length;

                                        preset.Regions[(int)regionIndex] = new Region(zoneRegion);
                                        regionIndex++;
                                        hadSampleId = true;
                                    }
                                    else
                                    {
                                        zoneRegion.Operator(igen.GenOper, igen.GenAmount);
                                    }
                                }

                                if (ibag == hydra.Ibags[inst.InstBagNdx] && !hadSampleId)
                                    instRegion = new Region(zoneRegion);
                            }

                            hadGenInstrument = true;
                        }
                        else
                        {
                            presetRegion.Operator(pgen.GenOper, pgen.GenAmount);
                        }
                    }

                    if (pbag == hydra.Pbags[phdr.PresetBagNdx] && !hadGenInstrument) globalRegion = presetRegion;
                }
            }

            if (!append || Presets == null)
                Presets = newPresets;
            else
                foreach (var preset in newPresets)
                    Presets.Add(preset);
        }

        #endregion

        #region Higher level channel based functions

        /// <summary>
        ///     Start playing a note on a channel
        /// </summary>
        /// <param name="channel">channel number</param>
        /// <param name="key">note value between 0 and 127 (60 being middle C)</param>
        /// <param name="vel">velocity as a float between 0.0 (equal to note off) and 1.0 (full)</param>
        public void ChannelNoteOn(int channel, int key, float vel)
        {
            if (_channels == null || channel > _channels.ChannelList.Count) return;

            // Add voice limiting here before playing new notes
            if (BmpPigeonhole.Instance.EnableSynthVoiceLimiter && ActiveVoiceCount >= 16)
            {
                // Get all active voices sorted by age (counter)
                var activeVoices = _voices.Where(v => v.PlayingPreset != -1 && !v.Stopped)
                    .OrderBy(v => v.Counter)
                    .ToList();

                // Stop the oldest voices until we're under the limit
                while (activeVoices.Count >= 16)
                {
                    var oldestVoice = activeVoices[0];
                    oldestVoice.EndQuick(OutSampleRate);
                    oldestVoice.Stopped = true;
                    activeVoices.RemoveAt(0);
                }
            }

            _channels.ActiveChannel = channel;
            NoteOn(_channels.ChannelList[channel].PresetIndex, key, vel);
        }

        /// <summary>
        ///     Stop playing notes on a channel
        /// </summary>
        /// <param name="channel">channel number</param>
        /// <param name="key">note value between 0 and 127 (60 being middle C)</param>
        public void ChannelNoteOff(int channel, int key)
        {
            var matches = new FastList<Voice>();
            Voice matchFirst = null;
            Voice matchLast = null;
            foreach (var v in _voices.Where(v =>
                         v.PlayingPreset != -1 && v.PlayingChannel == channel && v.PlayingKey == key &&
                         v.AmpEnv.Segment < VoiceEnvelopeSegment.Release))
                if (matchFirst == null || v.PlayIndex < matchFirst.PlayIndex)
                {
                    matchFirst = matchLast = v;
                    matches.Add(v);
                }
                else if (v.PlayIndex == matchFirst.PlayIndex)
                {
                    matchLast = v;
                    matches.Add(v);
                }

            if (matchFirst == null) return;

            foreach (var v in matches.Where(v => v == matchFirst || v == matchLast ||
                                                 (v.PlayIndex == matchFirst.PlayIndex && v.PlayingPreset != -1 &&
                                                  v.PlayingChannel == channel &&
                                                  v.PlayingKey == key &&
                                                  v.AmpEnv.Segment < VoiceEnvelopeSegment.Release)))
                v.End(OutSampleRate);
        }

        /// <summary>
        ///     Stop playing all notes on a channel with sustain and release.
        /// </summary>
        /// <param name="channel">channel number</param>
        public void ChannelNoteOffAll(int channel)
        {
            foreach (var v in _voices.Where(v => v.PlayingPreset != -1 && v.PlayingChannel == channel &&
                                                 v.AmpEnv.Segment < VoiceEnvelopeSegment.Release))
                v.End(OutSampleRate);
        }

        /// <summary>
        ///     Stop playing all notes on a channel immediately
        /// </summary>
        /// <param name="channel">channel number</param>
        public void ChannelSoundsOffAll(int channel)
        {
            foreach (var v in _voices.Where(v => v.PlayingPreset != -1 && v.PlayingChannel == channel &&
                                                 (v.AmpEnv.Segment < VoiceEnvelopeSegment.Release ||
                                                  v.AmpEnv.Parameters.Release == 0)))
                v.EndQuick(OutSampleRate);
        }

        /// <summary>
        /// </summary>
        /// <param name="channel">channel number</param>
        /// <param name="presetIndex">preset index &gt;= 0 and &lt; <see cref="PresetCount" /></param>
        public void ChannelSetPresetIndex(int channel, int presetIndex)
        {
            ChannelInit(channel).PresetIndex = (ushort)presetIndex;
        }

        /// <summary>
        /// </summary>
        /// <param name="channel">channel number</param>
        /// <param name="presetNumber">preset number (alternative to preset_index)</param>
        /// <param name="midiDrums">false for normal channels, otherwise apply MIDI drum channel rules</param>
        /// <returns>return false if preset does not exist, otherwise true</returns>
        public bool ChannelSetPresetNumber(int channel, int presetNumber, bool midiDrums = false)
        {
            var c = ChannelInit(channel);
            int presetIndex;
            if (midiDrums)
            {
                presetIndex = GetPresetIndex(128 | (c.Bank & 0x7FFF), presetNumber);
                if (presetIndex == -1) presetIndex = GetPresetIndex(128, presetNumber);

                if (presetIndex == -1) presetIndex = GetPresetIndex(128, 0);

                if (presetIndex == -1) presetIndex = GetPresetIndex(c.Bank & 0x7FF, presetNumber);
            }
            else
            {
                presetIndex = GetPresetIndex(c.Bank & 0x7FF, presetNumber);
            }

            if (presetIndex == -1) presetIndex = GetPresetIndex(0, presetNumber);

            if (presetIndex == -1) return false;

            c.PresetIndex = (ushort)presetIndex;
            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="channel">channel number</param>
        /// <param name="bank">instrument bank number (alternative to preset_index)</param>
        public void ChannelSetBank(int channel, int bank)
        {
            ChannelInit(channel).Bank = (ushort)bank;
        }

        /// <summary>
        /// </summary>
        /// <param name="channel">channel number</param>
        /// <param name="bank">instrument bank number (alternative to preset_index)</param>
        /// <param name="presetNumber">preset number (alternative to preset_index)</param>
        /// <returns>return false if preset does not exist, otherwise true</returns>
        public bool ChannelSetBankPreset(int channel, int bank, int presetNumber)
        {
            var c = ChannelInit(channel);
            var presetIndex = GetPresetIndex(bank, presetNumber);
            if (presetIndex == -1) return false;

            c.PresetIndex = (ushort)presetIndex;
            c.Bank = (ushort)bank;
            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="channel">channel number</param>
        /// <param name="pan">stereo panning value from 0.0 (left) to 1.0 (right) (default 0.5 center)</param>
        public void ChannelSetPan(int channel, float pan)
        {
            foreach (var v in _voices)
                if (v.PlayingChannel == channel && v.PlayingPreset != -1)
                {
                    var newPan = v.Region.Pan + pan - 0.5f;
                    switch (newPan)
                    {
                        case <= -0.5f:
                            v.PanFactorLeft = 1;
                            v.PanFactorRight = 0;
                            break;
                        case >= 0.5f:
                            v.PanFactorLeft = 0;
                            v.PanFactorRight = 1;
                            break;
                        default:
                            v.PanFactorLeft = (float)Math.Sqrt(0.5f - newPan);
                            v.PanFactorRight = (float)Math.Sqrt(0.5f + newPan);
                            break;
                    }
                }

            ChannelInit(channel).PanOffset = pan - 0.5f;
        }

        /// <summary>
        /// </summary>
        /// <param name="channel">channel number</param>
        /// <param name="volume">linear volume scale factor (default 1.0 full)</param>
        public void ChannelSetVolume(int channel, float volume)
        {
            var c = ChannelInit(channel);
            var gainDb = SynthHelper.GainToDecibels(volume);
            var gainDBChange = gainDb - c.GainDb;
            if (gainDBChange == 0) return;

            foreach (var v in _voices.Where(v => v.PlayingChannel == channel && v.PlayingPreset != -1))
                v.NoteGainDb += gainDBChange;

            c.GainDb = gainDb;
        }

        /// <summary>
        /// </summary>
        /// <param name="channel">channel number</param>
        /// <param name="pitchWheel">pitch wheel position 0 to 16383 (default 8192 unpitched)</param>
        public void ChannelSetPitchWheel(int channel, int pitchWheel)
        {
            var c = ChannelInit(channel);
            if (c.PitchWheel == pitchWheel) return;

            c.PitchWheel = (ushort)pitchWheel;
            ChannelApplyPitch(channel, c);
        }

        private void ChannelApplyPitch(int channel, Channel c)
        {
            var pitchShift = c.PitchWheel == 8192
                ? c.Tuning
                : c.PitchWheel / 16383.0f * c.PitchRange * 2f - c.PitchRange + c.Tuning;
            foreach (var v in _voices.Where(v => v.PlayingChannel == channel && v.PlayingPreset != -1))
                v.CalcPitchRatio(pitchShift, OutSampleRate);
        }

        /// <summary>
        /// </summary>
        /// <param name="channel">channel number</param>
        /// <param name="pitchRange">range of the pitch wheel in semitones (default 2.0, total +/- 2 semitones)</param>
        public void ChannelSetPitchRange(int channel, float pitchRange)
        {
            var c = ChannelInit(channel);
            if (c.PitchRange == pitchRange) return;

            c.PitchRange = pitchRange;
            if (c.PitchWheel != 8192) ChannelApplyPitch(channel, c);
        }

        /// <summary>
        /// </summary>
        /// <param name="channel">channel number</param>
        /// <param name="tuning">tuning of all playing voices in semitones (default 0.0, standard (A440) tuning)</param>
        public void ChannelSetTuning(int channel, float tuning)
        {
            var c = ChannelInit(channel);
            if (c.Tuning == tuning) return;

            c.Tuning = tuning;
            ChannelApplyPitch(channel, c);
        }

        /// <summary>
        ///     Apply a MIDI control change to the channel (not all controllers are supported!)
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="controller"></param>
        /// <param name="controlValue"></param>
        public void ChannelMidiControl(int channel, int controller, int controlValue)
        {
            var c = ChannelInit(channel);
            switch (controller)
            {
                case 5: /*Portamento_Time_MSB*/
                case 96: /*DATA_BUTTON_INCREMENT*/
                case 97: /*DATA_BUTTON_DECREMENT*/
                case 64: /*HOLD_PEDAL*/
                case 65: /*Portamento*/
                case 66: /*SostenutoPedal */
                case 122: /*LocalKeyboard */
                case 124: /*OmniModeOff */
                case 125: /*OmniModeon */
                case 126: /*MonoMode */
                case 127: /*PolyMode*/
                    return;

                case 38 /*DATA_ENTRY_LSB*/:
                    c.MidiData = (ushort)((c.MidiData & 0x3F80) | controlValue);
                    switch (c.MidiRpn)
                    {
                        case 0:
                            ChannelSetPitchRange(channel, (c.MidiData >> 7) + 0.01f * (c.MidiData & 0x7F));
                            break;
                        case 1:
                            ChannelSetTuning(channel, (int)c.Tuning + (c.MidiData - 8192.0f) / 8192.0f); //fine tune
                            break;
                    }

                    return;

                case 7 /*VOLUME_MSB*/:
                    c.MidiVolume = (ushort)((c.MidiVolume & 0x7F) | (controlValue << 7));
                    //Raising to the power of 3 seems to result in a decent sounding volume curve for MIDI
                    ChannelSetVolume(channel,
                        (float)Math.Pow(c.MidiVolume / 16383.0f * (c.MidiExpression / 16383.0f), 3.0f));
                    return;
                case 39 /*VOLUME_LSB*/:
                    c.MidiVolume = (ushort)((c.MidiVolume & 0x3F80) | controlValue);
                    //Raising to the power of 3 seems to result in a decent sounding volume curve for MIDI
                    ChannelSetVolume(channel,
                        (float)Math.Pow(c.MidiVolume / 16383.0f * (c.MidiExpression / 16383.0f), 3.0f));
                    return;
                case 11 /*EXPRESSION_MSB*/:
                    c.MidiExpression = (ushort)((c.MidiExpression & 0x7F) | (controlValue << 7));
                    //Raising to the power of 3 seems to result in a decent sounding volume curve for MIDI
                    ChannelSetVolume(channel,
                        (float)Math.Pow(c.MidiVolume / 16383.0f * (c.MidiExpression / 16383.0f), 3.0f));
                    return;
                case 43 /*EXPRESSION_LSB*/:
                    c.MidiExpression = (ushort)((c.MidiExpression & 0x3F80) | controlValue);
                    //Raising to the power of 3 seems to result in a decent sounding volume curve for MIDI
                    ChannelSetVolume(channel,
                        (float)Math.Pow(c.MidiVolume / 16383.0f * (c.MidiExpression / 16383.0f), 3.0f));
                    return;
                case 10 /*PAN_MSB*/:
                    c.MidiPan = (ushort)((c.MidiPan & 0x7F) | (controlValue << 7));
                    ChannelSetPan(channel, c.MidiPan / 16383.0f);
                    return;
                case 42 /*PAN_LSB*/:
                    c.MidiPan = (ushort)((c.MidiPan & 0x3F80) | controlValue);
                    ChannelSetPan(channel, c.MidiPan / 16383.0f);
                    return;
                case 6 /*DATA_ENTRY_MSB*/:
                    c.MidiData = (ushort)((c.MidiData & 0x7F) | (controlValue << 7));
                    switch (c.MidiRpn)
                    {
                        case 0:
                            ChannelSetPitchRange(channel, (c.MidiData >> 7) + 0.01f * (c.MidiData & 0x7F));
                            break;
                        case 1:
                            ChannelSetTuning(channel, (int)c.Tuning + (c.MidiData - 8192.0f) / 8192.0f); //fine tune
                            break;
                        case 2:
                            ChannelSetTuning(channel, controlValue - 64.0f + (c.Tuning - (int)c.Tuning)); //coarse tune
                            break;
                    }

                    return;
                case 0 /*BANK_SELECT_MSB*/:
                    c.Bank = (ushort)(0x8000 | controlValue);
                    return; //bank select MSB alone acts like LSB
                case 32 /*BANK_SELECT_LSB*/:
                    c.Bank = (ushort)(((c.Bank & 0x8000) != 0 ? (c.Bank & 0x7F) << 7 : 0) | controlValue);
                    return;
                case 101 /*RPN_MSB*/:
                    c.MidiRpn = (ushort)(((c.MidiRpn == 0xFFFF ? 0 : c.MidiRpn) & 0x7F) | (controlValue << 7));
                    // TODO
                    return;
                case 100 /*RPN_LSB*/:
                    c.MidiRpn = (ushort)(((c.MidiRpn == 0xFFFF ? 0 : c.MidiRpn) & 0x3F80) | controlValue);
                    // TODO
                    return;
                case 98 /*NRPN_LSB*/:
                    c.MidiRpn = 0xFFFF;
                    // TODO
                    return;
                case 99 /*NRPN_MSB*/:
                    c.MidiRpn = 0xFFFF;
                    // TODO
                    return;
                case 120 /*ALL_SOUND_OFF*/:
                    ChannelSoundsOffAll(channel);
                    return;
                case 123 /*ALL_NOTES_OFF*/:
                    ChannelNoteOffAll(channel);
                    return;
                case 121 /*ALL_CTRL_OFF*/:
                    c.MidiVolume = c.MidiExpression = 16383;
                    c.MidiPan = 8192;
                    c.Bank = 0;
                    ChannelSetVolume(channel, 1);
                    ChannelSetPan(channel, 0.5f);
                    ChannelSetPitchRange(channel, 2);
                    // TODO
                    return;
            }
        }

        /// <summary>
        ///     Gets the current preset index of the given channel.
        /// </summary>
        /// <param name="channel">The channel index</param>
        /// <returns>The current preset index of the given channel.</returns>
        public int ChannelGetPresetIndex(int channel)
        {
            return _channels != null && channel < _channels.ChannelList.Count
                ? _channels.ChannelList[channel].PresetIndex
                : 0;
        }

        /// <summary>
        ///     Gets the current bank of the given channel.
        /// </summary>
        /// <param name="channel">The channel index</param>
        /// <returns>The current bank of the given channel.</returns>
        public int ChannelGetPresetBank(int channel)
        {
            return _channels != null && channel < _channels.ChannelList.Count
                ? _channels.ChannelList[channel].Bank & 0x7FFF
                : 0;
        }

        /// <summary>
        ///     Gets the current pan of the given channel.
        /// </summary>
        /// <param name="channel">The channel index</param>
        /// <returns>The current pan of the given channel.</returns>
        public float ChannelGetPan(int channel)
        {
            return _channels != null && channel < _channels.ChannelList.Count
                ? _channels.ChannelList[channel].PanOffset - 0.5f
                : 0.5f;
        }

        /// <summary>
        ///     Gets the current volume of the given channel.
        /// </summary>
        /// <param name="channel">The channel index</param>
        /// <returns>The current volune of the given channel.</returns>
        public float ChannelGetVolume(int channel)
        {
            return _channels != null && channel < _channels.ChannelList.Count
                ? SynthHelper.DecibelsToGain(_channels.ChannelList[channel].GainDb)
                : 1.0f;
        }

        /// <summary>
        ///     Gets the current pitch wheel of the given channel.
        /// </summary>
        /// <param name="channel">The channel index</param>
        /// <returns>The current pitch wheel of the given channel.</returns>
        public int ChannelGetPitchWheel(int channel)
        {
            return _channels != null && channel < _channels.ChannelList.Count
                ? _channels.ChannelList[channel].PitchWheel
                : 8192;
        }

        /// <summary>
        ///     Gets the current pitch range of the given channel.
        /// </summary>
        /// <param name="channel">The channel index</param>
        /// <returns>The current pitch range of the given channel.</returns>
        public float ChannelGetPitchRange(int channel)
        {
            return _channels != null && channel < _channels.ChannelList.Count
                ? _channels.ChannelList[channel].PitchRange
                : 2.0f;
        }

        /// <summary>
        ///     Gets the current tuning of the given channel.
        /// </summary>
        /// <param name="channel">The channel index</param>
        /// <returns>The current tuning of the given channel.</returns>
        public float ChannelGetTuning(int channel)
        {
            return _channels != null && channel < _channels.ChannelList.Count
                ? _channels.ChannelList[channel].Tuning
                : 0.0f;
        }

        #endregion
    }
}