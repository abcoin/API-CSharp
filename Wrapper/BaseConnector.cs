using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ABCoinWrapper.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ABCoinWrapper.Wrapper
{
    public class BaseConnector
    {
        private string AbcoinConfigFile = ConfigurationManager.AppSettings.Get("AbcoinConfigFile");

        private string serverIp = ConfigurationManager.AppSettings.Get("serverip");
        private string username = ConfigurationManager.AppSettings.Get("username");
        private string password = ConfigurationManager.AppSettings.Get("password");

        public static TextWriter w;
        private static object _syncObject;

        public BaseConnector(TextWriter w_, object syncObject)
        {
            w = w_;
            _syncObject = syncObject;
            Log("Inited BaseConnector");
        }
        public BaseConnector()
        {
          
        }

        public void ReadConfigOld()
        {
            Log("AbcoinConfigFile is:" + AbcoinConfigFile);
            Log("serverIp is:" + serverIp);
        }

        public void ReadConfig(bool IsServiceAccount)
        {
            if (string.IsNullOrEmpty(AbcoinConfigFile))
            {
                //Log("AbcoinConfigFile is :" + AbcoinConfigFile);
             //   throw new ArgumentException("You have to add a AbcoinConfigFile setting : AbcoinConfigFile");
            }
            string appfolder = @"C:\Users\Administrator\AppData\Roaming\abcoin\abcoin.conf";
            //Log("SpecialFolder path is:" + Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + ".");
            if (!IsServiceAccount)
            {
                appfolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AbcoinConfigFile.Replace(@"%appdata%\", ""));
            }

            //Log("StreamReader path is :" + appfolder);
            StreamReader reader = null;
            try
            {
                reader = new StreamReader(appfolder);
                string line;
                //Log("inside read config");
                while ((line = reader.ReadLine()) != null)
                {
                    string[] pair = line.Split('=');
                    if (pair.Length == 2)
                    {
                        if (pair[0].Trim() == "rpcuser")
                        {
                            username = pair[1].Trim();

                        }
                        if (pair[0].Trim() == "rpcpassword")
                        {

                            password = pair[1].Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ;// Log("read config failed:" + ex.Message);
              
            }
            finally
            {
                //Log("read config closed reader");
                if(reader != null)
                    reader.Close();
            }

            //Log("reading result:");
        
            if (string.IsNullOrEmpty(serverIp))
            {
                //Log("serverIp is :" + serverIp);
             //   throw new ArgumentException("You have to add a server IP setting with key: serverip");
            }
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentException("You have to add a bitcoin qt username setting with key: username");
            }
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("You have to add a bitcoin qt password setting with key: password");
            }
            if(IsServiceAccount)
                Log("Finished Reading connfig.");
          
        }

        public  void Log(string logMessage)
        {
            // only one thread can own this lock, so other threads
            // entering this method will wait here until lock is
            // available.
            lock (_syncObject)
            {
                w.WriteLine("{0} {1} : {2}", DateTime.Now.ToLongTimeString(),
                    DateTime.Now.ToLongDateString(), logMessage);
                // Update the underlying file.
                w.Flush();
            }
        }
        public JObject RequestServer(MethodName methodName)
        {
            return RequestServer(methodName, parameters: null);
        }

        public JObject RequestServer(MethodName methodName, object parameter)
        {
            return RequestServer(methodName, new List<object>() { parameter });
        }

        public JObject RequestServer(MethodName methodName, List<object> parameters)
        {
            var rawRequest = GetRawRequest();

            // basic required info to qt
            JObject joe = new JObject();
            joe.Add(new JProperty("jsonrpc", "1.0"));
            joe.Add(new JProperty("id", "1"));
            joe.Add(new JProperty("method", methodName.ToString()));

            // adds provided paramters

            JArray props = new JArray();
            if (parameters != null && parameters.Any())
            {

                foreach (var parameter in parameters)
                {
                    props.Add(parameter);
                }


            }
            StreamReader streamReader = null;
            joe.Add(new JProperty("params", props));

            // serialize json for the request

            try
            {
                string s = JsonConvert.SerializeObject(joe);
                byte[] byteArray = Encoding.UTF8.GetBytes(s);
                rawRequest.ContentLength = byteArray.Length;
                Stream dataStream = rawRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();


                WebResponse webResponse = rawRequest.GetResponse();

                streamReader = new StreamReader(webResponse.GetResponseStream(), true);

                return (JObject)JsonConvert.DeserializeObject(streamReader.ReadToEnd());
            }
            finally
            {
                if (streamReader != null)
                {
                    streamReader.Close();
                }

            }
            return null;
        }

        private HttpWebRequest GetRawRequest()
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(serverIp);
            webRequest.Credentials = new NetworkCredential(username, password);

            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";

            return webRequest;
        }
    }
}
