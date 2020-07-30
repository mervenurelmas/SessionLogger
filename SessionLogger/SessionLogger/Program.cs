using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Management;
using Microsoft.Win32;
using System.Net;

namespace SessionLogger
{
    static class Program
    {
        /// <summary>
        /// Uygulamanın ana girdi noktası.
        /// </summary>
        //[STAThread]

        public static string ConnectionString = ConfigurationManager.AppSettings["SqlConnectionString"];
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Microsoft.Win32.SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            Application.Run();
        }

        private static void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            string user_name = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            string time = DateTime.Now.ToString();
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLogon:
                    WriteLog(0);
                    break;
                case SessionSwitchReason.SessionLogoff:
                    WriteLog(1);
                    break;
                case SessionSwitchReason.SessionLock:
                    WriteLog(2);
                    break;
                case SessionSwitchReason.SessionUnlock:
                    WriteLog(3);
                    break;
            }


        }

        private static void WriteLog(int SwichType)
        {
            string Personnel_ID = GetPersonnelID();
            string Computer_ID = GetComputerID();
            string Query = $@"INSERT INTO [SessionLogger].[dbo].[LOGS] VALUES('{Personnel_ID}','{Computer_ID}',GETDATE(),{SwichType})";
            InsertSqlWithQuery(Query);
        }

        private static string GetComputerID()
        {
            string Computer_Name = Dns.GetHostName();
            string macAdress = GetMacAdress();
            string IpAdress = GetIpAdress(Computer_Name);
            string Query = $@"SELECT  [Computer_ID] FROM [SessionLogger].[dbo].[COMPUTERS] WHERE Computer_Mac_Adress='{macAdress}' ";
            var res = ExecuteSqlWithQuery(Query);
            if (string.IsNullOrEmpty(res))
            {
                string ID = Guid.NewGuid().ToString();
                string InsertQuery = $@"INSERT INTO [SessionLogger].[dbo].[COMPUTERS] VALUES ('{ID}','{Computer_Name}','{macAdress}','{IpAdress}')";
                InsertSqlWithQuery(InsertQuery);
                return ID;
                //kayıt yok yeni oluştur.
            }
            else
            {
                return res;
            }
        }

        private static string GetMacAdress()
        {
            ManagementClass manager = new ManagementClass("Win32_NetworkAdapterConfiguration");
            foreach (ManagementObject obj in manager.GetInstances())
            {
                if ((bool)obj["IPEnabled"])
                {
                    return obj["MacAddress"].ToString();
                }
            }

            return String.Empty;
        }

        private static string GetIpAdress(string Computer_Name)
        {
            string ipAdresi = Dns.GetHostByName(Computer_Name).AddressList[0].ToString();
            return ipAdresi;
        }

        private static string GetPersonnelID()
        {
            string User_Name = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            string Query = $@"SELECT  [Personnel_ID] FROM [SessionLogger].[dbo].[PERSONNEL] where User_Name='{User_Name}'";
            var res = ExecuteSqlWithQuery(Query);
            if (string.IsNullOrEmpty(res))
            {
                string ID = Guid.NewGuid().ToString();
                string InsertQuery = $@"INSERT INTO [SessionLogger].[dbo].[PERSONNEL] VALUES ('{ID}','{User_Name}','Name','Surname')";
                InsertSqlWithQuery(InsertQuery);
                return ID;
                //kayıt yok yeni oluştur.
            }
            else
            {
                return res;
            }

            return "";
        }

        private static string ExecuteSqlWithQuery(string Query)
        {
            string tempStr = "";
            SqlConnection sqlConnection = new SqlConnection(ConnectionString);
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(Query, sqlConnection))
            {
                cmd.CommandType = CommandType.Text;
             tempStr= cmd.ExecuteScalar()?.ToString();
            }
            sqlConnection.Close();
            return tempStr;
        }

        private static void InsertSqlWithQuery(string Query)
        {
            SqlConnection sqlConnection = new SqlConnection(ConnectionString);
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(Query, sqlConnection))
            {
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
            }

            sqlConnection.Close();
        }
    }
}
