//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    public abstract class BufferedTextFormatProvider : TextFormatProvider
    {
        TextChange currentChange;
        List<TextFormatInfo> data;
        ParseEpisode parseEpisode;
        object lockObject;

        protected BufferedTextFormatProvider(TextBuffer buffer) : base(buffer)
        {
            this.lockObject = new object();
            this.currentChange = buffer.LastChange;
            this.data = new List<TextFormatInfo>();
        }

        protected override void OnBufferTextDataChanged(object sender, TextDataChangedEventArgs e)
        {
            var change = e.Change;

            while (change.NextChange != null)
            {
                // We actually need the last change; we're "as of" that change.
                change = change.NextChange;
            }

            StartNewParse(change);
        }

        protected void StartNewParse(TextChange change)
        {
            OnStartingNewParse();

            lock (this.lockObject)
            {
                if (this.parseEpisode != null)
                {
                    this.parseEpisode.Canceled = true;
                }

                this.parseEpisode = new ParseEpisode();
                this.parseEpisode.Start(this, change);
            }
        }

        protected virtual void OnStartingNewParse()
        {
        }

        protected void Invalidate()
        {
            StartNewParse(this.Buffer.LastChange);
        }

        void OnParseCompleted(ParseEpisode episode)
        {
            bool raiseEvent = false;

            lock (this.lockObject)
            {
                if (episode == this.parseEpisode)
                {
                    this.data = episode.FormatData;
                    this.currentChange = episode.Change;
                    raiseEvent = true;
                }
            }

            if (raiseEvent)
            {
                RaiseFormatDataChangedEvent();
            }
        }

        public override TextFormatInfo[] GetFormatDataForLine(int line, out TextChange change)
        {
            lock (this.lockObject)
            {
                if (this.parseEpisode == null)
                {
                    // We've never started a parse, so do it now.
                    StartNewParse(this.currentChange);
                }

                change = this.currentChange;

                if (this.data.Count > 0)
                {
                    int firstInRange = FindFirstInfoWhere(0, this.data.Count - 1, idx => this.data[idx].Range.End.Line >= line);
                    int firstBeyondRange = FindFirstInfoWhere(0, this.data.Count - 1, idx => this.data[idx].Range.Start.Line > line);

                    if (firstBeyondRange == -1)
                    {
                        firstBeyondRange = this.data.Count;
                    }

                    if (firstInRange == -1 || firstInRange > firstBeyondRange)
                    {
                        return null;
                    }

                    var result = new TextFormatInfo[firstBeyondRange - firstInRange];
                    this.data.CopyTo(firstInRange, result, 0, result.Length);
                    return result;
                }

                return null;
            }
        }

        int FindFirstInfoWhere(int first, int last, Func<int, bool> comparer)
        {
            int size = last - first + 1;

            if (size <= 2)
            {
                if (comparer(first))
                {
                    return first;
                }

                return comparer(last) ? last : -1;
            }

            int mid = first + ((last - first) / 2);

            if (comparer(mid))
            {
                return FindFirstInfoWhere(first, mid, comparer);
            }
            else
            {
                return FindFirstInfoWhere(mid, last, comparer);
            }
        }

        protected abstract List<TextFormatInfo> Parse(TextData textData, Func<bool> isCanceled);

        class ParseEpisode
        {
            BufferedTextFormatProvider owner;

            public bool Canceled { get; set; }
            public List<TextFormatInfo> FormatData { get; private set; }
            public TextChange Change { get; private set; }

            public void Start(BufferedTextFormatProvider owner, TextChange change)
            {
                this.owner = owner;
                this.Change = change;
                ThreadPool.QueueUserWorkItem((o) => DoParse());
            }

            void DoParse()
            {
                this.FormatData = this.owner.Parse(this.Change.NewTextData, () => this.Canceled);
                this.owner.OnParseCompleted(this);
            }
        }
    }
}
