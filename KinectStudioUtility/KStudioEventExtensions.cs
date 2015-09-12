//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.Kinect.Tools;

    public static class KStudioEventExtensions
    {
        private class WeakReferenceTuple
        {
            public WeakReferenceTuple(KStudioEvent eventObj, HGlobalBuffer buffer)
            {
                EventReference = new WeakReference<KStudioEvent>(eventObj);
                BufferReference = new WeakReference<HGlobalBuffer>(buffer);
            }

            public readonly WeakReference<KStudioEvent> EventReference;
            public readonly WeakReference<HGlobalBuffer> BufferReference;
        };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public static HGlobalBuffer GetRetainableEventDataBuffer(this KStudioEvent eventObj)
        {
            HGlobalBuffer value = null;

            if (eventObj != null)
            {
                lock (KStudioEventExtensions.buffers)
                {
                    // this list should be short (at max a couple dozen); if it starts to get longer than that, 
                    // something is probably saving more large raw event data than it should be or the GC isn't kicking in.
                    Debug.Assert(KStudioEventExtensions.buffers.Count < 100);

                    List<int> toRemove = new List<int>();

                    for (int i = 0; i < KStudioEventExtensions.buffers.Count; ++i)
                    {
                        WeakReferenceTuple tuple = KStudioEventExtensions.buffers[i];

                        KStudioEvent eventKey;

                        if (tuple.EventReference.TryGetTarget(out eventKey))
                        {
                            HGlobalBuffer bufferValue;

                            if (tuple.BufferReference.TryGetTarget(out bufferValue))
                            {
                                if (eventKey == eventObj)
                                {
                                    value = bufferValue;
                                    break;
                                }
                            }
                            else
                            {
                                toRemove.Insert(0, i);
                            }
                        }
                        else
                        {
                            toRemove.Insert(0, i);
                        }
                    }

                    foreach (int i in toRemove)
                    {
                        KStudioEventExtensions.buffers.RemoveAt(i);
                    }

                    if (value == null)
                    {
                        uint bufferSize;
                        IntPtr buffer;
                        eventObj.AccessUnderlyingEventDataBuffer(out bufferSize, out buffer);

                        if (bufferSize != 0)
                        {
                            value = new HGlobalBuffer(bufferSize);
                            UnsafeNativeMethods.RtlMoveMemory(value.Buffer, buffer, bufferSize);

                            WeakReferenceTuple newTuple = new WeakReferenceTuple(eventObj, value);
                            KStudioEventExtensions.buffers.Insert(0, newTuple);
                        }
                    }
                }
            }

            return value;
        }

        private static List<WeakReferenceTuple> buffers = new List<WeakReferenceTuple>();
    }
}
