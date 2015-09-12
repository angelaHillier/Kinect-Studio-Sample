//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Microsoft.Xbox.Tools.Shared
{
    public class TimelineSelectionVisual : Shape
    {
        Geometry geometry;
        List<DataBarClipSpan> clips = new List<DataBarClipSpan>();

        protected override Geometry DefiningGeometry
        {
            get
            {
                if (this.geometry == null)
                {
                    this.geometry = BuildGeometry();
                }

                return this.geometry;
            }
        }

        public IList<DataBarClipSpan> ClipSpans { get { return this.clips; } }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            this.InvalidateGeometry();
            base.OnRenderSizeChanged(sizeInfo);
        }

        public void InvalidateGeometry()
        {
            this.geometry = null;
            this.InvalidateVisual();
        }

        public void SetClips(double startingHeight, IEnumerable<DataBar> dataBars)
        {
            this.clips.Clear();

            // Each data bar may provide "clip spans" for the selection.  Here we map them to top/height
            // ranges for creation of the selection geometry.
            foreach (var dataBar in dataBars)
            {
                foreach (var clip in dataBar.SelectionClipSpans)
                {
                    if (this.clips.Count == 0)
                    {
                        this.clips.Add(new DataBarClipSpan { Top = clip.Top + startingHeight, Height = clip.Height });
                    }
                    else
                    {
                        var lastClip = this.clips[this.clips.Count - 1];

                        // Make sure the clips don't overlap.  They shouldn't, but just in case...
                        if (lastClip.Top + lastClip.Height >= clip.Top)
                        {
                            this.clips[this.clips.Count - 1] = new DataBarClipSpan { Top = lastClip.Top, Height = (clip.Top + clip.Height) - lastClip.Top };
                        }
                        else
                        {
                            this.clips.Add(new DataBarClipSpan { Top = clip.Top + startingHeight, Height = clip.Height });
                        }
                    }
                }

                startingHeight += dataBar.ActualHeight;
            }

            this.InvalidateGeometry();
        }

        Geometry BuildGeometry()
        {
            double top = 0;
            var pathGeometry = new PathGeometry();
            PathFigure figure;

            // Create rectangles (stroked on sides only, not top/bottom) for each section of the selection visual
            // not "obscured" by clip ranges provided by the data bars.
            foreach (var clip in this.clips)
            {
                figure = new PathFigure { IsClosed = true, StartPoint = new Point(0, top) };
                figure.Segments.Add(new LineSegment(new Point(0, clip.Top), true));
                figure.Segments.Add(new LineSegment(new Point(this.Width - this.StrokeThickness, clip.Top), false));
                figure.Segments.Add(new LineSegment(new Point(this.Width - this.StrokeThickness, top), true));
                figure.Segments.Add(new LineSegment(new Point(0, top), false));
                top = clip.Top + clip.Height;
                pathGeometry.Figures.Add(figure);
            }

            if (top < this.ActualHeight)
            {
                figure = new PathFigure { IsClosed = true, StartPoint = new Point(0, top) };
                figure.Segments.Add(new LineSegment(new Point(0, this.ActualHeight), true));
                figure.Segments.Add(new LineSegment(new Point(this.Width - this.StrokeThickness, this.ActualHeight), false));
                figure.Segments.Add(new LineSegment(new Point(this.Width - this.StrokeThickness, top), true));
                figure.Segments.Add(new LineSegment(new Point(0, top), false));
                pathGeometry.Figures.Add(figure);
            }

            return pathGeometry;
        }
    }
}
