/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Maestro;
using BardMusicPlayer.Seer;
using Neo.IronLua;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BardMusicPlayer.Script.Engines
{
    public class BmpLuaScript : IBmpScript
    {
        private Thread thread { get; set; }
        private Lua lua { get; set; } = null;

        public BmpLuaScript(string Id)
        {
            UId = Id;
        }

        #region IBmpScript Members
        public event EventHandler<KeyValuePair<string, bool>> OnRunningStateChanged;
        public string UId { get; set; }

        //Load and run a script from file
        public void LoadAndRun(string filename)
        {
            Task task = Task.Run(() =>
            {
                thread = Thread.CurrentThread;
                OnRunningStateChanged?.Invoke(this, new KeyValuePair<string, bool>(UId, true));
                using (lua = new Lua())
                {
                    string text = File.ReadAllText(filename);
                    dynamic env = lua.CreateEnvironment<LuaGlobal>();
                    env.print = new Action<object[]>(Print); //for debug use
                    env.Sleep = new Action<int>(Sleep);
                    env.Say = new Action<string, string, LuaTable>(Say);
                    env.Macro = new Action<string, string, LuaTable>(Macro);
                    env.GetPerformerNames = new Func<LuaTable>(GetPerformerNames);
                    env.GetPerformerCIDs = new Func<LuaTable>(GetPerformerCIDs);
                    env.GetPerformerPIDs = new Func<LuaTable>(GetPerformerPIDs);
                    env.GetPlayedTime = new Func<double>(GetPlayedTime);
                    try
                    {
                        var chunk = lua.CompileChunk(text, filename, new LuaCompileOptions() { DebugEngine = LuaStackTraceDebugger.Default });
                        env.dochunk(chunk);
                    }
                    catch (Exception e)
                    {
                        OnRunningStateChanged?.Invoke(this, new KeyValuePair<string, bool>(UId, false));
                        Console.WriteLine("Expception: {0}", e.Message);
                        var d = LuaExceptionData.GetData(e); // get stack trace
                        Console.WriteLine("StackTrace: {0}", d.FormatStackTrace(0, false));
                    }
                    OnRunningStateChanged?.Invoke(this, new KeyValuePair<string, bool>(UId, false));
                    lua.Dispose();
                    lua = null;
                }
            });
            return;
        }

        //Stop this Script
        public void StopExecution()
        {
            if (thread == null)
                return;

            if (thread.ThreadState != ThreadState.Stopped)
            {
                if (lua is not null)
                    lua.Dispose();
                thread.Abort();
            }
        }
        #endregion

        #region Lua Implementations
        private static LuaTable GetPerformerNames()
        {
            LuaTable tbl = new LuaTable();
            BmpMaestro.Instance.GetAllPerformers().ToList().OrderBy(n => n.game.ConfigId).ToList().ForEach(n => tbl.Add(n.game.PlayerName));
            return tbl;
        } // Table PlayerNames

        private static LuaTable GetPerformerCIDs()
        {
            LuaTable tbl = new LuaTable();
            BmpMaestro.Instance.GetAllPerformers().ToList().OrderBy(n => n.game.ConfigId).ToList().ForEach(n => tbl.Add(n.game.ConfigId));
            return tbl;
        } // Table Performer CIDS

        private static LuaTable GetPerformerPIDs()
        {
            LuaTable tbl = new LuaTable();
            BmpMaestro.Instance.GetAllPerformers().ToList().OrderBy(n => n.game.ConfigId).ToList().ForEach(n => tbl.Add(n.game.Pid));
            return tbl;
        } // Table Performer PIDS

        private double GetPlayedTime()
        {
            return BmpScript.Instance.PlaytimeTotalInSeconds;
        }

        private static void Print(object[] texts)
        {
            foreach (object o in texts)
                Console.Write(o);
            Console.WriteLine();
        } // proc Print

        private static void Sleep(int time)
        {
            Thread.Sleep(time);
        } // proc Sleep

        private static void Say(string text, string bard, LuaTable unselected_bards)
        {
            BmpMaestro.Instance.SendText(bard, Quotidian.Structs.ChatMessageChannelType.Say, text, unselected_bards == null ? null : ((IDictionary<object, object>)unselected_bards).Values.OfType<string>().ToList());
        } // proc Say

        private static void Macro(string text, string bard, LuaTable unselected_bards)
        {
            BmpMaestro.Instance.SendText(bard, Quotidian.Structs.ChatMessageChannelType.None, "/" + text, unselected_bards == null ? null : ((IDictionary<object, object>)unselected_bards).Values.OfType<string>().ToList());
        } // proc Macro
        #endregion

        public Game GetGameByPid(int pid)
        {
            var game = BmpSeer.Instance.Games.FirstOrDefault(n => n.Value.Pid == pid).Value;
            if (game != null)
                return game;
            return null;
        }
    }
}
