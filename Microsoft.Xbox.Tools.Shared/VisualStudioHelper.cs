using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Xbox.Tools.Shared
{
    public static class VisualStudioHelper
    {
        private static readonly string VS2012DTEString = "VisualStudio.DTE.11.0";
        private static readonly int numRetries = 5;

        /// <summary>
        /// Private helper to make the file passed in VS friendly
        /// </summary>
        /// <param name="filePath">Path to file</param>
        /// <returns>Cleaned up path</returns>
        private static string FixPath(string filePath)
        {
            string result = filePath.Trim();
            result = result.Trim('"');
            result = '"' + result + '"'; 
            return result;
        }

        /// <summary>
        /// Opens a file in VS 2012
        /// </summary>
        /// <param name="filePath">Path to file</param>
        /// <param name="lineNumber">Line to jump to</param>
        /// <returns>bool representing if it was able to open VS or not</returns>
        public static bool OpenFileInVS(string filePath, uint lineNumber)
        {
            string vsFilePath = FixPath(filePath);
            bool openCompleted = false; 
            for (int i = 0; i < numRetries; i++)
            {
                if (openCompleted)
                {
                    Debug.WriteLine("Open in VS completed with num of retries = " + i);
                    break;
                }
                openCompleted = OpenFileInVSNoRetry(vsFilePath, lineNumber);
                //Give VS a chance to recover before retrying
                System.Threading.Thread.Sleep(200); 
            }
            return openCompleted;
        }

        private static bool OpenFileInVSNoRetry(string filePath, uint lineNumber)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath.Trim('"')))
            {
                Debug.Assert(false, "Unable to find file " + filePath);
                return false;
            }
            Object obj = null;
            try
            {
                // Attach to existing VS
                try
                {
                    obj = System.Runtime.InteropServices.Marshal.GetActiveObject(VS2012DTEString);
                }
                catch (Exception e)
                {
                    Debug.Write("No instance of VS found " + e.Message);
                }
                
                if (obj == null)
                {
                    // Create a new VS
                    System.Type t = System.Type.GetTypeFromProgID(VS2012DTEString, true);
                    if (t == null)
                    {
                        Debug.WriteLine("Unable to get VS type, might not be installed"); 
                    }
                    obj = Activator.CreateInstance(t, true);
                    if (obj == null)
                    {
                        Debug.WriteLine("Unable to create new instance of VS"); 
                    }
                }

                ExecuteDTECommand(ref obj, new object[]
                        {
                            "File.OpenFile",
                            filePath
                        }); 

                ExecuteDTECommand(ref obj, new object[]
                        {
                            "Edit.GoTo",
                            lineNumber
                        }); 

                // Make sure VS stays open 
                obj.GetType().InvokeMember("UserControl", BindingFlags.Instance | BindingFlags.SetProperty,
                    null, obj, new object[] { true }, CultureInfo.InvariantCulture);

                // Now that we are all set up, display to user
                Object window = obj.GetType().InvokeMember("MainWindow", BindingFlags.GetProperty,
                    null, obj, new object[] { }, CultureInfo.InvariantCulture); 
                window.GetType().InvokeMember("Activate", BindingFlags.InvokeMethod,
                    null, window, new object[] { }, CultureInfo.InvariantCulture);

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Cleanup(ref obj);
                return false;
            }
            Cleanup(ref obj);
            return true; 
        }

        private static void Cleanup(ref object obj)
        {
            Marshal.FinalReleaseComObject(obj);
            obj = null;
        }

        // VS com thread helper. Needed because sometimes VS isnt ready for commands and we have to retry
        // http://msdn.microsoft.com/en-us/library/ms228772.aspx
        private static void ExecuteDTECommand(ref object dteObj, object[] command)
        {
            int i = 0;
            while (i < numRetries)
            {
                try
                {
                    dteObj.GetType().InvokeMember("ExecuteCommand", BindingFlags.InvokeMethod,
                    null, dteObj, command,
                    CultureInfo.InvariantCulture);
                    Debug.WriteLine("Command in VS completed with num of retries = " + i);
                    break;
                }
                catch
                {
                    i++;
                }
                System.Threading.Thread.Sleep(200); 
            }
        }
    }
}
