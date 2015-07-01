using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using WindowsService.Models;

namespace WindowsService
{
    public partial class Service : ServiceBase
    {
        private Timer timer = null;        
        
        public static Settings settings;
        public Service()
        {
            InitializeComponent();            
        }

        protected override void OnStart(string[] args)
        {
            this.timer = new Timer();
            this.timer.Interval = 30000;
            this.timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
            this.timer.Start();
            //logger.logInfo("ABBYY Integration service started.");
        }

        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            //step++;
            //logger.logInfo("Test service step: " + step);
            //RecognitionProcessManager rpm = new RecognitionProcessManager();
            //if (rpm.isReady())
            //    rpm.run();

        }

        protected override void OnStop()
        {
            this.timer.Enabled = false;
            //logger.logInfo("ABBYY Integration service stopped.");            
        }
    }
}
