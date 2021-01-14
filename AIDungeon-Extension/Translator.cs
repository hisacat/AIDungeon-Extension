using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Threading;

namespace AIDungeon_Extension
{
    public class Translator
    {
        List<TranslateWorker> works = null;
        ChromeDriver driver = null;
        Thread workThread = null;
        public void Run()
        {
            works = new List<TranslateWorker>();
            workThread = new Thread(Update);
            workThread.Start();
        }

        public class TranslateWorker
        {
            public readonly string Text;
            public readonly string From;
            public readonly string To;
            private Action<string> translated;
            private Action<string> failed;
            private Action finished;

            public void TranslatedCallback(string text)
            {
                this.translated?.Invoke(text);
                this.finished?.Invoke();
            }
            public void FailedCallback(string reason)
            {
                this.failed?.Invoke(reason);
                this.finished?.Invoke();
            }

            public TranslateWorker(string text, string from, string to, Action<string> translated, Action<string> failed = null, Action finished = null)
            {
                this.Text = text;
                this.From = from;
                this.To = to;

                this.translated = translated;
                this.failed = failed;
                this.finished = finished;
            }
        }

        public TranslateWorker Translate(string text, string from, string to, Action<string> translated, Action<string> failed = null, Action finished = null)
        {
            var work = new TranslateWorker(text, from, to, translated, failed, finished);
            this.works.Add(work);
            return work;
        }

        private void Update()
        {
            if (driver != null)
            {
                driver.Quit();
                driver = null;
            }

            var options = new ChromeOptions();
            options.AddArgument("headless");

            driver = new ChromeDriver(System.Environment.CurrentDirectory, options);
            driver.Navigate().GoToUrl("http://www.google.com/translate_t?hl=en");

            while (true)
            {
                TranslateWorker currentWork = null;
                lock (works)
                {
                    if (works.Count > 0)
                    {
                        currentWork = works[0];
                        works.RemoveAt(0);
                    }
                }

                if (currentWork != null)
                {
                    if (string.IsNullOrEmpty(currentWork.Text) || string.IsNullOrEmpty(currentWork.From) || string.IsNullOrEmpty(currentWork.To))
                    {
                        currentWork.FailedCallback("Input string empty");
                        continue;
                    }

                    try
                    {
                        string url = String.Format("http://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair={1}", HttpUtility.UrlEncode(currentWork.Text), string.Format("{0}|{1}", currentWork.From, currentWork.To));
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

                        currentWork.TranslatedCallback(translatedElement.GetAttribute("data-text"));
                    }
                    catch (Exception e)
                    {
                        currentWork.FailedCallback(e.Message);
                    }
                }

                System.Threading.Thread.Sleep(1);
            }
        }

        public void Dispose()
        {
            if (works != null)
            {
                lock (works)
                {
                    foreach (var work in works)
                        work.FailedCallback("Abort");

                    works.Clear();
                }
                works = null;
            }
            if (workThread != null)
            {
                workThread.Abort();
                workThread = null;
            }
            if (driver != null)
            {
                driver.Quit();
                driver = null;
            }
        }

    }
}
