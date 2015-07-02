using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindowsService.Models;
using WindowsService.RSSoapService;

using NLog;

namespace WindowsService.Common
{
    public class AbbyyRSWrapper
    {

        public AbbyyRSWrapper(Settings settings)
        {
            this.clientObject = new RSSoapService.RSSoapService();
            this.location = settings.abbyyRSLocation;
            this.clientObject.Url = settings.abbyyRSServicesUrl;        
        }

        private RSSoapService.RSSoapService clientObject;
        private static NLog.Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
        private string workFlowName;
        private string location;       

        List<OutputFormatSettings> formats = new List<OutputFormatSettings>();

        
        public void setWorkflow(string workFlowName)
        {
            this.workFlowName = workFlowName;            
        }

        internal void setTimeout(int p)
        {
            this.clientObject.Timeout = p;
        }
        

        //public AbbyyRSWrapper addRecords(List<Record> recordsToProceed)
        //{
        //    List<InputFile> list = new List<InputFile>();
        //    foreach (Record record in recordsToProceed)
        //    {
        //        FileContainer fileContainer = new FileContainer();
        //        fileContainer.FileContents = record.content;

        //        InputFile inputFile = new InputFile();
        //        inputFile.FileData = fileContainer;

        //        list.Add(inputFile);
        //    }
        //    this.inputFiles = list.ToArray();
        //    return this;
        //}

        //public AbbyyRSWrapper addRecord(Record record)
        //{
        //    this.inputFiles = new InputFile[1];

        //    FileContainer fileContainer = new FileContainer();
        //    fileContainer.FileContents = record.content;

        //    InputFile inputFile = new InputFile();
        //    inputFile.FileData = fileContainer;
                        
        //    inputFiles[0] = inputFile;
            
        //    return this;
        //}

        //public void proceedRecordsRecognition()
        //{
        //    this.clientObject.Timeout = 60000;           

        //    XmlTicket ticket = clientObject.CreateTicket(this.location, this.workFlowName);
        //    ticket.ExportParams.Formats = formats.ToArray();
        //    ticket.InputFiles = this.inputFiles;

        //    XmlResult xmlResult = clientObject.ProcessTicket(this.location, this.workFlowName, ticket);
        //    if (xmlResult.IsFailed)
        //    {
        //        Console.WriteLine("Recognition failed");
        //    }
        //    else
        //    {
        //        Console.WriteLine("Recognition passed");
        //    }
        //}

        //public byte[] proceedRecordRecognition()
        //{
        //    this.clientObject.Timeout = 60000;
        //    XmlTicket ticket = clientObject.CreateTicket(this.location, this.workFlowName);
        //    ticket.ExportParams.Formats = formats.ToArray();
        //    ticket.InputFiles = this.inputFiles;

        //    XmlResult xmlResult = clientObject.ProcessTicket(this.location, this.workFlowName, ticket);
        //    if (xmlResult.IsFailed)
        //    {
        //        Console.WriteLine("Recognition failed");
        //        return null;
        //    }
        //    else
        //    {
        //        Console.WriteLine("Recognition passed");
        //        byte[] fileContent = xmlResult.JobDocuments[0].OutputDocuments[0].Files[0].FileContents; //СДЕЛАТЬ НОРМАЛЬНО
        //        return fileContent;
        //    }
        //}

        public XmlTicket createTicket(ExportSettings es)
        {
            try
            {
                XmlTicket ticket = clientObject.CreateTicket(this.location, es.workFlowName);
                List<OutputFormatSettings> formats = new List<OutputFormatSettings>();
                formats.Add(es.getFormat());
                ticket.ExportParams.Formats = formats.ToArray();
                return ticket;
            }
            catch (Exception e)
            {
                log.Error(e, "Exception in method 'createTicket' while trying to create ticket with location: '{0}' and workflow name: '{1}'.", new object[] { this.location, es.workFlowName });
                return null;
            }            
        }
        
        public JobDocument[] processTicket(XmlTicket t, Record r, string workFlowName)
        {            
            try
            {
                XmlResult xmlResult = clientObject.ProcessTicket(this.location, workFlowName, t);
                
                if (xmlResult.IsFailed)
                {
                    log.Error("Recognition failed for object: '{0}', version: '{1}', work type id: '{1}'.", new object[] { r.objectId, r.versionNum, r.workTypeId });
                    return null;                 
                }
                else
                {                    
                    log.Info("Recognition succeed for object: '{0}', version: '{1}', work type id: '{1}'.", new object[] { r.objectId, r.versionNum, r.workTypeId });                    
                    return xmlResult.JobDocuments;
                }
            }
            catch (Exception e)
            {
                log.Error(e, "Exception in method 'processTicket' while trying to recognize object: '{0}', version: '{1}', using work type with id: '{1}'.", new object[] { r.objectId, r.versionNum, r.workTypeId });
                return null;
            }           
           
        }
        public RSSoapService.RSSoapService getClientObject()
        {
            return this.clientObject;
        }
    }
}
