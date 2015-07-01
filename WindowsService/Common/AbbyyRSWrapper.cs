using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindowsService.Models;
using WindowsService.RSSoapService;

namespace WindowsService.Common
{
    public class AbbyyRSWrapper
    {

        public AbbyyRSWrapper(Settings programSettings)
        {
            this.clientObject = new RSSoapService.RSSoapService();
            this.location = programSettings.getAbbyyRSLocation();
            this.clientObject.Url = programSettings.getAbbyyRSServicesUrl();

        
        }

        private RSSoapService.RSSoapService clientObject;
        private string workFlowName;
        private string location;

        //public Settings programSettings;

        List<OutputFormatSettings> formats = new List<OutputFormatSettings>();

        
        public void setWorkflow(string workFlowName)
        {
            this.workFlowName = workFlowName;            
        }

        internal void setTimeout(int p)
        {
            //this.clientObject.Timeout = p;
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
            XmlTicket ticket = clientObject.CreateTicket(this.location, es.workFlowName);
            List<OutputFormatSettings> formats = new List<OutputFormatSettings>();
            formats.Add(es.getFormat());
            ticket.ExportParams.Formats = formats.ToArray();

            return ticket;
        }

        public RSSoapService.RSSoapService getClientObject()
        {
            return this.clientObject;
        }


        public JobDocument[] processTicket(XmlTicket t, Record r, string workFlowName)
        {
            JobDocument[] jds = null;
            try
            {
                XmlResult xmlResult = clientObject.ProcessTicket(this.location, workFlowName, t);
                
                if (xmlResult.IsFailed)
                {
                    //Utils.logError("Recognition failed for object: " +r.objectId+ " version: "+r.versionNum+ " work type id: "+r.workTypeId);
                    Console.WriteLine("Recognition failed");                    
                }
                else
                {
                    jds = xmlResult.JobDocuments;
                    Console.WriteLine("Recognition passed");
                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine("Recognition failed: "+e.Message);  
                //Utils.logError("Recognition failed for object: " + r.objectId + " version: " + r.versionNum + " work type id: " + r.workTypeId + " with exception: "+e.Message);
            }
           
            return jds;
           
        }
    }
}
