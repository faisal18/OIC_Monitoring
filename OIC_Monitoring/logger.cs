using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OIC_Monitoring
{
    class Logger
    {

        public static string baseDir = System.Configuration.ConfigurationManager.AppSettings.Get("basedir");

        public static void Info(string data)
        {
            using (StreamWriter writer = File.AppendText(baseDir + @"\Log\Infolog.csv"))
            {
                writer.Write(System.DateTime.Now + " : " + data + "\n");
            }
        }

        public static void Error(Exception data)
        {
            using (StreamWriter writer = File.AppendText(baseDir + @"\Log\Exceptionlog.csv"))
            {
                writer.Write(System.DateTime.Now + " : " + data + "\n");
            }
        }

        public static void CreateResult()
        {
            if (!File.Exists(baseDir + @"\Output.csv"))
            {
                using (StreamWriter writer = File.AppendText(baseDir + @"\Output.csv"))
                {
                    writer.Write("DateTime|HAAD WS Count|DHPO WS Count|OIC Provider DB Count|OIC_ProviderPortalUICount|PBMM_NotPicked|DHPO DB Exceptions|PI_Backlog|DifferenceInTransaction|PBMM_Notprocessed_OIC|PBMM_Notprocessed_General|RulesHit\n");
                }
            }
        }

        public static void CreateResult(string data)
        {
            if (File.Exists(baseDir + @"\Output.csv"))
            {
                using (StreamWriter writer = File.AppendText(baseDir + @"\Output.csv"))
                {
                    writer.Write(DateTime.Now + "|" + data);
                }
            }
        }

       
        
    }
}
