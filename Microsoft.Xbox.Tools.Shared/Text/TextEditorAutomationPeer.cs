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
    public class TextEditorAutomationPeer : FrameworkElementAutomationPeer
    {
        TextEditor editor;

        public TextEditorAutomationPeer(TextEditor editor) : base(editor) { this.editor = editor; }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Edit;
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Text)
            {
                return new TextEditorTextPattern(this.editor);
            }

            return base.GetPattern(patternInterface);
        }
    }

    public class TextEditorTextPattern : ITextProvider
    {
        TextEditor editor;

        public TextEditorTextPattern(TextEditor editor)
        {
            this.editor = editor;
        }

        public ITextRangeProvider DocumentRange
        {
            get { return new TextEditorTextRangeProvider(this.editor, new TextRange(new TextLocation(), this.editor.Buffer.TextData.End)); }
        }

        public ITextRangeProvider[] GetSelection()
        {
            var selection = this.editor.GetSelection();
            return new ITextRangeProvider[] { new TextEditorTextRangeProvider(this.editor, selection.Range) };
        }

        public ITextRangeProvider[] GetVisibleRanges()
        {
            return new ITextRangeProvider[] 
            { 
                new TextEditorTextRangeProvider(this.editor, new TextRange(new TextLocation(this.editor.TopVisibleLine, 0), new TextLocation(this.editor.TopVisibleLine + this.editor.VisibleLineCount, 0))) 
            };
        }

        public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement)
        {
            return null;
        }

        public ITextRangeProvider RangeFromPoint(Point screenLocation)
        {
            return null;
        }

        public SupportedTextSelection SupportedTextSelection
        {
            get { return SupportedTextSelection.Single; }
        }
    }

    public class TextEditorTextRangeProvider : ITextRangeProvider
    {
        TextEditor editor;
        TextRange range;

        public TextEditorTextRangeProvider(TextEditor editor, TextRange range)
        {
            this.editor = editor;
            this.range = range;
        }

        public void AddToSelection()
        {
        }

        public ITextRangeProvider Clone()
        {
            return new TextEditorTextRangeProvider(this.editor, this.range);
        }

        public bool Compare(ITextRangeProvider range)
        {
            var that = range as TextEditorTextRangeProvider;

            if (that != null)
            {
                return this.editor == that.editor && this.range.Start == that.range.Start && this.range.End == that.range.End;
            }

            return false;
        }

        public int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            return 0;
        }

        public void ExpandToEnclosingUnit(TextUnit unit)
        {
        }

        public ITextRangeProvider FindAttribute(int attribute, object value, bool backward)
        {
            return null;
        }

        public ITextRangeProvider FindText(string text, bool backward, bool ignoreCase)
        {
            return null;
        }

        public object GetAttributeValue(int attribute)
        {
            return null;
        }

        public double[] GetBoundingRectangles()
        {
            return new double[] { 0, 0, 0, 0 };
        }

        public IRawElementProviderSimple[] GetChildren()
        {
            return new IRawElementProviderSimple[0];
        }

        public IRawElementProviderSimple GetEnclosingElement()
        {
            return null;
        }

        public string GetText(int maxLength)
        {
            var text = this.editor.Buffer.TextData.ToString(this.range.Start, this.range.End);

            if (maxLength >= 0)
            {
                text = text.Substring(0, Math.Min(text.Length, maxLength));
            }

            return text;
        }

        public int Move(TextUnit unit, int count)
        {
            return 0;
        }

        public void MoveEndpointByRange(TextPatternRangeEndpoint startpoint, ITextRangeProvider provider, TextPatternRangeEndpoint endpoint)
        {
        }


        public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
        {
            if (unit == TextUnit.Character)
            {
                int actual;

                if (endpoint == TextPatternRangeEndpoint.End)
                {
                    this.range.End = this.editor.Buffer.TextData.OffsetByCharacter(this.range.End, count, out actual);
                }
                else
                {
                    this.range.Start = this.editor.Buffer.TextData.OffsetByCharacter(this.range.Start, count, out actual);
                }

                return actual;
            }

            return 0;
        }

        public void RemoveFromSelection()
        {

        }

        public void ScrollIntoView(bool alignToTop)
        {

        }

        public void Select()
        {
            this.editor.Select(this.editor.MapToCoordinate(this.range.Start.Line, this.range.Start.Index), this.editor.MapToCoordinate(this.range.End.Line, this.range.End.Index));
        }
    }
}
