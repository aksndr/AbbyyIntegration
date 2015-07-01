using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WindowsService.Models;
using WindowsService.RSSoapService;
using WindowsService.Authentication;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;

namespace WindowsService.Common
{
    class BarcodeWorker : DefaultWorker, IRecognitionWorker
    {
        public BarcodeWorker() { }

        private Dictionary<string, byte[]> recognizedContent = new Dictionary<string, byte[]>();
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


        public void proceedRecord(AbbyyRSWrapper abbyyRs)
        {
            abbyyRs.setTimeout(60000);

            List<InputFile[]> inputFiles = getInputFilesFromRecord(this.record.content);
            JobDocument[] jds = null;

            List<ExportSettings> exportSettingsList = getExportSettings(this.record.workTypeId);
            //byte[] recognizedContent = null;
            int i = 0;
            foreach (ExportSettings es in exportSettingsList)
            {
                i++;
                foreach (InputFile[] infile in inputFiles)
                {
                    XmlTicket ticket = getManager().getAbbyService().createTicket(es);

                    ticket.InputFiles = infile;
                    jds = abbyyRs.processTicket(ticket, this.record, es.workFlowName);
                    if (jds != null && i<exportSettingsList.Count())
                    {
                        //uploadResultonDisc(jd);
                        inputFiles = getInputFilesFromJobDocuments(jds);
                    }
                    else
                    {
                        addResultToRecognizedContentList(recognizedContent, jds);
                    }
                }
            }
             //if (jds != null)
             //   {
             //       OutputDocument[] ods = jds[0].OutputDocuments;
             //       foreach (JobDocument jd in jds)
             //       {
             //           String barcode = jd.CustomText;
             //           byte[] content = jd.OutputDocuments[0].Files[0].FileContents;
             //           recognizedContent.Add(barcode, content); //TODO: Сделать проверку на наличие элемента с таким ключём перед добавлением
             //       } 
             //   }                    
        }

        private void addResultToRecognizedContentList(Dictionary<string, byte[]> recognizedContent, JobDocument[] jds)
        {
            foreach (JobDocument jd in jds)
            {
                String barcode = jd.CustomText;
                byte[] content = jd.OutputDocuments[0].Files[0].FileContents;
                if (recognizedContent.Keys.Contains(barcode))
                {
                    //TODO: log warn "Дублирование штрихкода в распознанном содержимом."
                    continue;
                }
                recognizedContent.Add(barcode, content); 
            }
        }
        
        public void uploadResult(OTAuthentication otAuth)
        {
            this.otAuth = otAuth;
            
            foreach (KeyValuePair<string, byte[]> entry in recognizedContent)
            {
                int targetObjectId = findObjectInOTCSbyBarcode(entry.Key);
                if (targetObjectId ==0 )
                {
                    //TODO: log warn "Object has not been found by barcode. "
                    //Сделать добавление распознанного контента во временную таблицу
                }
                else
                {
                    string contextId = getVersionContext(targetObjectId);
                    addVersion(contextId, entry.Value);
                    updateVersionDescription(targetObjectId);
                }
            }                        
        }

        private int findObjectInOTCSbyBarcode(string barcode)
        {
            string url = getManager().settings.getObjectInOTCSbyBarcodeRequestHandlerURL();
            OTCSRequestResult r = new OTCSRequestResult();

            if (String.IsNullOrEmpty(url))
            {
                //TODO: Add error log
                return 0;
            }
            try
            {
                url = url + "&barcode=" + barcode;
                string reqRes = Utils.makeOTCSRequest(otAuth.AuthenticationToken, url);
                r = new JavaScriptSerializer().Deserialize<OTCSRequestResult>(reqRes);
            }
            catch (Exception ex)
            {
                string exMessage = ex.Message.ToString();
                //TODO: Add error log "Error while making request to OTCS system from service tier. Reason: " + exMessage + " ."); 

            }
            if (r.ok)
            {
                if (r.value != null)
                {
                    return (Int32)r.value;
                }
                else
                {
                    //TODO Add warn log record
                }                
            }
            else
            { 
                //TODO Add warn log record
            }
            return 0;
        }



        private string getVersionContext(int objectId)
        {
            DocumentManagement.DocumentManagementClient docManClient = new DocumentManagement.DocumentManagementClient();
            DocumentManagement.OTAuthentication docManOTAuth = new DocumentManagement.OTAuthentication();
            docManOTAuth.AuthenticationToken = otAuth.AuthenticationToken;

            string contextID = docManClient.AddVersionContext(ref docManOTAuth, objectId, null);
            return contextID;            
        }


        private void addVersion(string contextId, byte[] content)
        {
            ContentService.ContentServiceClient contentClient = new ContentService.ContentServiceClient();
            ContentService.OTAuthentication contentOTAuth = new ContentService.OTAuthentication();
            contentOTAuth.AuthenticationToken = otAuth.AuthenticationToken;

            ContentService.FileAtts fileAtts = Utils.createFileAtts(record, content.Length);
            byte[] recognizedContent = content;
            Stream stream = new MemoryStream(recognizedContent);
                        
            string objectId = contentClient.UploadContent(ref contentOTAuth, contextId, fileAtts, stream);

            stream.Close(); //TODO: surround with try catch finally
        }
        



        private List<InputFile[]> getInputFilesFromRecord(byte[] content)
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

        private void uploadResultonDisc(JobDocument[] jds)
        {
            foreach (JobDocument jd in jds)
            {
                OutputDocument od = jd.OutputDocuments[0];
                FileContainer file = od.Files[0];
                          
                String filename= jd.CustomText + ".tiff";
                byte[] content = file.FileContents;
                Utils.saveToFileSystem(content, filename);
            }
        }

        private List<InputFile[]> getInputFilesFromJobDocuments(JobDocument[] jds)
        {
            var vs = jds.Select(jd => jd.CustomText.Substring(0, 7)).Distinct();
            List<JobDocument[]> jdaList = new List<JobDocument[]>(vs.Count());

            foreach (var v in vs)
            {
                IEnumerable<JobDocument> x = jds.Where(j => j.CustomText.StartsWith(v));
                jdaList.Add(x.ToArray());               
            }

            List<InputFile[]> list = new List<InputFile[]>(jdaList.Count());

            foreach (JobDocument[] jda in jdaList)
            {
                List<InputFile> inputFiles = new List<InputFile>();
                foreach (JobDocument jd in jda)
                {
                    OutputDocument od = jd.OutputDocuments[0];
                    FileContainer file = od.Files[0];
                    InputFile inputFile = new InputFile();
                    inputFile.OutputDocuments = jd.OutputDocuments;
                    inputFile.Id = jd.CustomText;
                    inputFile.FileData = file;
                    inputFiles.Add(inputFile);
                }
                list.Add(inputFiles.ToArray());               
            }
            return list;
        }

        //private List<XmlTicket> getTicketsForRecord(Record r)
        //{
        //    List<XmlTicket> tickets = new List<XmlTicket>();
        //    List<ExportSettings> exportSettingsList = getExportSettings(r.workTypeId);

        //    exportSettingsList = exportSettingsList.OrderBy(o => o.order).ToList();
        //    foreach (ExportSettings es in exportSettingsList)
        //    {
        //        XmlTicket ticket = getFactory().getAbbyService().createTicket(es);
        //        List<OutputFormatSettings> formats = new List<OutputFormatSettings>();
        //        formats.Add(es.getFormat());
        //        ticket.ExportParams.Formats = formats.ToArray();
        //        tickets.Add(ticket);
        //    }
        //    return tickets;
        //}                
       

    }


}
