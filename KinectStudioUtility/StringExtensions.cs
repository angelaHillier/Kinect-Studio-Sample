//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;

    public static class StringExtensions
    {
        public static string Reverse(this string value)
        {
            if (value != null)
            {
                char[] array = value.ToCharArray();
                Array.Reverse(array);
                value = new string(array);
            }

            return value;
        }
    }
}
