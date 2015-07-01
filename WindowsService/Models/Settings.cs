using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;

using WindowsService.Common;

namespace WindowsService.Models
{
    public class Settings
    {
        private static string activeRecordsRequestHandlerURL;
        private static string exportFormatsRequestHandlerURL;
        private static string findObjectInOTCSbyBarcodeRequestHandlerURL;
        private static string abbyyRSServicesUrl;
        private static string abbyyRSLocation;
                
        public static Settings instance;

        public Settings() { }

        public static Settings getInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Settings();
                }
                return instance;
            }
        }

        public static Settings getSettings()        {
            
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
                    Utils.logError("SettingsRequestHandlerURl parameter not found in app settings. Exception: "+ex.Message);
                    return instance = null;
                }                               

                
                Utils.logInfo("App setting SettingsRequestHandlerURl value is: " + rhUrl);
                instance = readSettings(rhUrl.ToString());
            }
            

            activeRecordsRequestHandlerURL = "http://localhost/OTCS/cs.exe?func=abbyyIntegration.getRecordsToProceed";
            exportFormatsRequestHandlerURL = "http://localhost/OTCS/cs.exe?func=abbyyIntegration.getExportFormats";
            abbyyRSServicesUrl = "http://localhost/Recognition4WS/RSSoapService.asmx";
            findObjectInOTCSbyBarcodeRequestHandlerURL = "http://localhost/OTCS/cs.exe?func=abbyyIntegration.getObjectInOTCSbyBarcode";
            abbyyRSLocation = "localhost";
            return instance;
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
               Utils.logError("Exeption while making request to url: " + rhUrl,ex);
            }
            if (r.ok)
            {
                if (r.value != null)
                {
                    return (Settings)r.value;
                }
                else
                {
                    Utils.logError("Failed to gt any value from response while proceeding request to Request Handler: " + rhUrl);
                }
            }
            else
            {
                Utils.logError("OTCS returned error: " + r.errMsg + "while proceeding request to Request Handler: " + rhUrl);
            }
            return null;
        }


        public string getActiveRecordsRequestHandlerURL()
        {
            return activeRecordsRequestHandlerURL;
        }

        public string getAbbyyRSLocation()
        {
            return abbyyRSLocation;
        }

        public string getAbbyyRSServicesUrl()
        {
            return abbyyRSServicesUrl;
        }

        public string getExportFormatsRequestHandlerURL()
        {
            return exportFormatsRequestHandlerURL;
        }

        internal string getObjectInOTCSbyBarcodeRequestHandlerURL()
        {
            return findObjectInOTCSbyBarcodeRequestHandlerURL;
        }
    }
}
