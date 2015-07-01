using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;


namespace WindowsService
{
    public class Logger        
    {
        private static NLog.Logger log;
        //private static readonly log4net.ILog log = log4net.LogManager.GetLogger
    //(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public Logger()
        {
            configureNLogger();
            log = LogManager.GetCurrentClassLogger();
            log.Error("FUCK!");
                    
        }

        private void configureNLogger()
        {
            try
            {
                var config = new NLog.Config.LoggingConfiguration();                
                var fileTarget = new FileTarget();
                config.AddTarget("file", fileTarget);

                fileTarget.FileName = "${basedir}/log/${shortdate}.log";
                fileTarget.Layout = "${longdate} ${level} ${message}";
                fileTarget.KeepFileOpen = false;
                fileTarget.Encoding = UTF8Encoding.UTF8;
                fileTarget.CreateDirs = true;

                fileTarget.ArchiveFileName = "${basedir}/archive/{##}.log";
                fileTarget.ArchiveEvery = FileArchivePeriod.Month;
                fileTarget.ArchiveNumbering = ArchiveNumberingMode.Rolling;
                fileTarget.MaxArchiveFiles = 12;
                fileTarget.ConcurrentWrites = true;

                var rule = new LoggingRule("*", LogLevel.Info, fileTarget);
                config.LoggingRules.Add(rule);

                LogManager.Configuration = config;
            }
            catch(Exception e)
            {
                log.Error("FUCK!" +e.Message);//logError(e.Message);
            }
        }        

        public void logInfo(string text)
        {
            log.Info(text);
        }
        public void logWarn(string text)
        {

            log.Warn(text);                   
        }
        public void logError(string text)
        {
            log.Error(text);
        }
    }
}
