using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NLog;
using NLog.Targets;
using NLog.Config;

namespace WindowsService
{
    public class Logger
    {
        private NLog.Logger log;
        public Logger()
        {           
            configureNLogger();
            log = LogManager.GetCurrentClassLogger();
        }

        private void configureNLogger()
        {
            var config = new LoggingConfiguration();                       
            var fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);

            fileTarget.FileName = "${basedir}/log/${shortdate}.log";
            fileTarget.Layout = "${longdate} ${level} ${message}";
            fileTarget.KeepFileOpen = false;
            fileTarget.Encoding = UTF8Encoding.UTF8;
            fileTarget.CreateDirs = true;
            
            fileTarget.ArchiveFileName="${basedir}/archive/{##}.log";
            fileTarget.ArchiveEvery=FileArchivePeriod.Month;
            fileTarget.ArchiveNumbering=ArchiveNumberingMode.Rolling;
            fileTarget.MaxArchiveFiles=12;
            fileTarget.ConcurrentWrites=true;

            var rule = new LoggingRule("*", LogLevel.Info, fileTarget);
            config.LoggingRules.Add(rule);
                        
            LogManager.Configuration = config;
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
