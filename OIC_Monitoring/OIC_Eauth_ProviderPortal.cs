using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System.IO;
using OpenQA.Selenium.Support.UI;
using System.Threading;

namespace OIC_Monitoring
{
    class OIC_Eauth_ProviderPortal
    {

        public static string baseDir = System.Configuration.ConfigurationManager.AppSettings.Get("basedir");

        public static int execute()
        {
            return GetCount();
        }

        private static int GetCount()
        {
            int count = 0;
            try
            {

                string url = "http://par.tameen.ae/OIC/Account/Login.aspx";
                string usernmae = "FAnsari";
                string password = "MIxhby2.=^2rYdG+N5$2";
                string license = "system";




                System.Environment.SetEnvironmentVariable("Webdriver.gecko.driver", @"C:\tmp\OIC_Monitoring\geckodriver.exe");

                IWebDriver driver = new FirefoxDriver();
                WebDriverWait _wait = new WebDriverWait(driver, new TimeSpan(0, 1, 0));

                Console.WriteLine("GECKO and WAIT initialized");
                Console.WriteLine("Goto URL called");

                driver.Navigate().GoToUrl(url);

                Thread.Sleep(1000);

                Console.WriteLine("Reached URL");

                _wait.Until(d => d.FindElement(By.Id("License")));

                Console.WriteLine("Waiting for License");


                IWebElement elem_licen = driver.FindElement(By.Id("License"));
                elem_licen.SendKeys(license);


                IWebElement elem_usernmae = driver.FindElement(By.Id("Username"));
                elem_usernmae.SendKeys(usernmae);

                IWebElement elem_password = driver.FindElement(By.Id("Password"));
                elem_password.SendKeys(password);

                driver.FindElement(By.Id("Button1")).Click();

                Console.WriteLine("Logged in Clicked");
                Thread.Sleep(1000);



                //ADD WAIT
                //_wait.Until(d => d.FindElement(By.Id("statusOptions")));
                Console.WriteLine("waiting for options");
                Thread.Sleep(1000);



                IWebElement elem_status_dd = driver.FindElement(By.Id("statusOptions"));
                elem_status_dd.FindElement(By.XPath("//*[@id='statusOptions']/option[2]")).Click();


                driver.FindElement(By.Id("Filter")).Click();
                Thread.Sleep(1000);

                Console.WriteLine("Options clicked");


                IWebElement elem_text = driver.FindElement(By.Id("dataList_info"));
                Console.WriteLine(elem_text.Text);

                if (elem_text.Text.Contains("Showing"))
                {
                    string result = elem_text.Text;
                    //result = "Showing 1 to 4 of 3 entries";


                    if (result.Length == 29)//double digit
                    {
                        result = result.Substring(result.Length - 10, 2);
                    }
                    else if (result.Length == 31)//triple digit
                    {
                        result = result.Substring(result.Length - 11, 3);
                    }
                    else if (result.Length == 33)//Quadraple digit
                    {
                        result = result.Substring(result.Length - 12, 4);
                    }
                    else if (result.Length == 27)//Quadraple digit
                    {
                        result = result.Substring(result.Length - 9, 1);
                    }

                    count = int.Parse(result);
                }

                driver.FindElement(By.Id("HeadLoginStatus")).Click();


                driver.Close();
                driver.Quit();

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                count = -1;
            }
            return count;
        }
    }
}
