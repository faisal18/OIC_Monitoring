using System;
using System.Configuration;
using System.Net.Mail;

namespace OIC_Monitoring
{
    class SendEmail
    {

        public static string execute_process(string subject, string body)
        {
            string result = string.Empty;
            try
            {

                string SMTP = ConfigurationManager.AppSettings.Get("SMTP");
                string SMTPPort = ConfigurationManager.AppSettings.Get("SMTPPort");
                string password = ConfigurationManager.AppSettings.Get("password");
                string emailID = ConfigurationManager.AppSettings.Get("emailID");
                string tosent_emailID = ConfigurationManager.AppSettings.Get("tosent_emailID");

                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(SMTP);

                mail.From = new MailAddress(emailID);
                mail.Subject = subject;
                mail.Body = body;


                if (tosent_emailID.Contains(","))
                {
                    string[] recevivers = tosent_emailID.Split(',');
                    foreach (string receiver in recevivers)
                    {
                        mail.To.Add(receiver);
                    }
                }
                else
                {
                    mail.To.Add(tosent_emailID);
                }


                //string[] files = Directory.GetFiles(directory);
                //for (int i = 0; i < files.Length; i++)
                //{
                //    mail.Attachments.Add(new Attachment(files[i]));
                //}

                SmtpServer.Port = Convert.ToInt32(SMTPPort);
                SmtpServer.Credentials = new System.Net.NetworkCredential(emailID, password);
                SmtpServer.EnableSsl = true;
                SmtpServer.Send(mail);

                mail.Attachments.Dispose();

                result = "Sent to " + tosent_emailID + System.Environment.NewLine;
            }
            catch (Exception ex)
            {
                //result = "Please Try Again! Email not sent to " + tosent_emailID + System.Environment.NewLine + ex.Message.ToString();

                Logger.Error(ex);
            }

            return result;
        }

    }
}
