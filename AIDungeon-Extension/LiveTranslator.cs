using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace AIDungeon_Extension
{
    //선택적으로 이용?
    class LiveTranslator
    {
        private const string inputElementXPath = "//*[@id=\"yDmH0d\"]/c-wiz/div/div[2]/c-wiz/div[2]/c-wiz/div[1]/div[2]/div[2]/c-wiz[1]/span/span/div/textarea";
        private const string translatedElementXPath = "//*[@id=\"yDmH0d\"]/c-wiz/div/div[2]/c-wiz/div[2]/c-wiz/div[1]/div[2]/div[2]/c-wiz[2]/div[5]/div/div[3]";
       
        ChromeDriver driver = null;
        IWebElement inputElement = null;
        IWebElement translatedElement = null;

        DispatcherTimer updateTimer = null;
        public string TranslatedText { get; private set; }
        public delegate void TranslatedDelegate(string text);
        public event TranslatedDelegate Translated = null;

        public void Run()
        {
            var options = new ChromeOptions();
            driver = new ChromeDriver(System.Environment.CurrentDirectory, options);
            driver.Navigate().GoToUrl("https://translate.google.com/?sl=ko&tl=en&op=translate");

            updateTimer = new DispatcherTimer();
            updateTimer.Tick += Update;
            updateTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            updateTimer.Start();
        }

        public void Translate(string str)
        {
            try
            {
                if (inputElement == null)
                    inputElement = driver.FindElementByXPath(inputElementXPath);

                inputElement.Click();

                inputElement.Clear();
                inputElement.SendKeys(str);
            }
            catch (Exception e)
            {
                inputElement = null;
            }
        }

        private void Update(object sender, EventArgs args)
        {
            try
            {
                if (translatedElement == null || !translatedElement.Displayed)
                    translatedElement = driver.FindElementByXPath(translatedElementXPath);

                //멈추거나 완료될때 호출하니까 주기가 짧아졌음. 고로 url을 새로 열어서 파싱하는게 더 나을수도 있음.

                var current = translatedElement.GetAttribute("data-text");

                if (!string.Equals(TranslatedText, current))
                {
                    TranslatedText = current;
                    Translated?.Invoke(TranslatedText);
                }
            }
            catch (Exception e)
            {
                translatedElement = null;
            }
        }

        public void Dispose()
        {
            if (driver != null)
            {
                driver.Quit();
            }
        }
    }
}
