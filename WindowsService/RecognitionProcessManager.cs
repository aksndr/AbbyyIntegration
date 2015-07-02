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
        private static WindowsService.Authentication.OTAuthentication otAuth;
        private static NLog.Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

        private bool ready = false;
        private int l = 0;

        public RecognitionProcessManager()
        {
            log.Info("RecognitionProcessManager init");
            settings = Settings.getSettings();
            if (settings == null) this.ready = false;
        }

        internal void run()
        {
            log.Info("RecognitionProcessManager started.");
            l = settings.getOTLogLevel();
            otAuth = Utils.auth(settings.getLogin(), settings.getPassword());
            if (otAuth.AuthenticationToken == string.Empty || otAuth.AuthenticationToken.Length == 0)
            {
                log.Error("Failed to authenticate.");
                return;
            }
            List<Record> activeRecords = getActiveRecordsList();
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
            if (getRecordsContent(activeRecords)>0)
            {
                proceedRecognition(activeRecords);
            }
            log.Info("RecognitionProcessManager finished.");
        }

        private static List<Record> getActiveRecordsList()
        {
            string url = settings.activeRecordsRHURL;
            OTCSRequestResultTypedList<Record> r = new OTCSRequestResultTypedList<Record>();
            List<Record> listToProceed = new List<Record>();
            string res;
            try
            {
                res = Utils.makeOTCSRequest(otAuth.AuthenticationToken, url);
            }
            catch (Exception ex)
            {
                log.Error("Exception while making request to OTCS system from service tier.", ex);
                return null;
            }
            try
            {
                r = new JavaScriptSerializer().Deserialize<OTCSRequestResultTypedList<Record>>(res);
            }
            catch (Exception ex)
            {
                log.Error("Exception while Deserialize OTCS request result: " + res, ex);
                return null;
            }
            if (r.ok)
            {
                listToProceed.AddRange((List<Record>)r.value);
                return listToProceed;
            }
            else
            {
                string s = (r.errMsg != null ? String.Format("OTCS returned error: {0}", r.errMsg) : "OTCS returned error");
                log.Error(s + " while proceeding request to Request Handler: " + url);
                return null;
            }
        }

        private static int getRecordsContent(List<Record> activeRecords)
        {
            int i = 0;
            foreach (Record record in activeRecords)
            {
                if (Utils.getVersionContent(otAuth, record)) i++;
            }
            log.Info("Loaded content for " + i + " records.");
            return i;            
        }

        private static void proceedRecognition(List<Record> activeRecords)
        {
            QueueManager qm = QueueManager.getInstance(settings);
            List<ExportSettings> exportSettingsList = getAllExportSettingsList();
            
            if (exportSettingsList == null)
            {
                log.Error("Service finished caused by error.");
                return;
            }
            
            if (exportSettingsList.Count == 0)
            {
                log.Error("Service does not received any export settings from OTCS.");
                return;
            }

            qm.loadExportSettings(exportSettingsList).putRecordsList(activeRecords).buildWorkers();

            if (qm.isReady())
            {
                qm.doRecognition();               
            }           

            if (qm.hasRecognizedContent())
            {
                qm.uploadResults(otAuth);
            }            
        }

        private static List<ExportSettings> getAllExportSettingsList()
        {
            OTCSRequestResultTypedList<ExportSettings> r = new OTCSRequestResultTypedList<ExportSettings>();
            List<ExportSettings> exportSettingsList = new List<ExportSettings>();
            string url = settings.exportFormatsRHURL;

            string res;
            try
            {
                res = Utils.makeOTCSRequest(otAuth.AuthenticationToken, url);
            }
            catch (Exception ex)
            {
                log.Error("Exception while making request to OTCS system from service tier.", ex);
                return null;
            }
            try
            {
                r = new JavaScriptSerializer().Deserialize<OTCSRequestResultTypedList<ExportSettings>>(res);
            }
            catch (Exception ex)
            {
                log.Error("Exception while Deserialize OTCS request result: " + res, ex);
                return null;
            }
            if (r.ok)
            {
                exportSettingsList.AddRange((List<ExportSettings>)r.value);
                return exportSettingsList;
            }
            else
            {
                string s = (r.errMsg != null ? String.Format("OTCS returned error: {0}", r.errMsg) : "OTCS returned error.");
                log.Error(s + " while proceeding request to Request Handler: " + url);
                return null;
            }
        }

        internal bool isReady()
        {
            return ready;
        }


    }
}
