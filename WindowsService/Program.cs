using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace WindowsService
{
    static class Program
    {        
        static void Main()
        {
            //ServiceBase[] ServicesToRun;
            //ServicesToRun = new ServiceBase[] 
            //{ 
            //    new Service() 
            //};
            //ServiceBase.Run(ServicesToRun);       
            RecognitionProcessManager rpm = new RecognitionProcessManager();
            if (rpm.isReady())
                rpm.run();
            
        }
    }
}
