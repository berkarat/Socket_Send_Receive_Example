using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Verisoft;

namespace Socket_Send_Receive
{
    class Program
    {
        /// <summary>
        /// berkarat.com SOCKET PROGRAMING & XML SERILIAZE 
        /// </summary>

        public class TestMessage
        {
            public string MERCHANT_ID { get; set; }
            public string KIOSK_ID { get; set; }
            public string DEVICE_ID { get; set; }
            public string APPID { get; set; }
            public int MESSAGETYPE { get; set; }
            public DateTime DATE { get; set; }
            public string KEYDATA { get; set; }
            public string MESSAGE_BODY_ID { get; set; }
            public string MESSAGE_BODY { get; set; }
            public string LABEL { get; set; }
            public string APPNAME { get; set; }
            public string RESPONSE_CODE { get; set; }
            public int COMMANDID { get; set; }
            public string DESCRIPTION { get; set; }
            public string ServiceDescription { get; set; }
            public string PathName { get; set; }
            public string ServiceType { get; set; }
            public string StartMode { get; set; }
            public string State { get; set; }


        }
        static void Main(string[] args)
        {
        }
        #region DECLARES
        static System.Net.IPAddress localAddr = IPAddress.Parse("127.0.0.1");
        static TcpClient serverSocket;
        static byte[] gonderveri_dizi;
        byte[] gelenveri_dizi = new byte[2048];
        TcpListener dinleyici;
        public static bool read_treadstop = true;
        Socket soket;
        Thread write;
        string gelen;
        string autcode = string.Empty;
        string ip;
        private static readonly Random random = new Random();
        private static readonly object syncLock = new object();
        #endregion
        #region METHODS
        private bool SendServerLogXML(TestMessage logMessage, string _BodyLabel, out string ret_errorCode, out string ret_errorDesc)
        {

            //TODO: SendServerLogXML
            bool _value = false;
            ret_errorCode = string.Empty;
            ret_errorDesc = string.Empty;
            string result = string.Empty;
            string errorCode = string.Empty;
            string errorDesc = string.Empty;

            // TODO: SERVER'A GÖNDERİLEN KISIM 
            #region connect server      
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Dgiworks\KioskAppLog");



            if (key != null)
            {
                if (key.GetValue("KioskServerIP").ToString().Length > 0 &&
                    key.GetValue("KioskServerPort").ToString().Length > 0
                    )
                {

                    localAddr = IPAddress.Parse(key.GetValue("KioskServerIP").ToString());
                    IPEndPoint ipe = new IPEndPoint(localAddr, Convert.ToInt32(key.GetValue("KioskServerPort")));
                    serverSocket = new TcpClient(ipe.Address.ToString(), ipe.Port);
                    NetworkStream ns = serverSocket.GetStream();
                    gonderveri_dizi = new byte[1024];
                    gonderveri_dizi = Encoding.ASCII.GetBytes(logMessage.Serialize());


                    if (gonderveri_dizi.Length < 10)
                    {

                    }
                    if (ns.CanWrite)
                    {
                        ns.Write(gonderveri_dizi, 0, gonderveri_dizi.Length);
                        ns.Flush();
                        _value = true;
                    }
                    else
                    {
                        _value = false;
                    }
                    ns.Close();
                }

            }
            #endregion


            Thread.Sleep(1000);
            return _value;
        }
        private void message_read()
        {

            TestMessage message = new TestMessage();

            gelenveri_dizi = new byte[1024];

            dinleyici.Start();
            do
            {
                if (read_treadstop)
                {
                    // TODO: SOKETİ DİNLEMEYE BAŞLADIĞI YER / SOCKET LISTENING START
                    Console.WriteLine("SOCKET LISTENING !!");
                    soket = dinleyici.AcceptSocket();
                    if (soket.Poll(1000, SelectMode.SelectRead))
                    {
                        //TODO: PARSE EDİP LOGA YAZMAYI YENİ BİR THREADLE YAPACAĞIZ !!

                        Thread th_write = new Thread(new ThreadStart(db_write));
                        th_write.IsBackground = true;
                        th_write.Name = "thread_write";
                        th_write.Start();

                        //Thread.Sleep(2000);
                    }
                    else
                    {
                        gelen = "NoReadData";
                        //no receive data
                    }

                    //	Console.WriteLine(gelen); 
                }

            }
            while (read_treadstop);

            //Log("port close...", EventLogEntryType.Information);
            //logyaz("port close...");
            dinleyici.Stop();

        }
        public void db_write()
        {
            string _error = null;
            try
            {
                IPEndPoint remoteIpEndPoint = soket.RemoteEndPoint as IPEndPoint;
                #region write db
                gelenveri_dizi = new byte[2048];
                soket.Receive(gelenveri_dizi, gelenveri_dizi.Length, 0);
                if (gelenveri_dizi[0] != 0)
                {
                    gelen = Encoding.ASCII.GetString(gelenveri_dizi);
                    ip = soket.RemoteEndPoint.ToString();

                    int c = gelen.Length;
                    if (gelen[0] == 0)
                    {
                        Console.WriteLine("ERROR");
                    }
                    autcode = generation_autcode(1, 10, 5).ToString();
                    string[] sonuc = xml_parser(gelen);

                    RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\DgiWorks\\KioskLogServerb");

                    DataSet ds = new DataSet("TimeRanges");
                    //XML ! 
                    XmlSerializer serializer = new XmlSerializer(typeof(TestMessage));
                    using (TextReader reader = new StringReader(gelen))
                    {
                        TestMessage result = (TestMessage)serializer.Deserialize(reader);
                        #region db stored procedure


                        //SqlConnection con = new SqlConnection(key.GetValue("dbconnection").ToString());
                        //con.Open();
                        //if (result.APPID != null)//sp_AppLogInsert
                        //{
                        //    try
                        //    {

                        //        SqlCommand sqlComm = new SqlCommand("sp_AppLogInsert", con);
                        //        //	sqlComm.Parameters.AddWithValue("@APP_NAME", sonuc[8]);
                        //        sqlComm.Parameters.AddWithValue("@KIOSK_ID", result.KIOSK_ID);
                        //        sqlComm.Parameters.AddWithValue("@MERCHANT_ID", result.MERCHANT_ID);
                        //        sqlComm.Parameters.AddWithValue("@APP_ID", result.APPID);
                        //        //sqlComm.Parameters.AddWithValue("@MESSAGETYPE", sonuc[3]);  
                        //        sqlComm.Parameters.AddWithValue("@MESSAGETYPE", result.MESSAGETYPE);
                        //        sqlComm.Parameters.AddWithValue("@DATE", DateTime.Now.AddMinutes(-1));
                        //        sqlComm.Parameters.AddWithValue("@MESSAGEBODYID", result.MESSAGE_BODY_ID);
                        //        sqlComm.Parameters.AddWithValue("@MESSAGEBODY", result.MESSAGE_BODY);
                        //        sqlComm.Parameters.AddWithValue("@INSERTDATE", Convert.ToDateTime(result.DATE));
                        //        sqlComm.Parameters.AddWithValue("@MESSAGELABEL", result.LABEL);
                        //        sqlComm.Parameters.AddWithValue("@KEYDATA", result.KEYDATA);
                        //        sqlComm.CommandType = CommandType.StoredProcedure;
                        //        sqlComm.ExecuteNonQuery();
                        //        //SqlDataAdapter da = new SqlDataAdapter();
                        //        //da.SelectCommand = sqlComm;
                        //        //da.Fill(ds, "InsertTransaction");
                        //        con.Close();
                        //        //Log("Log Insert Success: " + DateTime.Now.ToString(), EventLogEntryType.Information);
                        //        Console.WriteLine("Log Insert Success:  \n" + DateTime.Now.ToString() + "\n APP NAME:" + sonuc[8] + "\n KIOSK ID:" + sonuc[1] + "\n INSERTED TABLE NAME:Vk_AppLog ");
                        //    }
                        //    catch (Exception ex)
                        //    {
                        //        //Log("CONNECTION HATASI : " + con.State.ToString(), EventLogEntryType.Information);
                        //        Console.WriteLine("CONNECTION HATASI : " + con.State.ToString());
                        //        Console.WriteLine("EX Error : " + ex.Message);
                        //        Console.WriteLine("Line 566");
                        //    }
                        //}
                        //else
                        //{
                        //    //sp_LogInsert
                        //    try
                        //    {
                        //        SqlCommand sqlComm = new SqlCommand("sp_LogInsert", con);
                        //        sqlComm.Parameters.AddWithValue("@KIOSK_ID", result.KIOSK_ID);
                        //        sqlComm.Parameters.AddWithValue("@DEVICE_ID", result.DEVICE_ID);
                        //        sqlComm.Parameters.AddWithValue("@MERCHANT_ID", result.MERCHANT_ID);
                        //        sqlComm.Parameters.AddWithValue("@MESSAGETYPE", result.MESSAGETYPE);
                        //        sqlComm.Parameters.AddWithValue("@DATE", DateTime.Now.AddMinutes(-1));
                        //        sqlComm.Parameters.AddWithValue("@MESSAGEBODYID", result.MESSAGE_BODY_ID);
                        //        sqlComm.Parameters.AddWithValue("@MESSAGEBODY", result.MESSAGE_BODY);
                        //        sqlComm.Parameters.AddWithValue("@INSERTDATE", Convert.ToDateTime(result.DATE));
                        //        sqlComm.Parameters.AddWithValue("@MESSAGELABEL", result.LABEL);
                        //        sqlComm.Parameters.AddWithValue("@KEYDATA", result.KEYDATA);
                        //        //sqlComm.Parameters.AddWithValue("@KIOSK_ID", sonuc[1]);
                        //        sqlComm.ExecuteNonQuery();
                        //        //SqlDataAdapter da = new SqlDataAdapter();
                        //        //da.SelectCommand = sqlComm;
                        //        //da.Fill(ds, "InsertTransaction");
                        //        con.Close();
                        //        //Log("Log Insert Success: " + DateTime.Now.ToString(), EventLogEntryType.Information);
                        //        Console.WriteLine("Log Insert Success:  \n" + DateTime.Now.ToString() + "\n APP NAME:" + sonuc[7] + "\n KIOSK ID:" + sonuc[1] + "\n INSERTED TABLE NAME:Vk_Log  ");

                        //    }
                        //    catch (Exception ex)
                        //    {
                        //        //Log("CONNECTION HATASI : " + con.State.ToString(), EventLogEntryType.Information);
                        //        Console.WriteLine("CONNECTION HATASI : " + con.State.ToString());
                        //        Console.WriteLine("EX Error : " + ex.Message);
                        //        Console.WriteLine("Line 566");
                        //    }
                        //}


                        #endregion
                    }
                }

                else
                {

                }
                #endregion

            }
            catch (Exception e)
            {
                string tt = gelen;
                Console.WriteLine(e.Message);
            }
        }
        public static string generation_autcode(int min, int max, int length)
        {
            lock (syncLock)
            { // synchronize

                string _randomnumber = "";
                for (int i = 0; i < length; i++)
                {
                    _randomnumber = _randomnumber + random.Next(min, max).ToString();
                }
                return _randomnumber;
            }
        }
        public string[] xml_parser(string xmlmessage)
        {
            string[] foundvalue;

            if (xmlmessage != null)
            {
                string _MERCHANT_ID = "<MERCHANT_ID>";
                string _KIOSK_ID = "<KIOSK_ID>";
                string _APP_ID = "<APPID>";
                string _MESSAGE_TYPE = "<MESSAGE_TYPE>";
                string _DATE = "<DATE>";
                string _MESSAGE_BODY_ID = "<MESSAGE_BODY_ID>";
                string _MESSAGE_BODY = "<MESSAGE_BODY>";
                string _APP_NAME = "<APP_NAME>";
                string _LABEL = "<LABEL>";
                string _KEYDATA = "<KEY_DATA>";
                string _DEVICE_ID = "<DEVICE_ID>";



                int[,] foundindex = new int[11, 2];
                foundvalue = new string[12];


                #region parse xml message



                if (xmlmessage.Length > 0) xmlmessage = xmlmessage.Replace("&lt;", "<");
                if (xmlmessage.Length > 0) xmlmessage = xmlmessage.Replace("&gt;", ">");

                //MERCHANT_ID
                foundindex[0, 0] = xmlmessage.IndexOf(_MERCHANT_ID) + _MERCHANT_ID.Length;
                foundindex[0, 1] = xmlmessage.IndexOf("</MERCHANT_ID>");
                if (foundindex[0, 1] != -1) foundvalue[0] = xmlmessage.Substring(foundindex[0, 0], foundindex[0, 1] - foundindex[0, 0]);

                //KIOSK_ID
                foundindex[1, 0] = xmlmessage.IndexOf(_KIOSK_ID) + _KIOSK_ID.Length;
                foundindex[1, 1] = xmlmessage.IndexOf("</KIOSK_ID>");
                if (foundindex[1, 1] != -1) foundvalue[1] = xmlmessage.Substring(foundindex[1, 0], foundindex[1, 1] - foundindex[1, 0]);
                //APP_ID

                foundindex[2, 0] = xmlmessage.IndexOf(_APP_ID) + _APP_ID.Length;
                foundindex[2, 1] = xmlmessage.IndexOf("</APPID>");
                if (foundindex[2, 1] != -1) foundvalue[2] = xmlmessage.Substring(foundindex[2, 0], foundindex[2, 1] - foundindex[2, 0]);

                // MESSAGE_TYPE

                foundindex[3, 0] = xmlmessage.IndexOf(_MESSAGE_TYPE) + _MESSAGE_TYPE.Length;
                foundindex[3, 1] = xmlmessage.IndexOf("</MESSAGE_TYPE>");
                if (foundindex[3, 1] != -1) foundvalue[3] = xmlmessage.Substring(foundindex[3, 0], foundindex[3, 1] - foundindex[3, 0]);

                //_DATE
                foundindex[4, 0] = xmlmessage.IndexOf(_DATE) + _DATE.Length;
                foundindex[4, 1] = xmlmessage.IndexOf("</DATE>");
                if (foundindex[4, 1] != -1) foundvalue[4] = xmlmessage.Substring(foundindex[4, 0], foundindex[4, 1] - foundindex[4, 0]);

                //<MESSAGE_BODY_ID>
                foundindex[5, 0] = xmlmessage.IndexOf(_MESSAGE_BODY_ID) + _MESSAGE_BODY_ID.Length;
                foundindex[5, 1] = xmlmessage.IndexOf("</MESSAGE_BODY_ID>");
                if (foundindex[5, 1] != -1) foundvalue[5] = xmlmessage.Substring(foundindex[5, 0], foundindex[5, 1] - foundindex[5, 0]);
                //<MESSAGE_BODY>
                foundindex[6, 0] = xmlmessage.IndexOf(_MESSAGE_BODY) + _MESSAGE_BODY.Length;
                foundindex[6, 1] = xmlmessage.IndexOf("</MESSAGE_BODY>");
                if (foundindex[6, 1] != -1) foundvalue[6] = xmlmessage.Substring(foundindex[6, 0], foundindex[6, 1] - foundindex[6, 0]);


                //< LABEL >
                foundindex[7, 0] = xmlmessage.IndexOf(_LABEL) + _LABEL.Length;
                foundindex[7, 1] = xmlmessage.IndexOf("</LABEL>");
                if (foundindex[7, 1] != -1) foundvalue[7] = xmlmessage.Substring(foundindex[7, 0], foundindex[7, 1] - foundindex[7, 0]);

                //APPNAME

                foundindex[8, 0] = xmlmessage.IndexOf(_APP_NAME) + _APP_NAME.Length;
                foundindex[8, 1] = xmlmessage.IndexOf("</APP_NAME>");
                if (foundindex[8, 1] != -1) foundvalue[8] = xmlmessage.Substring(foundindex[8, 0], foundindex[8, 1] - foundindex[8, 0]);

                //KEYDATA
                foundindex[9, 0] = xmlmessage.IndexOf(_KEYDATA) + _KEYDATA.Length;
                foundindex[9, 1] = xmlmessage.IndexOf("</KEY_DATA>");
                if (foundindex[9, 1] != -1) foundvalue[9] = xmlmessage.Substring(foundindex[9, 0], foundindex[9, 1] - foundindex[9, 0]);
                //<MESSAGE_BODY>
                foundindex[10, 0] = xmlmessage.IndexOf(_DEVICE_ID) + _DEVICE_ID.Length;
                foundindex[10, 1] = xmlmessage.IndexOf("</DEVICE_ID>");
                if (foundindex[10, 1] != -1) foundvalue[10] = xmlmessage.Substring(foundindex[10, 0], foundindex[10, 1] - foundindex[10, 0]);
                #endregion
                return foundvalue;
            }

            else
            {
                return null;
            }
        }
        #endregion
    }
}
