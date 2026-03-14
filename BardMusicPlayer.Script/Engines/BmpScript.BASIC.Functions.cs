/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Maestro.Performance;
using BardMusicPlayer.Maestro;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using BasicSharp;
using System.Threading;
using System.Windows;
using System;
using System.IO;

namespace BardMusicPlayer.Script.Engines
{
    public partial class BmpBASICScript : IBmpScript
    {
        public Thread thread { get; set; }
        private Interpreter basic { get; set; } = null;
        public BmpBASICScript(string Id)
        {
            UId = Id;
        }

        #region IBmpScript Members
        public event EventHandler<KeyValuePair<string, bool>> OnRunningStateChanged;
        public string UId { get; set; }

        public void LoadAndRun(string filename)
        {
            Task basictask = Task.Run(() =>
            {
                thread = Thread.CurrentThread;
                OnRunningStateChanged?.Invoke(this, new KeyValuePair<string, bool>(UId, true));
                unselected_bards = new List<string>();
                basic = new Interpreter(File.ReadAllText(filename));
                basic.printHandler += Print;
                basic.cprintHandler += Console.WriteLine;
                basic.tapKeyHandler += TapKey;
                basic.selectedBardHandler += SetSelectedBard;
                basic.selectedBardAsStringHandler += SetSelectedBardName;
                basic.unselectBardHandler += UnSelectBardName;
                basic.playbackPositionHandler += PlaybackPosition;
                try
                {
                    basic.Exec();
                }
                catch (ThreadAbortException /*e*/)
                {
                    CleanUp();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message + "\r\n" + basic.GetLine(), "Exec Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                CleanUp();

            });
        }

        public void StopExecution()
        {
            if (thread == null)
                return;

            if (basic is not null)
                basic.StopExec();

            if (thread.ThreadState != ThreadState.Stopped)
                thread.Abort();
        }
        #endregion

        private void CleanUp()
        {
            OnRunningStateChanged?.Invoke(this, new KeyValuePair<string, bool>(UId, false));

            unselected_bards = null;
            basic.printHandler -= Print;
            basic.cprintHandler -= Console.WriteLine;
            basic.tapKeyHandler -= TapKey;
            basic.selectedBardHandler -= SetSelectedBard;
            basic.selectedBardAsStringHandler -= SetSelectedBardName;
            basic.unselectBardHandler -= UnSelectBardName;
            basic.playbackPositionHandler -= PlaybackPosition;
            basic = null;
        }

        #region BASIC Commandhelper
        private string selectedBardName { get; set; } = "";
        private List<string> unselected_bards { get; set; } = null;
        string playtime { get; set; } = "";

        public void SetSelectedBard(int num)
        {
            if (num == 0)
            {
                selectedBardName = "all";
                return;
            }

            var plist = BmpMaestro.Instance.GetAllPerformers();
            if (plist.Count() <= 0)
            {
                selectedBardName = "";
                return;
            }

            Performer performer = plist.ElementAt(num - 1);
            if (performer != null)
                selectedBardName = performer.game.PlayerName;
            else
                selectedBardName = "";
        }

        public void SetSelectedBardName(string name)
        {
            selectedBardName = name;
        }

        public void UnSelectBardName(string name)
        {
            if (name.ToLower().Equals(""))
                unselected_bards.Clear();
            else
            {
                if (name.Contains(","))
                {
                    var names = name.Split(',');
                    Parallel.ForEach(names, n =>
                    {
                        string cname = n.Trim();
                        if (cname != "")
                            unselected_bards.Add(cname);
                    });
                }
                else
                    unselected_bards.Add(name);
            }
        }

        public void Print(Quotidian.Structs.ChatMessageChannelType type, string text)
        {
            BmpMaestro.Instance.SendText(selectedBardName, type, text, unselected_bards);
        }

        public void TapKey(string modifier, string character)
        {
            BmpMaestro.Instance.TapKey(selectedBardName, modifier, character, unselected_bards);
        }

        public string PlaybackPosition()
        {
            return playtime;
        }

        private void Instance_PlaybackTimeChanged(object sender, Maestro.Events.CurrentPlayPositionEvent e)
        {
            string Seconds = e.timeSpan.Seconds.ToString();
            string Minutes = e.timeSpan.Minutes.ToString();
            playtime = ((Minutes.Length == 1) ? "0" + Minutes : Minutes) + ":" +
                    ((Seconds.Length == 1) ? "0" + Seconds : Seconds);
        }
        #endregion
    }
}
