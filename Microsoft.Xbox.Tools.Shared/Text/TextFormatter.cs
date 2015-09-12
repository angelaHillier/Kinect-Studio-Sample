//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    public class TextFormatter : DispatcherObject
    {
        List<TextFormatProvider> providers;
        List<HighlightRange> highlightRanges;

        public TextFormatter(TextBuffer buffer)
        {
            this.providers = new List<TextFormatProvider>();
            this.highlightRanges = new List<HighlightRange>();
            this.Buffer = buffer;
        }

        public TextBuffer Buffer { get; private set; }
        public IEnumerable<TextFormatProvider> FormatProviders { get { return this.providers; } }
        public IEnumerable<HighlightRange> HighlightRanges { get { return this.highlightRanges; } }

        public event EventHandler FormatDataChanged;
        public event EventHandler HighlightRangesChanged;

        void OnFormatProvidersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaiseEvent(this.FormatDataChanged);
        }

        void RaiseEvent(EventHandler handler)
        {
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        [SuppressMessage("Microsoft.Usage", "#pw26506")]
        public void AddProvider(TextFormatProvider provider)
        {
            this.providers.Add(provider);
            provider.FormatDataChanged += OnProviderFormatDataChanged;
            RaiseEvent(this.FormatDataChanged);
        }

        [SuppressMessage("Microsoft.Usage", "#pw26506")]
        public void RemoveProvider(TextFormatProvider provider)
        {
            if (this.providers.Remove(provider))
            {
                provider.FormatDataChanged -= OnProviderFormatDataChanged;
                RaiseEvent(this.FormatDataChanged);
            }
        }

        public HighlightRangeUpdater CreateHighlightRangeUpdater()
        {
            return new Updater(this);
        }

        void OnProviderFormatDataChanged(object sender, EventArgs e)
        {
            if (CheckAccess())
            {
                RaiseEvent(this.FormatDataChanged);
            }
            else
            {
                this.Dispatcher.BeginInvoke((Action)(() => RaiseEvent(this.FormatDataChanged)));
            }
        }

        public void FormatLine(int line, FormattedText text)
        {
            foreach (var provider in this.providers)
            {
                TextChange change;
                var formatInfos = provider.GetFormatDataForLine(line, out change);

                if (formatInfos != null)
                {
                    foreach (var info in formatInfos)
                    {
                        var range = info.Range;

                        if (change != null)
                        {
                            for (var c = change.NextChange; c != null; c = c.NextChange)
                            {
                                range = c.ApplyTo(range);
                            }
                        }

                        int start = (range.Start.Line < line) ? 0 : range.Start.Index;
                        int end = (range.End.Line > line) ? this.Buffer.TextData.Lines[line].Length : range.End.Index;

                        if (end > start && end <= this.Buffer.TextData.Lines[line].Length)
                        {
                            if (info.Foreground != null)
                            {
                                text.SetForegroundBrush(info.Foreground, start, end - start);
                            }

                            if (info.Flags.HasFlag(FormatFlags.Bold))
                            {
                                text.SetFontWeight(FontWeights.Bold, start, end - start);
                            }

                            if (info.Flags.HasFlag(FormatFlags.Italic))
                            {
                                text.SetFontStyle(FontStyles.Italic, start, end - start);
                            }

                            if (info.Flags.HasFlag(FormatFlags.Underline))
                            {
                                var decorations = new TextDecorationCollection(new TextDecoration[] { new TextDecoration(TextDecorationLocation.Underline, null, 0, TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended) });
                                text.SetTextDecorations(decorations, start, end - start);
                            }
                        }
                    }
                }
            }
        }

        class Updater : HighlightRangeUpdater
        {
            TextFormatter formatter;
            bool changed;

            public Updater(TextFormatter formatter)
            {
                this.formatter = formatter;
            }

            public override HighlightRange AddHighlightRange(TextRange range)
            {
                var highlightRange = new HighlightRange(this.formatter.Buffer, range);

                this.formatter.highlightRanges.Add(highlightRange);
                this.changed = true;
                return highlightRange;
            }

            public override void RemoveHighlightRange(HighlightRange highlightRange)
            {
                if (this.formatter.highlightRanges.Remove(highlightRange))
                {
                    this.changed = true;
                }
            }

            public override void Dispose()
            {
                if (changed)
                {
                    formatter.RaiseEvent(formatter.HighlightRangesChanged);
                }
            }
        }
    }

    public abstract class HighlightRangeUpdater : IDisposable
    {
        public abstract HighlightRange AddHighlightRange(TextRange range);
        public abstract void RemoveHighlightRange(HighlightRange range);
        public abstract void Dispose();
    }
}
