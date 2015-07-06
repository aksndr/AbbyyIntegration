using System;
using WindowsService.Models;

namespace WindowsService.Common
{
    interface IRecognitionWorker
    {
        void setRecord(Record record);
        bool proceedRecord(AbbyyRSWrapper abbyyRs);
        void setQueueManager(QueueManager qm);
        void uploadResult();
    }
}
