﻿using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AIDungeon_Extension
{
    public class Translator
    {
        public const int TimeOut = 10;
        public const string TranslateResultXPath = "//*[@id=\"yDmH0d\"]/c-wiz/div/div[2]/c-wiz/div[2]/c-wiz/div[1]/div[2]/div[2]/c-wiz[2]/div[5]/div/div[3]";


        ChromeDriver driver = null;
        Thread workThread = null;
        List<TranslateWorker> works = null;
        Dictionary<string, string> translateDictionary = null;

        public bool Ready { get; private set; }
        public bool IsAborted { get; private set; }
        public void Run()
        {
            this.Ready = false;
            this.IsAborted = false;

            this.translateDictionary = new Dictionary<string, string>();
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

            public bool isAborted = false;
            public void Abort()
            {
                this.isAborted = true;
            }

            public TranslateWorker(string text, string from, string to, Action<string> translated, Action<string> failed = null, Action finished = null)
            {
                this.Text = text;
                this.From = from;
                this.To = to;

                this.translated = translated;
                this.failed = failed;
                this.finished = finished;

                this.isAborted = false;
            }

            public override string ToString()
            {
                return string.Format("{0}", Text);
            }
        }
        public string BlockTranslate(string text, string from, string to, Action<string> failed = null)
        {
            bool complited = false;
            string result = null;
            Action finished = () => { complited = true; };
            Action<string> translated = (r) => { result = r; };
            var work = new TranslateWorker(text, from, to, translated, failed, finished);
            lock (this.works)
            {
                this.works.Add(work);
            }

            while (!complited)
                System.Threading.Thread.Sleep(1);

            return result;
        }

        public TranslateWorker Translate(string text, string from, string to, Action<string> translated, Action<string> failed = null, Action finished = null)
        {
            var work = new TranslateWorker(text, from, to, translated, failed, finished);
            lock (this.works)
            {
                this.works.Insert(0, work);
            }
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
            options.AddArgument("disable-gpu");
            options.AddArgument("disable-infobars");
            options.AddArgument("--disable-extensions");
            //options.AddUserProfilePreference("profile.default_content_settings", 2);
            //options.AddUserProfilePreference("profile.default_content_setting_values", 2);

            options.AddArgument("user-agent=Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6)" +
                "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.100 Safari/537.36"); //Not bot

            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            try
            {
                driver = new ChromeDriver(service, options);
                driver.Navigate().GoToUrl("http://www.google.com/translate_t?hl=en");
                this.Ready = true;
            }
            catch (Exception e)
            {
                if (!this.IsAborted)
                    Console.WriteLine("[Exception] Hooker-CrawlingScripts-LoadChromeDriver: " + e.Message);
                Process[] chromeDriverProcesses = Process.GetProcessesByName("chromedriver");
                foreach (var chromeDriverProcess in chromeDriverProcesses)
                {
                    if (chromeDriverProcess.MainModule.FileName.StartsWith(
                        System.AppDomain.CurrentDomain.BaseDirectory))
                    {
                        chromeDriverProcess.Kill();
                    }
                }
                System.Environment.Exit(-1);
            }

            LoadDictionary();

            while (true)
            {
                if (this.IsAborted) break;

                TranslateWorker currentWork = null;
                lock (works)
                {
                    if (works.Count > 0)
                    {
                        currentWork = works[0];
                        works.RemoveAt(0);

                        if (currentWork.isAborted)
                            continue;
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
                        var targetText = currentWork.Text;

                        //var replaceFormat = "\"{{{0}}}\"";
                        //int currentReplaceIndex = 0;
                        //Dictionary<string, string> replaceStrings = new Dictionary<string, string>();

                        lock (this.translateDictionary)
                        {
                            using (var enumerator = this.translateDictionary.Keys.GetEnumerator())
                            {
                                while (enumerator.MoveNext())
                                {
                                    var key = enumerator.Current;
                                    var value = this.translateDictionary[key];
                                    var regex = string.Format("\\b({0})\\b", key);

                                    if (Regex.IsMatch(targetText, regex))
                                    {
                                        targetText = Regex.Replace(targetText, regex, value);
                                        //var replaced = string.Format(replaceFormat, currentReplaceIndex);
                                        //targetText = Regex.Replace(targetText, regex, replaced);
                                        //replaceStrings.Add(replaced, value);
                                        //currentReplaceIndex++;
                                    }
                                }
                            }
                        }

                        //string url = String.Format("http://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair={1}", HttpUtility.UrlEncode(targetText), string.Format("{0}|{1}", currentWork.From, currentWork.To));
                        string url = String.Format("https://translate.google.com/?sl={1}&tl={2}&text={0}&op=translate", HttpUtility.UrlEncode(targetText), currentWork.From, currentWork.To);
                        driver.Navigate().GoToUrl(url);
                        IWebElement translatedElement = null;

                        var startedAt = DateTime.Now;
                        do
                        {
                            if ((DateTime.Now - startedAt).TotalSeconds > TimeOut)
                                throw new Exception("Timeout");

                            try
                            {
                                translatedElement = driver.FindElementByXPathOrNull(TranslateResultXPath);
                            }
                            catch (Exception e)
                            {
                                if (!this.IsAborted)
                                    Console.WriteLine("[Exception] Translator-Update(TranslateWork)-FindElement: " + e.Message);
                            } //Needs timeout
                        } while (translatedElement == null);

                        var translated = translatedElement.GetAttribute("data-text");
                        //foreach (var key in replaceStrings.Keys)
                        //    translated = translated.Replace(key, replaceStrings[key]);

                        //Add empty lines.
                        string[] lines = targetText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (string.IsNullOrWhiteSpace(lines[i])) translated = System.Environment.NewLine + translated;
                            else break;
                        }
                        for (int i = lines.Length - 1; i >= 0; i--)
                        {
                            if (string.IsNullOrWhiteSpace(lines[i])) translated = translated + System.Environment.NewLine;
                            else break;
                        }

                        currentWork.TranslatedCallback(translated);
                    }
                    catch (Exception e)
                    {
                        if (!this.IsAborted)
                            Console.WriteLine("[Exception] Translator-Update(TranslateWork): " + e.Message);
                        currentWork.FailedCallback(e.Message);
                    }
                }

                System.Threading.Thread.Sleep(1);
            }

            //Dispose
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
            if (driver != null)
            {
                driver.Quit();
                driver = null;
            }
            workThread = null;
        }

        public static void OpenDictionaryFile()
        {
            var dictionaryPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Dictionary.txt");
            if (!System.IO.File.Exists(dictionaryPath)) System.IO.File.Create(dictionaryPath);
            Process.Start(dictionaryPath);
        }
        public void LoadDictionary()
        {
            lock (this.translateDictionary)
            {
                this.translateDictionary.Clear();
                var dictionaryPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Dictionary.txt");
                if (System.IO.File.Exists(dictionaryPath))
                {
                    var lines = System.IO.File.ReadAllLines(dictionaryPath);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        var info = line.Split(':');
                        if (info.Length == 2)
                        {
                            var key = info[0];
                            var value = info[1];
                            if (this.translateDictionary.ContainsKey(key))
                                this.translateDictionary[key] = value;
                            else
                                this.translateDictionary.Add(key, value);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            if (driver != null)
            {
                driver.Quit();
                driver = null;
            }
            this.Ready = false;
            this.IsAborted = true;
        }

    }
}
