//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Xml.Linq;

namespace Microsoft.Xbox.Tools.Shared
{
    public interface ISessionStateService
    {
        // This event will fire when it's time to save session state.  Listen to this, and in
        // response to it, call SetSessionState to save your session data.
        event EventHandler StateSaveRequested;

        XElement GetSessionState(string stateKey);
        void SetSessionState(string stateKey, XElement state);

        // This is an easier model than Get/SetSessionState.  Supply an object w/ a key name at startup.  This will immediately load
        // any previously stored properties back into that object, and will hold the object to save its property values
        // at shutdown time.  Supported primitive types are string, bool, int, long, float, double.  Custom types
        // will also be created/saved (recursively) if they have a public default constructor.  List<T> is also supported, but only if the
        // property is writable (i.e., a new List<T> will be created at load time).  Note that the type of the given variable 
        // must be a custom type -- primitive or list types can't be directly used, they can only be properties.
        void DeclareSessionStateVariable(string name, object variable);
    }
}
