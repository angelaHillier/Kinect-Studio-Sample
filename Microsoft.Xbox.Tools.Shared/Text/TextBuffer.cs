//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class TextBuffer
    {
        public const int DefaultGetPencilTimeout = 100;
        const double MinimumMillisecondsTimeBetweenUndoUnits = 400;

        AutoResetEvent pencilEvent;
        Pencil activePencil;        // This is pretty much just for debugging purposes
        Stack<TextUndoUnit> undoStack;
        Stack<TextUndoUnit> redoStack;
        List<ITextUndoObserver> undoObservers;
        DateTime timeOfLastEdit;

        public TextBuffer() : this(TextData.Empty) { }

        public TextBuffer(TextData data)
        {
            this.TextData = data;
            this.LastChange = new TextChange
            {
                OldTextData = TextData.Empty,
                NewTextData = data,
                Replacement = data
            };

            this.Formatter = new TextFormatter(this);
            this.pencilEvent = new AutoResetEvent(true);    // Signalled -- pencil is available
            this.activePencil = null;
            this.undoStack = new Stack<TextUndoUnit>();
            this.redoStack = new Stack<TextUndoUnit>();
            this.undoObservers = new List<ITextUndoObserver>();
        }

        public TextData TextData { get; private set; }
        public TextChange LastChange { get; private set; }
        public TextFormatter Formatter { get; private set; }

        public event EventHandler<TextDataChangedEventArgs> TextDataChanged;

        public bool TryGetPencil(out TextPencil pencil)
        {
            return TryGetPencil(DefaultGetPencilTimeout, out pencil);
        }

        public bool TryGetPencil(int timeoutMilliseconds, out TextPencil pencil)
        {
            if (!this.pencilEvent.WaitOne(timeoutMilliseconds))
            {
                pencil = null;
                return false;
            }

            this.activePencil = new Pencil(this, forUndoRedo: false);
            pencil = this.activePencil;
            return true;
        }

        public TextPencil GetPencil()
        {
            return GetPencil(false);
        }

        Pencil GetPencil(bool forUndoRedo)
        {
            if (!this.pencilEvent.WaitOne())
            {
                throw new InvalidOperationException();
            }

            this.activePencil = new Pencil(this, forUndoRedo);
            return this.activePencil;
        }

        public bool CanUndo { get { return this.undoStack.Count > 0; } }
        public bool CanRedo { get { return this.redoStack.Count > 0; } }

        public void Undo(object instigator)
        {
            TextUndoUnit unit = null;

            using (var pencil = GetPencil(forUndoRedo: true))
            {
                if (this.undoStack.Count > 0)
                {
                    unit = this.undoStack.Pop();
                    NotifyBeforeUndoRedo(unit, instigator, isUndo: true);
                    unit.Undo(pencil);
                    this.redoStack.Push(unit);
                }
            }

            if (unit != null)
            {
                NotifyAfterUndoRedo(unit, instigator, isUndo: true);
            }
        }

        public void Redo(object instigator)
        {
            TextUndoUnit unit = null;

            using (var pencil = GetPencil(forUndoRedo: true))
            {
                if (this.redoStack.Count > 0)
                {
                    unit = this.redoStack.Pop();
                    NotifyBeforeUndoRedo(unit, instigator, isUndo: false);
                    unit.Redo(pencil);
                    this.undoStack.Push(unit);
                }
            }

            if (unit != null)
            {
                NotifyAfterUndoRedo(unit, instigator, isUndo: false);
            }
        }

        public void ObserveUndoRedo(ITextUndoObserver observer)
        {
            this.undoObservers.Add(observer);
        }

        public void StopObservingUndoRedo(ITextUndoObserver observer)
        {
            this.undoObservers.Remove(observer);
        }

        void NotifyBeforeUndoRedo(TextUndoUnit unit, object instigator, bool isUndo)
        {
            foreach (var observer in this.undoObservers)
            {
                observer.OnBeforeUndoRedoUnit(unit, instigator, isUndo);
            }
        }

        void NotifyAfterUndoRedo(TextUndoUnit unit, object instigator, bool isUndo)
        {
            foreach (var observer in this.undoObservers)
            {
                observer.OnAfterUndoRedoUnit(unit, instigator, isUndo);
            }
        }

        void OnPencilDisposed(Pencil pencil, TextDataChangedEventArgs args)
        {
            Debug.Assert(pencil == this.activePencil, "Disposing a pencil that isn't the active one.");

            if (args != null)
            {
                if (this.LastChange != null)
                {
                    this.LastChange.NextChange = args.Change;
                }

                TextChange finalChange = args.Change;

                while (finalChange.NextChange != null)
                {
                    finalChange = finalChange.NextChange;
                }

                this.TextData = finalChange.NewTextData;
                this.LastChange = finalChange;

                var handler = this.TextDataChanged;
                if (handler != null)
                {
                    handler(this, args);
                }

                if (pencil.UndoUnit != null)
                {
                    var now = DateTime.Now;
                    bool merged = false;

                    if (((now - this.timeOfLastEdit).TotalMilliseconds < MinimumMillisecondsTimeBetweenUndoUnits) && (this.undoStack.Count > 0))
                    {
                        // If possible, merge this unit with the one on the stack.
                        TextUndoUnit existingUnit = this.undoStack.Peek();
                        TextUndoUnit incomingUnit = pencil.UndoUnit;

                        if (incomingUnit.Metadata != null && incomingUnit.Metadata.TryMerge(existingUnit.Metadata))
                        {
                            // Successful merge
                            existingUnit.Metadata = incomingUnit.Metadata;
                            existingUnit.Merge(pencil.UndoUnit);
                            merged = true;
                        }
                    }

                    if (!merged)
                    {
                        this.undoStack.Push(pencil.UndoUnit);
                    }

                    this.redoStack.Clear();
                    this.timeOfLastEdit = now;
                }
            }

            this.activePencil = null;
            this.pencilEvent.Set();
        }

        public static TextBuffer FromText(string text, bool normalizeEndMarkers = false)
        {
            return new TextBuffer(TextData.FromString(text, normalizeEndMarkers));
        }

        class Pencil : TextPencil
        {
            TextBuffer buffer;
            TextChange firstChange;
            TextChange lastChange;
            TextUndoUnit undoUnit;

            public Pencil(TextBuffer buffer, bool forUndoRedo)
            {
                this.buffer = buffer;
                this.firstChange = null;
                this.lastChange = null;

                if (!forUndoRedo)
                {
                    this.undoUnit = new TextUndoUnit();
                }
            }

            public override TextBuffer Buffer { get { return this.buffer; } }
            public override TextChange Change { get { return this.firstChange; } }
            public override TextUndoUnit UndoUnit { get { return this.undoUnit; } }

            TextData CurrentTextData
            {
                get
                {
                    if (this.lastChange == null)
                    {
                        return this.buffer.TextData;
                    }

                    return this.lastChange.NewTextData;
                }
            }

            bool IsLocationValid(TextData currentData, TextLocation loc)
            {
                if ((loc.Line >= 0) && (loc.Line < currentData.Lines.Count) && (loc.Index >= 0) && (loc.Index <= currentData.Lines[loc.Line].Length))
                {
                    return true;
                }

                return false;
            }

            public override bool CanWrite(TextLocation start, TextLocation end)
            {
                var currentData = this.CurrentTextData;

                return IsLocationValid(currentData, start) && IsLocationValid(currentData, end);
            }

            TextLocation DetermineNewEnd(TextLocation start, TextLocation end, TextData replacement)
            {
                if (replacement.Lines.Count == 1)
                {
                    // Single-line replacement, meaning the span between start and end (which may be multiple lines)
                    // is being replaced by a single sub-line of text, so the end is on the same line as
                    // the start, past the start index by the length of the inserted sub-line
                    return new TextLocation(start.Line, start.Index + replacement.Lines[0].Length);
                }

                // Multi-line replacement, so the end index is always the length of the last line of the replacement.
                // The end line is simply the start line plus the number of replacement lines (minus one because
                // the last line of every TextData ends w/ EOB)
                return new TextLocation(start.Line + replacement.Lines.Count - 1, replacement.Lines[replacement.Lines.Count - 1].Length);
            }

            public override bool Write(TextLocation start, TextLocation end, TextData replacement)
            {
                if (!CanWrite(start, end) || replacement == null)
                {
                    throw new ArgumentException();
                }

                var currentData = this.CurrentTextData;
                var newData = TextData.ApplyEdit(currentData, start, end, replacement);
                var newEnd = DetermineNewEnd(start, end, replacement);

                var newChange = new TextChange()
                {
                    OldTextData = currentData,
                    NewTextData = newData,
                    Start = start,
                    OldEnd = end,
                    NewEnd = newEnd,
                    Replacement = replacement
                };

                if (this.lastChange == null)
                {
                    this.firstChange = this.lastChange = newChange;
                }
                else
                {
                    this.lastChange.NextChange = newChange;
                    this.lastChange = newChange;
                }

                if (this.undoUnit != null)
                {
                    this.undoUnit.AddChange(newChange);
                }

                return true;
            }

            public override void Dispose()
            {
                this.buffer.OnPencilDisposed(this, this.firstChange != null ? new TextDataChangedEventArgs(this.firstChange) : null);
            }
        }
    }
}
