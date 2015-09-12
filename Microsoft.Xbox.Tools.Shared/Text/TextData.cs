//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    public class TextData
    {
        static TextLine[] emptyBlock = new TextLine[0];

        TextLine[] beforeBlock;
        TextLine activeLine;
        TextLine[] afterBlock;
        TextLineList lines;

        static TextData()
        {
            Empty = TextData.FromString(null);
        }

        private TextData(TextLine[] beforeBlock, TextLine activeLine, TextLine[] afterBlock, int length)
        {
            this.lines = new TextLineList(this);
            this.beforeBlock = beforeBlock;
            this.activeLine = activeLine;
            this.afterBlock = afterBlock;
            this.TextLength = length;
        }

        public int TextLength { get; private set; }
        public ITextLineList Lines { get { return this.lines; } }
        public TextLocation End { get { return new TextLocation(lines.Count - 1, lines[lines.Count - 1].Length); } }
        int ActiveLineIndex { get { return this.beforeBlock.Length; } }
        public static TextData Empty { get; private set; }

        public static TextData ApplyEdit(TextData previous, TextLocation start, TextLocation end, TextData replacement)
        {
            // Optimized for the vast majority of text edits -- an edit (single-line, typically single-character) on the active line
            if (replacement.Lines.Count == 1 && start.Line == previous.ActiveLineIndex && end.Line == start.Line)
            {
                Debug.Assert(replacement.Lines[0].EndMarker == EndMarker.EOB);

                string editedLineText = previous.activeLine.Text.Substring(0, start.Index) + replacement.lines[0].Text + previous.activeLine.Text.Substring(end.Index);
                TextLine editedLine = new TextLine(editedLineText, previous.activeLine.EndMarker);

                return new TextData(previous.beforeBlock, editedLine, previous.afterBlock, previous.TextLength + editedLine.Length - previous.activeLine.Length);
            }

            // All other cases require reconstruction of the before/after blocks (in the current implementation -- more intelligent
            // mechanisms could be built to increase memory reuse).  This is still not terrible, as the untouched lines are reused; only the
            // blocks (arrays of line references) are recreated.

            // After this edit, we assume that the *end* of the inserted text is the active line.
            int linesAdded = replacement.Lines.Count - 1;
            int linesRemoved = end.Line - start.Line;
            int totalLines = previous.Lines.Count + linesAdded - linesRemoved;
            int beforeLines = start.Line + replacement.Lines.Count - 1;
            int afterLines = totalLines - beforeLines - 1;

            Debug.Assert(totalLines == beforeLines + afterLines + 1, "Miscalculation in line totals");
            var before = new TextLine[beforeLines];
            var after = new TextLine[afterLines];
            TextLine newActiveLine;
            string editLineText;
            int newLength = 0;

            // There are two cases -- the replacement is only one line (so we only end up creating one new TextLine), or it is more than one
            // (so we end up creating 2 new TextLines).  There are other opportunities for optimization in the latter (inserting at index 0, or
            // the last line of the replacement being empty) to consider later.
            if (replacement.Lines.Count == 1)
            {
                for (int i = 0; i < beforeLines; i++)
                {
                    before[i] = previous.Lines[i];
                    newLength += before[i].LengthWithEndMarker;
                }

                editLineText = previous.Lines[start.Line].Text.Substring(0, start.Index) + replacement.Lines[0].Text + previous.Lines[end.Line].Text.Substring(end.Index);
                newActiveLine = new TextLine(editLineText, previous.Lines[end.Line].EndMarker);
                newLength += newActiveLine.LengthWithEndMarker;

                for (int i = 0; i < afterLines; i++)
                {
                    after[i] = previous.Lines[end.Line + i + 1];
                    newLength += after[i].LengthWithEndMarker;
                }
            }
            else
            {
                for (int i = 0; i < start.Line; i++)
                {
                    before[i] = previous.Lines[i];
                    newLength += before[i].LengthWithEndMarker;
                }

                editLineText = previous.Lines[start.Line].Text.Substring(0, start.Index) + replacement.Lines[0].Text;
                before[start.Line] = new TextLine(editLineText, replacement.Lines[0].EndMarker);
                newLength += before[start.Line].LengthWithEndMarker;

                for (int i = 1; i < replacement.Lines.Count - 1; i++)
                {
                    before[start.Line + i] = replacement.Lines[i];
                    newLength += before[start.Line + i].LengthWithEndMarker;
                }

                editLineText = replacement.Lines[replacement.Lines.Count - 1].Text + previous.Lines[end.Line].Text.Substring(end.Index);
                newActiveLine = new TextLine(editLineText, previous.Lines[end.Line].EndMarker);
                newLength += newActiveLine.LengthWithEndMarker;

                for (int i = 0; i < afterLines; i++)
                {
                    after[i] = previous.Lines[end.Line + i + 1];
                    newLength += after[i].LengthWithEndMarker;
                }
            }

            return new TextData(before, newActiveLine, after, newLength);
        }

        public static TextData FromString(string text, bool normalizeEndMarkers = false)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new TextData(emptyBlock, new TextLine(string.Empty, EndMarker.EOB), emptyBlock, 0);
            }

            // Estimate line count at 20 chars/line
            List<TextLine> lines = new List<TextLine>(text.Length / 20);
            int lineStart = 0;
            int i = 0;

            while (i < text.Length)
            {
                if (text[i] == '\r' || text[i] == '\n')
                {
                    string lineText = text.Substring(lineStart, i - lineStart);
                    TextLine line;

                    if ((text[i] == '\r') && (text.Length > i + 1) && (text[i + 1] == '\n'))
                    {
                        line = new TextLine(lineText, EndMarker.CRLF);
                        i += 2;
                    }
                    else
                    {
                        line = new TextLine(lineText, normalizeEndMarkers ? EndMarker.CRLF : (text[i] == '\r') ? EndMarker.CR : EndMarker.LF);
                        i += 1;
                    }

                    lines.Add(line);
                    lineStart = i;
                }
                else
                {
                    i += 1;
                }
            }

            TextLine lastLine = new TextLine(text.Substring(lineStart), EndMarker.EOB);
            return new TextData(lines.Count > 0 ? lines.ToArray() : emptyBlock, lastLine, emptyBlock, text.Length);
        }

        public override string ToString()
        {
            int lastLine = this.Lines.Count - 1;
            int lastIndex = this.Lines[lastLine].LengthWithEndMarker;

            return this.ToString(new TextLocation(0, 0), new TextLocation(lastLine, lastIndex));
        }

        public TextData GetSubrange(TextLocation start, TextLocation end)
        {
            return TextData.FromString(this.ToString(start, end));
        }

        public string ToString(TextLocation start, TextLocation end)
        {
            if (start.Line == end.Line)
            {
                return this.Lines[start.Line].TextWithEndMarker.Substring(start.Index, end.Index - start.Index);
            }

            StringBuilder sb = new StringBuilder(this.TextLength);

            sb.Append(this.Lines[start.Line].TextWithEndMarker.Substring(start.Index));
            for (int i = start.Line + 1; i < end.Line; i++)
            {
                sb.Append(this.lines[i].TextWithEndMarker);
            }
            sb.Append(this.Lines[end.Line].Text.Substring(0, end.Index));

            return sb.ToString();
        }

        public TextLocation OffsetByCharacter(TextLocation startLocation, int characterDelta, out int actualDelta)
        {
            int line = startLocation.Line;
            int index = startLocation.Index;
            int leftToMove = Math.Abs(characterDelta);

            actualDelta = characterDelta;
            if (characterDelta > 0)
            {
                while ((leftToMove > 0) && (line < this.lines.Count - 1))
                {
                    int restOfLine = this.lines[line].LengthWithEndMarker - index;
                    int restOfLineSansEndMarker = this.lines[line].Length - index;

                    if (restOfLineSansEndMarker >= leftToMove)
                    {
                        // We're on the destination line, just update the index and we're done.
                        // actualDelta is already correct.
                        index += leftToMove;
                        return new TextLocation(line, index);
                    }

                    line += 1;
                    index = 0;
                    leftToMove -= restOfLine;   // NOTE: Always move past ALL of the end markers (could leave leftToMove < 0)
                }

                actualDelta = characterDelta - leftToMove;
                return new TextLocation(line, 0);
            }
            else if (characterDelta < 0)
            {
                while ((leftToMove > 0))
                {
                    if (index > leftToMove)
                    {
                        index -= leftToMove;
                        return new TextLocation(line, index);
                    }

                    leftToMove -= index;

                    if (line > 0)
                    {
                        line -= 1;
                        index = this.lines[line].Length;
                        leftToMove -= (this.lines[line].LengthWithEndMarker - this.lines[line].Length);
                    }
                    else
                    {
                        index = 0;
                        break;
                    }
                }

                actualDelta = characterDelta + leftToMove;
                return new TextLocation(line, index);
            }
            else
            {
                actualDelta = characterDelta;
                return startLocation;
            }
        }

        public TextLocation OffsetByLine(TextLocation startLocation, int lineDelta, out int actualDelta)
        {
            var location = new TextLocation(Math.Min(Math.Max(0, startLocation.Line + lineDelta), this.lines.Count - 1), startLocation.Index);

            if (this.lines[location.Line].Length < location.Index)
            {
                location.Index = this.lines[location.Line].Length;
            }

            actualDelta = location.Line - startLocation.Line;
            return location;
        }

        class TextLineList : ITextLineList
        {
            TextData owner;

            public TextLineList(TextData owner)
            {
                this.owner = owner;
            }

            public TextLine this[int index]
            {
                get
                {
                    if (index < this.owner.beforeBlock.Length)
                    {
                        return this.owner.beforeBlock[index];
                    }
                    else if (index == this.owner.beforeBlock.Length)
                    {
                        return this.owner.activeLine;
                    }
                    else if (index < this.Count)
                    {
                        return this.owner.afterBlock[index - (this.owner.beforeBlock.Length + 1)];
                    }

                    throw new IndexOutOfRangeException();
                }
            }

            public int Count
            {
                get { return this.owner.beforeBlock.Length + this.owner.afterBlock.Length + 1; }
            }

            IEnumerable<TextLine> AllLines
            {
                get
                {
                    foreach (var line in this.owner.beforeBlock)
                    {
                        yield return line;
                    }

                    yield return this.owner.activeLine;

                    foreach (var line in this.owner.afterBlock)
                    {
                        yield return line;
                    }
                }
            }

            public IEnumerator<TextLine> GetEnumerator()
            {
                return this.AllLines.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }

    public interface ITextLineList : IEnumerable<TextLine>
    {
        int Count { get; }
        TextLine this[int index] { get; }
    }
}
