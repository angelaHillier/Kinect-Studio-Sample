//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    public class TextEditor : Control, ITextUndoObserver
    {
        public static readonly DependencyProperty BufferProperty = DependencyProperty.Register(
            "Buffer", typeof(TextBuffer), typeof(TextEditor), new FrameworkPropertyMetadata(OnBufferChanged));

        public static readonly DependencyProperty CaretProperty = DependencyProperty.Register(
            "Caret", typeof(TextCoordinate), typeof(TextEditor), new FrameworkPropertyMetadata(OnCaretOrSelectionAnchorChanged));

        static readonly DependencyPropertyKey visibleLineCountPropertyKey = DependencyProperty.RegisterReadOnly(
            "VisibleLineCount", typeof(int), typeof(TextEditor), new FrameworkPropertyMetadata(0));
        public static readonly DependencyProperty VisibleLineCountProperty = visibleLineCountPropertyKey.DependencyProperty;

        public static readonly DependencyProperty TopVisibleLineProperty = DependencyProperty.Register(
            "TopVisibleLine", typeof(int), typeof(TextEditor), new FrameworkPropertyMetadata(OnTopVisibleLineChanged));

        static readonly DependencyPropertyKey verticalScrollRangePropertyKey = DependencyProperty.RegisterReadOnly(
            "VerticalScrollRange", typeof(int), typeof(TextEditor), new FrameworkPropertyMetadata(0));
        public static readonly DependencyProperty VerticalScrollRangeProperty = verticalScrollRangePropertyKey.DependencyProperty;

        static readonly DependencyPropertyKey verticalScrollPageSizePropertyKey = DependencyProperty.RegisterReadOnly(
            "VerticalScrollPageSize", typeof(int), typeof(TextEditor), new FrameworkPropertyMetadata(0));
        public static readonly DependencyProperty VerticalScrollPageSizeProperty = verticalScrollPageSizePropertyKey.DependencyProperty;

        static readonly DependencyPropertyKey horizontalScrollRangePropertyKey = DependencyProperty.RegisterReadOnly(
            "HorizontalScrollRange", typeof(double), typeof(TextEditor), new FrameworkPropertyMetadata(0d));
        public static readonly DependencyProperty HorizontalScrollRangeProperty = horizontalScrollRangePropertyKey.DependencyProperty;

        static readonly DependencyPropertyKey horizontalScrollPageSizePropertyKey = DependencyProperty.RegisterReadOnly(
            "HorizontalScrollPageSize", typeof(double), typeof(TextEditor), new FrameworkPropertyMetadata(0d));
        public static readonly DependencyProperty HorizontalScrollPageSizeProperty = horizontalScrollPageSizePropertyKey.DependencyProperty;

        public static readonly DependencyProperty HorizontalOffsetProperty = DependencyProperty.Register(
            "HorizontalOffset", typeof(double), typeof(TextEditor), new FrameworkPropertyMetadata(OnHorizontalOffsetChanged));

        public static readonly DependencyProperty TabSizeProperty = DependencyProperty.Register(
            "TabSize", typeof(int), typeof(TextEditor), new FrameworkPropertyMetadata(4, OnTabSizeChanged));

        public static readonly DependencyProperty UseTabsProperty = DependencyProperty.Register(
            "UseTabs", typeof(bool), typeof(TextEditor));

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
            "IsReadOnly", typeof(bool), typeof(TextEditor));

        static readonly DependencyPropertyKey selectionAnchorPropertyKey = DependencyProperty.RegisterReadOnly(
            "SelectionAnchor", typeof(TextCoordinate), typeof(TextEditor), new FrameworkPropertyMetadata(OnCaretOrSelectionAnchorChanged));
        public static readonly DependencyProperty SelectionAnchorProperty = selectionAnchorPropertyKey.DependencyProperty;

        Canvas canvas;
        Canvas highlightCanvas;
        CaretControl caret;
        SelectionVisual selection;
        Dictionary<int, TextLineVisual> displayedLines = new Dictionary<int, TextLineVisual>();
        double lineHeight;
        double charWidth;
        Typeface typeface;
        bool doubleClickSelect;
        bool inputBasedEdit;
        bool inUndoRedo;
        Binding horizontalOffsetBinding;
        double maxHorizontal;
        TranslateTransform highlightTransform;

        static TextEditor()
        {
            FontFamilyProperty.OverrideMetadata(typeof(TextEditor), new FrameworkPropertyMetadata(new FontFamily("Consolas"), OnFontMetricsChanged));
            FontSizeProperty.OverrideMetadata(typeof(TextEditor), new FrameworkPropertyMetadata(13d, OnFontMetricsChanged));
        }

        public TextEditor()
        {
            this.GetWordExtentFunc = this.DefaultGetWordExtent;

            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, OnCutExecuted, OnCutCanExecute));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, OnCopyExecuted, OnCopyCanExecute));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, OnPasteExecuted, OnPasteCanExecute));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, OnSelectAllExecuted, OnSelectAllCanExecute));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo, OnUndoExecuted, OnUndoCanExecute));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo, OnRedoExecuted, OnRedoCanExecute));

            this.Buffer = new TextBuffer();

            this.horizontalOffsetBinding = new Binding { Source = this, Path = new PropertyPath(HorizontalOffsetProperty), Converter = new FactorConverter { Factor = -1 } };

            this.highlightTransform = new TranslateTransform();
            BindingOperations.SetBinding(this.highlightTransform, TranslateTransform.XProperty, this.horizontalOffsetBinding);
            BindingOperations.SetBinding(this.highlightTransform, TranslateTransform.YProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(TopVisibleLineProperty),
                Converter = new TopLineToTransformYConverter(this)
            });

            CalculateFontSizes();
        }

        public TextBuffer Buffer
        {
            get { return (TextBuffer)GetValue(BufferProperty); }
            set { SetValue(BufferProperty, value); }
        }

        public TextCoordinate Caret
        {
            get { return (TextCoordinate)GetValue(CaretProperty); }
            set { SetValue(CaretProperty, value); }
        }

        public int VisibleLineCount
        {
            get { return (int)GetValue(VisibleLineCountProperty); }
            private set { SetValue(visibleLineCountPropertyKey, value); }
        }

        public int TopVisibleLine
        {
            get { return (int)GetValue(TopVisibleLineProperty); }
            set { SetValue(TopVisibleLineProperty, value); }
        }

        public int VerticalScrollRange
        {
            get { return (int)GetValue(VerticalScrollRangeProperty); }
            private set { SetValue(verticalScrollRangePropertyKey, value); }
        }

        public int VerticalScrollPageSize
        {
            get { return (int)GetValue(VerticalScrollPageSizeProperty); }
            private set { SetValue(verticalScrollPageSizePropertyKey, value); }
        }

        public double HorizontalScrollRange
        {
            get { return (double)GetValue(HorizontalScrollRangeProperty); }
            private set { SetValue(horizontalScrollRangePropertyKey, value); }
        }

        public double HorizontalScrollPageSize
        {
            get { return (double)GetValue(HorizontalScrollPageSizeProperty); }
            private set { SetValue(horizontalScrollPageSizePropertyKey, value); }
        }

        public double HorizontalOffset
        {
            get { return (double)GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }

        public int TabSize
        {
            get { return (int)GetValue(TabSizeProperty); }
            set { SetValue(TabSizeProperty, value); }
        }

        public bool UseTabs
        {
            get { return (bool)GetValue(UseTabsProperty); }
            set { SetValue(UseTabsProperty, value); }
        }

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public TextCoordinate SelectionAnchor
        {
            get { return (TextCoordinate)GetValue(SelectionAnchorProperty); }
            private set { SetValue(selectionAnchorPropertyKey, value); }
        }

        public bool HasSelection { get { return this.SelectionAnchor != this.Caret; } }

        public Func<int, int, TextRange> GetWordExtentFunc { get; private set; }

        public event EventHandler CaretMoved;
        public event EventHandler SelectionChanged;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.canvas = this.Template.FindName("PART_Canvas", this) as Canvas;
            this.highlightCanvas = this.Template.FindName("PART_HighlightCanvas", this) as Canvas;
            this.caret = this.Template.FindName("PART_Caret", this) as CaretControl;
            this.selection = this.Template.FindName("PART_Selection", this) as SelectionVisual;
            this.selection.Editor = this;
            CalculateFontSizes();
            RefreshHighlightVisuals(false);
            UpdateLineLayout(true);
            this.caret.Height = lineHeight;
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            InsertText(e.Text, true);
        }

        void InsertText(string text, bool moveCaretToEnd)
        {
            if (this.IsReadOnly)
            {
                return;
            }

            TextPencil pencil;

            if (!string.IsNullOrEmpty(text) && this.Buffer.TryGetPencil(out pencil))
            {
                var selection = GetSelection();
                var meta = new SelectionUndoMetadata { OldSelection = selection };

                if (selection.TopVirtualSpaces > 0)
                {
                    text = new string(' ', selection.TopVirtualSpaces) + text;
                }

                var data = TextData.FromString(text);

                this.inputBasedEdit = true;
                using (pencil)
                {
                    pencil.UndoUnit.Metadata = meta;
                    pencil.Write(selection.Range.Start, selection.Range.End, data);
                }
                this.inputBasedEdit = false;

                if (moveCaretToEnd)
                {
                    TextLocation caretLoc;

                    if (data.Lines.Count > 1)
                    {
                        caretLoc = new TextLocation(selection.Range.Start.Line + data.Lines.Count - 1, data.Lines[data.Lines.Count - 1].Length);
                    }
                    else
                    {
                        caretLoc = new TextLocation(selection.Range.Start.Line, selection.Range.Start.Index + data.Lines[0].Length);
                    }

                    var lineVisual = GetLineVisual(caretLoc.Line);
                    MoveCaret(lineVisual.MapToCoordinate(caretLoc), false);
                }

                meta.NewSelection = GetSelection();
            }
        }

        void MoveCaretHorizontally(int direction, bool extendSelection)
        {
            int virtualSpaces;
            var lineVisual = GetLineVisual(this.Caret.Line);
            var loc = lineVisual.MapToLocation(this.Caret, out virtualSpaces);

            if (virtualSpaces > 0)
            {
                virtualSpaces += direction;
            }
            else if (direction > 0 && loc.Index == this.Buffer.TextData.Lines[loc.Line].Length)
            {
                virtualSpaces = 1;
            }
            else
            {
                loc.Index = Math.Max(0, loc.Index + direction);
            }

            var coord = lineVisual.MapToCoordinate(loc);
            coord.Index += virtualSpaces;
            MoveCaret(coord, extendSelection);
        }

        void MoveWordLeft(bool extendSelection)
        {
            var lineVisual = GetLineVisual(this.Caret.Line);
            int virtualSpaces;
            var loc = lineVisual.MapToLocation(this.Caret.Index, out virtualSpaces);
            TextLocation newLocation;

            if (virtualSpaces > 0)
            {
                newLocation = new TextLocation(this.Caret.Line, lineVisual.Line.Length);
            }
            else if (loc.Index > 0)
            {
                var range = GetWordExtent(loc.Line, loc.Index - 1);
                if (char.IsWhiteSpace(lineVisual.Line.Text[range.Start.Index]))
                {
                    if (range.Start.Index > 0)
                    {
                        range = GetWordExtent(range.Start.Line, range.Start.Index - 1);
                    }
                    else
                    {
                        range.Start = new TextLocation(range.Start.Line - 1, this.Buffer.TextData.Lines[range.Start.Line - 1].Length);
                        lineVisual = GetLineVisual(range.Start.Line);
                    }
                }
                newLocation = range.Start;
            }
            else if (loc.Line > 0)
            {
                newLocation = new TextLocation(loc.Line - 1, this.Buffer.TextData.Lines[loc.Line - 1].Length);
                lineVisual = GetLineVisual(newLocation.Line);
            }
            else
            {
                return;
            }

            MoveCaret(lineVisual.MapToCoordinate(newLocation), extendSelection);
        }

        void MoveWordRight(bool extendSelection)
        {
            var lineVisual = GetLineVisual(this.Caret.Line);
            int virtualSpaces;
            var loc = lineVisual.MapToLocation(this.Caret.Index, out virtualSpaces);
            TextLocation newLocation;

            if (loc.Index == lineVisual.Line.Length)
            {
                if (loc.Line < this.Buffer.TextData.Lines.Count - 1)
                {
                    newLocation = new TextLocation(loc.Line + 1, 0);
                    lineVisual = GetLineVisual(newLocation.Line);
                }
                else
                {
                    return;
                }
            }
            else
            {
                var range = GetWordExtent(loc.Line, loc.Index);
                if (range.End.Index < lineVisual.Line.Length && char.IsWhiteSpace(lineVisual.Line.Text[range.End.Index]))
                {
                    range = GetWordExtent(loc.Line, range.End.Index);
                }

                newLocation = range.End;
            }

            MoveCaret(lineVisual.MapToCoordinate(newLocation), extendSelection);
        }

        public void MoveCaret(TextCoordinate newCoordinate, bool extendSelection)
        {
            bool wasSelection = this.HasSelection;

            this.Caret = newCoordinate;

            if (!extendSelection)
            {
                this.SelectionAnchor = newCoordinate;
            }

            EnsureMaxHorizontal((this.Caret.Index + 1) * this.charWidth);
            EnsureCaretVisible();

            var handler = this.CaretMoved;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }

            if (extendSelection || wasSelection)
            {
                handler = this.SelectionChanged;

                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }

        public void Select(TextCoordinate anchor, TextCoordinate end)
        {
            this.SelectionAnchor = anchor;
            this.MoveCaret(end, true);
        }

        void EnsureMaxHorizontal(double horizontal)
        {
            if (horizontal > this.maxHorizontal)
            {
                this.maxHorizontal = horizontal;

                if (this.canvas != null)
                {
                    this.HorizontalScrollRange = Math.Max(0, this.maxHorizontal - this.canvas.ActualWidth);
                }
            }
        }

        void EnsureCaretVisible()
        {
            if (this.Caret.Line < this.TopVisibleLine)
            {
                this.TopVisibleLine = this.Caret.Line;
            }
            else if (this.Caret.Line >= this.TopVisibleLine + this.VisibleLineCount - 1)
            {
                if (this.VisibleLineCount == 1)
                {
                    this.TopVisibleLine = this.Caret.Line;
                }
                else
                {
                    this.TopVisibleLine = (this.Caret.Line - this.VisibleLineCount) + 2;
                }
            }

            if (this.canvas != null)
            {
                double caretX = this.Caret.Index * this.charWidth;

                if (caretX < this.HorizontalOffset)
                {
                    this.HorizontalOffset = caretX;
                }
                else if (caretX > this.HorizontalOffset + this.canvas.ActualWidth - this.charWidth)
                {
                    this.HorizontalOffset = caretX - this.canvas.ActualWidth + this.charWidth;
                }
            }
        }

        void ReplaceText(TextLocation start, TextLocation end, TextData replacement, bool moveCaretToEnd)
        {
            TextPencil pencil;

            if (!this.IsReadOnly && this.Buffer.TryGetPencil(out pencil))
            {
                var meta = new SelectionUndoMetadata { OldSelection = GetSelection() };

                this.inputBasedEdit = true;
                using (pencil)
                {
                    pencil.UndoUnit.Metadata = meta;
                    pencil.Write(start, end, replacement);
                }
                this.inputBasedEdit = false;

                if (moveCaretToEnd)
                {
                    TextLocation caretLoc;

                    if (replacement.Lines.Count > 1)
                    {
                        caretLoc = new TextLocation(start.Line + replacement.Lines.Count - 1, replacement.Lines[replacement.Lines.Count - 1].Length);
                    }
                    else
                    {
                        caretLoc = new TextLocation(start.Line, start.Index + replacement.Lines[0].Length);
                    }

                    var lineVisual = GetLineVisual(caretLoc.Line);
                    MoveCaret(lineVisual.MapToCoordinate(caretLoc), false);
                }

                meta.NewSelection = GetSelection();
            }
        }

        string BuildTabifiedWhitepace(int size)
        {
            if (this.UseTabs)
            {
                int spaces = size % this.TabSize;
                int tabs = size / this.TabSize;

                if (tabs > 0 && spaces > 0)
                {
                    return new string('\t', tabs) + new string(' ', spaces);
                }
                else if (tabs > 0)
                {
                    return new string('\t', tabs);
                }
            }

            return new string(' ', size);
        }

        void Backspace()
        {
            if (this.IsReadOnly)
            {
                return;
            }

            var selection = GetSelection();

            if (!selection.IsEmpty)
            {
                DeleteSelection(selection);
            }
            else
            {
                if (selection.Range.Start.Index > 0)
                {
                    if (selection.TopVirtualSpaces == 0)
                    {
                        // Not in virtual space, so must delete text.
                        ReplaceText(new TextLocation(selection.Range.Start.Line, selection.Range.Start.Index - 1), selection.Range.Start, TextData.FromString(""), true);
                    }
                    else
                    {
                        // Virtual space -- just a caret movement.
                        MoveCaret(new TextCoordinate(selection.Span.Start.Line, selection.Span.Start.Index - 1), false);
                    }
                }
                else
                {
                    if (selection.Range.Start.Line > 0)
                    {
                        // Backspace at beginning of line -- remove end marker and join w/ previous line
                        var textLine = this.Buffer.TextData.Lines[selection.Range.Start.Line - 1];
                        var loc = new TextLocation(selection.Range.Start.Line - 1, textLine.Length);

                        ReplaceText(loc, selection.Range.Start, TextData.FromString(""), true);
                    }
                }
            }
        }

        void DeleteSelection(Selection selection)
        {
            Debug.Assert(!selection.IsEmpty, "Can't delete an empty selection");

            if ((selection.TopVirtualSpaces > 0) && (selection.BottomVirtualSpaces > 0) && (selection.Span.Start.Line == selection.Span.End.Line))
            {
                // No change to document, only deleting virtual space.
                MoveCaret(selection.Span.Start, false);
                return;
            }

            ReplaceText(selection.Range.Start, selection.Range.End, TextData.FromString(new string(' ', selection.TopVirtualSpaces)), true);
        }

        void Delete(bool copyToClipboard)
        {
            if (this.IsReadOnly)
            {
                return;
            }

            var selection = GetSelection();

            if (copyToClipboard)
            {
                CopySelectionToClipboard(selection);
            }

            if (selection.IsEmpty)
            {
                if (selection.Range.Start.Index == this.Buffer.TextData.Lines[selection.Range.Start.Line].Length)
                {
                    // Odd, but this means deletion might actually be an insertion of spaces.
                    ReplaceText(selection.Range.Start, new TextLocation(selection.Range.Start.Line + 1, 0), TextData.FromString(new string(' ', selection.TopVirtualSpaces)), false);
                }
                else
                {
                    ReplaceText(selection.Range.Start, new TextLocation(selection.Range.Start.Line, selection.Range.Start.Index + 1), TextData.Empty, false);
                }
            }
            else
            {
                DeleteSelection(selection);
            }
        }

        void Tab(bool shiftDown)
        {
            if (this.IsReadOnly)
            {
                return;
            }

            var selection = GetSelection();

            if (!selection.IsEmpty && selection.Span.Start.Line != selection.Span.End.Line)
            {
                return;
            }

            int virtualSpaces;
            var lineVisual = GetLineVisual(this.Caret.Line);
            var loc = lineVisual.MapToLocation(this.Caret, out virtualSpaces);

            if (shiftDown)
            {
                if (selection.IsEmpty)
                {
                    var home = lineVisual.GetHomeCoordinate();

                    if (home < this.Caret)
                    {
                        // Shift-tab doesn't work past the home coordinate
                        return;
                    }

                    var homeLoc = lineVisual.MapToLocation(home, out virtualSpaces);
                    Debug.Assert(virtualSpaces == 0, "Can't be in virtual space, already checked...");

                    if (homeLoc.Index > 0)
                    {
                        ReplaceText(new TextLocation(homeLoc.Line, 0), homeLoc, TextData.FromString(BuildTabifiedWhitepace(Math.Max(0, home.Index - this.TabSize))), true);
                    }
                }
            }
            else
            {
                if (this.UseTabs)
                {
                    InsertText("\t", true);
                }
                else
                {
                    int index = this.Caret.Index;
                    int spacesToInsert = this.TabSize - (index % this.TabSize);

                    InsertText(new string(' ', spacesToInsert), true);
                }
            }
        }

        void NewLine()
        {
            if (this.IsReadOnly)
            {
                return;
            }

            if (this.SelectionAnchor != this.Caret)
            {
                // No auto-indent in this case.
                InsertText("\r\n", true);
                return;
            }

            int virtualSpaces;
            var lineVisual = GetLineVisual(this.Caret.Line);
            var loc = lineVisual.MapToLocation(this.Caret.Index, out virtualSpaces);
            var home = lineVisual.GetHomeCoordinate();

            if (loc.Index == lineVisual.Line.Length)
            {
                // There will be nothing after the caret on the new line, so do the auto-indent via virtual space.
                // Note that the caret will have moved to the next line, so we can't use home.Line...
                InsertText("\r\n", true);
                MoveCaret(new TextCoordinate(this.Caret.Line, home.Index), false);
            }
            else
            {
                // There's text after the caret on the new line.  To auto-indent, we must actually insert
                // whitespace. Match the whitespace on the line above.
                var homeLoc = lineVisual.MapToLocation(home, out virtualSpaces);
                Debug.Assert(virtualSpaces == 0, "Can't be in virtual space, we already checked for that!");
                InsertText("\r\n" + lineVisual.Line.Text.Substring(0, homeLoc.Index), true);
            }
        }

        public Selection GetSelection()
        {
            int caretVirtualSpaces;
            var caretLineVisual = GetLineVisual(this.Caret.Line);
            var caretLocation = caretLineVisual.MapToLocation(this.Caret, out caretVirtualSpaces);

            if (this.Caret == this.SelectionAnchor)
            {
                return new Selection
                {
                    Range = new TextRange(caretLocation, caretLocation),
                    Span = new TextCoordinateSpan(this.Caret, this.Caret),
                    TopVirtualSpaces = caretVirtualSpaces,
                    BottomVirtualSpaces = caretVirtualSpaces
                };
            }

            int anchorVirtualSpaces;
            var anchorLineVisual = (this.Caret.Line == this.SelectionAnchor.Line) ? caretLineVisual : GetLineVisual(this.SelectionAnchor.Line);
            var anchorLocation = anchorLineVisual.MapToLocation(this.SelectionAnchor, out anchorVirtualSpaces);

            if (this.Caret < this.SelectionAnchor)
            {
                return new Selection
                {
                    Range = new TextRange(caretLocation, anchorLocation),
                    Span = new TextCoordinateSpan(this.Caret, this.SelectionAnchor),
                    TopVirtualSpaces = caretVirtualSpaces,
                    BottomVirtualSpaces = anchorVirtualSpaces,
                    AnchoredAtTop = false
                };
            }
            else
            {
                return new Selection
                {
                    Range = new TextRange(anchorLocation, caretLocation),
                    Span = new TextCoordinateSpan(this.SelectionAnchor, this.Caret),
                    TopVirtualSpaces = anchorVirtualSpaces,
                    BottomVirtualSpaces = caretVirtualSpaces,
                    AnchoredAtTop = true
                };
            }
        }

        public void CopySelectionToClipboard()
        {
            CopySelectionToClipboard(GetSelection());
        }

        void CopySelectionToClipboard(Selection selection)
        {
            if (selection.IsEmpty)
            {
                return;
            }

            WpfUtilities.SetClipboardText(this.Buffer.TextData.ToString(selection.Range.Start, selection.Range.End));
        }

        void PasteFromClipboard()
        {
            if (this.IsReadOnly)
            {
                return;
            }

            try
            {
                if (Clipboard.ContainsText())
                {
                    this.InsertText(Clipboard.GetText(), true);
                }
            }
            catch (Exception)
            {
            }
        }

        void OnCutExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.IsReadOnly)
            {
                return;
            }

            if (this.HasSelection)
            {
                Delete(true);
            }
        }

        void OnCutCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.HasSelection && !this.IsReadOnly;
            e.Handled = true;
        }

        void OnCopyExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            CopySelectionToClipboard();
        }

        void OnCopyCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.HasSelection;
            e.Handled = true;
        }

        void OnPasteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            PasteFromClipboard();
        }

        void OnPasteCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !this.IsReadOnly;
            e.Handled = true;
        }

        void OnSelectAllExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Select(new TextCoordinate(), MapToCoordinate(this.Buffer.TextData.End));
            e.Handled = true;
        }

        void OnSelectAllCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.Buffer.TextData.End != new TextLocation();
            e.Handled = true;
        }

        void OnUndoExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.Buffer.CanUndo && !this.IsReadOnly)
            {
                this.Buffer.Undo(this);
            }
        }

        void OnUndoCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.Buffer.CanUndo && !this.IsReadOnly;
            e.Handled = true;
        }

        void OnRedoExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.Buffer.CanRedo && !this.IsReadOnly)
            {
                this.Buffer.Redo(this);
            }
        }

        void OnRedoCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.Buffer.CanRedo && !this.IsReadOnly;
            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            bool shiftDown = (e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            bool ctrlDown = (e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            switch (e.Key)
            {
                case Key.Escape:
                    MoveCaret(this.Caret, false);
                    break;

                case Key.Tab:
                    Tab(shiftDown);
                    break;

                case Key.Return:
                    NewLine();
                    break;

                case Key.Back:
                    Backspace();
                    break;

                case Key.Delete:
                    Delete(shiftDown);
                    break;

                case Key.Left:
                    if (ctrlDown)
                    {
                        MoveWordLeft(shiftDown);
                    }
                    else
                    {
                        MoveCaretHorizontally(-1, shiftDown);
                    }
                    break;

                case Key.Right:
                    if (ctrlDown)
                    {
                        MoveWordRight(shiftDown);
                    }
                    else
                    {
                        MoveCaretHorizontally(1, shiftDown);
                    }
                    break;

                case Key.Home:
                    if (ctrlDown)
                    {
                        MoveCaret(new TextCoordinate(0, 0), shiftDown);
                    }
                    else
                    {
                        var lineVisual = GetLineVisual(this.Caret.Line);
                        var coord = lineVisual.GetHomeCoordinate();

                        if (coord == this.Caret && coord.Index > 0)
                        {
                            coord.Index = 0;
                        }

                        MoveCaret(coord, shiftDown);
                    }
                    break;

                case Key.End:
                    if (ctrlDown)
                    {
                        int endLineIndex = this.Buffer.TextData.Lines.Count - 1;
                        var endLine = GetLineVisual(endLineIndex);
                        var coord = endLine.MapToCoordinate(this.Buffer.TextData.Lines[endLineIndex].Length);

                        MoveCaret(coord, shiftDown);
                    }
                    else
                    {
                        var lineVisual = GetLineVisual(this.Caret.Line);
                        var coord = lineVisual.GetEndCoordinate();

                        MoveCaret(coord, shiftDown);
                    }
                    break;

                case Key.Up:
                    if (ctrlDown)
                    {
                        this.TopVisibleLine = Math.Max(0, this.TopVisibleLine - 1);
                        if (this.Caret.Line >= this.TopVisibleLine + this.VerticalScrollPageSize)
                        {
                            MoveCaret(new TextCoordinate(this.Caret.Line - 1, this.Caret.Index), false);
                        }
                    }
                    else if (this.Caret.Line > 0)
                    {
                        MoveCaret(new TextCoordinate(this.Caret.Line - 1, this.Caret.Index), shiftDown);
                    }
                    break;

                case Key.Down:
                    if (ctrlDown)
                    {
                        this.TopVisibleLine = Math.Min(this.Buffer.TextData.Lines.Count - 1, this.TopVisibleLine + 1);
                        if (this.Caret.Line < this.TopVisibleLine)
                        {
                            MoveCaret(new TextCoordinate(this.TopVisibleLine, this.Caret.Index), false);
                        }
                    }
                    else if (this.Caret.Line < this.Buffer.TextData.Lines.Count - 1)
                    {
                        MoveCaret(new TextCoordinate(this.Caret.Line + 1, this.Caret.Index), shiftDown);
                    }
                    break;

                case Key.PageUp:
                    {
                        EnsureCaretVisible();

                        int linesMoved = Math.Min(this.VerticalScrollPageSize, this.Caret.Line);
                        int linesScrolled = Math.Min(linesMoved, this.TopVisibleLine);

                        if (linesScrolled > 0)
                        {
                            this.TopVisibleLine -= linesScrolled;
                        }
                        if (linesMoved > 0)
                        {
                            MoveCaret(new TextCoordinate(this.Caret.Line - linesMoved, this.Caret.Index), shiftDown);
                        }

                        break;
                    }

                case Key.PageDown:
                    {
                        EnsureCaretVisible();

                        int linesMoved = Math.Min(this.VerticalScrollPageSize, this.Buffer.TextData.Lines.Count - this.Caret.Line - 1);
                        int linesScrolled = Math.Min(linesMoved, this.Buffer.TextData.Lines.Count - this.TopVisibleLine - 1);

                        if (linesScrolled > 0)
                        {
                            this.TopVisibleLine += linesScrolled;
                        }
                        if (linesMoved > 0)
                        {
                            MoveCaret(new TextCoordinate(this.Caret.Line + linesMoved, this.Caret.Index), shiftDown);
                        }

                        break;
                    }

                default:
                    base.OnKeyDown(e);
                    return;
            }

            e.Handled = true;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            this.Focus();
            this.doubleClickSelect = (e.ClickCount == 2);
            MoveCaretToMouse(e, (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift);
            e.MouseDevice.Capture(this.canvas);
            this.canvas.MouseMove += OnSelectingMouseMove;
            this.canvas.MouseLeftButtonUp += OnSelectingMouseLeftButtonUp;
            this.canvas.LostMouseCapture += OnSelectingLostMouseCapture;
            base.OnMouseLeftButtonDown(e);
        }

        void OnSelectingMouseMove(object sender, MouseEventArgs e)
        {
            MoveCaretToMouse(e, true);
        }

        void MoveCaretToMouse(MouseEventArgs e, bool extendSelection)
        {
            var pos = e.GetPosition(this.canvas);

            var index = (int)(Math.Max(0, pos.X + this.HorizontalOffset + (this.charWidth / 3)) / this.charWidth);
            var line = Math.Max(0, Math.Min(this.Buffer.TextData.Lines.Count - 1, this.TopVisibleLine + (int)(pos.Y / this.lineHeight)));

            var coord = new TextCoordinate { Line = line, Index = index };
            TextLineVisual lineVisual = GetLineVisual(coord.Line);
            int virtualSpaces;

            // Must map to location and back to coordinate to "land" on tabs correctly.
            var loc = lineVisual.MapToLocation(coord, out virtualSpaces);

            if (this.doubleClickSelect)
            {
                if (loc.Index == lineVisual.Line.Length)
                {
                    if (this.SelectionAnchor > coord)
                    {
                        virtualSpaces = 0;
                    }
                }
                else
                {
                    var wordRange = GetWordExtent(loc.Line, loc.Index);
                    var startCoord = lineVisual.MapToCoordinate(wordRange.Start);
                    var endCoord = lineVisual.MapToCoordinate(wordRange.End);

                    if (!extendSelection)
                    {
                        // This is the initial double-click. Select the word.
                        this.SelectionAnchor = startCoord;
                        MoveCaret(endCoord, true);
                    }
                    else if (this.SelectionAnchor > endCoord)
                    {
                        MoveCaret(startCoord, true);
                    }
                    else
                    {
                        MoveCaret(endCoord, true);
                    }
                    return;
                }
            }

            coord = lineVisual.MapToCoordinate(loc);
            coord.Index += virtualSpaces;

            MoveCaret(coord, extendSelection);
        }

        void OnSelectingMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.MouseDevice.Capture(null);
        }

        void OnSelectingLostMouseCapture(object sender, MouseEventArgs e)
        {
            this.canvas.MouseMove -= OnSelectingMouseMove;
            this.canvas.MouseLeftButtonUp -= OnSelectingMouseLeftButtonUp;
            this.canvas.LostMouseCapture -= OnSelectingLostMouseCapture;
        }

        protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs e)
        {
            var delta = Math.Sign(e.Delta);

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                this.FontSize = Math.Min(56, Math.Max(10, this.FontSize + delta));
            }
            else
            {
                this.TopVisibleLine -= delta;
            }

            base.OnMouseWheel(e);
        }

        public Geometry BuildSelectionGeometry()
        {
            return BuildGeometryForRange(this.Caret, this.SelectionAnchor);
        }

        public Geometry BuildGeometryForRange(TextCoordinate end1, TextCoordinate end2)
        {
            if (end1 == end2)
            {
                // No selection. Empty geometry.
                return Geometry.Empty;
            }

            Geometry resultGeometry;

            if (end1.Line == end2.Line)
            {
                int left = Math.Min(end1.Index, end2.Index);
                int right = Math.Max(end1.Index, end2.Index);

                var rect = new Rect(left * this.charWidth, end1.Line * this.lineHeight, (right - left) * this.charWidth, this.lineHeight);

                // Must inflate the rect just a bit -- otherwise, rounding issues cause incomplete union combinations (below -- even though this
                // particular rect won't ever be combined, we want it to have the same size when single-line).
                rect.Inflate(.5, .5);
                resultGeometry = new RectangleGeometry(rect);
            }
            else
            {
                var top = (end1.Line < end2.Line) ? end1 : end2;
                var bottom = (end1.Line < end2.Line) ? end2 : end1;

                var topRect = new Rect(top.Index * charWidth, top.Line * this.lineHeight, this.maxHorizontal + this.canvas.ActualWidth + 2, this.lineHeight);
                var midRect = new Rect(0, (top.Line + 1) * this.lineHeight, this.maxHorizontal + this.canvas.ActualWidth + 2, (bottom.Line - top.Line - 1) * this.lineHeight);
                var bottomRect = new Rect(0, bottom.Line * this.lineHeight, bottom.Index * this.charWidth, this.lineHeight);

                // Inflate to ensure overlap for correct union combination
                topRect.Inflate(.5, .5);
                midRect.Inflate(.5, .5);
                bottomRect.Inflate(.5, .5);
                var topGeometry = new RectangleGeometry(topRect);
                var bottomGeometry = new RectangleGeometry(bottomRect);

                if (top.Line == bottom.Line - 1)
                {
                    // Just top and bottom -- no lines between
                    resultGeometry = new CombinedGeometry(GeometryCombineMode.Union, topGeometry, bottomGeometry);
                }
                else
                {
                    // Two combinations for 3 geometries
                    var midGeometry = new RectangleGeometry(midRect);
                    var firstCombinedGeometry = new CombinedGeometry(GeometryCombineMode.Union, topGeometry, midGeometry);

                    resultGeometry = new CombinedGeometry(GeometryCombineMode.Union, firstCombinedGeometry, bottomGeometry);
                }
            }

            resultGeometry.Transform = this.highlightTransform;
            return resultGeometry;
        }

        void CalculateFontSizes()
        {
            this.typeface = new Typeface(this.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var formattedText = new FormattedText("X", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, this.FontSize, Brushes.Black);
            this.lineHeight = formattedText.Height;
            this.charWidth = formattedText.Width;
        }

        TextRange DefaultGetWordExtent(int line, int index)
        {
            if (line < 0 || line >= this.Buffer.TextData.Lines.Count)
            {
                throw new ArgumentException();
            }

            var textLine = this.Buffer.TextData.Lines[line];

            if (index < 0 || index > textLine.Length)
            {
                throw new ArgumentException();
            }

            char originalChar = textLine.Text[index];

            // This is pretty simplistic; we can probably do better...
            Func<char, char, bool> classifier = (o, c) =>
            {
                if (char.IsWhiteSpace(o))
                {
                    return char.IsWhiteSpace(c);
                }

                if (char.IsLetterOrDigit(o))
                {
                    return char.IsLetterOrDigit(c);
                }

                return false;
            };

            int start = index;
            int end = index + 1;

            while (start > 0 && classifier(originalChar, textLine.Text[start - 1]))
            {
                start -= 1;
            }

            while (end < textLine.Length && classifier(originalChar, textLine.Text[end]))
            {
                end += 1;
            }

            return new TextRange(new TextLocation(line, start), new TextLocation(line, end));
        }

        public TextRange GetWordExtent(int line, int index)
        {
            return GetWordExtentFunc(line, index);
        }

        TextLineVisual GetLineVisual(int line)
        {
            TextLineVisual visual;

            if (!this.displayedLines.TryGetValue(line, out visual))
            {
                // Note: This will not be displayed and will just GC away when
                // the caller is done with it.  Still useful for coord/loc mapping.
                visual = new TextLineVisual(this, line);
            }

            return visual;
        }

        void UpdateLineLayout(bool invalidateAll)
        {
            if (this.canvas == null)
            {
                // Not yet... no template
                return;
            }

            // Determine number of visible lines
            this.VisibleLineCount = (int)Math.Ceiling(this.canvas.ActualHeight / this.lineHeight);
            this.VerticalScrollPageSize = this.VisibleLineCount - 1;
            this.VerticalScrollRange = this.Buffer.TextData.Lines.Count;

            var lineNumbers = this.displayedLines.Keys.ToArray();
            List<TextLineVisual> recycledLines = null;

            // Remove any line not in visible range
            if (invalidateAll)
            {
                // Put all currently displayed lines in the recycle bin
                recycledLines = this.displayedLines.Values.ToList();
                this.displayedLines.Clear();
            }
            else
            {
                foreach (var line in lineNumbers)
                {
                    if (line < this.TopVisibleLine || line >= this.TopVisibleLine + this.VisibleLineCount)
                    {
                        // Put the line in the recycle bin, leaving it parented by the canvas.  This
                        // greatly reduces visual tree noise.
                        if (recycledLines == null)
                        {
                            recycledLines = new List<TextLineVisual>();
                        }

                        recycledLines.Add(this.displayedLines[line]);
                        this.displayedLines.Remove(line);
                    }
                }
            }

            // Now update and position each line.
            for (int i = 0; i < this.VisibleLineCount && i + this.TopVisibleLine < this.Buffer.TextData.Lines.Count; i++)
            {
                int lineIndex = i + this.TopVisibleLine;
                TextLineVisual visual;

                if (!this.displayedLines.TryGetValue(lineIndex, out visual))
                {
                    if (recycledLines != null && recycledLines.Count > 0)
                    {
                        visual = recycledLines[recycledLines.Count - 1];
                        recycledLines.RemoveAt(recycledLines.Count - 1);
                        visual.SetLineIndex(lineIndex);
                    }
                    else
                    {
                        visual = new TextLineVisual(this, lineIndex);
                        visual.SetBinding(Canvas.LeftProperty, this.horizontalOffsetBinding);
                        this.canvas.Children.Add(visual);
                    }

                    this.displayedLines[lineIndex] = visual;
                }

                Canvas.SetTop(visual, i * this.lineHeight);
            }

            // Remove any remaining lines in the recycle bin
            if (recycledLines != null)
            {
                foreach (var visual in recycledLines)
                {
                    this.canvas.Children.Remove(visual);
                }
            }

            PlaceCaretAndSelection();
        }

        void PlaceCaretAndSelection()
        {
            // Turn this off unconditionally when this is called.  Turning it back on forces the caret to
            // be immediately visible, which should happen while you are typing or moving/scrolling, etc.
            this.caret.IsInView = false;

            if (this.Caret.Line >= this.TopVisibleLine && this.Caret.Line < this.TopVisibleLine + this.VisibleLineCount)
            {
                // Need to compute based on the line's text (could be tabs; also required if we support variable pitch fonts)
                Canvas.SetLeft(this.caret, (this.Caret.Index * charWidth) - this.HorizontalOffset);
                Canvas.SetTop(this.caret, (this.Caret.Line - this.TopVisibleLine) * lineHeight);
                this.caret.IsInView = true;
            }

            this.selection.Invalidate();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateLineLayout(false);
            this.HorizontalScrollPageSize = this.canvas.ActualWidth;
            this.HorizontalScrollRange = Math.Max(0, this.maxHorizontal - this.canvas.ActualWidth);
            this.HorizontalOffset = Math.Min(this.HorizontalOffset, this.HorizontalScrollRange);
        }

        void OnBufferTextDataChanged(object sender, TextDataChangedEventArgs e)
        {
            if (!this.inputBasedEdit && !this.inUndoRedo)
            {
                // This is an edit that we didn't make.  Need to update the caret
                // as appropriate.
                var lineVisual = GetLineVisual(this.Caret.Line);
                int spaces;
                var location = lineVisual.MapToLocation(this.Caret, out spaces);

                for (var change = e.Change; change != null; change = change.NextChange)
                {
                    location = change.ApplyTo(location, false);
                }

                lineVisual = GetLineVisual(location.Line);
                this.MoveCaret(lineVisual.MapToCoordinate(location.Index + spaces), false);
            }

            UpdateLineLayout(true);
        }

        void OnFormatDataChanged(object sender, EventArgs e)
        {
            UpdateLineLayout(true);
        }

        void OnHighlightRangesChanged(object sender, EventArgs e)
        {
            RefreshHighlightVisuals(false);
        }

        void RefreshHighlightVisuals(bool invalidateAll)
        {
            if (this.highlightCanvas == null)
            {
                return;
            }

            var existing = this.highlightCanvas.Children.OfType<HighlightVisual>().ToDictionary(v => v.Range);

            foreach (var range in this.Buffer.Formatter.HighlightRanges)
            {
                HighlightVisual visual;

                if (!existing.TryGetValue(range, out visual))
                {
                    visual = new HighlightVisual(this, range);
                    this.highlightCanvas.Children.Add(visual);
                }
                else
                {
                    existing.Remove(range);
                    if (invalidateAll)
                    {
                        visual.Invalidate();
                    }
                }
            }

            foreach (var visual in existing.Values)
            {
                this.highlightCanvas.Children.Remove(visual);
            }
        }

        void ITextUndoObserver.OnBeforeUndoRedoUnit(TextUndoUnit unit, object instigator, bool isUndo)
        {
            if (object.ReferenceEquals(instigator, this))
            {
                this.inUndoRedo = true;
            }
        }

        void ITextUndoObserver.OnAfterUndoRedoUnit(TextUndoUnit unit, object instigator, bool isUndo)
        {
            if (object.ReferenceEquals(instigator, this))
            {
                var meta = unit.Metadata as SelectionUndoMetadata;

                if (meta != null)
                {
                    var selection = isUndo ? meta.OldSelection : meta.NewSelection;

                    if (selection.IsEmpty)
                    {
                        MoveCaret(selection.Span.Start, false);
                    }
                    else if (selection.AnchoredAtTop)
                    {
                        this.SelectionAnchor = selection.Span.Start;
                        MoveCaret(selection.Span.End, true);
                    }
                    else
                    {
                        this.SelectionAnchor = selection.Span.End;
                        MoveCaret(selection.Span.Start, true);
                    }
                }

                this.inUndoRedo = false;
            }
        }

        void OnBufferChanged(TextBuffer oldBuffer, TextBuffer newBuffer)
        {
            if (oldBuffer != null)
            {
                oldBuffer.TextDataChanged -= OnBufferTextDataChanged;
                oldBuffer.Formatter.FormatDataChanged -= OnFormatDataChanged;
                oldBuffer.Formatter.HighlightRangesChanged -= OnHighlightRangesChanged;
                oldBuffer.StopObservingUndoRedo(this);
            }

            if (newBuffer != null)
            {
                newBuffer.TextDataChanged += OnBufferTextDataChanged;
                newBuffer.Formatter.FormatDataChanged += OnFormatDataChanged;
                newBuffer.Formatter.HighlightRangesChanged += OnHighlightRangesChanged;
                newBuffer.ObserveUndoRedo(this);
            }

            this.TopVisibleLine = 0;
            MoveCaret(new TextCoordinate(), false);
            UpdateLineLayout(true);
            RefreshHighlightVisuals(true);
        }

        public TextCoordinate MapToCoordinate(TextLocation loc)
        {
            return MapToCoordinate(loc.Line, loc.Index);
        }

        public TextCoordinate MapToCoordinate(int line, int index)
        {
            var visual = this.GetLineVisual(line);
            return visual.MapToCoordinate(index);
        }

        public TextLocation MapToLocation(TextCoordinate coord, out int virtualSpaces)
        {
            return MapToLocation(coord.Line, coord.Index, out virtualSpaces);
        }

        public TextLocation MapToLocation(int line, int index, out int virtualSpaces)
        {
            var visual = this.GetLineVisual(line);
            return visual.MapToLocation(index, out virtualSpaces);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TextEditorAutomationPeer(this);
        }

        static void OnBufferChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = obj as TextEditor;

            if (editor != null)
            {
                editor.OnBufferChanged(e.OldValue as TextBuffer, e.NewValue as TextBuffer);
            }
        }

        static void OnFontMetricsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = obj as TextEditor;

            if (editor != null)
            {
                editor.CalculateFontSizes();
                if (editor.caret != null)
                {
                    editor.caret.Height = editor.lineHeight;
                }

                // All lines must be invalidated...
                editor.UpdateLineLayout(true);
                editor.RefreshHighlightVisuals(true);

                // Because the binding for the highlight transform Y uses, but is not notified of changes to, the lineHeight
                // property, we must refresh the target(s) manually.
                var expr = BindingOperations.GetBindingExpressionBase(editor.highlightTransform, TranslateTransform.YProperty);

                if (expr != null)
                {
                    expr.UpdateTarget();
                }
            }
        }

        static void OnCaretOrSelectionAnchorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = obj as TextEditor;

            if (editor != null)
            {
                editor.PlaceCaretAndSelection();
            }
        }

        static void OnTopVisibleLineChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = obj as TextEditor;

            if (editor != null)
            {
                if (editor.TopVisibleLine >= editor.Buffer.TextData.Lines.Count)
                {
                    editor.TopVisibleLine = editor.Buffer.TextData.Lines.Count - 1;
                }
                else if (editor.TopVisibleLine < 0)
                {
                    editor.TopVisibleLine = 0;
                }

                editor.UpdateLineLayout(false);
            }
        }

        static void OnTabSizeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = obj as TextEditor;

            if (editor != null)
            {
                editor.UpdateLineLayout(true);
            }
        }

        static void OnHorizontalOffsetChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = obj as TextEditor;

            if (editor != null)
            {
                if (editor.HorizontalOffset >= editor.HorizontalScrollRange)
                {
                    editor.HorizontalOffset = editor.HorizontalScrollRange;
                }
                else if (editor.HorizontalOffset < 0)
                {
                    editor.HorizontalOffset = 0;
                }
            }

            editor.PlaceCaretAndSelection();
        }

        class TopLineToTransformYConverter : IValueConverter
        {
            TextEditor editor;

            public TopLineToTransformYConverter(TextEditor editor)
            {
                this.editor = editor;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return -((int)value * this.editor.lineHeight);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        class SelectionUndoMetadata : TextUndoMetadata
        {
            public Selection OldSelection { get; set; }
            public Selection NewSelection { get; set; }

            public override bool TryMerge(TextUndoMetadata previousMetadata)
            {
                var previousSelection = previousMetadata as SelectionUndoMetadata;

                if (previousSelection == null)
                {
                    // Can't merge -- previous metadata must be one of us
                    return false;
                }

                // Merge is simple -- just carry along the original OldSelection value
                this.OldSelection = previousSelection.OldSelection;
                return true;
            }
        }

        class TextLineVisual : FrameworkElement
        {
            TextEditor editor;
            FormattedText formattedText;
            string detabifiedText;
            int[] tabs;
            int originalLineLength;
            int line;

            public TextLineVisual(TextEditor editor, int line)
            {
                this.editor = editor;
                this.line = line;
            }

            public TextLine Line { get { return this.editor.Buffer.TextData.Lines[this.line]; } }

            void EnsureFormattedText()
            {
                if (this.formattedText == null)
                {
                    BuildDetabifiedText();
                    this.formattedText = new FormattedText(this.detabifiedText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        this.editor.typeface, this.editor.FontSize, this.editor.Foreground);
                    this.editor.Buffer.Formatter.FormatLine(this.line, this.formattedText);
                }
            }

            void BuildDetabifiedText()
            {
                string originalText = this.Line.Text;

                this.originalLineLength = originalText.Length;
                if (!originalText.Contains('\t'))
                {
                    this.tabs = null;
                    this.detabifiedText = originalText;
                }
                else
                {
                    string[] chunks = originalText.Split('\t');
                    int originalLength = 0;
                    StringBuilder sb = new StringBuilder();

                    this.tabs = new int[chunks.Length - 1];
                    for (int i = 0; i < chunks.Length; i++)
                    {
                        sb.Append(chunks[i]);
                        originalLength += chunks[i].Length;

                        if (i < chunks.Length - 1)
                        {
                            this.tabs[i] = originalLength;
                            sb.Append(' ', this.editor.TabSize - (sb.Length % this.editor.TabSize));
                            originalLength += 1;    // This accounts for the tab character
                        }
                    }

                    Debug.Assert(originalLength == this.originalLineLength, "Miscalculation of original length");
                    this.detabifiedText = sb.ToString();
                }
            }

            public TextLocation MapToLocation(TextCoordinate coord, out int virtualSpaces)
            {
                Debug.Assert(coord.Line == this.line);
                return MapToLocation(coord.Index, out virtualSpaces);
            }

            public TextLocation MapToLocation(int coordinateIndex, out int virtualSpaces)
            {
                EnsureFormattedText();
                if (this.tabs == null)
                {
                    virtualSpaces = Math.Max(0, coordinateIndex - this.detabifiedText.Length);
                    return new TextLocation(this.line, Math.Min(coordinateIndex, this.detabifiedText.Length));
                }

                int locationIndex = coordinateIndex;
                int accumulatedTabOffset = 0;

                virtualSpaces = 0;

                for (int i = 0; i < this.tabs.Length; i++)
                {
                    if (locationIndex <= this.tabs[i])
                    {
                        return new TextLocation(this.line, locationIndex);
                    }

                    // Tab offset = number of *additional* spaces required (considering the tab char itself as a space)
                    int thisTabOffset = (this.editor.TabSize - ((this.tabs[i] + accumulatedTabOffset) % this.editor.TabSize)) - 1;

                    if (locationIndex <= this.tabs[i] + thisTabOffset)
                    {
                        // Landed on this tab.  If the index is > half the tab size past, land after the tab.
                        if (locationIndex - this.tabs[i] <= (thisTabOffset / 2))
                        {
                            return new TextLocation(this.line, this.tabs[i]);
                        }

                        return new TextLocation(this.line, this.tabs[i] + 1);
                    }

                    locationIndex -= thisTabOffset;
                    accumulatedTabOffset += thisTabOffset;
                }

                virtualSpaces = Math.Max(0, locationIndex - this.originalLineLength);
                return new TextLocation(this.line, locationIndex - virtualSpaces);
            }

            public TextCoordinate MapToCoordinate(TextLocation location)
            {
                Debug.Assert(location.Line == this.line);
                return MapToCoordinate(location.Index);
            }

            public TextCoordinate MapToCoordinate(int locationIndex)
            {
                EnsureFormattedText();

                int accumulatedOffset = 0;

                if (this.tabs != null)
                {

                    for (int i = 0; i < this.tabs.Length; i++)
                    {
                        if (locationIndex > this.tabs[i])
                        {
                            accumulatedOffset += (this.editor.TabSize - ((this.tabs[i] + accumulatedOffset) % this.editor.TabSize)) - 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                return new TextCoordinate(this.line, locationIndex + accumulatedOffset);
            }

            public TextCoordinate GetHomeCoordinate()
            {
                EnsureFormattedText();

                string text = this.detabifiedText;

                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] != ' ')
                    {
                        return new TextCoordinate(this.line, i);
                    }
                }

                return new TextCoordinate(this.line, text.Length);
            }

            public TextLocation GetHomeLocation()
            {
                EnsureFormattedText();

                string text = this.Line.Text;

                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] != ' ' && text[i] != '\t')
                    {
                        return new TextLocation(this.line, i);
                    }
                }

                return new TextLocation(this.line, text.Length);
            }

            public TextCoordinate GetEndCoordinate()
            {
                return MapToCoordinate(GetEndLocation());
            }

            public TextLocation GetEndLocation()
            {
                EnsureFormattedText();
                return new TextLocation(this.line, this.originalLineLength);
            }

            public void SetLineIndex(int lineIndex)
            {
                this.line = lineIndex;
                InvalidateTextLine();
            }

            public void InvalidateTextLine()
            {
                this.formattedText = null;
                this.InvalidateMeasure();
                this.InvalidateVisual();
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                EnsureFormattedText();
                this.editor.EnsureMaxHorizontal(this.formattedText.Width + this.editor.charWidth);
                return new Size(this.formattedText.Width, this.formattedText.Height);
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                EnsureFormattedText();
                drawingContext.DrawText(this.formattedText, new Point(0, 0));
            }
        }
    }

    public class CaretControl : Control
    {
        public static readonly DependencyProperty IsInViewProperty = DependencyProperty.Register(
            "IsInView", typeof(bool), typeof(CaretControl));

        public bool IsInView
        {
            get { return (bool)GetValue(IsInViewProperty); }
            set { SetValue(IsInViewProperty, value); }
        }
    }

    public class Selection
    {
        public TextCoordinateSpan Span { get; set; }
        public TextRange Range { get; set; }
        public int TopVirtualSpaces { get; set; }
        public int BottomVirtualSpaces { get; set; }
        public bool AnchoredAtTop { get; set; }
        public bool IsEmpty
        {
            get
            {
                return this.Span.IsEmpty && this.Range.IsEmpty;
            }
        }
    }

    public class SelectionVisual : Shape
    {
        Geometry selectionGeometry;

        public TextEditor Editor { get; set; }

        protected override Geometry DefiningGeometry
        {
            get
            {
                if (this.selectionGeometry == null)
                {
                    if (this.Editor == null)
                    {
                        this.selectionGeometry = Geometry.Empty;
                    }
                    else
                    {
                        this.selectionGeometry = this.Editor.BuildSelectionGeometry();
                    }
                }

                return this.selectionGeometry;
            }
        }

        public void Invalidate()
        {
            this.selectionGeometry = null;
            this.InvalidateVisual();
        }
    }

    public class HighlightVisual : Shape
    {
        Geometry geometry;
        TextEditor editor;

        public HighlightVisual(TextEditor editor, HighlightRange range)
        {
            this.editor = editor;
            this.Range = range;
            range.RangeChanged += OnRangeChanged;
            this.SetBinding(StrokeProperty, new Binding { Source = range, Path = new PropertyPath(HighlightRange.StrokeProperty) });
            this.SetBinding(StrokeThicknessProperty, new Binding { Source = range, Path = new PropertyPath(HighlightRange.StrokeThicknessProperty) });
            this.SetBinding(FillProperty, new Binding { Source = range, Path = new PropertyPath(HighlightRange.FillProperty) });
        }

        public HighlightRange Range { get; private set; }

        protected override Geometry DefiningGeometry
        {
            get
            {
                if (this.geometry == null)
                {
                    var start = this.editor.MapToCoordinate(this.Range.Range.Start);
                    var end = this.editor.MapToCoordinate(this.Range.Range.End);

                    this.geometry = this.editor.BuildGeometryForRange(start, end);
                }

                return this.geometry;
            }
        }

        void OnRangeChanged(object sender, EventArgs e)
        {
            Invalidate();
        }

        public void Invalidate()
        {
            this.geometry = null;
            this.InvalidateVisual();
        }
    }
}
