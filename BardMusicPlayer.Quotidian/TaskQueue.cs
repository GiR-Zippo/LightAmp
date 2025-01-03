﻿/*
 * Copyright(c) 2025 GiR-Zippo, 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Threading.Tasks;

namespace BardMusicPlayer.Quotidian
{
    public sealed class TaskQueue
    {
        private Task previous = Task.FromResult(false);
        private readonly object key = new();

        public Task<T> Enqueue<T>(Func<Task<T>> taskGenerator)
        {
            lock (key)
            {
                var next = previous.ContinueWith(t => taskGenerator()).Unwrap();
                previous = next;
                return next;
            }
        }

        public Task Enqueue(Func<Task> taskGenerator)
        {
            lock (key)
            {
                var next = previous.ContinueWith(t => taskGenerator()).Unwrap();
                previous = next;
                return next;
            }
        }
    }
}
