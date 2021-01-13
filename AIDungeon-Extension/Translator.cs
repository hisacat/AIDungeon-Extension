using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AIDungeon_Extension
{
    public class Translator
    {
        ChromeDriver driver = null;

        public void Run()
        {
            var options = new ChromeOptions();
            options.AddArgument("headless");
            //trans_options.
            driver = new ChromeDriver(System.Environment.CurrentDirectory, options);

        }

        public string DoTranslate(string text)
        {
            try
            {
                string url = String.Format("http://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair={1}", HttpUtility.UrlEncode(text), "en|ko");
                driver.Navigate().GoToUrl(url);
                IWebElement translatedElement = null;

                do
                {
                    try
                    {
                        translatedElement = driver.FindElementByXPath("//*[@id=\"yDmH0d\"]/c-wiz/div/div[2]/c-wiz/div[2]/c-wiz/div[1]/div[2]/div[2]/c-wiz[2]/div[5]/div/div[3]");
                    }
                    catch (Exception e) { }
                } while (translatedElement == null);

                return translatedElement.GetAttribute("data-text");
            }
            catch (Exception e)
            {
                return "#ERROR# " + e.Message;
            }
        }

        public void Dispose()
        {
            if (driver != null)
            {
                driver.Quit();
                driver = null;
            }
        }

    }
}
