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
        private Dictionary<int, IRecognitionWorker> workers;
        private List<ExportSettings> exportSettingsLibrary = new List<ExportSettings>();
        private Settings settings;
        private AbbyyRSWrapper abbyyRs;
        private bool ready = true;
        private int recognizedContent = 0;

        private static NLog.Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

        private QueueManager(Settings settings)
        {            
            this.workers = new Dictionary<int, IRecognitionWorker>();
            this.abbyyRs = new AbbyyRSWrapper(settings);
            this.settings = settings;
        }

        public static QueueManager getInstance(Settings settings)
        {
            return new QueueManager(settings);            
        }

        public QueueManager loadExportSettings(List<ExportSettings> exportSettingsList)
        {
            //foreach (ExportSettings es in exportSettingsList)
            //{
            //    int esId = es.ID;
            //    this.exportSettingsLibrary.Add(es);
            //}            
            this.exportSettingsLibrary = exportSettingsList;
            if (this.exportSettingsLibrary.Count == 0)
            {
                this.ready = false;
                this.errMsg = "QueueManager does not received any Export Settings.";
            }
            return this;
        }        
        
        public QueueManager putRecordsList(List<Record> recordsList){
            this.recordsList = recordsList;
            if (this.recordsList.Count == 0)
            {
                this.ready = false;
                this.errMsg = "QueueManager does not received any records to proceed recognition.";
            }
            return this;
        }
                

        public Dictionary<int, IRecognitionWorker> getWorkers()
        {
            return workers;
        }

        public QueueManager buildWorkers()
        {
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

        public void uploadResults(OTAuthentication otAuth)
        {
            foreach (KeyValuePair<int, IRecognitionWorker> entry in workers)
            {
                IRecognitionWorker worker = entry.Value;
                worker.uploadResult(otAuth);
            }
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

        public Settings getSettings()
        {
            return this.settings;
        }
    }
}
