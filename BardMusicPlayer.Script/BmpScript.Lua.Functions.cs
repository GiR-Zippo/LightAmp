/*
 * Copyright(c) 2024 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Maestro;
using Neo.IronLua;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BardMusicPlayer.Script
{
    public partial class BmpScript
    {
        private void LoadLua(string filename)
        {
            Task task = Task.Run(() =>
            {
                thread = Thread.CurrentThread;
                OnRunningStateChanged?.Invoke(this, true);
                using (lua = new Lua())
                {
                    string text = File.ReadAllText(filename);
                    dynamic env = lua.CreateEnvironment<LuaGlobal>();
                    env.print = new Action<object[]>(Print); //for debug use
                    env.Sleep = new Action<int>(Sleep);
                    env.Say = new Action<string, string, LuaTable>(Say);
                    env.Macro = new Action<string, string, LuaTable>(Macro);
                    env.GetPerformerNames = new Func<LuaTable> (GetPerformerNames);
                    try
                    {
                        var chunk = lua.CompileChunk(text, filename, new LuaCompileOptions() { DebugEngine = LuaStackTraceDebugger.Default });
                        env.dochunk(chunk);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Expception: {0}", e.Message);
                        var d = LuaExceptionData.GetData(e); // get stack trace
                        Console.WriteLine("StackTrace: {0}", d.FormatStackTrace(0, false));
                        OnRunningStateChanged?.Invoke(this, false);
                    }
                    lua.Dispose();
                    lua = null;
                }
                OnRunningStateChanged?.Invoke(this, false);
            });

            return;
        }

        private static LuaTable GetPerformerNames()
        {
            LuaTable tbl = new LuaTable();
            BmpMaestro.Instance.GetAllPerformers().ToList().ForEach(n => tbl.Add(n.PlayerName));
            return tbl;
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
    }
}
