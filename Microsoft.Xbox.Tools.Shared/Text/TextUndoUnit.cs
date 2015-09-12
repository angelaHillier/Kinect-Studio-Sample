//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    public class TextUndoUnit
    {
        List<TextChange> changes;

        public TextUndoMetadata Metadata { get; set; }

        public void AddChange(TextChange change)
        {
            if (this.changes == null)
            {
                this.changes = new List<TextChange>();
            }

            this.changes.Add(change);
        }

        internal void Merge(TextUndoUnit incomingUnit)
        {
            if (this.changes == null)
            {
                this.changes = incomingUnit.changes;
            }
            else if (incomingUnit.changes != null)
            {
                this.changes.AddRange(incomingUnit.changes);
            }
        }

        [SuppressMessage("Microsoft.Usage", "#pw26506")]
        public void Undo(TextPencil pencil)
        {
            if (this.changes != null)
            {
                // Changes get undone in reverse order
                for (int i = this.changes.Count - 1; i >= 0; i--)
                {
                    var change = this.changes[i];
                    pencil.Write(change.Start, change.NewEnd, change.OldTextData.GetSubrange(change.Start, change.OldEnd));
                }
            }
        }

        [SuppressMessage("Microsoft.Usage", "#pw26506")]
        public void Redo(TextPencil pencil)
        {
            if (this.changes != null)
            {
                // Changes get redone in original order
                for (int i = 0; i < this.changes.Count; i++)
                {
                    var change = this.changes[i];
                    pencil.Write(change.Start, change.OldEnd, change.Replacement);
                }
            }
        }

        public int Size
        {
            get
            {
                if (this.changes == null)
                {
                    return 0;
                }

                return this.changes.Sum(c => c.OldTextData.TextLength + c.Replacement.TextLength);
            }
        }
    }

    public abstract class TextUndoMetadata
    {
        public abstract bool TryMerge(TextUndoMetadata previousMetadata); 
    }
}
