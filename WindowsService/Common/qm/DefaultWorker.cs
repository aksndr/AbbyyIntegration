using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WindowsService.Models;
using WindowsService.RSSoapService;
using WindowsService.Authentication;

namespace WindowsService.Common
{
    class DefaultWorker : IRecognitionWorker
    {

        public DefaultWorker() { }

        protected Record record;
        protected List<ExportSettings> exportSettingsList = new List<ExportSettings>(2);

        protected Authentication.OTAuthentication otAuth;

        protected QueueManager qmf;

        public void setQueueManager(QueueManager qmf)
        {
            this.qmf = qmf;
        }

        public void setRecord(Record record)
        {
            //addExportSettings(record);
            this.record = record;

        }

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
                Utils.logWarning("Found dupliacate export settings for workType with ID: " + es.workTypeId); //TODO: Actualize
            }
        }

        //private ExportSettings getExportSetting(int workTypeId)
        //{
        //    ExportSettings es;
        //    this.getFactory().getExportSettingsLibrary().TryGetValue(workTypeId, out es);
        //    return es;

        //}

        public void proceedRecord(AbbyyRSWrapper abbyyRs)
        {
            abbyyRs.setTimeout(60000);

            InputFile[] inputFiles = getInputFilesFromRecord(this.record.content);
            JobDocument[] jd = null;

            List<ExportSettings> exportSettingsList = getExportSettings(this.record.workTypeId);

            foreach (ExportSettings es in exportSettingsList)
            {
                XmlTicket ticket = getManager().getAbbyService().createTicket(es);
                byte[] recognizedContent = null;
                ticket.InputFiles = inputFiles;
                jd = abbyyRs.processTicket(ticket, this.record, es.workFlowName);
                if (jd != null)
                {
                    recognizedContent = jd[0].OutputDocuments[0].Files[0].FileContents;
                    inputFiles = getInputFilesFromRecord(recognizedContent);
                }

                if (recognizedContent != null)
                {
                    this.record.content = recognizedContent;
                }
            }
        }

        public void uploadResult(OTAuthentication otAuth)
        {
            this.otAuth = otAuth;
            string contextId = getVersionContext();
            addVersion(contextId);
            updateVersionDescription(record.objectId);
        }



        private string getVersionContext()
        {
            DocumentManagement.DocumentManagementClient docManClient = new DocumentManagement.DocumentManagementClient();
            DocumentManagement.OTAuthentication docManOTAuth = new DocumentManagement.OTAuthentication();
            docManOTAuth.AuthenticationToken = otAuth.AuthenticationToken;

            string contextID = docManClient.AddVersionContext(ref docManOTAuth, record.objectId, null);
            return contextID;
        }


        private void addVersion(string contextID)
        {
            ContentService.ContentServiceClient contentClient = new ContentService.ContentServiceClient();
            ContentService.OTAuthentication contentOTAuth = new ContentService.OTAuthentication();
            contentOTAuth.AuthenticationToken = otAuth.AuthenticationToken;

            ContentService.FileAtts fileAtts = Utils.createFileAtts(record);
            byte[] recognizedContent = record.content;
            Stream stream = new MemoryStream(recognizedContent);

            //Utils.saveToFileSystem(record);

            string objectId = contentClient.UploadContent(ref contentOTAuth, contextID, fileAtts, stream);

            stream.Close(); //TODO surround with try catch finally
        }

        internal void updateVersionDescription(int objectId)
        {
            DocumentManagement.DocumentManagementClient docManClient = new DocumentManagement.DocumentManagementClient();
            DocumentManagement.OTAuthentication docManOTAuth = new DocumentManagement.OTAuthentication();
            docManOTAuth.AuthenticationToken = otAuth.AuthenticationToken;

            DocumentManagement.MetadataLanguage[] langs = docManClient.GetMetadataLanguages(ref docManOTAuth);
            string[] langsArray = Utils.getFromMetadataLangArray(langs);

            DocumentManagement.Version version = docManClient.GetVersion(ref docManOTAuth, objectId, 0);
            String versionName = version.Filename;
            if (versionName == Utils.replaceFileExtension(record.fileName, ".pdf"))
            {
                version.Comment = "Содержимое версии получено из сервиса распознавания ABBYY";
            }

            docManClient.UpdateVersion(ref docManOTAuth, version);
        }

        private InputFile[] getInputFilesFromRecord(byte[] content)
        {
            InputFile[] inputFiles = new InputFile[1];

            FileContainer fileContainer = new FileContainer();
            fileContainer.FileContents = content;

            InputFile inputFile = new InputFile();
            inputFile.FileData = fileContainer;

            inputFiles[0] = inputFile;

            return inputFiles;
        }

        private InputFile[] getInputFilesFromOutputDocuments(OutputDocument od)
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

        private List<XmlTicket> getTicketsForRecord(Record r)
        {
            List<XmlTicket> tickets = new List<XmlTicket>();
            List<ExportSettings> exportSettingsList = getExportSettings(r.workTypeId);

            exportSettingsList = exportSettingsList.OrderBy(o => o.order).ToList();
            foreach (ExportSettings es in exportSettingsList)
            {
                XmlTicket ticket = getManager().getAbbyService().createTicket(es);
                List<OutputFormatSettings> formats = new List<OutputFormatSettings>();
                formats.Add(es.getFormat());
                ticket.ExportParams.Formats = formats.ToArray();
                tickets.Add(ticket);
            }

            return tickets;
        }

        internal QueueManager getManager()
        {
            return this.qmf;
        }

        internal List<ExportSettings> getExportSettings(int workTypeId)
        {
            List<ExportSettings> esList = this.getManager().getExportSettingsLibrary().FindAll(o => o.workTypeId == workTypeId);
            esList = esList.OrderBy(o => o.order).ToList();
            return esList;
        }

    }
}


