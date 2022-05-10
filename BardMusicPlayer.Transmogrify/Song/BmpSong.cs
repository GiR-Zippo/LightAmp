/*
 * Copyright(c) 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/BardMusicPlayer/BardMusicPlayer/blob/develop/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Transmogrify.Processor.Utilities;
using BardMusicPlayer.Transmogrify.Song.Config;
using BardMusicPlayer.Transmogrify.Song.Utilities;
using LiteDB;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Newtonsoft.Json;

namespace BardMusicPlayer.Transmogrify.Song
{
    public sealed class BmpSong
    {
        /// <summary>
        /// 
        /// </summary>
        [BsonId]
        public ObjectId Id { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public string Title { get; set; } = "";

        /// <summary>
        /// 
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// 
        /// </summary>
        public TempoMap SourceTempoMap { get; set; } = TempoMap.Default;

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<long, TrackContainer> TrackContainers { get; set; } = new();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<MidiFile> GetProcessedMidiFile()
        {
            var sourceMidiData = new MidiFile(TrackContainers.Values.SelectMany(track => track.ConfigContainers).SelectMany(track => track.Value.ProccesedTrackChunks));
            sourceMidiData.ReplaceTempoMap(Tools.GetMsTempoMap());
            var midiFile = new MidiFile();
            if (sourceMidiData.GetNotes().Count < 1) return Task.FromResult(midiFile);
            var delta = sourceMidiData.GetNotes().First().Time;
            foreach (var trackChunk in sourceMidiData.GetTrackChunks())
            {
                var trackName = trackChunk.Events.OfType<SequenceTrackNameEvent>().First().Text;
                if (trackName.StartsWith("tone:"))
                {
                    var newTrackChunk = new TrackChunk(new SequenceTrackNameEvent(trackName));
                    var newNotes = new List<Note>();
                    foreach (var note in trackChunk.GetNotes())
                    {
                        if (note.Time - delta < 0) continue; // TODO: log this error, though this shouldn't be possible.
                        note.Time -= delta;
                        newNotes.Add(note);
                    }
                    newTrackChunk.AddObjects(newNotes);
                    midiFile.Chunks.Add(newTrackChunk);
                }
                else if (trackName.StartsWith("lyric:"))
                {
                    var newTrackChunk = new TrackChunk(new SequenceTrackNameEvent(trackName));
                    var newLyrics = new List<TimedEvent>();
                    foreach (var midiEvent in trackChunk.GetTimedEvents().Where(e => e.Event.EventType == MidiEventType.Lyric))
                    {
                        if (midiEvent.Time - delta < 0) continue; // TODO: log that you cannot have lyrics come before the first note.
                        midiEvent.Time -= delta;
                        newLyrics.Add(midiEvent);
                    }
                    newTrackChunk.AddObjects(newLyrics);
                    midiFile.Chunks.Add(newTrackChunk);
                }
            }
            midiFile.ReplaceTempoMap(Tools.GetMsTempoMap());
            return Task.FromResult(midiFile);
        }

        public static Task<BmpSong> OpenFile(string path)
        {
            BmpSong song = null;
            if (Path.GetExtension(path).Equals(".mmsong"))
                song = OpenMMSongFile(path);
            else
                song = OpenMidiFile(path);
            return Task.FromResult(song);
        }

        /// <summary>
        /// Open and process the midifile, tracks with note placed first
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static BmpSong OpenMidiFile(string path)
        {
            if (!File.Exists(path)) throw new BmpTransmogrifyException("File " + path + " does not exist!");

            using var fileStream = File.OpenRead(path);

            var midiFile = fileStream.ReadAsMidiFile();
            fileStream.Dispose();

            return CovertMidiToSong(midiFile, path);
        }

        /// <summary>
        /// Opens and process a mmsong file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static BmpSong OpenMMSongFile(string path)
        {
            if (!File.Exists(path)) throw new BmpTransmogrifyException("File " + path + " does not exist!");

            MMSongContainer songContainer = null;

            FileInfo fileToDecompress = new FileInfo(path);
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);
                using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        decompressionStream.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                        var data = "";
                        using (var reader = new StreamReader(memoryStream, System.Text.Encoding.ASCII))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                data += line;
                            }
                        }
                        memoryStream.Close();
                        decompressionStream.Close();
                        songContainer = JsonConvert.DeserializeObject<MMSongContainer>(data);
                    }
                }
            }

            MidiFile midiFile = new MidiFile();
            foreach (MMSong msong in songContainer.songs)
            {
                if (msong.bards.Count() == 0)
                    continue;
                else
                {
                    Parallel.ForEach(msong.bards, bard =>
                    {
                        var thisTrack = new TrackChunk(new SequenceTrackNameEvent(Instrument.Parse(bard.instrument).Name));
                        using (var manager = new TimedEventsManager(thisTrack.Events))
                        {
                            TimedEventsCollection timedEvents = manager.Events;
                            int last = 0;
                            foreach (var note in bard.sequence)
                            {
                                if (note.Value == 254)
                                {
                                    var pitched = last + 24;
                                    timedEvents.Add(new TimedEvent(new NoteOffEvent((Melanchall.DryWetMidi.Common.SevenBitNumber)pitched, (Melanchall.DryWetMidi.Common.SevenBitNumber)127), note.Key));
                                }
                                else
                                {
                                    var pitched = (Melanchall.DryWetMidi.Common.SevenBitNumber)note.Value + 24;
                                    timedEvents.Add(new TimedEvent(new NoteOnEvent((Melanchall.DryWetMidi.Common.SevenBitNumber)pitched, (Melanchall.DryWetMidi.Common.SevenBitNumber)127), note.Key));
                                    last = note.Value;
                                }
                            }
                        }
                        midiFile.Chunks.Add(thisTrack);
                    });
                    break; //Only the first song for now
                }
            }
            midiFile.ReplaceTempoMap(TempoMap.Create(Tempo.FromBeatsPerMinute(25)));
            return CovertMidiToSong(midiFile, path);
        }

        private static BmpSong CovertMidiToSong(MidiFile midiFile, string path)
        {
            var timer = new Stopwatch();
            timer.Start();

            var song = new BmpSong
            {
                Title = Path.GetFileNameWithoutExtension(path),
                SourceTempoMap = midiFile.GetTempoMap().Clone(),
                TrackContainers = new Dictionary<long, TrackContainer>()
            };

            //some midifiles have a ChannelPrefixEvent with a channel greater than 0xF. remove 'em.
            foreach (var chunk in midiFile.GetTrackChunks())
            {
                using (TimedEventsManager timedEventsManager = chunk.ManageTimedEvents())
                {
                    TimedEventsCollection events = timedEventsManager.Events;
                    List<TimedEvent> prefixList = events.Where(e => e.Event is ChannelPrefixEvent).ToList();
                    foreach (TimedEvent tevent in prefixList)
                        if((tevent.Event as ChannelPrefixEvent).Channel > 0xF)
                            events.Remove(tevent);
                }
            }

            var trackChunkArray = midiFile.GetTrackChunks().ToArray();

            //Set note tracks at first
            List<int> skippedTracks = new List<int>();
            int index = 0;
            for (var i = 0; i < midiFile.GetTrackChunks().Count(); i++)
            {
                //ignore tracks without notes
                if (trackChunkArray[i].ManageNotes().Notes.Count() > 0)
                {
                    song.TrackContainers[index] = new TrackContainer { SourceTrackChunk = (TrackChunk)trackChunkArray[i].Clone() };
                    index++;
                }
                else
                    skippedTracks.Add(i);
            }
            //set the ignored tracks for data
            foreach (int i in skippedTracks)
            {
                song.TrackContainers[index] = new TrackContainer { SourceTrackChunk = (TrackChunk)trackChunkArray[i].Clone() };
                index++;
            }

            //check the tracks for data
            for (var i = 0; i < song.TrackContainers.Count(); i++)
            {
                song.TrackContainers[i].ConfigContainers = song.TrackContainers[i].SourceTrackChunk.ReadConfigs(i, song);
            }
            //process the tracks we've got
            Parallel.For(0, song.TrackContainers.Count, i =>
            {
                Parallel.For(0, song.TrackContainers[i].ConfigContainers.Count, async j =>
                {
                    switch (song.TrackContainers[i].ConfigContainers[j].ProcessorConfig)
                    {
                        case ClassicProcessorConfig classicConfig:
                            Console.WriteLine("Processing: Track:" + i + " ConfigContainer:" + j + " ConfigType:" +
                                              classicConfig.GetType() +
                                              " Instrument:" + classicConfig.Instrument + " OctaveRange:" +
                                              classicConfig.OctaveRange + " PlayerCount:" + classicConfig.PlayerCount +
                                              " IncludeTracks:" + string.Join(",", classicConfig.IncludedTracks));
                            song.TrackContainers[i].ConfigContainers[j].ProccesedTrackChunks =
                                await song.TrackContainers[i].ConfigContainers[j].RefreshTrackChunks(song);
                            break;
                        case LyricProcessorConfig lyricConfig:
                            Console.WriteLine("Processing: Track:" + i + " ConfigContainer:" + j + " ConfigType:" +
                                              lyricConfig.GetType() + " PlayerCount:" + lyricConfig.PlayerCount +
                                              " IncludeTracks:" + string.Join(",", lyricConfig.IncludedTracks));
                            song.TrackContainers[i].ConfigContainers[j].ProccesedTrackChunks =
                                await song.TrackContainers[i].ConfigContainers[j].RefreshTrackChunks(song);
                            break;
                        case VSTProcessorConfig vstConfig:
                            Console.WriteLine("Processing: Track:" + i + " ConfigContainer:" + j + " ConfigType:" +
                                              vstConfig.GetType() + " PlayerCount:" + vstConfig.PlayerCount +
                                              " IncludeTracks:" + string.Join(",", vstConfig.IncludedTracks));
                            song.TrackContainers[i].ConfigContainers[j].ProccesedTrackChunks =
                                await song.TrackContainers[i].ConfigContainers[j].RefreshTrackChunks(song);
                            break;
                        default:
                            Console.WriteLine("error unknown config.");
                            break;
                    }
                });
            });
            skippedTracks.Clear();
            timer.Stop();

            var timeTaken = timer.Elapsed;
            Console.WriteLine("Time taken: " + timeTaken.ToString(@"m\:ss\.fff"));
            return song;
        }
    }
}
