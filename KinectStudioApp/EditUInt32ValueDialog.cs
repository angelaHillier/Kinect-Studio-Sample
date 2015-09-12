//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;

    public class EditUInt32Dialog : EditValueDialog<UInt32>
    {
        protected override bool IsValidText(string text)
        {
            bool result = true;

            if (text != null)
            {
                foreach (char ch in text)
                {
                    if (!Char.IsDigit(ch))
                    {
                        result = false;
                        break;
                    }
                }
            }

            return result;
        }

        protected override UInt32? ParseText(string text)
        {
            UInt32? result = null;

            if (!String.IsNullOrWhiteSpace(text))
            {
                UInt32 num;
                if (UInt32.TryParse(text, out num))
                {
                    result = num;
                }
            }

            return result;
        }
    }
}
