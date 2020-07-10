using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;

namespace OIC_Monitoring
{
    class Monitoring
    {

        public static void exectue()
        {
            try
            {
                string username = ConfigurationManager.AppSettings.Get("DHPOWS_username");
                string password = ConfigurationManager.AppSettings.Get("DHPOWS_password"); 

                string usernameHAAD = ConfigurationManager.AppSettings.Get("HAADWS_username"); 
                string passwordHAAD = ConfigurationManager.AppSettings.Get("HAADWS_password");

                int direction = 2;
                int downloadstatus = 1;
                int transactionType = 16;

                Logger.CreateResult();

                //string HAAD_WS = CallHAADSearch(usernameHAAD, passwordHAAD, "", direction, downloadstatus, transactionType, "").ToString();
                Result HAAD_WS = new Result(); HAAD_WS = CallHAADSearch(usernameHAAD, passwordHAAD, "", direction, downloadstatus, transactionType, "", HAAD_WS);

                string OIC_ProviderPortalUICount = OIC_Eauth_ProviderPortal.execute().ToString();
                Result obj_OIC_ProviderPortalUICount = new Result(); obj_OIC_ProviderPortalUICount.result = OIC_ProviderPortalUICount;

                //OICEauthResult obj = new OICEauthResult();

                //obj = GetOICProviderDBStatus(obj);
                Result Provider_DB = new Result(); Provider_DB = GetOICProviderDBStatus(Provider_DB);
                //string OIC_ProviderPortalDB = obj.OIC_ProviderCount.ToString();
                //int OIC_PayerFound = GetOICPayerDBStatus(obj);
                Result Payer_DB = new Result(); Payer_DB.List_transactionID = Provider_DB.List_transactionID; Payer_DB = GetOICPayerDBStatus(Payer_DB);
                //string DifferenceInTransaction = (obj.OIC_ProviderCount - OIC_PayerFound).ToString();

                //string DHPO_WS = CallDHPOSearch(username, password, "", direction, downloadstatus, transactionType, "").ToString();
                Result DHPO_WS = new Result(); DHPO_WS = CallDHPOSearch(username, password, "", direction, downloadstatus, transactionType, "", DHPO_WS);

                //string DHPO_Exceptions = CheckDHPODB().ToString();
                Result DHPO_Exc = new Result(); DHPO_Exc = CheckDHPODB(DHPO_Exc);

                //string PBMM_Notprocessed = CheckPBM_NotProcessedtransactions().ToString();
                Result PBMM_Notprocessed_OIC = new Result(); PBMM_Notprocessed_OIC = CheckPBM_NotProcessedtransactions_OIC(PBMM_Notprocessed_OIC);
                Result PBMM_Notprocessed_General = new Result(); PBMM_Notprocessed_General = CheckPBM_NotProcessedtransactions_General(PBMM_Notprocessed_General);

                //string PBMM_NotPicked = PBMMNotPicked().ToString();
                Result PBMM_NotPicked = new Result(); PBMM_NotPicked = PBMMNotPicked(PBMM_NotPicked);

                //int RulesHit = PBMM_ClinicalEditsWorking();
                Result RulesHit = new Result(); RulesHit = PBMM_ClinicalEditsWorking(RulesHit);

                //string PI_Backlog = CheckPI_PR_NotProcessed().ToString();
                Result PI_Backlog = new Result(); CheckPI_PR_NotProcessed(PI_Backlog);

                Result PBMSynch_Benefit = new Result(); PBMSync_Benefit(PBMSynch_Benefit);
                Result PBMSynch_Group = new Result(); PBMSync_Group(PBMSynch_Group);



                Logger.Info("HAAD WS: " + HAAD_WS.result_count );
                Logger.Info("DHPO WS: " + DHPO_WS.result_count);
                Logger.Info("OIC Provider DB: " + Provider_DB.result_count);
                Logger.Info("OIC Provider UI: " + obj_OIC_ProviderPortalUICount.result_count);
                Logger.Info("PBMM Not picked: " + PBMM_NotPicked.result_count);
                Logger.Info("DHPO DB Exceptions: " + DHPO_Exc.result_count);
                Logger.Info("PI Backlog : " + PI_Backlog.result_count);
                Logger.Info("Difference in Transaction : " + (Payer_DB.result_count - Provider_DB.result_count).ToString());
                Logger.Info("PBMM OIC pending : " + PBMM_Notprocessed_OIC.result_count);
                Logger.Info("PBMM General pending : " + PBMM_Notprocessed_General.result_count);
                Logger.Info("RulesHit : " + RulesHit.result_count);

                Logger.Info("PBMSynch_Benefit : " + PBMSynch_Benefit.result_count);
                Logger.Info("PBMSynch_Group : " + PBMSynch_Group.result_count);


                //Logger.CreateResult(HAAD_WS + "|" + DHPO_WS + "|" + OIC_ProviderPortalDB + "|" + OIC_ProviderPortalUICount + "|" + PBMM_NotPicked + "|" + DHPO_Exceptions + "|" + PI_Backlog + "|" + DifferenceInTransaction + "|" + PBMM_Notprocessed + "|" + RulesHit + "\n");
                ChecktoSendEmail(HAAD_WS, DHPO_WS, Provider_DB, obj_OIC_ProviderPortalUICount, PBMM_NotPicked,DHPO_Exc, PI_Backlog,
                    Payer_DB, PBMM_Notprocessed_OIC,PBMM_Notprocessed_General, RulesHit,
                    PBMSynch_Benefit, PBMSynch_Group);


                Console.WriteLine("program complete");

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        private static void ChecktoSendEmail(Result HAAD_WS, Result DHPO_WS, Result OIC_ProviderDB, Result OIC_ProviderPortalUICount, 
            Result PBMM_NotPicked, Result DHPO_Exceptions, Result PI_Backlog, Result DifferenceInTransaction, Result PBMM_Notprocessed_OIC,
            Result PBMM_Notprocessed_General,Result RulesHit,
            Result PBMSynch_Benefit,Result PBMSynch_Group)
        {
            try
            {

                int Threshold_HaadWS = int.Parse(ConfigurationManager.AppSettings.Get("Threshold_HaadWS"));
                int Threshold_DHPOWS = int.Parse(ConfigurationManager.AppSettings.Get("Threshold_DHPOWS"));
                int Threshold_OICeAuthProvider = int.Parse(ConfigurationManager.AppSettings.Get("Threshold_OICeAuthProvider"));
                int Threshold_OICeAuthProviderPortal = int.Parse(ConfigurationManager.AppSettings.Get("Threshold_OICeAuthProviderPortal"));
                int Threshold_PBMM_NotPicked = int.Parse(ConfigurationManager.AppSettings.Get("Threshold_PBMM_NotPicked"));
                int Threshold_DHPO_Exceptions = int.Parse(ConfigurationManager.AppSettings.Get("Threshold_DHPO_Exceptions"));
                int Threshold_PI_Backlog = int.Parse(ConfigurationManager.AppSettings.Get("Threshold_PI_Backlog"));
                int Threshold_DifferenceInTransaction = int.Parse(ConfigurationManager.AppSettings.Get("Threshold_DifferenceInTransaction"));
                int Threshold_PBMM_Notprocessed_OIC = int.Parse(ConfigurationManager.AppSettings.Get("Threshold_PBMM_Notprocessed_OIC"));
                int Threshold_PBMM_Notprocessed_General = int.Parse(ConfigurationManager.AppSettings.Get("Threshold_PBMM_Notprocessed_General"));
                int Threshold_ClinicialEdits_minimum = int.Parse(ConfigurationManager.AppSettings.Get("Threshold_ClinicialEdits_minimum"));

                int Threshold_PBMSynch_Benefit = int.Parse(ConfigurationManager.AppSettings.Get("Threshold_PBMSynch_Benefit"));
                int Threshold_PBMSynch_Group = int.Parse(ConfigurationManager.AppSettings.Get("Threshold_PBMSynch_Group"));





                if (HAAD_WS.result_count > Threshold_HaadWS)
                {
                    Logger.Info("Sending email for HAAD WS");
                    HAAD_WS.threshold = Threshold_HaadWS;
                    HAAD_WS.email_subject = "[OIC Alert] HAAD WS PR Breached";
                    HAAD_WS.email_body = Emailyfier(HAAD_WS, "transactions that are pending in DOH PO", "restart the eauth downloader service running on server 10.162.176.128 or restart the PBMM App1 & App2");
                    SendEmail.execute_process(HAAD_WS.email_subject, HAAD_WS.email_body);
                    //SendEmail.execute_process("[OIC Alert] HAAD WS PR Breached", "Hey!!\n\n\n\n Call L2 and Inform L1 & OIC\n\n\nThere are " + int.Parse(HAAD_WS) + " transactions pending\n\n\n Call Faisal/Michael  to restart the eauth downloader service running on server 10.162.176.128 ");
                }

                if (DHPO_WS.result_count > Threshold_DHPOWS)
                {
                    Logger.Info("Sending email for DHPO WS");

                    DHPO_WS.threshold = Threshold_DHPOWS;
                    DHPO_WS.email_subject = "[OIC Alert] DHPO WS PR Breached";
                    DHPO_WS.email_body = Emailyfier(DHPO_WS, "transactions that are pending in DHPO PO", "restart the eauth downloader service running on server 10.162.176.128 or restart the PBMM App1 & App2");
                    SendEmail.execute_process(DHPO_WS.email_subject, DHPO_WS.email_body);
                    //SendEmail.execute_process("[OIC Alert] DHPO WS PR Breached", "Hey!!\n\n\n\n Call L2 and Inform L1 & OIC\n\n\nThere are " + int.Parse(DHPO_WS) + " transactions pending\n\n\n Call Faisal/Michael  to restart the eauth downloader service running on server 10.162.176.128 ");
                }

                if (OIC_ProviderDB.result_count > Threshold_OICeAuthProvider)
                {
                    Logger.Info("Sending email for Provide DB");

                    OIC_ProviderDB.threshold = Threshold_OICeAuthProvider;
                    OIC_ProviderDB.email_subject = "[OIC Alert] Eauth DB PR Database Breached";
                    OIC_ProviderDB.email_body = Emailyfier(OIC_ProviderDB, "transactions that are pending in the par.Tameen Database.", "Check the DB logs if there are any exceptions");
                    SendEmail.execute_process(OIC_ProviderDB.email_subject, OIC_ProviderDB.email_body);
                    //SendEmail.execute_process("[OIC Alert] Eauth DB PR Database Breached", "Hey!!\n\n\n\n Call L2 and Inform L1 & OIC\n\n\nThere are " + int.Parse(OIC_ProviderPortal) + " transactions pending \n\n\n Call Faisal/Shadi to check the DB logs");
                }

                if (OIC_ProviderPortalUICount.result_count > Threshold_OICeAuthProviderPortal)
                {
                    Logger.Info("Sending email for Provider UI");

                    OIC_ProviderPortalUICount.threshold = Threshold_OICeAuthProviderPortal;
                    OIC_ProviderPortalUICount.email_subject = "[OIC Alert] PR UI Breached";
                    OIC_ProviderPortalUICount.email_body = Emailyfier(OIC_ProviderPortalUICount, "transactions that are pending in the par.Tameen UI", "Try reaching the UI and check the logs if there are excptions or restart IIS");
                    SendEmail.execute_process(OIC_ProviderPortalUICount.email_subject, OIC_ProviderPortalUICount.email_body);
                    //SendEmail.execute_process("[OIC Alert] PR UI Breached", "Hey!!\n\n\n\n Call L2 and Inform L1 & OIC\n\n\nThere are " + int.Parse(OIC_ProviderPortalUICount) + " transactions pending\n\n\n Call Faisal/Michael  to restart the eauth downloader service running on server 10.162.176.128 ");
                }

                if (PBMM_NotPicked.result_count > Threshold_PBMM_NotPicked)
                {
                    Logger.Info("Sending email for PBM Not picked");

                    PBMM_NotPicked.threshold = Threshold_PBMM_NotPicked;
                    PBMM_NotPicked.email_subject = "[General Alert] PR PBMM not Picked";
                    PBMM_NotPicked.email_body = Emailyfier(PBMM_NotPicked,"transactions that are not yet PICKED by PI script","Check if the scripts are running on PBMM DB Server and if they are being transferred successfully to PI server");
                    SendEmail.execute_process(PBMM_NotPicked.email_subject, PBMM_NotPicked.email_body);
                    //SendEmail.execute_process("[OIC Alert] PR PBMM not Picked", "Hey!!\n\n\n\n Call OIC if everything fine with the WS\n\n\nThere are " + int.Parse(PBMM_NotPicked) + " transactions that are not picked yet\n\n\n call Faisal/Fahad to check the payerintegration service from both server 10.162.176.85");
                }

                if (DHPO_Exceptions.result_count > Threshold_DHPO_Exceptions)
                {
                    Logger.Info("Sending email for DHPO Exception");

                    DHPO_Exceptions.threshold = Threshold_DHPO_Exceptions;
                    DHPO_Exceptions.email_subject = "[General Alert] DHPO Exceptions Breached";
                    DHPO_Exceptions.email_body = Emailyfier(DHPO_Exceptions, "exceptions that have been found in DHPO","See if the logs addresses to a particular issue, else restart the SQLService of DHPO Proudciton Server");
                    SendEmail.execute_process(DHPO_Exceptions.email_subject, DHPO_Exceptions.email_body);
                    //SendEmail.execute_process("[OIC Alert] DHPO Exceptions Breached", "Hey!!\n\n\n\n Call L2 and Inform L1 & OIC\n\n\nThere are " + int.Parse(DHPO_Exceptions) + " exceptions encountered in the DHPO Database\n\n\nCall Faisal/Shareef/Haidar to restart the SQL Service");
                }

                if (PI_Backlog.result_count > Threshold_PI_Backlog)
                {
                    Logger.Info("Sending email for PI Backlog");

                    PI_Backlog.threshold = Threshold_PI_Backlog;
                    PI_Backlog.email_subject = "[OIC Alert] PayerIntegration Backlog Breached";
                    PI_Backlog.email_body = Emailyfier(PI_Backlog, "transactions that have not been processed by OIC", "Inform OIC for this backlog to check if they are able to reach the WS for processing or any other issue");
                    SendEmail.execute_process(PI_Backlog.email_subject, PI_Backlog.email_body);
                    //SendEmail.execute_process("[OIC Alert] PayerIntegration Backlog Breached", "Hey!!\n\n\n\n Call L2 and Inform L1 & OIC\n\n\nThere are " + int.Parse(PI_Backlog) + " PR transaction not yet processed by OIC\n\n\nCall Faisal/Rana Abu Hayla");
                }

                if (DifferenceInTransaction.result_count > Threshold_DifferenceInTransaction)
                {
                    Logger.Info("Sending email for Difference in PR");

                    DifferenceInTransaction.threshold = Threshold_DifferenceInTransaction;
                    DifferenceInTransaction.email_body = "[OIC Alert] Payer Authorization Difference Breached";
                    DifferenceInTransaction.email_subject = Emailyfier(DifferenceInTransaction, "transactions that have not yet reached the OIC Payer system for authorization", "Check DHPO Not-Downloaded PR pages - if its more than 2 then restart the eauth downloader service running on server 10.162.176.128 else check with L3 for possible causes");
                    SendEmail.execute_process(DifferenceInTransaction.email_subject, DifferenceInTransaction.email_body);
                    //SendEmail.execute_process("[OIC Alert] Payer Authorization Difference Breached", "Hey!!\n\n\n\n Call L2 and Inform L1 & OIC\n\n\nThere are " + int.Parse(DifferenceInTransaction) + " transaction missing from the PAYER system\n\n\n Call Faisal/Michael  to restart the eauth downloader service running on server 10.162.176.128 ");
                }

                if (PBMM_Notprocessed_OIC.result_count > Threshold_PBMM_Notprocessed_OIC)
                {
                    Logger.Info("Sending email for PBM OIC pending");

                    PBMM_Notprocessed_OIC.threshold = Threshold_PBMM_Notprocessed_OIC;
                    PBMM_Notprocessed_OIC.email_body = "[OIC Alert] PBMM delay in processing transaction";
                    PBMM_Notprocessed_OIC.email_subject = Emailyfier(PBMM_Notprocessed_OIC, "transactions that are not yet processed by PBMM", "Check if they are processed in PBMLINK or PBMAP, try restarting App 1 & 2 along with MW 1 & 2 according to the Sharepoint uploaded");
                    SendEmail.execute_process(PBMM_Notprocessed_OIC.email_subject, PBMM_Notprocessed_OIC.email_body);
                    //SendEmail.execute_process("[OIC Alert] PBMM delay in processing transaction", "Hey!!\n\n\n\n Call L2 and Inform L1 & OIC\n\n\nThere are " + int.Parse(PBMM_Notprocessed) + " transaction still not processed in PBMM\n\n\n Call Faisal/Michael  to restart the App1 (PBMSwitchApp-01-10.162.176.98 - Admin-Account) App2 (PBMSwitchApp-02-10.162.176.89-LocalAdmin) Middleware 1 and 2 on 10.162.176.184 run this command /etc/init.d/AIMS-MiddleWare1-service stop ");
                }

                if (PBMM_Notprocessed_General.result_count > Threshold_PBMM_Notprocessed_General)
                {
                    Logger.Info("Sending email for PBM Pending");

                    PBMM_Notprocessed_General.threshold = Threshold_PBMM_Notprocessed_General;
                    PBMM_Notprocessed_General.email_body = "[General Alert] PBMM delay in processing transaction";
                    PBMM_Notprocessed_General.email_subject = Emailyfier(PBMM_Notprocessed_General, "transactions that are not yet processed by PBMM", "Check if they are processed in PBMLINK or PBMAP, try restarting App 1 & 2 along with MW 1 & 2 according to the Sharepoint uploaded");
                    SendEmail.execute_process(PBMM_Notprocessed_General.email_subject, PBMM_Notprocessed_General.email_body);
                    //SendEmail.execute_process("[General Alert] PBMM delay in processing transaction", "Hey!!\n\n\n\n Call L2 and Inform L1 & OIC\n\n\nThere are " + int.Parse(PBMM_Notprocessed) + " transaction still not processed in PBMM\n\n\n Call Faisal/Michael  to restart the App1 (PBMSwitchApp-01-10.162.176.98 - Admin-Account) App2 (PBMSwitchApp-02-10.162.176.89-LocalAdmin) Middleware 1 and 2 on 10.162.176.184 run this command /etc/init.d/AIMS-MiddleWare1-service stop ");
                }

                if (RulesHit.result_count < Threshold_ClinicialEdits_minimum)
                {
                    Logger.Info("Sending email for Clinical Edits");
                    RulesHit.threshold = Threshold_ClinicialEdits_minimum;
                    RulesHit.email_subject = "[OIC Alert] Clinicial Edit Rule";
                    RulesHit.email_body = Emailyfier(RulesHit, "clinical rejections only as per last day in PBMM", "Check if the CEED service is working and rejections are placed accordingly");
                    SendEmail.execute_process(RulesHit.email_subject, RulesHit.email_body);
                    //SendEmail.execute_process("[OIC Alert] Clinicial Edit Rule", "Hey!!\n\n\n\nYesterday's PBMSwitch rejection due to Clinical Rules were only " + RulesHit + " count for these denial codes ('MN-DA-01','MN-DC-01','MN-DG-01','MN-DI-01','MN-DN-01','MN-DT-01')\n\n\nPlease report this to L2 immediately to check the CEED response if they are active");
                }

                if (PBMSynch_Benefit.result_count > Threshold_PBMSynch_Benefit) 
                {
                    Logger.Info("Sending email for PBMSynch_Benefit");
                    PBMSynch_Benefit.threshold = Threshold_PBMSynch_Benefit;
                    PBMSynch_Benefit.email_subject = "[OIC Alert] PBMSynch Benefits";
                    PBMSynch_Benefit.email_body = Emailyfier(PBMSynch_Benefit, "records of Benefits that are not yet processed", "Check if the application is running on 10.156.62.23, also check the diskspace of the drives, if its full check the TaskScheduler for batch created by Faisal");
                    SendEmail.execute_process(PBMSynch_Benefit.email_subject, PBMSynch_Benefit.email_body);
                }

                if (PBMSynch_Group.result_count > Threshold_PBMSynch_Group)
                {
                    Logger.Info("Sending email for PBMSynch_Groups");
                    PBMSynch_Group.threshold = Threshold_PBMSynch_Group;
                    PBMSynch_Group.email_subject = "[OIC Alert] PBMSynch Groups";
                    PBMSynch_Group.email_body = Emailyfier(PBMSynch_Group, "records of Groups that are not yet processed", "Check if the application is running on 10.156.62.23, also check the diskspace of the drives, if its full check the TaskScheduler for batch created by Faisal");
                    SendEmail.execute_process(PBMSynch_Group.email_subject, PBMSynch_Group.email_body);
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }


        #region Monitors
        private static Result CallHAADSearch(string username, string password, string license, int direction, int downloadstatus, int TransactionType, string SearchDate, Result obj_res)
        {
            int i = 0;
            try
            {
                Logger.Info("Haad Search method called Started");
                string foundTransactions = string.Empty;
                string errorMessage = string.Empty;


                string SearchDateFrom = "";
                string SearchDateTo = "";
                string ePartner = "";

                HaaD.WebservicesSoapClient WS = new HaaD.WebservicesSoapClient();
                obj_res.query = "Searching HAAD WS for username: " + username + " transactiontype: " + TransactionType + " direction: " + direction + " downloadstatus: " + downloadstatus ;
                int result = WS.SearchTransactions(username, password, direction, license, ePartner, TransactionType, downloadstatus, string.Empty, SearchDateFrom, SearchDateTo, -1, -1, out foundTransactions, out errorMessage);
                if (foundTransactions != "<Files></Files>")
                {
                    obj_res.result_count = GetNumberofFiles(foundTransactions);
                    obj_res.result = foundTransactions;
                    Logger.Info(foundTransactions);
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                obj_res.result_count = -1;
            }

            return obj_res;
        }
        private static Result GetOICProviderDBStatus(Result obj_res)
        {
            int J = 0;
            Logger.Info("Checking for OIC Provider DB pending count");
            try
            {
                string username = System.Configuration.ConfigurationManager.AppSettings.Get("username_OICProvider");
                string password = System.Configuration.ConfigurationManager.AppSettings.Get("password_OICProvider");
                string database = System.Configuration.ConfigurationManager.AppSettings.Get("database_OICProvider");
                string IP = System.Configuration.ConfigurationManager.AppSettings.Get("IP_OICProvider");

                string Connection = "Data Source=" + IP + ";Initial Catalog=" + database + ";User ID=" + username + ";Password=" + password + ";Connection Timeout=30;";
                string query = " select Auth.ID as 'TransactionID',LogXml.At 'PostedDatetime',Auth.PostOffice, prov.License,prov.POUsername,prov.POPassword       from [Authorization] Auth      inner join Provider prov on prov.id = Auth.Provider      inner join LogXml on LogXml.TransactionId = Auth.ID      where status = 3 and       DateOrdered >= CONVERT(date,getdate())      and LogXml.At >= CONVERT(date,getdate())      order by Auth.PostOffice";
                obj_res.query = "IP:" + IP + " DB:" + database + " Query:" + query;
                Logger.Info("Checking OIC DB for pending");

                DataTable dt = Execute_Query(Connection, query);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {

                        List<string> list_reslt = new List<string>();
                        obj_res.result_count = dt.Rows.Count;
                        StringBuilder sb = new StringBuilder();

                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            sb.Append("'" + dt.Rows[i][0] + "','" + dt.Rows[i][1] + "','" + dt.Rows[i][2] + "','" + dt.Rows[i][3] + "','" + dt.Rows[i][4] + "','" + dt.Rows[i][5] + "'\n");
                            list_reslt.Add(dt.Rows[i][0].ToString());
                        }

                        obj_res.result = sb.ToString();
                        obj_res.List_transactionID = list_reslt;

                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                obj_res.result_count = -1;
            }
           

            return obj_res;
        }
        private static Result GetOICPayerDBStatus(Result obj_res)
        {
            int result = 0;
            DataTable dt = new DataTable();
            try
            {
                string username = System.Configuration.ConfigurationManager.AppSettings.Get("username_OICPayer");
                string password = System.Configuration.ConfigurationManager.AppSettings.Get("password_OICPayer");
                string database = System.Configuration.ConfigurationManager.AppSettings.Get("database_OICPayer");
                string IP = System.Configuration.ConfigurationManager.AppSettings.Get("IP_OICPayer");


                string Connection = "server=" + IP + ";database=" + database + ";uid=" + username + ";pwd=" + password + ";AllowUserVariables=True;";
                string transactions = "(" + split_array(obj_res.List_transactionID) + ")";
                string query = "select Count(*) from transaction \n" +
                                " where authorization_id in " + transactions + " \n" +
                                " order by creation_Date desc \n";
                obj_res.query = "IP:" + IP + " DB:" + database + " Query:" + query;


                dt = Execute_QueryMySQL(Connection, query);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        obj_res.result_count = int.Parse(dt.Rows[0][0].ToString());

                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                obj_res.result_count = -1;
            }

            return obj_res;
        }
        private static Result CallDHPOSearch(string username, string password, string license, int direction, int downloadstatus, int TransactionType, string SearchDate,Result obj_Res)
        {
            int i = 0;
            try
            {
                Logger.Info("DHPO Search method called Started");
                string foundTransactions = string.Empty;
                string errorMessage = string.Empty;

                string SearchDateFrom = "";
                string SearchDateTo = "";
                string ePartner = "";

                DHPO.ValidateTransactionsSoapClient WS = new DHPO.ValidateTransactionsSoapClient();
                obj_Res.query = "Searching DHPO WS for username: " + username + " transactiontype: " + TransactionType + " direction: " + direction + " downloadstatus: " + downloadstatus ;

                int result = WS.SearchTransactions(username, password, direction, license, ePartner, TransactionType, downloadstatus, string.Empty, SearchDateFrom, SearchDateTo, -1, -1, out foundTransactions, out errorMessage);


                if (foundTransactions != "<Files></Files>")
                {
                    obj_Res.result_count = GetNumberofFiles(foundTransactions);
                    obj_Res.result = foundTransactions;
                    Logger.Info(foundTransactions);

                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                obj_Res.result_count = -1;
            }

            return obj_Res;
        }
        private static Result CheckDHPODB(Result obj_res)
        {
            int J = 0;
            Logger.Info("Checking for exception in DHPO");
            try
            {
                string username = System.Configuration.ConfigurationManager.AppSettings.Get("username_DHPO");
                string password = System.Configuration.ConfigurationManager.AppSettings.Get("password_DHPO");
                string database = System.Configuration.ConfigurationManager.AppSettings.Get("database_DHPO");
                string IP = System.Configuration.ConfigurationManager.AppSettings.Get("IP_DHPO");


                string Connection = "Data Source=" + IP + ";Initial Catalog=" + database + ";User ID=" + username + ";Password=" + password + ";Connection Timeout=30;";
                string query = "select ExceptionMessage,ExceptionSource,ExceptionStackTrace,Count(*) as 'Count' from Error_Log  where HandledAt>= DATEADD(mi,-30,current_timestamp)   and ExceptionMessage = 'Timeout expired.  The timeout period elapsed prior to completion of the operation or the server is not responding.' group by ExceptionMessage,ExceptionSource,ExceptionStackTrace order by 4 desc";
                obj_res.query = "IP:" + IP + " DB:" + database + " Query:" + query;

                Logger.Info("Executing query: " + query);

                DataTable dt = Execute_Query(Connection, query);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        J = int.Parse(dt.Rows[0][3].ToString());
                        obj_res.result_count = J;

                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            sb.Append(dt.Rows[i][0].ToString() + "|" + dt.Rows[i][1].ToString() + "|" + dt.Rows[i][2].ToString() + "|" + dt.Rows[i][3].ToString());
                        }
                        Logger.Info(sb.ToString());
                        obj_res.result = sb.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                obj_res.result_count = -1;
            }

            return obj_res;
        }
        private static Result PBMMNotPicked(Result obj_res)
        {
            int J = 0;
            Logger.Info("Checking for PBMM not picked");
            try
            {
                //string Connection = "Data Source=" + "10.162.176.85" + ";Initial Catalog=" + "PBMM" + ";User ID=" + "payerinteg_user" + ";Password=" + "Apy_8211" + ";Connection Timeout=30;";

                string username = System.Configuration.ConfigurationManager.AppSettings.Get("username_PBMM");
                string password = System.Configuration.ConfigurationManager.AppSettings.Get("password_PBMM");
                string database = System.Configuration.ConfigurationManager.AppSettings.Get("database_PBMM");
                string IP = System.Configuration.ConfigurationManager.AppSettings.Get("IP_PBMM");


                string Connection = "Data Source=" + IP + ";Initial Catalog=" + database + ";User ID=" + username + ";Password=" + password + ";Connection Timeout=30;";
                string query = "Select at.id          from            AUTHORIZATION_TRANSACTION at  with (Nolock)            inner join PRIOR_REQUEST PR  with (Nolock) on At.id = pr.id          where            at.payer_id in (select PAY2.id from PBMM..PAYER PAY2 with(nolock) where bucket_mapping_id in (7))          and PR.download_batch_id in (null, '0')          and at.state_id IN (3,4,8)          and at.created_dt  < DATEADD(HOUR, -1, GETDATE())and pr.[type] = 'Authorization'";
                obj_res.query = "IP:" + IP + " DB:" + database + " Query:" + query;


                Logger.Info("Executing query: " + query);


                DataTable dt = Execute_Query(Connection, query);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        J = dt.Rows.Count;
                        obj_res.result_count = J;

                        StringBuilder sb = new StringBuilder();
                        sb.Append("(");
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            sb.Append("'" + dt.Rows[i][0].ToString() + "',");
                        }
                        sb = sb.Remove(sb.Length - 1, 1);
                        sb.Append(")");
                        obj_res.result = sb.ToString();
                        Logger.Info(sb.ToString());
                    }
                }


            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                obj_res.result_count = -1;
            }

            return obj_res;
        }
        private static Result CheckPBM_NotProcessedtransactions_OIC(Result obj_res)
        {
            int J = 0;
            try
            {

                string username = System.Configuration.ConfigurationManager.AppSettings.Get("username_PBMM");
                string password = System.Configuration.ConfigurationManager.AppSettings.Get("password_PBMM");
                string database = System.Configuration.ConfigurationManager.AppSettings.Get("database_PBMM");
                string IP = System.Configuration.ConfigurationManager.AppSettings.Get("IP_PBMM");


                string Connection = "Data Source=" + IP + ";Initial Catalog=" + database + ";User ID=" + username + ";Password=" + password + ";Connection Timeout=30;";
                string query = "select * from AUTHORIZATION_TRANSACTION where created_dt>=DATEADD(HOUR,-4,GETDATE())  and state_id = 1 and payer_id = 7";

                obj_res.query = "IP:" + IP + " DB:" + database + " Query:" + query;
                Logger.Info("Executing query: " + query);


                DataTable dt = Execute_Query(Connection, query);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        J = dt.Rows.Count;
                        obj_res.result_count = J;

                        StringBuilder sb = new StringBuilder();

                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            for (int j = 0; j < dt.Columns.Count; j++)
                            {
                                sb.Append("'" + dt.Rows[i][j] + "',");
                            }
                            sb = sb.Remove(sb.Length - 1, 1);
                            sb.Append("\n");
                        }

                        Logger.Info(sb.ToString());
                        obj_res.result = sb.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                obj_res.result_count = -1;
            }

            return obj_res;
        }
        private static Result CheckPBM_NotProcessedtransactions_General(Result obj_res)
        {
            int J = 0;
            try
            {

                string username = System.Configuration.ConfigurationManager.AppSettings.Get("username_PBMM");
                string password = System.Configuration.ConfigurationManager.AppSettings.Get("password_PBMM");
                string database = System.Configuration.ConfigurationManager.AppSettings.Get("database_PBMM");
                string IP = System.Configuration.ConfigurationManager.AppSettings.Get("IP_PBMM");


                string Connection = "Data Source=" + IP + ";Initial Catalog=" + database + ";User ID=" + username + ";Password=" + password + ";Connection Timeout=30;";
                string query = "select * from AUTHORIZATION_TRANSACTION where created_dt>=DATEADD(HOUR,-4,GETDATE())  and state_id = 1";

                obj_res.query = "IP:" + IP + " DB:" + database + " Query:" + query;
                Logger.Info("Executing query: " + query);


                DataTable dt = Execute_Query(Connection, query);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        J = dt.Rows.Count;
                        obj_res.result_count = J;

                        StringBuilder sb = new StringBuilder();

                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            for (int j = 0; j < dt.Columns.Count; j++)
                            {
                                sb.Append("'" + dt.Rows[i][j] + "',");
                            }
                            sb = sb.Remove(sb.Length - 1, 1);
                            sb.Append("\n");
                        }

                        Logger.Info(sb.ToString());
                        obj_res.result = sb.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                obj_res.result_count = -1;
            }

            return obj_res;
        }
        private static Result PBMM_ClinicalEditsWorking(Result obj_res)
        {
            int J = 0;
            Logger.Info("Checking for Clinical Edits if working");
            try
            {
                //string Connection = "Data Source=" + "10.162.176.85" + ";Initial Catalog=" + "PBMM" + ";User ID=" + "payerinteg_user" + ";Password=" + "Apy_8211" + ";Connection Timeout=30;";

                string username = System.Configuration.ConfigurationManager.AppSettings.Get("username_PBMM");
                string password = System.Configuration.ConfigurationManager.AppSettings.Get("password_PBMM");
                string database = System.Configuration.ConfigurationManager.AppSettings.Get("database_PBMM");
                string IP = System.Configuration.ConfigurationManager.AppSettings.Get("IP_PBMM");


                string Connection = "Data Source=" + IP + ";Initial Catalog=" + database + ";User ID=" + username + ";Password=" + password + ";Connection Timeout=30;";
                string query = "select denial_code,count(*) as 'Count' from PRIOR_AUTHORIZATION where denial_code in ('MN-DA-01','MN-DC-01','MN-DG-01','MN-DI-01','MN-DN-01','MN-DT-01') and created_dt between CONVERT(date,getdate()-1) and CONVERT(date,getdate()) group by denial_code order by 2 desc";
                obj_res.query = "IP:" + IP + " DB:" + database + " Query:" + query;
                Logger.Info("Executing query: " + query);

                DataTable dt = Execute_Query(Connection, query);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        obj_res.result_count = int.Parse(dt.Rows[0][1].ToString());

                        StringBuilder sb = new StringBuilder();
                        sb.Append("(");
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                           sb.Append("'"+dt.Rows[i][0].ToString() + "'," + dt.Rows[i][1].ToString()+"\n");
                        }
                       
                        sb.Append(")");
                        obj_res.result = sb.ToString();
                    }
                }


            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                obj_res.result_count = -1;
            }

            return obj_res;
        }
        private static Result CheckPI_PR_NotProcessed(Result obj_res)
        {
            //int i = 0;
            string result = string.Empty;
            Logger.Info("Checking for Payer Integration Prior Requests backlog");
            try
            {
                string URL_PI_PR = ConfigurationManager.AppSettings.Get("PI_PR_URL");
                using (WebClient wc = new WebClient())
                {
                    result = wc.DownloadString(URL_PI_PR);
                    Logger.Info(result);

                    JArray arr = JArray.Parse(result);
                    obj_res.result_count = arr.Count;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                obj_res.result_count = -1;
            }
            return obj_res;
        }
        private static Result PBMSync_Benefit(Result obj_res)
        {
            int J = 0;
            try
            {

                string username = System.Configuration.ConfigurationManager.AppSettings.Get("username_PBMPayer");
                string password = System.Configuration.ConfigurationManager.AppSettings.Get("password_PBMPayer");
                string database = System.Configuration.ConfigurationManager.AppSettings.Get("database_PBMPayer");
                string IP = System.Configuration.ConfigurationManager.AppSettings.Get("IP_PBMPayer");


                string Connection = "Data Source=" + IP + ";Initial Catalog=" + database + ";User ID=" + username + ";Password=" + password + ";Connection Timeout=30;";
                string query = "select * Pending_FileProcessing_BenefitCount from BenefitRecord WHERE IsTransferredToAims = 0 AND PayerId = 7 AND UpdatedDt >= dateadd(day,datediff(day,1,GETDATE()),0) AND UpdatedDt < dateadd(day,datediff(day,0,GETDATE()),0) ";

                obj_res.query = "IP:" + IP + " DB:" + database + " Query:" + query;
                Logger.Info("Executing query: " + query);


                DataTable dt = Execute_Query(Connection, query);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        J = dt.Rows.Count;
                        obj_res.result_count = J;

                        StringBuilder sb = new StringBuilder();

                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            for (int j = 0; j < dt.Columns.Count; j++)
                            {
                                sb.Append("'" + dt.Rows[i][j] + "',");
                            }
                            sb = sb.Remove(sb.Length - 1, 1);
                            sb.Append("\n");
                        }

                        Logger.Info(sb.ToString());
                        obj_res.result = sb.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                obj_res.result_count = -1;
            }

            return obj_res;
        }
        private static Result PBMSync_Group(Result obj_res)
        {
            int J = 0;
            try
            {

                string username = System.Configuration.ConfigurationManager.AppSettings.Get("username_PBMPayer");
                string password = System.Configuration.ConfigurationManager.AppSettings.Get("password_PBMPayer");
                string database = System.Configuration.ConfigurationManager.AppSettings.Get("database_PBMPayer");
                string IP = System.Configuration.ConfigurationManager.AppSettings.Get("IP_PBMPayer");


                string Connection = "Data Source=" + IP + ";Initial Catalog=" + database + ";User ID=" + username + ";Password=" + password + ";Connection Timeout=30;";
                string query = "select * Pending_FileProcessing_GroupCount from GroupRecord  WHERE IsTransferredToAims = 0 AND PayerId = 7 AND updated_date >= dateadd(day,datediff(day,1,GETDATE()),0) AND updated_date < dateadd(day,datediff(day,0,GETDATE()),0) ";

                obj_res.query = "IP:" + IP + " DB:" + database + " Query:" + query;
                Logger.Info("Executing query: " + query);


                DataTable dt = Execute_Query(Connection, query);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        J = dt.Rows.Count;
                        obj_res.result_count = J;

                        StringBuilder sb = new StringBuilder();

                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            for (int j = 0; j < dt.Columns.Count; j++)
                            {
                                sb.Append("'" + dt.Rows[i][j] + "',");
                            }
                            sb = sb.Remove(sb.Length - 1, 1);
                            sb.Append("\n");
                        }

                        Logger.Info(sb.ToString());
                        obj_res.result = sb.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                obj_res.result_count = -1;
            }

            return obj_res;
        }


        //private static int CallHAADSearch(string username, string password, string license, int direction, int downloadstatus, int TransactionType, string SearchDate)
        //{
        //    int i = 0;
        //    try
        //    {
        //        Logger.Info("Haad Search method called Started");
        //        string foundTransactions = string.Empty;
        //        string errorMessage = string.Empty;


        //        string SearchDateFrom = "";
        //        string SearchDateTo = "";
        //        string ePartner = "";

        //        HaaD.WebservicesSoapClient WS = new HaaD.WebservicesSoapClient();
        //        int result = WS.SearchTransactions(username, password, direction, license, ePartner, TransactionType, downloadstatus, string.Empty, SearchDateFrom, SearchDateTo, -1, -1, out foundTransactions, out errorMessage);

        //        if (foundTransactions != "<Files></Files>")
        //        {
        //            i = GetNumberofFiles(foundTransactions);
        //            Logger.Info(foundTransactions);
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex);
        //        i = -1;
        //    }

        //    return i;
        //}

        //private static OICEauthResult GetOICProviderDBStatus(OICEauthResult obj)
        //{
        //    int J = 0;
        //    Logger.Info("Checking for OIC Provider DB pending count");
        //    try
        //    {
        //        //string Connection = "Data Source=" + "10.162.176.27" + ";Initial Catalog=" + "OIC.eAuth" + ";User ID=" + "michael" + ";Password=" + "asdf@1234" + ";Connection Timeout=30;";


        //        string username = System.Configuration.ConfigurationManager.AppSettings.Get("username_OICProvider");
        //        string password = System.Configuration.ConfigurationManager.AppSettings.Get("password_OICProvider");
        //        string database = System.Configuration.ConfigurationManager.AppSettings.Get("database_OICProvider");
        //        string IP = System.Configuration.ConfigurationManager.AppSettings.Get("IP_OICProvider");


        //        string Connection = "Data Source=" + IP + ";Initial Catalog=" + database + ";User ID=" + username + ";Password=" + password + ";Connection Timeout=30;";



        //        string query = " select Auth.ID as 'TransactionID',LogXml.At 'PostedDatetime',Auth.PostOffice, prov.License,prov.POUsername,prov.POPassword       from [Authorization] Auth      inner join Provider prov on prov.id = Auth.Provider      inner join LogXml on LogXml.TransactionId = Auth.ID      where status = 3 and       DateOrdered >= CONVERT(date,getdate())      and LogXml.At >= CONVERT(date,getdate())      order by Auth.PostOffice";
        //        Logger.Info("Checking OIC DB for pending");

        //        DataTable dt = Execute_Query(Connection, query);
        //        if (dt != null)
        //        {
        //            if (dt.Rows.Count > 0)
        //            {

        //                List<string> list_reslt = new List<string>();
        //                J = dt.Rows.Count;

        //                for (int i = 0; i < dt.Rows.Count; i++)
        //                {
        //                    Logger.Info(dt.Rows[i][0] + "," + dt.Rows[i][1] + "," + dt.Rows[i][2] + "," + dt.Rows[i][3] + "," + dt.Rows[i][4] + "," + dt.Rows[i][5]);
        //                    list_reslt.Add(dt.Rows[i][0].ToString());
        //                }

        //                obj.List_transactionID = list_reslt;

        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex);
        //        J = -1;
        //    }
        //    obj.OIC_ProviderCount = J;

        //    return obj;
        //}
        //private static int GetOICPayerDBStatus(OICEauthResult obj)
        //{
        //    int result = 0;
        //    DataTable dt = new DataTable();
        //    try
        //    {
        //        string username = System.Configuration.ConfigurationManager.AppSettings.Get("username_OICPayer");
        //        string password = System.Configuration.ConfigurationManager.AppSettings.Get("password_OICPayer");
        //        string database = System.Configuration.ConfigurationManager.AppSettings.Get("database_OICPayer");
        //        string IP = System.Configuration.ConfigurationManager.AppSettings.Get("IP_OICPayer");


        //        string Connection = "server=" + IP + ";database=" + database + ";uid=" + username + ";pwd=" + password + ";AllowUserVariables=True;";
        //        string transactions = "(" + split_array(obj.List_transactionID) + ")";
        //        string query = "select Count(*) from transaction \n" +
        //                        " where authorization_id in " + transactions + " \n" +
        //                        " order by creation_Date desc \n";


        //        dt = Execute_QueryMySQL(Connection, query);
        //        if(dt!= null)
        //        {
        //            if(dt.Rows.Count>0)
        //            {
        //                result = int.Parse(dt.Rows[0][0].ToString());
        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex);
        //        result = -1;
        //    }

        //    return result;
        //}

        //private static int CallDHPOSearch(string username, string password, string license, int direction, int downloadstatus, int TransactionType, string SearchDate)
        //{
        //    int i = 0;
        //    try
        //    {
        //        Logger.Info("DHPO Search method called Started");
        //        string foundTransactions = string.Empty;
        //        string errorMessage = string.Empty;

        //        //string tolerance = ConfigurationManager.AppSettings.Get("tolerance");
        //        //int int_tol = int.Parse(tolerance);

        //        //string SearchDateFrom = Convert.ToDateTime(SearchDate).AddMinutes(-int_tol).ToString("yyyy-MM-dd HH:mm");
        //        //string SearchDateTo = Convert.ToDateTime(SearchDate).AddMinutes(int_tol).ToString("yyyy-MM-dd HH:mm");
        //        //string ePartner = "A023";

        //        string SearchDateFrom = "";
        //        string SearchDateTo = "";
        //        string ePartner = "";

        //        DHPO.ValidateTransactionsSoapClient WS = new DHPO.ValidateTransactionsSoapClient();
        //        int result = WS.SearchTransactions(username, password, direction, license, ePartner, TransactionType, downloadstatus, string.Empty, SearchDateFrom, SearchDateTo, -1, -1, out foundTransactions, out errorMessage);

        //        if (foundTransactions != "<Files></Files>")
        //        {
        //            i = GetNumberofFiles(foundTransactions);
        //            Logger.Info(foundTransactions);

        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex);
        //        i = -1;
        //    }

        //    return i;
        //}
        //private static int CheckDHPODB()
        //{
        //    int J = 0;
        //    Logger.Info("Checking for exception in DHPO");
        //    try
        //    {
        //        string username = System.Configuration.ConfigurationManager.AppSettings.Get("username_DHPO");
        //        string password = System.Configuration.ConfigurationManager.AppSettings.Get("password_DHPO");
        //        string database = System.Configuration.ConfigurationManager.AppSettings.Get("database_DHPO");
        //        string IP = System.Configuration.ConfigurationManager.AppSettings.Get("IP_DHPO");


        //        string Connection = "Data Source=" +IP+ ";Initial Catalog=" + database + ";User ID=" + username + ";Password=" + password + ";Connection Timeout=30;";
        //        string query = "select ExceptionMessage,ExceptionSource,ExceptionStackTrace,Count(*) as 'Count' from Error_Log  where HandledAt>= DATEADD(mi,-30,current_timestamp)   and ExceptionMessage = 'Timeout expired.  The timeout period elapsed prior to completion of the operation or the server is not responding.' group by ExceptionMessage,ExceptionSource,ExceptionStackTrace order by 4 desc";
        //        Logger.Info("Executing query: " + query);

        //        DataTable dt = Execute_Query(Connection, query);
        //        if (dt != null)
        //        {
        //            if (dt.Rows.Count > 0)
        //            {
        //                J = int.Parse(dt.Rows[0][3].ToString());

        //                for (int i = 0; i < dt.Rows.Count; i++)
        //                {
        //                    Logger.Info(dt.Rows[i][0].ToString() + "|" + dt.Rows[i][1].ToString() + "|" + dt.Rows[i][2].ToString() + "|" + dt.Rows[i][3].ToString());
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex);
        //        J = -1;
        //    }

        //    return J;
        //}

        //private static int PBMMNotPicked()
        //{
        //    int J = 0;
        //    Logger.Info("Checking for PBMM not picked");
        //    try
        //    {
        //        //string Connection = "Data Source=" + "10.162.176.85" + ";Initial Catalog=" + "PBMM" + ";User ID=" + "payerinteg_user" + ";Password=" + "Apy_8211" + ";Connection Timeout=30;";

        //        string username = System.Configuration.ConfigurationManager.AppSettings.Get("username_PBMM");
        //        string password = System.Configuration.ConfigurationManager.AppSettings.Get("password_PBMM");
        //        string database = System.Configuration.ConfigurationManager.AppSettings.Get("database_PBMM");
        //        string IP = System.Configuration.ConfigurationManager.AppSettings.Get("IP_PBMM");


        //        string Connection = "Data Source=" + IP + ";Initial Catalog=" + database + ";User ID=" + username + ";Password=" + password + ";Connection Timeout=30;";
        //        string query = "Select at.id          from            AUTHORIZATION_TRANSACTION at  with (Nolock)            inner join PRIOR_REQUEST PR  with (Nolock) on At.id = pr.id          where            at.payer_id in (select PAY2.id from PBMM..PAYER PAY2 with(nolock) where bucket_mapping_id in (7))          and PR.download_batch_id in (null, '0')          and at.state_id IN (3,4,8)          and at.created_dt  < DATEADD(HOUR, -1, GETDATE())and pr.[type] = 'Authorization'";
        //        Logger.Info("Executing query: " + query);


        //        DataTable dt = Execute_Query(Connection, query);
        //        if (dt != null)
        //        {
        //            if (dt.Rows.Count > 0)
        //            {
        //                J = dt.Rows.Count;

        //                for (int i = 0; i < dt.Rows.Count; i++)
        //                {
        //                    Logger.Info(dt.Rows[i][0].ToString());
        //                }
        //            }
        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex);
        //        J = -1;
        //    }

        //    return J;
        //}
        //private static int CheckPBM_NotProcessedtransactions()
        //{
        //    int J = 0;
        //    try
        //    {

        //        string username = System.Configuration.ConfigurationManager.AppSettings.Get("username_PBMM");
        //        string password = System.Configuration.ConfigurationManager.AppSettings.Get("password_PBMM");
        //        string database = System.Configuration.ConfigurationManager.AppSettings.Get("database_PBMM");
        //        string IP = System.Configuration.ConfigurationManager.AppSettings.Get("IP_PBMM");


        //        string Connection = "Data Source=" + IP + ";Initial Catalog=" + database + ";User ID=" + username + ";Password=" + password + ";Connection Timeout=30;";
        //        string query = "select * from AUTHORIZATION_TRANSACTION where created_dt>=DATEADD(HOUR,-4,GETDATE())  and state_id = 1";
        //        Logger.Info("Executing query: " + query);


        //        DataTable dt = Execute_Query(Connection, query);
        //        if (dt != null)
        //        {
        //            if (dt.Rows.Count > 0)
        //            {
        //                J = dt.Rows.Count;

        //                StringBuilder sb = new StringBuilder();

        //                for (int i = 0; i < dt.Rows.Count; i++)
        //                {
        //                    for (int j = 0; j < dt.Columns.Count; j++) 
        //                    {
        //                        sb.Append(dt.Rows[i][j] + ",");
        //                    }
        //                    sb.Append("\n");
        //                }

        //                Logger.Info(sb.ToString());
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex);
        //        J = -1;
        //    }

        //    return J;
        //}
        //private static int PBMM_ClinicalEditsWorking()
        //{
        //    int J = 0;
        //    Logger.Info("Checking for Clinical Edits if working");
        //    try
        //    {
        //        //string Connection = "Data Source=" + "10.162.176.85" + ";Initial Catalog=" + "PBMM" + ";User ID=" + "payerinteg_user" + ";Password=" + "Apy_8211" + ";Connection Timeout=30;";

        //        string username = System.Configuration.ConfigurationManager.AppSettings.Get("username_PBMM");
        //        string password = System.Configuration.ConfigurationManager.AppSettings.Get("password_PBMM");
        //        string database = System.Configuration.ConfigurationManager.AppSettings.Get("database_PBMM");
        //        string IP = System.Configuration.ConfigurationManager.AppSettings.Get("IP_PBMM");


        //        string Connection = "Data Source=" + IP + ";Initial Catalog=" + database + ";User ID=" + username + ";Password=" + password + ";Connection Timeout=30;";
        //        string query = "select denial_code,count(*) as 'Count' from PRIOR_AUTHORIZATION where denial_code in ('MN-DA-01','MN-DC-01','MN-DG-01','MN-DI-01','MN-DN-01','MN-DT-01') and created_dt between CONVERT(date,getdate()-1) and CONVERT(date,getdate()) group by denial_code order by 2 desc";
        //        Logger.Info("Executing query: " + query);

        //        DataTable dt = Execute_Query(Connection, query);
        //        if (dt != null)
        //        {
        //            if (dt.Rows.Count > 0)
        //            {
        //                J = int.Parse(dt.Rows[0][1].ToString());

        //                for (int i = 0; i < dt.Rows.Count; i++)
        //                {
        //                    Logger.Info(dt.Rows[i][0].ToString() + "," + dt.Rows[i][1].ToString());
        //                }
        //            }
        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex);
        //        J = -1;
        //    }

        //    return J;
        //}
        //private static int CheckPI_PR_NotProcessed()
        //{
        //    int i = 0;
        //    string result = string.Empty;
        //    Logger.Info("Checking for Payer Integration Prior Requests backlog");
        //    try
        //    {
        //        string URL_PI_PR = ConfigurationManager.AppSettings.Get("PI_PR_URL");
        //        using (WebClient wc = new WebClient())
        //        {
        //            result = wc.DownloadString(URL_PI_PR);
        //            Logger.Info(result);

        //            JArray arr = JArray.Parse(result);
        //            i = arr.Count;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex);
        //        i = -1;
        //    }
        //    return i;
        //}

        #endregion

        #region custom control
        static string split_array(List<string> data)
        {
            try
            {
                //string concat = null;
                StringBuilder cot = new StringBuilder();
                foreach (string s in data)
                {

                    cot.Append(string.Format("'" + s.Trim() + "',"));
                   
                }
                //return concat.Remove(concat.Length - 1);
                return Convert.ToString(cot).Remove(cot.Length - 1);
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
        }
        private static DataTable Execute_Query(string connection, string query)
        {
            DataTable dt = new DataTable();
            try
            {
                Logger.Info("SELECT query in progress");
                Logger.Info("Connection: " + connection);

                using (SqlConnection con = new SqlConnection(connection))
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        con.Open();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.SelectCommand.CommandTimeout = 1800;
                            da.Fill(dt);
                            Logger.Info("Query executed successfully");
                        }
                        con.Close();
                    }
                }
                return dt;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }
        private static DataTable Execute_QueryMySQL(string connection, string query)
        {
            DataTable dt = new DataTable();
            try
            {
                Logger.Info("MySQL query in progress");
                Logger.Info("Connection: " + connection);

                using (MySqlConnection con = new MySqlConnection(connection))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        con.Open();
                        using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                        {
                            da.SelectCommand.CommandTimeout = 1800;
                            da.Fill(dt);
                            Logger.Info("Query executed successfully");
                        }
                        con.Close();
                    }
                }
                return dt;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }
        private static int GetNumberofFiles(string files)
        {
            int i = 0;

            try
            {
                XmlDocument xdoc = new XmlDocument();
                xdoc.LoadXml(files);

                XmlNodeList nodelist = xdoc.SelectNodes("//File");
                i = nodelist.Count;

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return i;
        }
        private static string Emailyfier(Result obj,string short_desc,string resolve)
        {
            string result = string.Empty;
            try
            {
                //obj.email_subject = "[OIC Alert] HAAD WS PR Breached";
                obj.email_body = "Dear Team,"+"\n\n\n"+
                    "Please inform L2 that the threshold of " + obj.threshold + " has been breached of this monitoring script." + "\n\n\n" +
                    "There are " + obj.result_count +" "+ short_desc + "\n\n\n" +
                    "Possible Resolve: " + resolve + "\n\n\n" +
                    "Monitoring on Query/Search: "+ obj.query + "\n\n\n" +
                    "Monitoring Result: " + obj.result + "\n\n\n" +
                    "Many thanks ;)";

                result = obj.email_body;

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return result;
        }

        #endregion
    }


    class OICEauthResult
    {
        public List<string> List_transactionID { get; set; }
        public int OIC_ProviderCount { get; set; }
    }

    class Result
    {
        public string query { get; set;}
        public string result { get; set; }
        public List<string> List_transactionID { get; set; }
        public int result_count { get; set; }
        public string email_body { get; set; }
        public string email_subject { get; set; }
        public int threshold { get; set; }
    }

}
