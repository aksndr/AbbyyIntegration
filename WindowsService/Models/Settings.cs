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
    public class Settings
    {
        public string activeRecordsRHURL { get; set; }
        public string exportFormatsRHURL { get; set; }
        public string findByBcodeRHURL { get; set; }
        public string updateStateRHURL { get; set; }
        public string abbyyRSServicesUrl { get; set; }
        public string abbyyRSLocation { get; set; }
        public string otcsHostName { get; set; }
        public string settingsRHURL { get; set; }

        private OTSettings otSettings;
                
        private static Settings instance;        
        private static readonly NLog.Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
        private static bool settingsIsValid = false;
        
        public Settings()
        {
            readAppSettings();
            readOTSettings(); 
        }

        public static Settings getSettings()
        {
            if (instance == null)
            {
                instance = new Settings();
                if (!settingsIsValid) return instance = null;                               
            }
            return instance;
        }

        private void readAppSettings()
        {
            try
            {
                otcsHostName        = ConfigurationManager.AppSettings["OTCSHostName"];
                abbyyRSServicesUrl = ConfigurationManager.AppSettings["AbbyyRSServicesURL"];
                abbyyRSLocation = ConfigurationManager.AppSettings["AbbyyRSSURL"];
            }
            catch (Exception e)
            {
                log.Error(e, "Exception while reading settings fro application config file.", null);
                return;

            }
            if (String.IsNullOrEmpty(otcsHostName) || String.IsNullOrEmpty(abbyyRSServicesUrl) || String.IsNullOrEmpty(abbyyRSLocation))
            {
                string s = "Application parametr '{0}' is not setted properly. Please check configuration file.";                
                if (String.IsNullOrEmpty(otcsHostName))
                    log.Error(s, new object[] { "OTCSHostName" });
                if (String.IsNullOrEmpty(abbyyRSServicesUrl))
                    log.Error(s, new object[] { "AbbyyRSServicesURL" });
                if (String.IsNullOrEmpty(abbyyRSLocation))
                    log.Error(s, new object[] { "AbbyyRSSURL" });
            }
            else
            {
                settingsIsValid = true;

                settingsRHURL       = String.Format(SettingsRequestHandlerURl,otcsHostName);
                activeRecordsRHURL  = String.Format(ActiveRecordsRequestHandlerURl,otcsHostName);
                exportFormatsRHURL  = String.Format(ExportFormatsRequestHandlerURl,otcsHostName);
                findByBcodeRHURL    = String.Format(FindByBarcodeRequestHandlerURl,otcsHostName);
                updateStateRHURL    = String.Format(UpdateRecordStateRequestHandlerURl, otcsHostName);

                logResult();                
            }
        }

        private void logResult()
        {
            string s = "App setting '{0}' value is: '{1}'.";
            log.Info(s, new object[] { "OTCSHostName", otcsHostName });
            log.Info(s, new object[] { "SettingsRequestHandlerURl", settingsRHURL });
            log.Info(s, new object[] { "ActiveRecordsRequestHandlerURl", activeRecordsRHURL });
            log.Info(s, new object[] { "ExportFormatsRequestHandlerURl", exportFormatsRHURL });
            log.Info(s, new object[] { "FindByBarcodeRequestHandlerURl", findByBcodeRHURL });
            log.Info(s, new object[] { "UpdateRecordStateRequestHandlerURl", updateStateRHURL });
            log.Info(s, new object[] { "AbbyyRSServicesURL", abbyyRSServicesUrl });
            log.Info(s, new object[] { "AbbyyRSSURL", abbyyRSLocation });
        }
        

        private void readOTSettings()
        {
            OTCSRequestResultT<OTSettings> r = new OTCSRequestResultT<OTSettings>();            
            string res;
            try
            {
                res = Utils.makeUnAuthenticatedOTCSRequest(settingsRHURL);
            }
            catch (Exception ex)
            {
                log.Error("Exception while making request to OTCS system from service tier to url: " + settingsRHURL, ex);
                settingsIsValid = false;
                return;
            }
            try
            {
                r = new JavaScriptSerializer().Deserialize<OTCSRequestResultT<OTSettings>>(res);
            }
            catch (Exception ex)
            {
                log.Error("Exception while Deserialize OTCS request result: " + res, ex);
                settingsIsValid = false;
                return;
            }
            if (r.ok)
            {
                if (r.value != null)
                {                    
                    this.otSettings = r.value;
                }
                else
                {
                    log.Error("Failed to gt any value from response while proceeding request to Request Handler: " + settingsRHURL);
                    settingsIsValid = false;
                    return;
                }
            }
            else
            {
                string s = (r.errMsg != null ? String.Format("OTCS returned error: {0}", r.errMsg) : "OTCS returned error");
                log.Error(s + " while proceeding request to Request Handler: " + settingsRHURL);
                settingsIsValid = false;
                return;
            }
        }
        
        public string getLogin()
        {
            return otSettings.login;
        }

        public string getPassword()
        {
            string encryptedPass = otSettings.password;
            string decryptedPass = Utils.decryptPass(encryptedPass);
            return decryptedPass;
        }

        public int getOTLogLevel()
        {
            return otSettings.otLogLevel;
        }
        
        public int getBarcodeStartPos() 
        {
            return otSettings.barcodeStartPos;
        }

        public int getBarcodeEndPos()
        {
            return otSettings.barcodeEndPos;
        }

        private const string SettingsRequestHandlerURl = "http://{0}/OTCS/cs.exe?func=abbyyIntegration.getServiceSettings";
        private const string ActiveRecordsRequestHandlerURl="http://{0}/OTCS/cs.exe?func=abbyyIntegration.getRecordsToProceed";
        private const string ExportFormatsRequestHandlerURl = "http://{0}/OTCS/cs.exe?func=abbyyIntegration.getExportFormats";
        private const string FindByBarcodeRequestHandlerURl = "http://{0}/OTCS/cs.exe?func=abbyyIntegration.getObjectInOTCSbyBarcode";
        private const string UpdateRecordStateRequestHandlerURl = "http://{0}/OTCS/cs.exe?func=abbyyIntegration.updateRecordState";            
    }   
    
}
