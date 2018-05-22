using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace DTM.Test.OMR
{
    public static class Settings
    {
        public static string AssemblyDirectory
        {
            get
            {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);

                return Path.GetDirectoryName(path);
            }
        } 
    }
}