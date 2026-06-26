/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System.Collections.Generic;

namespace BardMusicPlayer.Transmogrify.Song.Importers.FruityLoops.FruityStrucs
{
    public record AutomationData : IChannelData
    {
        public Channel Channel { get; set; } = null;
        public int Parameter { get; set; } = 0;
        public int InsertId { get; set; } = -1;
        public int SlotId { get; set; } = -1;
        public bool VstParameter { get; set; } = true;
        public AutomationKeyframe[] Keyframes { get; set; } = new AutomationKeyframe[0];
    }

    public record AutomationKeyframe
    {
        public int Position { get; set; } = 0;
        public double Value { get; set; } = 0;
        public float Tension { get; set; } = 0;
    }

    public record Channel
    {
        public int Id { get; set; } = 0;
        public string ChannelName { get; set; } = "";
        public uint Color { get; set; } = 0x4080FF;
        public IChannelData Data { get; set; } = null;
    }

    public record ChannelPlaylistItem : IPlaylistItem
    {
        public int Position { get; set; }
        public int Length { get; set; }
        public int StartOffset { get; set; }
        public int EndOffset { get; set; }
        public bool Muted { get; set; }
        public Channel Channel { get; set; }
    }

    public record GeneratorData : IChannelData
    {
        public byte[] PluginSettings { get; set; } = null;
        public Plugin Plugin { get; set; } = new Plugin();
        public string GeneratorName { get; set; } = "";
        public double Volume { get; set; } = 100;
        public double Panning { get; set; } = 0;
        public uint BaseNote { get; set; } = 57;
        public int Insert { get; set; } = -1;
        public int LayerParent { get; set; } = -1;

        public string SampleFileName { get; set; } = "";
        public int SampleAmp { get; set; } = 100;
        public bool SampleReversed { get; set; } = false;
        public bool SampleReverseStereo { get; set; } = false;
        public bool SampleUseLoopPoints { get; set; } = false;

        public FruityEnums.ArpDirection ArpDir { get; set; } = FruityEnums.ArpDirection.Off;
        public int ArpRange { get; set; } = 0;
        public int ArpChord { get; set; } = 0;
        public int ArpRepeat { get; set; } = 0;
        public double ArpTime { get; set; } = 100;
        public double ArpGate { get; set; } = 100;
        public bool ArpSlide { get; set; } = false;
    }

    public record OldAutomationData : IChannelData
    {
        public byte Command;
        public byte Channel;
        public List<ProgramChangeData> Programchanges { get; set; } = new List<ProgramChangeData>();
    }

    public record ProgramChangeData : IChannelData
    {
        public uint Timestamp;
        public byte InstrumentNumber;
    }

    public interface IChannelData
    {
    }

    public interface IPlaylistItem
    {
        int Position { get; set; }
        int Length { get; set; }
        int StartOffset { get; set; }
        int EndOffset { get; set; }
    }

    public record Insert
    {
        public const int MaxSlotCount = 10;

        public int Id { get; set; } = 0;
        public string Name { get; set; } = "";
        public uint Color { get; set; } = 0x000000;
        public ushort Icon { get; set; } = 0;
        public FruityEnums.InsertFlags Flags { get; set; } = 0;
        public int Volume { get; set; } = 100;
        public int Pan { get; set; } = 0;
        public int StereoSep { get; set; } = 0;
        public int LowLevel { get; set; } = 0;
        public int BandLevel { get; set; } = 0;
        public int HighLevel { get; set; } = 0;
        public int LowFreq { get; set; } = 0;
        public int BandFreq { get; set; } = 0;
        public int HighFreq { get; set; } = 0;
        public int LowWidth { get; set; } = 0;
        public int BandWidth { get; set; } = 0;
        public int HighWidth { get; set; } = 0;
        public bool[] Routes { get; set; } = new bool[FruityProject.MaxInsertCount];
        public int[] RouteVolumes { get; set; } = new int[FruityProject.MaxInsertCount];
        public InsertSlot[] Slots { get; set; } = new InsertSlot[MaxSlotCount];

        public Insert()
        {
            for (var i = 0; i < MaxSlotCount; i++)
                Slots[i] = new InsertSlot();

            for (var i = 0; i < FruityProject.MaxInsertCount; i++)
                RouteVolumes[i] = 12800;
        }
    }

    public record InsertSlot
    {
        public int Volume { get; set; } = 100;
        public int State { get; set; } = 0;
        public int DryWet { get; set; } = -1;
        public byte[] PluginSettings { get; set; } = null;
        public Plugin Plugin { get; set; } = new Plugin();
    }

    public record Note
    {
        public int Position { get; set; }
        public int Length { get; set; }
        public byte Key { get; set; }
        public ushort FinePitch { get; set; }
        public ushort Release { get; set; }
        public byte Pan { get; set; }
        public byte Velocity { get; set; }
    }

    public record Pattern
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = "";
        public Dictionary<Channel, List<Note>> Notes { get; set; } = new Dictionary<Channel, List<Note>>();
    }

    public record PatternPlaylistItem : IPlaylistItem
    {
        public int Position { get; set; }
        public int Length { get; set; }
        public int StartOffset { get; set; }
        public int EndOffset { get; set; }
        public bool Muted { get; set; }
        public Pattern Pattern { get; set; }
    }

    public record Plugin
    {
        public int MidiInPort { get; set; }
        public int MidiOutPort { get; set; }
        public byte PitchBendRange { get; set; }

        public uint Flags { get; set; }

        public int NumInputs { get; set; }
        public int NumOutputs { get; set; }

        public PluginIoInfo[] InputInfo { get; set; }
        public PluginIoInfo[] OutputInfo { get; set; }

        public int InfoKind { get; set; }

        public uint VstNumber { get; set; }
        public string VstId { get; set; }

        public byte[] Guid { get; set; }

        public byte[] State { get; set; }

        public string Name { get; set; }

        public string FileName { get; set; }

        public string VendorName { get; set; }
    }

    public record PluginIoInfo
    {
        public int MixerOffset { get; set; } = 0;
        public byte Flags { get; set; } = 0;
    }

    public record Track
    {
        public string Name { get; set; }
        public uint Color { get; set; }
        public List<IPlaylistItem> Items { get; set; } = new List<IPlaylistItem>();
    }

    public struct EnVars
    {
        public Pattern CurPattern;
        public Channel CurChannel;
        public Insert CurInsert;
        public InsertSlot CurSlot;
        public int VersionMajor;
        public int TrackIndex;
    }
}
