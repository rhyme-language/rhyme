using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;

namespace Rhyme.C
{
    /// <summary>
    /// Handles C Standard Library
    /// </summary>
    internal static class CRT
    {
        public static string IncludePath { get; private set; }

        static CRT()
        {

            if(Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // Load latest MSVC Windows SDK CRT
                var sdkReg = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Microsoft SDKs\\Windows\\v10.0");
                if(sdkReg == null)
                {
                    Console.WriteLine("Error: Windows SDK can't be located.");
                    Environment.Exit(-1);
                }
                var installationFolder = (string)sdkReg.GetValue("InstallationFolder");
                var version = (string)sdkReg.GetValue("ProductVersion");
                IncludePath = Path.Combine(installationFolder, "Include", version + ".0", "ucrt");
            }
            else if(Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // Load glibc
            }
            else
            {
                Console.WriteLine("Wrong Platform");
                Environment.Exit(-1);
            }
        }
    }
}
