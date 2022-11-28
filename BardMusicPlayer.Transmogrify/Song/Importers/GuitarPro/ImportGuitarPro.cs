using BardMusicPlayer.Transmogrify.Song.Utilities;
using Melanchall.DryWetMidi.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace BardMusicPlayer.Transmogrify.Song.Importers.GuitarPro
{
    public static class ImportGuitarPro
    {
        private static GPFile gpfile;
        public static MidiFile OpenGTPSongFile(string path)
        {
            var loader = File.ReadAllBytes(path);
            //Detect Version by Filename
            int version = 7;
            string fileEnding = Path.GetExtension(path);
            if (fileEnding.Equals(".gp3")) version = 3;
            if (fileEnding.Equals(".gp4")) version = 4;
            if (fileEnding.Equals(".gp5")) version = 5;
            if (fileEnding.Equals(".gpx")) version = 6;
            if (fileEnding.Equals(".gp")) version = 7;


            switch (version)
            {
                case 3:
                    gpfile = new GP3File(loader);
                    gpfile.readSong();
                    break;
                case 4:
                    gpfile = new GP4File(loader);
                    gpfile.readSong();
                    break;
                case 5:
                    gpfile = new GP5File(loader);
                    gpfile.readSong();

                    break;
                case 6:
                    gpfile = new GP6File(loader);
                    gpfile.readSong();
                    gpfile = gpfile.self; //Replace with transferred GP5 file

                    break;
                /*case 7:
                    string archiveName = url.Substring(8).Replace("%20", " ");
                    byte[] buffer = new byte[8200000];
                    MemoryStream stream = new MemoryStream(buffer);
                    using (var unzip = new Unzip(archiveName))
                    {
                        //Console.WriteLine("Listing files in the archive:");
                        ListFiles(unzip);

                        unzip.Extract("Content/score.gpif", stream);
                        stream.Position = 0;
                        var sr = new StreamReader(stream);
                        string gp7xml = sr.ReadToEnd();

                        gpfile = new GP7File(gp7xml);
                        gpfile.readSong();
                        gpfile = gpfile.self; //Replace with transferred GP5 file

                    }
                    break;*/
                default:
                    Debug.WriteLine("Unknown File Format");
                    break;
            }
            Debug.WriteLine("Done");

            var song = new Native.NativeFormat(gpfile);
            var midi = song.toMidi();
            List<byte> data = midi.createBytes();
            var dataArray = data.ToArray();

            MemoryStream memoryStream = new MemoryStream();
            memoryStream.Write(dataArray, 0, dataArray.Length);
            memoryStream.Position = 0;
            var midiFile = memoryStream.ReadAsMidiFile();
            memoryStream.Dispose();
            return midiFile;
        }
    }
}