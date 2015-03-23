using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GoodBet
{
    public class GBCommon
    {
        static object reportLock = new object();
        static object logMethodLock = new object();
        static int defaultRetries = 3;
        static LogMethod logMethod = LogMethod.Console; // Log to Console by default

        private static LogMethod CurrentLogMethod
        {
            get { return GBCommon.logMethod; }
        }

        static EventLog eventLog;
        static string dataFolder = ".";

        public static string DataFolder
        {
            get { return GBCommon.dataFolder; }
            set { GBCommon.dataFolder = value; }
        }

        enum LogMethod
        {
            Console,
            EventLog
        }

        #region Logging
        static public void LogInfo(string msg, params object[] args)
        {
            if (logMethod == LogMethod.Console)
            {
                Console.WriteLine(msg, args);
            }
            else if (logMethod == LogMethod.EventLog)
            {
                eventLog.WriteEntry(string.Format(msg, args));
            }
        }

        static public void ChangeToEventLog(string newSource, string newEventLog)
        {
            if (string.IsNullOrEmpty(newSource) || string.IsNullOrEmpty(newEventLog))
            {
                throw new ApplicationException("Wrong event entries");
            }

            lock (logMethodLock)
            {
                if (!System.Diagnostics.EventLog.SourceExists(newSource))
                {
                    System.Diagnostics.EventLog.CreateEventSource(
                        newSource, newEventLog);
                }
                eventLog = new EventLog();
                eventLog.Source = newSource;
                eventLog.Log = newEventLog;
                logMethod = LogMethod.EventLog;
            }
        }

        static public void ChangeToConsole()
        {
            lock(logMethodLock)
            {
                eventLog = null;
                logMethod = LogMethod.Console;
            }
        }
        #endregion


        #region Reporting
        static public void Report(string content, GameType type)
        {
            Report(content, type, defaultRetries);
        }
        static public void Report(string content, GameType type, int retryCount)
        {
            if (retryCount <= 0)
                return;

            string fileName = ConstructReportFileName(type);

            try
            {
                lock (reportLock)
                {
                    using (Stream stream = File.Open(fileName, FileMode.Append))
                    {
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            writer.WriteLine(content);
                        }
                    }
                }
            }
            catch (FileLoadException)
            {
                LogInfo("Retrying for {0}", fileName);
                Report(content, type, retryCount - 1);
            }
        }
        #endregion


        #region Common functions
        /// <summary>
        /// File for one analysis report
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ConstructReportFileName(GameType type)
        {
            string filename = type.ToString() + "-" + DateTime.Today.ToShortDateString().Replace('/', '_') + "-Report" + ".txt";
            string workdir = Path.Combine(DataFolder, type.ToString());
            return Path.Combine(workdir, filename);
        }

        /// <summary>
        /// File for recording odds of a match
        /// </summary>
        /// <param name="type"></param>
        /// <param name="team1"></param>
        /// <param name="team2"></param>
        /// <param name="dateStr"></param>
        /// <returns></returns>
        public static string ConstructRecordFileName(GameType type, string team1, string team2, string dateStr)
        {
            string filename = type.ToString() + "-" + team1 + "-" + team2 + "-" + Convert.ToDateTime(dateStr).ToShortDateString().Replace('/', '_') + ".json";
            string workdir = Path.Combine(DataFolder, type.ToString()); ;
            return Path.Combine(workdir, filename);
        }

        /// <summary>
        /// File for recording report continuation info
        /// </summary>
        /// <param name="gameType"></param>
        /// <returns></returns>
        public static string ConstructReportContinuationFileName(GameType gameType)
        {
            string filename = gameType.ToString() + "-Report.ini";
            string workdir = Path.Combine(DataFolder, gameType.ToString()); ;
            return Path.Combine(workdir, filename);
        }

        /// <summary>
        /// File for recording collection continuation info
        /// </summary>
        /// <param name="gameType"></param>
        /// <returns></returns>
        public static string ConstructCollectContinuationFileName(GameType gameType)
        {
            string filename = gameType.ToString() + "-Collect.ini";
            string workdir = Path.Combine(DataFolder, gameType.ToString()); ;
            return Path.Combine(workdir, filename);
        }
        #endregion


        #region Methods handling continuation information
        public static DateTime ReadContinuationDate(string continuationFile)
        {
            DateTime continuationDate = DateTime.Parse(@"1/1/1980");
            using (Stream stream = File.Open(continuationFile, FileMode.OpenOrCreate))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string content = reader.ReadLine();
                    if (!DateTime.TryParse(content, out continuationDate))
                    {
                        GBCommon.LogInfo("{0} does not exist or no last date found. Will parse all files.", continuationFile);
                    }
                }
            }
            return continuationDate;
        }

        public static void WriteContinuationDate(string continuationFile, DateTime date)
        {
            using (Stream stream = File.Open(continuationFile, FileMode.OpenOrCreate))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine(date);
                }
            }
        }

        public static int ReadContinuationIndex(string continuationFile)
        {
            int continuationIndex = 0;
            using (Stream stream = File.Open(continuationFile, FileMode.OpenOrCreate))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string content = reader.ReadLine();
                    if (!int.TryParse(content, out continuationIndex))
                    {
                        GBCommon.LogInfo("{0} does not exist or no last index found. Will start from 0.", continuationFile);
                    }
                }
            }
            return continuationIndex;
        }

        public static void WriteContinuationIndex(string continuationFile, int index)
        {
            using (Stream stream = File.Open(continuationFile, FileMode.OpenOrCreate))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine(index);
                }
            }
        }


        internal class ThreeWayOddsTypeConverter : CustomCreationConverter<IOdds>
        {
            public override IOdds Create(Type objectType)
            {
                return new ThreeWayOdds();
            }
        }
        

        public static bool Serialize(List<BetItem> items, string fileName)
        {
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                serializer.TypeNameHandling = TypeNameHandling.Auto;
                using (StreamWriter streamWriter = new StreamWriter(fileName, true))
                {
                    using (JsonWriter jsonWriter = new JsonTextWriter(streamWriter))
                    {
                        serializer.Serialize(jsonWriter, items);
                    }
                }
                return true;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Source);
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            return false;
        }


        public static bool Deserialize<T>(out T result, string fileName, OddsType oddsType)
        {
            result = default(T);
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                if (oddsType == OddsType.ThreeWay)
                {
                    serializer.Converters.Add(new ThreeWayOddsTypeConverter());
                }
                using (StreamReader streamReader = new StreamReader(fileName))
                {
                    using (JsonReader jsonReader = new JsonTextReader(streamReader))
                    {
                        result = serializer.Deserialize<T>(jsonReader);
                    }
                }
                return true;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Source);
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            return false;
        }


        public static HttpWebResponse SendRequest(string query, string datastore, string apikey, string passwd, string contentType, string method)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(datastore);
            request.KeepAlive = false;
            request.Credentials = new NetworkCredential(
                apikey,
                passwd
                );
            request.Method = method;
            request.ContentType = contentType;
            using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string json = query;
                streamWriter.Write(json);
            }
            request.GetRequestStream().Close();
            var response = (HttpWebResponse)request.GetResponse();
            return response;
        }

        #endregion
    }

}
