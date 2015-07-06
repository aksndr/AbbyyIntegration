using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;

using WindowsService.Authentication;
using WindowsService.Models;

using NLog;

namespace WindowsService.Common
{
    class QueueManager
    {                
        private List<Record> recordsList;
        private Dictionary<int, IRecognitionWorker> workers = new Dictionary<int, IRecognitionWorker>();
        private List<ExportSettings> exportSettingsLibrary = new List<ExportSettings>();
        private OTUtils otUtils;
        private AbbyyRSWrapper abbyyRs;
        private bool ready = true;
        private int recognizedContent = 0;

        private static NLog.Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

        private QueueManager(OTUtils otUtils)
        {
            this.abbyyRs = new AbbyyRSWrapper(otUtils.getSettings());
            this.otUtils = otUtils;
            loadExportSettings();
        }

        public static QueueManager getInstance(OTUtils otUtils)
        {
            return new QueueManager(otUtils);            
        }


        public void loadExportSettings()
        {
           this.exportSettingsLibrary = otUtils.getAllExportSettingsList(); 
            
            if (this.exportSettingsLibrary.Count == 0)
            {
                this.ready = false;
                this.errMsg = "QueueManager does not received any Export Settings.";
            }           
        }        
        
        public void putRecordsList(List<Record> recordsList){
            this.recordsList = recordsList;
            if (this.recordsList.Count == 0)
            {
                this.ready = false;
                this.errMsg = "QueueManager does not received any records to proceed recognition.";
            }           
        }                

        public QueueManager buildWorkers(List<Record> recordsList)
        {
            putRecordsList(recordsList);
            foreach (Record r in this.recordsList)
            {                
                IRecognitionWorker worker;
                if (workers.ContainsKey(r.ID))
                {
                    workers.TryGetValue(r.ID, out worker);
                }
                else
                {
                    worker = createWorker(r.workTypeId);
                    workers.Add(r.ID, worker);
                }
                if (worker != null)
                {
                    worker.setRecord(r);                    
                    worker.setQueueManager(this);
                }
            }
            return this;
            
        }

        private IRecognitionWorker createWorker(int workTypeId)
        {            
            switch (workTypeId)
            {
                case 1 : return new DefaultWorker();
                case 2 : return new BarcodeWorker();
                default: return new DefaultWorker();
            }
        }

        public void doRecognition()
        {
            foreach (KeyValuePair<int, IRecognitionWorker> entry in workers)
            {
                IRecognitionWorker worker = entry.Value;
                if (worker.proceedRecord(this.abbyyRs)) recognizedContent++;
            }
        }

        public bool hasRecognizedContent()
        {
            return (recognizedContent > 0);
        }

        public void uploadResults()
        {
            foreach (KeyValuePair<int, IRecognitionWorker> entry in workers)
            {
                IRecognitionWorker worker = entry.Value;
                worker.uploadResult();
            }
        }

        public Dictionary<int, IRecognitionWorker> getWorkers()
        {
            return workers;
        }

        public AbbyyRSWrapper getAbbyService()
        {            
            return this.abbyyRs;
        }

        public List<ExportSettings> getExportSettingsLibrary()
        {
            return this.exportSettingsLibrary;
        }

        public bool isReady()
        { 
            return this.ready; 
        }

        public string errMsg { get; set; }

        public OTUtils getOTUtils()
        {
            return this.otUtils;
        }

        public Settings getSettings()
        {
            return this.otUtils.getSettings();
        }
    }
}
