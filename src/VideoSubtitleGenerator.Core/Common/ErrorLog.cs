using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VideoSubtitleGenerator.Core
{
    public class ErrorLog
    {
        protected static string StrLogFilePath = string.Empty;
        protected static string StrEventLogName = "ErrorSample";
        protected static string StrEventInforName = "LogInfor";
        private static StreamWriter _sw;
        public delegate void LogUpdatedEventHandler();
        //private static BusinessDB _db;


        // Declare the event.
        public static event LogUpdatedEventHandler LogUpdatedEvent;

        private static void RaiseLogUpdatedEvent()
        {
            if (LogUpdatedEvent != null)
            {
                LogUpdatedEvent();
            }
        }

        /// <summary>
        /// Setting LogFile path. If the logfile path is null then it will update error info into LogFile.txt under
        /// application directory.
        /// </summary>

        public static string LogFilePath
        {
            set
            {
                StrLogFilePath = value;
            }
            get
            {
                return StrLogFilePath;
            }
        }

        public static string EventLogName
        {
            set
            {
                StrEventLogName = value;
            }
            get
            {
                return StrEventLogName;
            }
        }

        public static string EventLogInfor
        {
            set
            {
                StrEventInforName = value;
            }
            get
            {
                return StrEventInforName;
            }
        }

        /// <summary>
        /// Write error log entry for window event if the bLogType is true. Otherwise, write the log entry to
        /// customized text-based text file
        /// </summary>
        /// <param name="bLogType"></param>
        /// <param name="objException"></param>
        /// <returns>false if the problem persists</returns>
        public static bool ErrorRoutine(bool bLogType, Exception objException)
        {
            try
            {
                //Check whether logging is enabled or not
                bool bLoggingEnabled;
                bLoggingEnabled = CheckLoggingEnabled();

                //Don't process more if the logging 
                if (false == bLoggingEnabled)
                    return true;

                //Write to Windows event log
                if (bLogType)
                {
                    if (!EventLog.SourceExists(EventLogName))
                        EventLog.CreateEventSource(objException.Message, EventLogName);

                    // Inserting into event log
                    EventLog log = new EventLog();
                    log.Source = EventLogName;
                    log.WriteEntry(objException.Message, EventLogEntryType.Error);
                }
                //Custom text-based event log
                else
                {
                    if (true != CustomErrorRoutine(objException))
                        return false;
                }


                RaiseLogUpdatedEvent();

                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// ErrorRoutinLogInfor
        /// </summary>
        /// <param name="strMessage"></param>
        /// <returns></returns>
        public static bool ErrorRoutinLogInfor(string strMessage)
        {
            try
            {
                //Check whether logging is enabled or not
                bool bLoggingEnabled;
                bLoggingEnabled = CheckLoggingEnabled();

                //Don't process more if the logging 
                if (false == bLoggingEnabled)
                    return true;

                //Write to Windows event log
                string strPathName = string.Empty;
                if (StrLogFilePath.Equals(string.Empty))
                {
                    //Get Default log file path "LogFile.txt"
                    strPathName = GetLogFilePath();
                }
                else
                {

                    //If the log file path is not empty but the file is not available it will create it
                    if (false == File.Exists(StrLogFilePath))
                    {
                        if (false == CheckDirectory(StrLogFilePath))
                            return false;

                        FileStream fs = new FileStream(StrLogFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        fs.Close();
                    }
                    strPathName = StrLogFilePath;

                }
                WriteErrorLog(strPathName, new Exception(strMessage), true);
                RaiseLogUpdatedEvent();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// ErrorRoutinInfor
        /// </summary>
        /// <param name="strMessage"></param>
        /// <returns></returns>
        public static bool ErrorRoutinInfor(string strMessage)
        {
            try
            {
                //Check whether logging is enabled or not
                bool bLoggingEnabled;
                bLoggingEnabled = CheckLoggingEnabled();

                //Don't process more if the logging 
                if (false == bLoggingEnabled)
                    return true;

                //Write to Windows event log
                string strPathName = string.Empty;
                if (StrLogFilePath.Equals(string.Empty))
                {
                    //Get Default log file path "LogFile.txt"
                    strPathName = GetLogFilePath();
                }
                else
                {

                    //If the log file path is not empty but the file is not available it will create it
                    if (false == File.Exists(StrLogFilePath))
                    {
                        if (false == CheckDirectory(StrLogFilePath))
                            return false;

                        FileStream fs = new FileStream(StrLogFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        fs.Close();
                    }
                    strPathName = StrLogFilePath;

                }
                WriteInfor(strPathName, new Exception(strMessage), true);
                RaiseLogUpdatedEvent();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// ErrorRoutinRequest
        /// </summary>
        /// <param name="strMessage"></param>
        /// <returns></returns>
        public static bool ErrorRoutinRequest(string strMessage)
        {
            try
            {
                //Check whether logging is enabled or not
                bool bLoggingEnabled;
                bLoggingEnabled = CheckLoggingEnabled();

                //Don't process more if the logging 
                if (false == bLoggingEnabled)
                    return true;

                //Write to Windows event log
                string strPathName = string.Empty;
                if (StrLogFilePath.Equals(string.Empty))
                {
                    //Get Default log file path "LogFile.txt"
                    strPathName = GetLogFilePath();
                }
                else
                {

                    //If the log file path is not empty but the file is not available it will create it
                    if (false == File.Exists(StrLogFilePath))
                    {
                        if (false == CheckDirectory(StrLogFilePath))
                            return false;

                        FileStream fs = new FileStream(StrLogFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        fs.Close();
                    }
                    strPathName = StrLogFilePath;

                }
                WriteInfor(strPathName, new Exception(strMessage), true);
                RaiseLogUpdatedEvent();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// ErrorRoutinRespone
        /// </summary>
        /// <param name="strMessage"></param>
        /// <returns></returns>
        public static bool ErrorRoutinRespone(string strMessage)
        {
            try
            {
                //Check whether logging is enabled or not
                bool bLoggingEnabled;
                bLoggingEnabled = CheckLoggingEnabled();

                //Don't process more if the logging 
                if (false == bLoggingEnabled)
                    return true;

                //Write to Windows event log
                string strPathName = string.Empty;
                if (StrLogFilePath.Equals(string.Empty))
                {
                    //Get Default log file path "LogFile.txt"
                    strPathName = GetLogFilePath();
                }
                else
                {

                    //If the log file path is not empty but the file is not available it will create it
                    if (false == File.Exists(StrLogFilePath))
                    {
                        if (false == CheckDirectory(StrLogFilePath))
                            return false;

                        FileStream fs = new FileStream(StrLogFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        fs.Close();
                    }
                    strPathName = StrLogFilePath;

                }
                WriteInfor(strPathName, new Exception(strMessage), true);
                RaiseLogUpdatedEvent();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// GetLogContents
        /// </summary>
        /// <returns></returns>
        public static string GetLogContents()
        {
            try
            {
                string strFileContent = string.Empty;
                var sr = new StreamReader(GetLogFilePath());
                sr.Read();
                strFileContent = sr.ReadToEnd();
                sr.Close();
                return strFileContent;
            }
            catch (Exception)
            {

                return "";
            }
        }
        /// <summary>
        /// Check Logginstatus config file is exist. If exist read the value set the loggig status
        /// </summary>
        private static bool CheckLoggingEnabled()
        {
            string strLoggingStatusConfig = string.Empty;

            strLoggingStatusConfig = GetLoggingStatusConfigFileName();

            //If it's empty then enable the logging status 
            if (strLoggingStatusConfig.Equals(string.Empty))
            {
                return true;
            }

            //Read the value from xml and set the logging status
            bool bTemp = GetValueFromXml(strLoggingStatusConfig);
            return bTemp;

        }
        /// <summary>
        /// Check the Logginstatus config under debug or release folder. If not exist, check under 
        /// project folder. If not exist again, return empty string
        /// </summary>
        /// <returns>empty string if file not exists</returns>
        private static string GetLoggingStatusConfigFileName()
        {
            string strCheckinBaseDirecotry = AppDomain.CurrentDomain.BaseDirectory + "LoggingStatus.Config";

            if (File.Exists(strCheckinBaseDirecotry))
                return strCheckinBaseDirecotry;

            string strCheckinApplicationDirecotry = GetApplicationPath() + "LoggingStatus.Config";

            if (File.Exists(strCheckinApplicationDirecotry))
                return strCheckinApplicationDirecotry;
            else return string.Empty;

        }
        /// <summary>
        /// Read the xml file and getthe logging status
        /// </summary>
        /// <param name="strXmlPath"></param>
        /// <returns></returns>
        private static bool GetValueFromXml(string strXmlPath)
        {
            try
            {
                //Open a FileStream on the Xml file
                FileStream docIn = new FileStream(strXmlPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                XmlDocument contactDoc = new XmlDocument();
                //Load the Xml Document
                contactDoc.Load(docIn);

                //Get a node
                XmlNodeList UserList = contactDoc.GetElementsByTagName("LoggingEnabled");

                //get the value
                string strGetValue = UserList.Item(0).InnerText.ToString();

                if (strGetValue.Equals("0"))
                    return false;
                else if (strGetValue.Equals("1"))
                    return true;
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// If the LogFile path is empty then, it will write the log entry to LogFile.txt under application directory.
        /// If the LogFile.txt is not availble it will create it
        /// If the Log File path is not empty but the file is not availble it will create it.
        /// </summary>
        /// <param name="objException"></param>
        /// <returns>false if the problem persists</returns>
        private static bool CustomErrorRoutine(Exception objException)
        {
            string strPathName = string.Empty;
            if (StrLogFilePath.Equals(string.Empty))
            {
                //Get Default log file path "LogFile.txt"
                strPathName = GetLogFilePath();
            }
            else
            {

                //If the log file path is not empty but the file is not available it will create it
                if (false == File.Exists(StrLogFilePath))
                {
                    if (false == CheckDirectory(StrLogFilePath))
                        return false;

                    FileStream fs = new FileStream(StrLogFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    fs.Close();
                }
                strPathName = StrLogFilePath;

            }

            bool bReturn = true;
            // write the error log to that text file
            if (true != WriteErrorLog(strPathName, objException))
            {
                bReturn = false;
            }
            return bReturn;
        }

        public static bool ClearLog()
        {
            string sfilepath = GetLogFilePath();
            try
            {
                if (File.Exists(sfilepath))
                {
                    StreamWriter sw = new StreamWriter(sfilepath);
                    sw.Write(string.Empty);
                    sw.Close();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// WriteInfor
        /// </summary>
        /// <param name="strPathName"></param>
        /// <param name="objException"></param>
        /// <param name="isLogInfor"></param>
        /// <returns></returns>
        private static bool WriteInfor(string strPathName, Exception objException, bool isLogInfor = false)
        {
            bool bReturn = false;
            string strException = string.Empty;
            _sw = new StreamWriter(strPathName, true);

            try
            {
                _sw.WriteLine("");
                if (isLogInfor)
                {
                    _sw.WriteLine("Infor		: " + objException.Message.ToString().Trim());
                }
                else
                {
                    _sw.WriteLine("Error		: " + objException.Message.ToString().Trim());
                }
                _sw.WriteLine("-------------------------------------------------------------------");
                _sw.Flush();
                _sw.Close();
                bReturn = true;
            }
            catch (Exception)
            {
                _sw.Close();
                bReturn = false;
            }
            return bReturn;
        }

        /// <summary>
        /// Write Source,method,date,time,computer,error and stack trace information to the text file
        /// </summary>
        /// <param name="strPathName"></param>
        /// <param name="objException"></param>
        /// <returns>false if the problem persists</returns>
        private static bool WriteErrorLog(string strPathName, Exception objException, bool isLogInfor = false)
        {
            bool bReturn = false;
            string strException = string.Empty;
            _sw = new StreamWriter(strPathName, true);

            if (objException.StackTrace == null)
            {
                try
                {
                    _sw.WriteLine("");
                    if (isLogInfor)
                    {
                        _sw.WriteLine("Log Infor		: " + objException.Message.ToString().Trim());
                    }
                    else
                    {
                        _sw.WriteLine("Error		: " + objException.Message.ToString().Trim());
                    }
                    _sw.WriteLine("-------------------------------------------------------------------");
                    _sw.Flush();
                    _sw.Close();
                    bReturn = true;
                }
                catch (Exception)
                {
                    _sw.Close();
                    bReturn = false;
                }

            }
            else
            {
                strException = objException.StackTrace.ToString().Trim();
                try
                {
                    _sw.WriteLine("Source		: " + objException.Source.ToString().Trim());
                    _sw.WriteLine("Method		: " + objException.TargetSite.Name.ToString());
                    _sw.WriteLine("Date		: " + DateTime.Now.ToLongTimeString());
                    _sw.WriteLine("Time		: " + DateTime.Now.ToShortDateString());
                    _sw.WriteLine("Error		: " + objException.Message.ToString().Trim());
                    _sw.WriteLine("Stack Trace	: " + objException.StackTrace.ToString());
                    _sw.WriteLine("-------------------------------------------------------------------");

                    int i = 0;
                    while (objException != null)
                    {
                        i++;
                        if (!string.IsNullOrEmpty(objException.Message))
                            _sw.WriteLine($"Inner Exception Error {i} : {objException.Message.ToString().Trim()} ");
                        if (!string.IsNullOrEmpty(objException.StackTrace))
                            _sw.WriteLine($"Inner Stack Trace {i} : {objException.StackTrace.ToString()} ");

                        // Tiếp tục với InnerException nếu có
                        objException = objException.InnerException;
                    }

                    _sw.Flush();
                    _sw.Close();
                    bReturn = true;
                }
                catch (Exception)
                {
                    _sw.Close();
                    bReturn = false;
                }
            }

            return bReturn;

        }
        /// <summary>
        /// Check the log file in applcation directory. If it is not available, creae it
        /// </summary>
        /// <returns>Log file path</returns>
        private static string GetLogFilePath()
        {
            try
            {
                string strLogFile = "LogFile" + DateTime.Now.ToString("ddMMyyyy") + ".txt";
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string retFilePath = Path.Combine(baseDir, "Log", strLogFile);
                string strPath = !string.IsNullOrEmpty(retFilePath) ? Path.GetDirectoryName(retFilePath) : string.Empty;
                if (!string.IsNullOrEmpty(strPath) && !Directory.Exists(strPath))
                    Directory.CreateDirectory(strPath);

                if (File.Exists(retFilePath))
                    return retFilePath;

                if (false == CheckDirectory(retFilePath))
                    return string.Empty;

                using (new FileStream(retFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))

                    return retFilePath;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        /// <summary>
        /// Create a directory if not exists
        /// </summary>
        /// <param name="strLogPath"></param>
        /// <returns></returns>
        private static bool CheckDirectory(string strLogPath)
        {
            try
            {
                int nFindSlashPos = strLogPath.Trim().LastIndexOf("\\");
                string strDirectoryname = strLogPath.Trim().Substring(0, nFindSlashPos);

                if (false == Directory.Exists(strDirectoryname))
                    Directory.CreateDirectory(strDirectoryname);

                return true;
            }
            catch (Exception)
            {
                return false;

            }
        }

        private static string GetApplicationPath()
        {
            try
            {
                string strBaseDirectory = AppDomain.CurrentDomain.BaseDirectory.ToString();
                int nFirstSlashPos = strBaseDirectory.LastIndexOf("\\");
                string strTemp = string.Empty;

                if (0 < nFirstSlashPos)
                    strTemp = strBaseDirectory.Substring(0, nFirstSlashPos);

                int nSecondSlashPos = strTemp.LastIndexOf("\\");
                string strTempAppPath = string.Empty;
                if (0 < nSecondSlashPos)
                    strTempAppPath = strTemp.Substring(0, nSecondSlashPos);

                string strAppPath = strTempAppPath.Replace("bin", "");
                return strAppPath;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
