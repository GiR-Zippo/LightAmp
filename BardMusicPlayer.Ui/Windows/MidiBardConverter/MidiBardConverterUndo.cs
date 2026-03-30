/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace BardMusicPlayer.Ui.Windows.MidiBardConverter
{
    public class UndoAction
    {
        public string Name { get; set; }
        public Action ApplyUndo { get; set; }
        public Action ApplyRedo { get; set; }
    }

    public class MidiBardConverterUndo
    {
        private Stack<UndoAction> _undoStack { get; set; } = new Stack<UndoAction>();
        private Stack<UndoAction> _redoStack { get; set; } = new Stack<UndoAction>();

        public void ExecuteAndRegisterUndo(string name, Action undoAction, Action redoAction)
        {
            redoAction();
            _undoStack.Push(new UndoAction { Name = name, ApplyUndo = undoAction, ApplyRedo = redoAction });
            _redoStack.Clear();
            if (_undoStack.Count > 50)
            {
                var temp = _undoStack.Reverse().Skip(1).Reverse();
                _undoStack = new Stack<UndoAction>(temp);
            }
        }

        public void Undo()
        {
            if (_undoStack.Any())
            {
                var a = _undoStack.Pop();
                a.ApplyUndo();
                _redoStack.Push(a);
            }
        }

        public void Redo()
        {
            if (_redoStack.Any())
            {
                var a = _redoStack.Pop();
                a.ApplyRedo();
                _undoStack.Push(a);
            }
        }
    }
}
