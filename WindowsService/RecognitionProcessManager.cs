using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;

using NLog;

using WindowsService.Common;
using WindowsService.Models;

using WindowsService.Authentication;

namespace WindowsService
{
    class RecognitionProcessManager
    {
        private static Settings settings;
        private static OTUtils otUtils;
        //private static WindowsService.Authentication.OTAuthentication otAuth;
        private static NLog.Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

        private bool ready = false;
                        
        public RecognitionProcessManager()
        {
            log.Info("RecognitionProcessManager init");
            settings = Settings.getSettings();
            this.ready = (settings == null) ? false : true;
            otUtils = OTUtils.init(settings);
        }

        internal void run()
        {
            log.Info("RecognitionProcessManager started.");
                        
            if (!otUtils.auth())
            {
                log.Error("Failed to authenticate.");                
                return;
            }

            List<Record> activeRecords = otUtils.getActiveRecordsList();
            if (activeRecords == null)
            {
                log.Error("Service finished caused by error.");
                return;
            }            
            if (activeRecords.Count <= 0)           
            {
                log.Info("No any records found in ready to proceed recognition state.");
                return;
            }
            log.Info("Got " + activeRecords.Count + " active record(s).");
            if (otUtils.getRecordsContent(activeRecords) > 0)
            {
                proceedRecognition(activeRecords);
            }

            log.Info("RecognitionProcessManager finished.");
        }

        private void proceedRecognition(List<Record> activeRecords)
        {
            QueueManager qm = QueueManager.getInstance(otUtils);

            qm.buildWorkers(activeRecords);

            if (!qm.isReady())
            {
                log.Error(qm.errMsg);
                return; 
            }

            qm.doRecognition();

            if (qm.hasRecognizedContent())
            {
                qm.uploadResults();
            }
        }

        internal bool isReady()
        {
            return ready;
        }


    }
}
