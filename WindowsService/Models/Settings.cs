using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;
using NLog;

using WindowsService.Common;

namespace WindowsService.Models
{
    [DataContract]
    public class Settings
    {
        [DataMember]
        public string activeRecordsRequestHandlerURL { get; set; }
        [DataMember]
        public string exportFormatsRequestHandlerURL { get; set; }
        [DataMember]
        public string findObjectInOTCSbyBarcodeRequestHandlerURL { get; set; }
        [DataMember]
        public string abbyyRSServicesUrl { get; set; }
        [DataMember]
        public string abbyyRSLocation { get; set; }
        [DataMember]
        public string login { get; set; }
        [DataMember]
        public string password { get; set; }
        [DataMember]
        public int otLogLevel { get; set; }
                
        public static Settings instance;
        private static readonly NLog.Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

        public Settings()
        {
            activeRecordsRequestHandlerURL = "http://localhost/OTCS/cs.exe?func=abbyyIntegration.getRecordsToProceed";
            exportFormatsRequestHandlerURL = "http://localhost/OTCS/cs.exe?func=abbyyIntegration.getExportFormats";
            abbyyRSServicesUrl = "http://localhost/Recognition4WS/RSSoapService.asmx";
            findObjectInOTCSbyBarcodeRequestHandlerURL = "http://localhost/OTCS/cs.exe?func=abbyyIntegration.getObjectInOTCSbyBarcode";
            abbyyRSLocation = "localhost";
        }

        public static Settings getSettings()        
        {            
            if (instance == null)
            {
                string rhUrl = null;
                try
                {
                    rhUrl = ConfigurationManager.AppSettings["SettingsRequestHandlerURl"];
                    if (String.IsNullOrEmpty(rhUrl))
                    {
                        return instance = null;
                    }
                }
                catch (Exception ex)
                {
                    log.Error("SettingsRequestHandlerURl parameter not found in app settings. Exception: "+ex.Message);
                    return instance = null;
                }                               

                
                log.Info("App setting SettingsRequestHandlerURl value is: " + rhUrl);
                instance = readSettings(rhUrl.ToString());
            }         
                       
            return validateFields();
        }
               

        private static Settings readSettings(string rhUrl)
        {
            OTCSRequestResult r = new OTCSRequestResult();
            try
            {                
                string reqRes = Utils.makeUnAuthenticatedOTCSRequest(rhUrl);
                r = new JavaScriptSerializer().Deserialize<OTCSRequestResult>(reqRes);
            }
            catch (Exception ex)
            {
               log.Error("Exeption while making request to url: " + rhUrl,ex);
               return null;
            }
            
            if (r.ok)
            {
                if (r.value != null)
                {
                    return (Settings)r.value;
                }
                else
                {
                    log.Error("Failed to gt any value from response while proceeding request to Request Handler: " + rhUrl);
                }
            }            
            {
                string s = (r.errMsg != null ? String.Format("OTCS returned error: {0}", r.errMsg) : "OTCS returned error");
                log.Error(s + " while proceeding request to Request Handler: " + rhUrl);
            }
            return null;
        }

        private static Settings validateFields()
        {
            return instance;
        }
    }
}
