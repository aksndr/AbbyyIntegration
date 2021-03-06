﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;

using WindowsService.Models;
using WindowsService.RSSoapService;
using WindowsService.Authentication;

using NLog;

namespace WindowsService.Common
{
    class DefaultWorker : IRecognitionWorker
    {
        public DefaultWorker() 
        {
            this.workerTypeName = "default";
        }

        protected Record record;
        protected List<ExportSettings> exportSettingsList = new List<ExportSettings>(2);        
        protected QueueManager qm;
        public string workerTypeName {get; set;}

        private Dictionary<int, List<byte[]>> recognizedContent = new Dictionary<int, List<byte[]>>();
        private static NLog.Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);        
        
        public void addExportSettings(ExportSettings es)
        {
            int order = es.order;
            if (!exportSettingsList.Contains(es))
            {
                exportSettingsList.Add(es);
                exportSettingsList = exportSettingsList.OrderBy(o => o.order).ToList();
            }
            else
            {
                log.Warn("Found dupliacate export settings for workType with ID: " + es.workTypeId); 
            }
        }

        //public bool proceedRecord(AbbyyRSWrapper abbyyRs)
        //{
        //    abbyyRs.setTimeout(60000);

        //    InputFile[] inputFiles = getInputFilesFromRecord(this.record.content);
        //    JobDocument[] jd = null;

        //    List<ExportSettings> exportSettingsList = getExportSettings(this.record.workTypeId);

        //    foreach (ExportSettings es in exportSettingsList)
        //    {
        //        XmlTicket ticket = getManager().getAbbyService().createTicket(es);
        //        byte[] recognizedContent = null;
        //        ticket.InputFiles = inputFiles;
        //        jd = abbyyRs.processTicket(ticket, this.record, es.workFlowName);

        //        if (jd == null) return false;

        //        recognizedContent = jd[0].OutputDocuments[0].Files[0].FileContents;
        //        inputFiles = getInputFilesFromRecord(recognizedContent);

        //        if (recognizedContent == null) return false;

        //        this.record.content = recognizedContent;
        //        return true;
        //    }
        //}
        public bool proceedRecord(AbbyyRSWrapper abbyyRs)
        {
            abbyyRs.setTimeout(60000);

            List<InputFile[]> inputFiles = getInputFilesFromRecord(this.record.content);
            JobDocument[] jds = null;

            List<ExportSettings> exportSettingsList = getExportSettings(this.record.workTypeId);
            int i = 0;
            foreach (ExportSettings es in exportSettingsList)
            {
                i++;
                foreach (InputFile[] infile in inputFiles)
                {
                    XmlTicket ticket = getManager().getAbbyService().createTicket(es);
                    if (ticket == null) return false;

                    ticket.InputFiles = infile;
                    jds = abbyyRs.processTicket(ticket, this.record, es.workFlowName);

                    if (jds == null)
                    {
                        getManager().getOTUtils().incrementIterationsCounter(record, "ABBYY RS Failed to proceed recognition of record content.");
                        return false;
                    }

                    if (i < exportSettingsList.Count())
                    {                        
                        inputFiles = getInputFilesFromJobDocuments(jds);
                    }
                    else
                    {
                        addResultToRecognizedContentList(jds);
                    }
                }
            }
            return true;
        }

        protected virtual List<InputFile[]> getInputFilesFromJobDocuments(JobDocument[] jds)
        {
            List<InputFile[]> list = new List<InputFile[]>(jds.Count());

            foreach (JobDocument jd in jds)
            {
                List<InputFile> inputFiles = new List<InputFile>();
                OutputDocument od = jd.OutputDocuments[0];
                FileContainer file = od.Files[0];
                InputFile inputFile = new InputFile();
                inputFile.OutputDocuments = jd.OutputDocuments;
                inputFile.Id = jd.CustomText;
                inputFile.FileData = file;
                inputFiles.Add(inputFile);

                list.Add(inputFiles.ToArray());
            }
            return list;
        }

        protected virtual void addResultToRecognizedContentList(JobDocument[] jds)
        {
            foreach (JobDocument jd in jds)
            {
                byte[] content = jd.OutputDocuments[0].Files[0].FileContents;
                if (content == null || content.Length == 0)
                {
                    log.Warn("Recognized content of object " + record.objectId + " is 0 bytes long.");
                    continue;
                }
                if (recognizedContent.Keys.Contains(this.record.objectId))
                {
                    List<byte[]> p = recognizedContent.Single(x => x.Key == record.objectId).Value;
                    p.Add(content);
                }
                else
                {
                    List<byte[]> p = new List<byte[]>();
                    p.Add(content);
                    recognizedContent.Add(record.objectId, p);
                }
            }
        }

        public virtual void uploadResult()
        {            
            bool versionAdded = false;
            foreach (KeyValuePair<int, List<byte[]>> entry in recognizedContent)
            {
                int targetObjectId = entry.Key;

                string contextId = getManager().getOTUtils().getVersionContext(targetObjectId);
                if (string.IsNullOrEmpty(contextId))
                {
                    getManager().getOTUtils().incrementIterationsCounter(record, "Exception in method 'getVersionContext' while trying to add version context for object.");
                    continue;
                }
                foreach (byte[] content in entry.Value)
                {
                    versionAdded = getManager().getOTUtils().addVersion(record, contextId, content, targetObjectId);
                    if (versionAdded)
                    {
                        getManager().getOTUtils().updateVersionDescription(record.fileName, targetObjectId);
                    }
                    else
                    {
                        getManager().getOTUtils().incrementIterationsCounter(record, "Exception in method 'addVersion' while trying to add version for object: " + targetObjectId);
                    }
                }
            }
            if (versionAdded) getManager().getOTUtils().updateRecordState(RecordStates.complete, record.ID);
        }       

        

        //internal bool addVersion(string contextId, byte[] content, int targetObjectId)
        //{
        //    ContentService.ContentServiceClient contentClient = new ContentService.ContentServiceClient();
        //    ContentService.OTAuthentication contentOTAuth = new ContentService.OTAuthentication();
        //    contentOTAuth.AuthenticationToken = getManager().getOTUtils().getAuthToken();

        //    ContentService.FileAtts fileAtts = Utils.createFileAtts(this.record, content.Length);
        //    byte[] recognizedContent = content;
        //    using (Stream stream = new MemoryStream(recognizedContent))
        //    {
        //        try
        //        {
        //            string objectId = contentClient.UploadContent(ref contentOTAuth, contextId, fileAtts, stream);
        //            return true;
        //        }
        //        catch (Exception e)
        //        {
        //            log.Error(e, "Exception in method 'addVersion' while trying to add version for object: {0}", new object[] { targetObjectId });
        //            getManager().getOTUtils().incrementIterationsCounter(record, "Exception in method 'addVersion' while trying to add version: "+e.Message);
        //            return false;
        //        }
        //    }
        //}

        //internal void updateVersionDescription(int objectId)
        //{
        //    DocumentManagement.DocumentManagementClient docManClient = new DocumentManagement.DocumentManagementClient();
        //    DocumentManagement.OTAuthentication docManOTAuth = new DocumentManagement.OTAuthentication();
        //    docManOTAuth.AuthenticationToken = getManager().getOTUtils().getAuthToken();
        //    try
        //    {
        //        DocumentManagement.MetadataLanguage[] langs = docManClient.GetMetadataLanguages(ref docManOTAuth);
        //        string[] langsArray = Utils.getFromMetadataLangArray(langs);

        //        DocumentManagement.Version version = docManClient.GetVersion(ref docManOTAuth, objectId, 0);
        //        String versionName = version.Filename;
        //        if (versionName == Utils.replaceFileExtension(record.fileName, ".pdf"))
        //        {
        //            version.Comment = "Содержимое версии получено из сервиса распознавания ABBYY";
        //        }

        //        docManClient.UpdateVersion(ref docManOTAuth, version);
        //    }
        //    catch (Exception e)
        //    {
        //        log.Error(e, "Exception in method 'updateVersionDescription' while trying to update description for object: {0}", new object[] { objectId });
        //        getManager().getOTUtils().incrementIterationsCounter(record, "Exception in method 'updateVersionDescription' while trying to update description for object: "+e.Message);
        //    }
        //}

        internal List<InputFile[]> getInputFilesFromRecord(byte[] content)
        {
            List<InputFile[]> list = new List<InputFile[]>(1);
            InputFile[] inputFiles = new InputFile[1];

            FileContainer fileContainer = new FileContainer();
            fileContainer.FileContents = content;

            InputFile inputFile = new InputFile();
            inputFile.FileData = fileContainer;

            inputFiles[0] = inputFile;
            list.Add(inputFiles);
            return list;
        }

        //private InputFile[] getInputFilesFromRecord(byte[] content)
        //{
        //    InputFile[] inputFiles = new InputFile[1];

        //    FileContainer fileContainer = new FileContainer();
        //    fileContainer.FileContents = content;

        //    InputFile inputFile = new InputFile();
        //    inputFile.FileData = fileContainer;

        //    inputFiles[0] = inputFile;

        //    return inputFiles;
        //}

        protected virtual InputFile[] getInputFilesFromOutputDocuments(OutputDocument od)
        {
            FileContainer[] files = od.Files;
            List<InputFile> inputFiles = new List<InputFile>(files.Count());

            foreach (FileContainer file in files)
            {
                InputFile inputFile = new InputFile();
                inputFile.FileData = file;
                inputFiles.Add(inputFile);
            }
            return inputFiles.ToArray();
        }

        //private List<XmlTicket> getTicketsForRecord(Record r)
        //{
        //    List<XmlTicket> tickets = new List<XmlTicket>();
        //    List<ExportSettings> exportSettingsList = getExportSettings(r.workTypeId);

        //    exportSettingsList = exportSettingsList.OrderBy(o => o.order).ToList();
        //    foreach (ExportSettings es in exportSettingsList)
        //    {
        //        XmlTicket ticket = getManager().getAbbyService().createTicket(es);
        //        if (ticket == null) return null;
        //        List<OutputFormatSettings> formats = new List<OutputFormatSettings>();
        //        formats.Add(es.getFormat());
        //        ticket.ExportParams.Formats = formats.ToArray();
        //        tickets.Add(ticket);
        //    }
        //    return tickets;
        //}

        internal QueueManager getManager()
        {
            return this.qm;
        }

        protected Settings getSettings()
        {
            return this.getManager().getSettings();
        }

        internal List<ExportSettings> getExportSettings(int workTypeId)
        {
            List<ExportSettings> esList = this.getManager().getExportSettingsLibrary().FindAll(o => o.workTypeId == workTypeId);
            esList = esList.OrderBy(o => o.order).ToList();
            return esList;
        }

        public void setQueueManager(QueueManager qmf)
        {
            this.qm = qmf;
        }

        public void setRecord(Record record)
        {
            this.record = record;
        }
        
    }
}


