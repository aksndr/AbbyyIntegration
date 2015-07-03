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

using NLog;

namespace WindowsService.Common
{
    class BarcodeWorker : DefaultWorker, IRecognitionWorker
    {
        public BarcodeWorker() { }

        private Dictionary<string, byte[]> recognizedContent = new Dictionary<string, byte[]>();
        private static NLog.Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
               

        //public bool proceedRecord(AbbyyRSWrapper abbyyRs)
        //{
        //    abbyyRs.setTimeout(60000);

        //    List<InputFile[]> inputFiles = getInputFilesFromRecord(this.record.content);
        //    JobDocument[] jds = null;

        //    List<ExportSettings> exportSettingsList = getExportSettings(this.record.workTypeId);            
        //    int i = 0;
        //    foreach (ExportSettings es in exportSettingsList)
        //    {
        //        i++;
        //        foreach (InputFile[] infile in inputFiles)
        //        {
        //            XmlTicket ticket = getManager().getAbbyService().createTicket(es);
        //            if (ticket == null) return false;

        //            ticket.InputFiles = infile;
        //            jds = abbyyRs.processTicket(ticket, this.record, es.workFlowName);
                    
        //            if (jds == null) return false;

        //            if (i<exportSettingsList.Count())
        //            {
        //                //uploadResultonDisc(jd);
        //                inputFiles = getInputFilesFromJobDocuments(jds);
        //            }
        //            else
        //            {
        //                addResultToRecognizedContentList(jds);
        //            }
        //        }
        //    }
        //    return true;                  
        //}

        protected override void addResultToRecognizedContentList(JobDocument[] jds)
        {
            foreach (JobDocument jd in jds)
            {
                String barcode = jd.CustomText;
                byte[] content = jd.OutputDocuments[0].Files[0].FileContents;
                if (recognizedContent.Keys.Contains(barcode))
                {
                    log.Warn("Recognized content contains duplicate barcode result sets."); //TODO: Сделать добавление обоих результатов, на случай добавления как вложений в составной документ
                    continue;
                }
                recognizedContent.Add(barcode, content); 
            }
        }

        public override void uploadResult(OTAuthentication otAuth)
        {
            this.otAuth = otAuth;
            bool versionAdded = false;
            foreach (KeyValuePair<string, byte[]> entry in recognizedContent)
            {
                int targetObjectId = findObjectInOTCSbyBarcode(entry.Key); //TODO: Сделать определение типа - составной\обычный. Для составных дублирующиеся баркоды класть в виде отдельных вложений (или искать по названию файла) для обычных - хз что делать.
                if (targetObjectId ==0 )
                {
                    log.Warn("Object has not been found by barcode: " + entry.Key);
                    //TODO: Сделать добавление распознанного контента во временный объект
                }
                else
                {
                    string contextId = getVersionContext(targetObjectId);
                    if (string.IsNullOrEmpty(contextId)) continue;

                    versionAdded = addVersion(contextId, entry.Value, targetObjectId);
                    if (versionAdded) updateVersionDescription(targetObjectId);                    
                }
            }
            if (versionAdded) updateRecordState(RecordStates.complete, record.objectId); 
        }              

        //private int findObjectInOTCSbyBarcode(string barcode)
        //{
        //    string url = getSettings().findObjectInOTCSbyBarcodeRequestHandlerURL;
        //    OTCSRequestResult r = new OTCSRequestResult();
                        
        //    try
        //    {
        //        url = url + "&barcode=" + barcode;
        //        string reqRes = Utils.makeOTCSRequest(otAuth.AuthenticationToken, url);
        //        r = new JavaScriptSerializer().Deserialize<OTCSRequestResult>(reqRes);
        //    }
        //    catch (Exception ex)
        //    {
        //        string exMessage = ex.Message.ToString();
        //        //TODO: Add error log "Error while making request to OTCS system from service tier. Reason: " + exMessage + " ."); 

        //    }
        //    if (r.ok)
        //    {
        //        if (r.value != null)
        //        {
        //            return (Int32)r.value;
        //        }
        //        else
        //        {
        //            //TODO Add warn log record
        //        }                
        //    }
        //    else
        //    { 
        //        //TODO Add warn log record
        //    }
        //    return 0;
        //}

        private int findObjectInOTCSbyBarcode(string barcode)
        {
            string url = getSettings().findByBcodeRHURL;
            OTCSRequestResult r = new OTCSRequestResult();

            string res;
            try
            {
                url = url + "&barcode=" + barcode;
                res = Utils.makeOTCSRequest(otAuth.AuthenticationToken, url);
            }
            catch (Exception ex)
            {
                log.Error("Exception while making request to OTCS system from service tier.", ex);
                return 0;
            }
            try
            {
                r = new JavaScriptSerializer().Deserialize<OTCSRequestResult>(res);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Exception while Deserialize OTCS request result: {0}", new object[]{res});
                return 0;
            }
            if (r.ok)
            {
                if (r.value != null)
                {
                    return (Int32)r.value;
                }
                else
                {
                    log.Error("OTCS request returned 'NULL', while success marker 'ok' has value equals 'true', while proceeding request to Request Handler: " + url);
                    return 0;
                }
            }
            else
            {
                string s = (r.errMsg != null ? String.Format("OTCS returned error: {0}", r.errMsg) : "OTCS returned error.");
                log.Error(s + " while proceeding request to Request Handler: " + url);
                return 0;
            }            
        }
                     
                
        //test meth
        //private void uploadResultonDisc(JobDocument[] jds)
        //{
        //    foreach (JobDocument jd in jds)
        //    {
        //        OutputDocument od = jd.OutputDocuments[0];
        //        FileContainer file = od.Files[0];
                          
        //        String filename= jd.CustomText + ".tiff";
        //        byte[] content = file.FileContents;
        //        Utils.saveToFileSystem(content, filename);
        //    }
        //}

        protected override List<InputFile[]> getInputFilesFromJobDocuments(JobDocument[] jds)
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
                    
       

    }


}
