using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;

using Authentication = WindowsService.Authentication;
using ContentService = WindowsService.ContentService;
using DocumentManagement = WindowsService.DocumentManagement;
using WindowsService.Models;

using NLog;


namespace WindowsService.Common
{
    public sealed class OTUtils
    {
        private static OTUtils instance = null;
        private static readonly object padlock = new object();


        private static NLog.Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
        private static Authentication.OTAuthentication otAuth;
        private Settings settings;
        private int logLevel = 1;

        OTUtils() { }

        public static OTUtils init(Settings s)
        {
            lock (padlock)
            {
                if (instance == null)
                {
                    instance = new OTUtils();
                    instance.settings = s;
                    instance.logLevel = s.getOTLogLevel();
                }
                return instance;
            }
        }

        public bool auth()
        {
            string login = settings.getLogin();
            string password = settings.getPassword();

            if (String.IsNullOrEmpty(login) || String.IsNullOrEmpty(password))
                return false;
            Authentication.AuthenticationClient authClient = null;
            try
            {
                authClient = new Authentication.AuthenticationClient();
                string authToken = authClient.AuthenticateUser(login, password);
                otAuth = new Authentication.OTAuthentication();
                otAuth.AuthenticationToken = authToken;
            }
            catch (Exception e)
            {
                log.Error(e, "Exception while proceeding authentication for user: " + login + ".", null);
                return false;
            }
            finally
            {
                if (authClient != null)
                    authClient.Close();
            }
            return true;
        }

        public string getAuthToken()
        {
            if (otAuth == null) return null;
            if (otAuth.AuthenticationToken == null) return null;

            Authentication.AuthenticationClient authClient = new Authentication.AuthenticationClient();
            try
            {
                string authToken = authClient.RefreshToken(ref otAuth);
                otAuth.AuthenticationToken = authToken;
                return authToken;
            }
            catch (Exception e)
            {
                log.Error(e, "Exception while proceeding authentication.", null);
            }
            finally
            {
                authClient.Close();
            }
            return null;
        }
        
        public string makeOTCSRequest(string requestUrl)
        {
            Stream receiveStream = null;
            string token = this.getAuthToken();
            if (String.IsNullOrEmpty(token)) return null;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUrl);
                request.Method = "GET";
                request.Headers.Add("Cookie", "LLCookie=" + token + ", LLTZCookie=0");

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                receiveStream = response.GetResponseStream();
                Encoding encode = System.Text.Encoding.GetEncoding("utf-8");

                StreamReader readStream = new StreamReader(receiveStream, encode);
                String responseText = readStream.ReadToEnd();
                return responseText;
            }
            catch (Exception e)
            {
                log.Error(e, "Exception while proceeding OTCS request.", null);
                return null;
            }
            finally
            {
                if (receiveStream != null)
                {
                    receiveStream.Close();
                    receiveStream.Dispose();
                }
            }
        }

        public static string makeUnAuthenticatedOTCSRequest(string requestUrl)
        {
            Stream receiveStream = null;
            try
            {
                HttpWebRequest requestLL = (HttpWebRequest)WebRequest.Create(requestUrl);
                requestLL.Method = "GET";

                HttpWebResponse responseLL = null;
                responseLL = (HttpWebResponse)requestLL.GetResponse();
                receiveStream = responseLL.GetResponseStream();
                Encoding encode = System.Text.Encoding.GetEncoding("utf-8");

                StreamReader readStream = new StreamReader(receiveStream, encode);
                String responseText = readStream.ReadToEnd();
                return responseText;
            }
            catch (Exception e)
            {
                log.Error(e, "Exception while proceeding OTCS request.", null);
                return null;
            }
            finally
            {
                if (receiveStream != null)
                {
                    receiveStream.Close();
                    receiveStream.Dispose();
                }
            }
        }


        public T getOTCSValue<T>(string url, Dictionary<string, string> requestParams, bool auth = true)
        {

            if (requestParams != null)
            {
                foreach (var param in requestParams)
                {
                    url += "&" + param.Key + "=" + param.Value;
                }
            }
            return getOTCSValue<T>(url, auth);
        }
        
        public T getOTCSValue<T>(string url, string paramName, string paramValue, bool auth = true)
        {
            url += "&" + paramName + "=" + paramValue;
            return getOTCSValue<T>(url, auth);
        }
        
        public T getOTCSValue<T>(string url, bool auth = true)
        {
            OTCSRequestResultT<T> r = new OTCSRequestResultT<T>();
            string res;
            try
            {
                if (auth)
                {
                    res = makeOTCSRequest(url);
                }
                else
                {
                    res = makeUnAuthenticatedOTCSRequest(url);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, "Exception while making request to OTCS system from service tier to url: {0}", new object[] { url });
                return default(T);
            }
            if (res == null)
            {
                log.Error("Failed to get response from OTCS while proceeding request to Request Handler: " + url);
                return default(T);
            }
            try
            {
                r = new JavaScriptSerializer().Deserialize<OTCSRequestResultT<T>>(res);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Exception while Deserialize OTCS request result: " + res, null);
                return default(T);
            }
            if (r.ok)
            {
                if (r.value != null)
                {
                    return r.value;
                }
                else
                {
                    log.Error("Failed to gt any value from response while proceeding request to Request Handler: " + url);
                    return default(T);
                }
            }
            else
            {
                string s = (r.errMsg != null ? String.Format("OTCS returned error: {0}", r.errMsg) : "OTCS returned error");
                log.Error(s + " while proceeding request to Request Handler: " + url);
                return default(T);
            }
        }


        public static T getOTCSValueUnAuthorized<T>(string url)
        {
            OTCSRequestResultT<T> r = new OTCSRequestResultT<T>();
            string res;
            try
            {
                res = makeUnAuthenticatedOTCSRequest(url);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Exception while making request to OTCS system from service tier to url: {0}", new object[] { url });
                return default(T);
            }
            if (res == null)
            {
                log.Error("Failed to get response from OTCS while proceeding request to Request Handler: " + url);
                return default(T);
            }
            try
            {
                r = new JavaScriptSerializer().Deserialize<OTCSRequestResultT<T>>(res);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Exception while Deserialize OTCS request result: " + res, null);
                return default(T);
            }
            if (r.ok)
            {
                if (r.value != null)
                {
                    return r.value;
                }
                else
                {
                    log.Error("Failed to gt any value from response while proceeding request to Request Handler: " + url);
                    return default(T);
                }
            }
            else
            {
                string s = (r.errMsg != null ? String.Format("OTCS returned error: {0}", r.errMsg) : "OTCS returned error");
                log.Error(s + " while proceeding request to Request Handler: " + url);
                return default(T);
            }
        }


        public bool getVersionContent(Record record)
        {
            string token = this.getAuthToken();
            if (String.IsNullOrEmpty(token)) return false;
            DocumentManagement.DocumentManagementClient docManClient = new DocumentManagement.DocumentManagementClient();
            DocumentManagement.OTAuthentication docManOTAuth = new DocumentManagement.OTAuthentication();
            docManOTAuth.AuthenticationToken = token;

            byte[] content;
            DocumentManagement.Attachment attachment = null;
            try
            {
                attachment = docManClient.GetVersionContents(ref docManOTAuth, record.objectId, record.versionNum);

                if (attachment != null)
                {
                    content = attachment.Contents;
                    record.content = content;
                    record.fileName = attachment.FileName;
                    return true;
                }
                else
                {
                    //TODO: Add error record to log add counter incrementation into table and getVersionContent method 
                    log.Error("Failed to load content for record: " + record.ToString());
                    incrementIterationsCounter(record, "Failed to load content of record.");
                }

            }
            catch (Exception e)
            {
                log.Error(e, "Failed in method getVersionContent while trying to load content for record: " + record.ToString(), null);
            }
            return false;

        }

        public void updateRecordState(RecordStates recordState, int recordId, bool increment = false, string errMsg = "Error text undefined")
        {
            string url = getSettings().updateStateRHURL;

            Dictionary<string, string> requestParams = new Dictionary<string, string>(3);
            requestParams.Add("state", recordState.ToString());
            requestParams.Add("recordId", recordId.ToString());
            requestParams.Add("increment", increment.ToString());

            Int32 updated = getOTCSValue<Int32>(getSettings().updateStateRHURL, requestParams);

            if (updated > 0)
            {
                log.Info("Record with ID = '{0}' state updated to value: '{1}'.", new object[] { recordId, recordState });
            }
            else
            {
                log.Info("Failed to update Record with ID = '{0}' state.", new object[] { recordId });
            }
        }


        public void incrementIterationsCounter(Record record, string errorMessage)
        {
            Dictionary<string, string> requestParams = new Dictionary<string, string>(3);
            requestParams.Add("objectId", record.objectId.ToString());
            requestParams.Add("errMsg", errorMessage);

            Int32 res = getOTCSValue<Int32>(getSettings().incrementIterRHURL, requestParams);
            if (res == 0) log.Error("Failed to increment content for record: " + record.ToString());
        }

        public List<Record> getActiveRecordsList()
        {
            List<Record> listToProceed = new List<Record>();
            listToProceed.AddRange(this.getOTCSValue<List<Record>>(settings.activeRecordsRHURL));
            return listToProceed;
        }

        public int getRecordsContent(List<Record> activeRecords)
        {
            int i = 0;
            foreach (Record record in activeRecords)
            {
                if (getVersionContent(record)) i++;
            }
            log.Info("Loaded content for " + i + " records.");
            return i;
        }

        public List<ExportSettings> getAllExportSettingsList()
        {
            List<ExportSettings> exportSettingsList = new List<ExportSettings>();
            exportSettingsList.AddRange(this.getOTCSValue<List<ExportSettings>>(settings.exportFormatsRHURL));

            return exportSettingsList;
        }

        public Settings getSettings()
        {
            return this.settings;
        }

        //public void logError(string p, Exception ex)
        //{
        //    throw new NotImplementedException();
        //}

        //public void logInfo(string p)
        //{
        //    throw new NotImplementedException();
        //}

        internal void uploadContentInTempObject(Record record, string filename, byte[] content, string workTypeName, string fileExt = "pdf")
        {
            string url = settings.getNewTempObjIdRHURL;
            if (String.IsNullOrEmpty(url))
            {
                log.Error("Setting 'GetNewTempObjectIdRequestHandlerURl' were not defined properly.");
                return;
            }

            Dictionary<string, string> requestParams = new Dictionary<string, string>(3);
            requestParams.Add("recordId", record.ID.ToString());
            requestParams.Add("filename", filename);
            requestParams.Add("fileExt", fileExt);
            requestParams.Add("workTypeName", workTypeName);
            
            //сходить в OTCS через RH, получить айди нового временного документа
            Int32 tempObjectId = getOTCSValue<Int32>(url, requestParams);
            if (tempObjectId == 0)
            {
                log.Error("Failed to get tempObjectId for record: " + record.ToString());
                return;
            }

            //добавить новую версию для этого документа
            string contextId = getVersionContext(tempObjectId);
            if (String.IsNullOrEmpty(contextId))
            {                
                updateRecordState(RecordStates.error, record.ID, true, String.Format("Exception in method 'getVersionContext' while trying to get context for temporary object with id: {0}.", tempObjectId));
                return;
            }

            bool versionAdded = addVersion(record, contextId, content, tempObjectId);
            bool descriptionUpdated;
            if (versionAdded)
            {
                descriptionUpdated = updateVersionDescription(record.fileName, tempObjectId);
            }
            else
            {
                incrementIterationsCounter(record, "Exception in method 'addVersion' while trying to add version for temporary object: " + tempObjectId);
                return;
            }
            if (!descriptionUpdated)
                updateRecordState(RecordStates.warn, record.ID, false, "Exception in method 'updateVersionDescription' while trying to update description for record." );

        }

        internal string getVersionContext(int objectId)
        {
            DocumentManagement.DocumentManagementClient docManClient = new DocumentManagement.DocumentManagementClient();
            DocumentManagement.OTAuthentication docManOTAuth = new DocumentManagement.OTAuthentication();
            docManOTAuth.AuthenticationToken = getAuthToken();

            string contextID = null;
            try
            {
                contextID = docManClient.AddVersionContext(ref docManOTAuth, objectId, null);
            }
            catch (Exception e)
            {
                log.Error(e, "Exception in method 'getVersionContext' while trying to add version context for object: {0}", new object[] { objectId });               
            }
            return contextID;
        }

        internal bool addVersion(Record record, string contextId, byte[] content, int targetObjectId)
        {
            ContentService.ContentServiceClient contentClient = new ContentService.ContentServiceClient();
            ContentService.OTAuthentication contentOTAuth = new ContentService.OTAuthentication();
            contentOTAuth.AuthenticationToken = getAuthToken();

            ContentService.FileAtts fileAtts = Utils.createFileAtts(record, content.Length);
            byte[] recognizedContent = content;
            using (Stream stream = new MemoryStream(recognizedContent))
            {
                try
                {
                    string objectId = contentClient.UploadContent(ref contentOTAuth, contextId, fileAtts, stream);
                    return true;
                }
                catch (Exception e)
                {
                    log.Error(e, "Exception in method 'addVersion' while trying to add version for object: {0}", new object[] { targetObjectId });                    
                    return false;
                }
            }
        }

        internal bool updateVersionDescription(String fileName, int objectId)
        {
            DocumentManagement.DocumentManagementClient docManClient = new DocumentManagement.DocumentManagementClient();
            DocumentManagement.OTAuthentication docManOTAuth = new DocumentManagement.OTAuthentication();
            docManOTAuth.AuthenticationToken = getAuthToken();
            try
            {
                DocumentManagement.MetadataLanguage[] langs = docManClient.GetMetadataLanguages(ref docManOTAuth);
                string[] langsArray = Utils.getFromMetadataLangArray(langs);

                DocumentManagement.Version version = docManClient.GetVersion(ref docManOTAuth, objectId, 0);
                String versionName = version.Filename;
                if (versionName == Utils.replaceFileExtension(fileName, ".pdf"))
                {
                    version.Comment = "Содержимое версии получено из сервиса распознавания ABBYY";
                }

                docManClient.UpdateVersion(ref docManOTAuth, version);
            }
            catch (Exception e)
            {
                log.Error(e, "Exception in method 'updateVersionDescription' while trying to update description for object: {0}", new object[] { objectId });
                return false;
            }
            return true;
        }


        internal string createVersionContext(Record record, string barcode, int objectId)
        {
            DocumentManagement.DocumentManagementClient docManClient = new DocumentManagement.DocumentManagementClient();
            DocumentManagement.OTAuthentication docManOTAuth = new DocumentManagement.OTAuthentication();
            docManOTAuth.AuthenticationToken = getAuthToken();

            string contextID = null;
            string comment = Utils.getRecognizedFileDescription("ru_RU");
            try
            {
                contextID = docManClient.CreateDocumentContext(ref docManOTAuth, objectId, barcode, comment, false, null);
            }
            catch (Exception e)
            {
                log.Error(e, "Exception in method 'createVersionContext' while trying to create document context for object with parent id = '{0}'", new object[] { objectId });                
            } 
            finally
            {
                if (docManClient != null)
                    docManClient.Close();
            }
            return contextID;
        }
    }
}
