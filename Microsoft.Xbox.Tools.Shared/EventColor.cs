//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class EventColor
    {
        public static uint UnattributedColor = 0xfdffffff;
        public static uint SystemProcessColor = 0xfe000000;

        //
        // Notes on the indexed colors:
        //
        // 1. The first color in the list is the default color, and the one that 
        //    will be seen a lot, so make it distinct from the UI colors but not 
        //    overly garish.
        // 2. Colors in general should not be overly bright -- but they should
        //    be distinguishable from each other and not be too hard for users to 
        //    reference by name, e.g., "this orange bar here is longer than expected".
        //
        static Color[] indexedColors = 
        {
            Color.FromRgb(0x3C, 0x9E, 0xAE), // cyan
            Color.FromRgb(0xFD, 0xBB, 0x2E), // orange
            Color.FromRgb(0x4F, 0xBB, 0x4F), // green
            Color.FromRgb(0xD7, 0xE8, 0x2B), // yellow
            Color.FromRgb(0xB9, 0x48, 0xCB), // magenta
            Color.FromRgb(0x48, 0x60, 0xCB), // blue
            Color.FromRgb(0xDE, 0x27, 0x27), // red
            Color.FromRgb(0x71, 0x4D, 0x33), // brown
        };

        UInt32 data;

        public EventColor(UInt32 data)
        {
            this.data = data;
        }

        public static implicit operator Color(EventColor c)
        {
            byte dataA = (byte)((c.data & 0xff000000) >> 24);
            byte dataR = (byte)((c.data & 0x00ff0000) >> 16);
            byte dataG = (byte)((c.data & 0x0000ff00) >> 08);
            byte dataB = (byte)((c.data & 0x000000ff) >> 00);

            if (dataA == 0xff || dataA == 0xfe || dataA == 0xfd)
            {
                // Absolute (non-indexed) color
                return Color.FromArgb(dataA, dataR, dataG, dataB);
            }
            else if (dataA == 0x00 || dataA == 0x01 || dataA == 0x02)
            {
                // Indexed color -- use least significant byte (blue channel) for index
                int index = dataB % indexedColors.Length;
                return indexedColors[index];
            }
            else
            {
                // Bad data -- we currently don't allow alpha values other than 0xff
                return indexedColors[0];
            }
        }

        public bool IsBarberPole
        {
            get
            {
                byte dataA = (byte)((this.data & 0xff000000) >> 24);

                return dataA == 0xfe || dataA == 0xfd || dataA == 0x01 || dataA == 0x02;
            }
        }

        public int ColorAsInt
        {
            get
            {
                var c = (Color)this;
                return (c.R << 16) | (c.G << 8) | c.B;
            }
        }

        public int BarberPoleStripeColorAsInt
        {
            get
            {
                byte dataA = (byte)((this.data & 0xff000000) >> 24);

                if (dataA == 0xfd || dataA == 0x02)
                {
                    return 0x000000;
                }

                return 0xffffff;
            }
        }
    }
}
