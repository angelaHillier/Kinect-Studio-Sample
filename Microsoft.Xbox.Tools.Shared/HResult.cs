//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

namespace Microsoft.Xbox.Tools.Shared
{
    public struct HResult
    {
        // Define commonly used HRESULT codes.
        public static readonly HResult S_OK = new HResult(0);
        public static readonly HResult S_FALSE = new HResult(1);
        public static readonly HResult E_ABORT = new HResult(0x80004004);
        public static readonly HResult E_ACCESSDENIED = new HResult(0x80070005); // General access denied error
        public static readonly HResult E_FAIL = new HResult(0x80004005); // Unspecified failure
        public static readonly HResult E_HANDLE = new HResult(0x80070006); // Handle that is not valid
        public static readonly HResult E_INVALIDARG = new HResult(0x80070057); //One or more arguments are not valid
        public static readonly HResult E_NOINTERFACE = new HResult(0x80004002); // No such interface supported
        public static readonly HResult E_NOTIMPL = new HResult(0x80004001); // Not implemented
        public static readonly HResult E_OUTOFMEMORY = new HResult(0x8007000E); // Failed to allocate necessary memory
        public static readonly HResult E_POINTER = new HResult(0x80004003); // Pointer that is not valid
        public static readonly HResult E_UNEXPECTED = new HResult(0x8000FFFF); // Unexpected failure
        public static readonly HResult E_BUSY = new HResult(0x800700AA); // __HRESULT_FROM_WIN32( ERROR_BUSY )
        public static readonly HResult E_DISK_FULL = new HResult(0x80070070); // __HRESULT_FROM_WIN32( ERROR_DISK_FULL )
        public static readonly HResult E_FILE_EXISTS = new HResult(0x80070050); // __HRESULT_FROM_WIN32( ERROR_FILE_EXISTS )
        public static readonly HResult E_PENDING = new HResult(0x8000000A); // E_PENDING
        public static readonly HResult E_OLE_CLASS_NOT_REGISTERED = new HResult(0x80040154); // OLE error: class not registered.
        public static readonly HResult RPC_E_SERVERFAULT = new HResult(0x80010105); // RPC error -- server threw an exception
        public static readonly HResult RPC_S_SERVER_UNAVAILABLE = new HResult(0x800706BA); // RPC error -- server unavailable
        public static readonly HResult E_WSAHOST_NOT_FOUND = new HResult(0X80072AF9); // __HRESULT_FROM_WIN32( WSAHOST_NOT_FOUND )
        public static readonly HResult E_WSAECONNRESET = new HResult(0x80072746); // __HRESULT_FROM_WIN32( WSAECONNRESET )
        public static readonly HResult E_WSAENOTSOCK = new HResult(0x80072736); // __HRESULT_FROM_WIN32( WSAENOTSOCK )
        public static readonly HResult E_FILE_NOT_FOUND = new HResult(0X80070002); // __HRESULT_FROM_WIN32( FILE_NOT_FOUND )
        public static readonly HResult E_USER_MAPPED_FILE = new HResult(0X800704C8); // __HRESULT_FROM_WIN32( ERROR_USER_MAPPED_FILE )
        public static readonly HResult E_OUTOFRANGE = new HResult(0x80001009);

        // The following are custom error codes defined by this component (as opposed to standard COM codes)
        public static readonly HResult E_REQUEST_CANCELED = new HResult(0X8A150001); // BackgroundRequest was canceled by user
        public static readonly HResult E_REQUIRES_PROFILING_MODE = new HResult(0X8A150002); // profiling mode required for operation


        static Dictionary<int, ErrorCodeData> errorCodes;
        static bool localCodesRegistered;
        static object lockObject = new object();

        private int value;
        private string detailedErrorMessage;

        public static HResult FromException(Exception e)
        {
            HResult hr;
            hr.value = Marshal.GetHRForException(e);
            hr.detailedErrorMessage = e.Message;
            return hr;
        }

        public static HResult FromInt(int i)
        {
            HResult hr;
            hr.value = i;
            hr.detailedErrorMessage = null;
            return hr;
        }

        public static HResult FromErrorText(string errorText)
        {
            HResult hr = HResult.E_FAIL;
            hr.detailedErrorMessage = errorText;
            return hr;
        }

        public static HResult FromErrorText(string errorText, HResult hrOld)
        {
            HResult hr = hrOld;
            hr.detailedErrorMessage = errorText;
            return hr;
        }

        public static implicit operator int(HResult hr)
        {
            return hr.value;
        }

        public static implicit operator HResult(int i)
        {
            HResult hr;
            hr.value = i;
            hr.detailedErrorMessage = null;
            return hr;
        }

        public HResult(int value)
        {
            this.value = value;
            detailedErrorMessage = null;
        }

        public HResult(System.UInt32 value)
        {
            this.value = (int)value;
            detailedErrorMessage = null;
        }

        public string DetailedMessage
        {
            get
            {
                if (detailedErrorMessage == null)
                    detailedErrorMessage = GetErrorMessageFromResource(this.value);

                return detailedErrorMessage;
            }
        }

        public string ErrorCodeAsString
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} (0x{1:X8})", GetErrorCodeName(value), value);
            }
        }

        public HResult Assign(int value)
        {
            this.value = value;
            this.detailedErrorMessage = null;
            return this;
        }

        public HResult Assign(System.UInt32 value)
        {
            this.value = (int)value;
            this.detailedErrorMessage = null;
            return this;
        }

        public bool Failed
        {
            get { return value < 0; }
        }

        public bool Succeeded
        {
            get { return value >= 0; }
        }

        public static bool IsFailed(int i)
        {
            return i < 0;
        }

        public static bool IsSucceeded(int i)
        {
            return i >= 0;
        }

        /// <summary>
        /// Registers error codes and names based on the given type, which can either be an enum or
        /// a class type with public static fields of type HResult.  Each of the fields of the type
        /// are registered as error codes with the corresponding names, and when HResult objects
        /// with those codes are asked for detailed messages, the given resource manager will be
        /// used to look up a string with the name of the field as the key.
        /// </summary>
        /// <param name="type">Either an enum type, or a class type containing public static HResult fields</param>
        /// <param name="resourceManager">Resource manager used to look up detailed messages for these error codes</param>
        public static void RegisterErrorCodes(Type type, ResourceManager resourceManager)
        {
            lock (lockObject)
            {
                if (errorCodes == null)
                {
                    errorCodes = new Dictionary<int, ErrorCodeData>();
                }

                if (type.IsEnum)
                {
                    string[] enumNames = Enum.GetNames(type);
                    Array enumValues = Enum.GetValues(type);

                    Debug.Assert(enumNames.Length == enumValues.Length);
                    for (int i = 0; i < enumNames.Length; i++)
                    {
                        int code = (int)enumValues.GetValue(i);
                        errorCodes[code] = new ErrorCodeData
                        {
                            Code = code,
                            Message = resourceManager == null ? "" : null,
                            Name = enumNames[i],
                            ResourceManager = resourceManager
                        };
                    }
                }
                else
                {
                    FieldInfo[] errorCodeFields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

                    foreach (var f in errorCodeFields)
                    {
                        if (f.FieldType == typeof(HResult))
                        {
                            int code = ((HResult)f.GetValue(null)).value;

                            errorCodes[code] = new ErrorCodeData
                            {
                                Code = code,
                                Message = resourceManager == null ? "" : null,
                                Name = f.Name,
                                ResourceManager = resourceManager
                            };
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(this.detailedErrorMessage))
            {
                return detailedErrorMessage + " (" + ErrorCodeAsString + ")";
            }
            else
            {
                return ErrorCodeAsString;
            }
        }

        static void EnsureLocalErrorCodesRegistered()
        {
            if (!localCodesRegistered)
            {
                lock (lockObject)
                {
                    if (!localCodesRegistered)
                    {
                        RegisterErrorCodes(typeof(HResult), StringResources.ResourceManager);
                        localCodesRegistered = true;
                    }
                }
            }
        }

        static string GetErrorCodeName(int code)
        {
            EnsureLocalErrorCodesRegistered();
            ErrorCodeData errorCodeData;

            if (!errorCodes.TryGetValue(code, out errorCodeData))
            {
                return StringResources.UnrecognizedErrorCode;
            }

            return errorCodeData.Name;
        }

        static string GetErrorMessageFromResource(int code)
        {
            EnsureLocalErrorCodesRegistered();
            ErrorCodeData errorCodeData;

            if (!errorCodes.TryGetValue(code, out errorCodeData))
            {
                return GetSystemErrorString(code);
            }

            if (errorCodeData.Message == null)
            {
                Debug.Assert(errorCodeData.ResourceManager != null);
                errorCodeData.Message = errorCodeData.ResourceManager.GetString(GetErrorCodeName(code)) ?? GetSystemErrorString(code);
            }

            return errorCodeData.Message;
        }

        // Return empty string, if the code is not a system error code.
        static string GetSystemErrorString(int code)
        {
            System.Text.StringBuilder sbMsg = new System.Text.StringBuilder(4096);
            if (Native.FormatMessageW(Native.FORMAT_MESSAGE_FROM_SYSTEM, IntPtr.Zero, code, Native.LANG_USER_DEFAULT, sbMsg, sbMsg.Capacity, IntPtr.Zero) != 0)
            {
                return sbMsg.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        struct ErrorCodeData
        {
            public int Code { get; set; }
            public string Name { get; set; }
            public ResourceManager ResourceManager { get; set; }
            public string Message { get; set; }
        }

        static class Native
        {
            // from header files
            public const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
            public const int LANG_USER_DEFAULT = 0x00000400;

            // the version, the sample is built upon:
            [DllImport("Kernel32.dll", SetLastError = true, CharSet=CharSet.Unicode)]
            public static extern int FormatMessageW(int dwFlags, IntPtr lpSource,
               int dwMessageId, int dwLanguageId, System.Text.StringBuilder lpBuffer,
               int nSize, IntPtr pArguments);

        };
    }
}