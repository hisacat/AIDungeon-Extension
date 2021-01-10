using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Linq;
using System.Collections.Generic;
using OpenQA.Selenium.Remote;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Reflection;
using System.Net;
using System.Web;

namespace AIDungeon_Extension
{
    class Program
    {
        //use selenium 3.7 for disable bug.
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var trans_options = new ChromeOptions();
            //trans_options.
            var trans_driver = new ChromeDriver(System.Environment.CurrentDirectory, trans_options);

            var aid_options = new ChromeOptions();
            aid_options.SetLoggingPreference("performance", LogLevel.All);
            var aid_driver = new ChromeDriver(System.Environment.CurrentDirectory, aid_options);

            aid_driver.Navigate().GoToUrl("https://play.aidungeon.io/");

            while (true)
            {
                try
                {
                    var logs = aid_driver.Manage().Logs.GetLog("performance");
                    foreach (var log in logs)
                    {
                        //Console.WriteLine($"{log.Timestamp}: {log.Message}");

                        var jToken = JToken.Parse(log.Message);
                        var message = jToken["message"];
                        var method = message["method"].Value<string>();

                        if(message.ToString().Contains("trackthis"))
                        {
                            Console.WriteLine(message);
                        }

                        switch (method)
                        {
                            case "Network.webSocketFrameReceived":
                                {
                                    var response = message["params"]["response"];
                                    if (response.HasValues)
                                    {
                                        var payloadData = JToken.Parse(response["payloadData"].Value<string>());
                                        //Console.WriteLine(payloadData);

                                        var payloadDataType = payloadData["type"].Value<string>();
                                        var payloadDataId = payloadData["id"].Value<string>();
                                        var payload = payloadData["payload"];

                                        if (payload.HasValues)
                                        {
                                            var datas = payload["data"].ToObject<JObject>();
                                            foreach (var dataPair in datas)
                                            {
                                                var dataKey = dataPair.Key;
                                                var dataValue = dataPair.Value;
                                                switch (dataKey)
                                                {
                                                    case "adventure":
                                                        {
                                                            var id = dataValue["id"];
                                                            var type = dataValue["type"];

                                                            var actionWindow = dataValue["actionWindow"];
                                                            var undoneWindow = dataValue["undoneWindow"];

                                                            //~~
                                                        }
                                                        break;
                                                    case "actionAdded":
                                                        {
                                                            var id = dataValue["id"];
                                                            var text = dataValue["text"].Value<string>();
                                                            var type = dataValue["type"];

                                                            Console.WriteLine(id);
                                                            Console.WriteLine(type);
                                                            Console.WriteLine(text);
                                                            Console.WriteLine(DoTranslate(trans_driver, text));
                                                            Console.WriteLine("");
                                                        }
                                                        break;
                                                    case "actionUpdated":
                                                        {
                                                            var id = dataValue["id"];
                                                            var text = dataValue["text"].Value<string>();
                                                            var type = dataValue["type"];

                                                            Console.WriteLine(id);
                                                            Console.WriteLine(type);
                                                            Console.WriteLine(text);
                                                            Console.WriteLine(DoTranslate(trans_driver, text));
                                                            Console.WriteLine("");
                                                        }
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    //Console.WriteLine(e.Message);
                }
            }
            aid_driver.Quit();
        }

        private static string DoTranslate(ChromeDriver driver, string text)
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
            catch (Exception exc)
            {
                return string.Empty;
            }
        }
    }
}
