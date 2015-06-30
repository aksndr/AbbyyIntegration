using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Timers;

namespace WindowsService
{
    public partial class Service : ServiceBase
    {
        private Timer t1 = null;
        private int step = 0;
        private Logger logger;
        public Service()
        {
            InitializeComponent();
            logger = new Logger();
        }

        protected override void OnStart(string[] args)
        {
            this.t1 = new Timer();
            this.t1.Interval = 30000;
            this.t1.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
            this.t1.Start();
            logger.logInfo("Test service started");
        }

        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            step++;
            logger.logWarn("Test service step: " + step);
        }

        protected override void OnStop()
        {
            t1.Enabled = false;
            logger.logWarn("Test service stopped");
            logger.logError("Test error logging");
        }
    }
}
