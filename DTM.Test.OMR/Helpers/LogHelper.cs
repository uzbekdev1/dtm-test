using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;

namespace DTM.Test.OMR.Helpers
{
    public static class LogHelper
    {
        public static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static LogHelper()
        {
            XmlConfigurator.Configure(new FileInfo(Path.Combine(Settings.AssemblyDirectory, "log4net.config")));
        }

    }
}