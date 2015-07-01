using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

using WindowsService.Common;
using WindowsService.Models;

namespace WindowsService
{
    class RecognitionProcessManager
    {
        public Settings settings;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);       

        private bool ready = false;

        public RecognitionProcessManager()
        {
            Logger lg = new Logger();
            settings = Settings.getSettings();
        }

        internal void run()
        {
            throw new NotImplementedException();
        }


        internal bool isReady()
        {
            return ready;
        }

        
    }
}
