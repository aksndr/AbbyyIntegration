using System;
using WindowsService.Models;

namespace WindowsService.Common
{
    interface IRecognitionWorker
    {
        //void addRecords(List<Record> records);
        void setRecord(Record record);
        //void proceedRecords();        
        void proceedRecord(AbbyyRSWrapper abbyyRs);
        void setQueueManager(QueueManager qm);


        void uploadResult(Authentication.OTAuthentication otAuth);
    }
}
