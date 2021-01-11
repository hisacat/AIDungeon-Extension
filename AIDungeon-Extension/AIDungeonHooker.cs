using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace AIDungeon_Extension.Core
{
    public class AIDungeonHooker : IDisposable
    {
        //Shift Enter : 개행
        //Enter : 번역하기
        //Ctrl Enter : 보내기

        private const string LogType_Performance = "performance";
        private const string AIDungeonURL = "https://play.aidungeon.io/main/loginRegister";

        private ChromeDriver driver = null;

        private System.Threading.Thread crawlingThread = null;

        public delegate void ActionUpdatedDelegate(List<AIDungeonAction> actions);
        public event ActionUpdatedDelegate OnAdventureLoaded = null;
        public event ActionUpdatedDelegate OnActionAdded = null;
        public event ActionUpdatedDelegate OnActionUpdated= null;

        public void Run()
        {
            var options = new ChromeOptions();
            options.SetLoggingPreference(LogType_Performance, LogLevel.All);

            var service = ChromeDriverService.CreateDefaultService();
            //service.HideCommandPromptWindow = true;

            driver = new ChromeDriver(service, options);

            driver.Navigate().GoToUrl(AIDungeonURL);

            crawlingThread = new System.Threading.Thread(CrawlingScripts);
            crawlingThread.Start();
        }

        public class AIDungeonAction
        {
            public string id { get; set; }
            public string text { get; set; }
            public string type { get; set; }
            public string adventureId { get; set; }
            public string undoneAt { get; set; }
            public string deletedAt { get; set; }
            public string createdAt { get; set; }
        }

        private void CrawlingScripts()
        {
            while (true)
            {
                try
                {
                    var logs = driver.Manage().Logs.GetLog(LogType_Performance);
                    foreach (var log in logs)
                    {
                        #region For hooking
                        var jToken = JToken.Parse(log.Message);
                        var message = jToken["message"];
                        var method = message["method"].Value<string>();

                        if (message.ToString().Contains("[trackthis]"))
                        {
                            Console.WriteLine(message);
                        }
                        #endregion

                        if (string.Equals(method, "Network.webSocketFrameReceived"))
                        {
                            var response = message["params"]["response"];
                            if (response.HasValues)
                            {
                                var payloadData = JToken.Parse(response["payloadData"].Value<string>());
                                ParsePayloadData(payloadData);
                            }
                        }
                    }
                }
                catch (Exception e)
                {

                }

                //System.Threading.Thread.Sleep(1);
            }
        }

        private void ParsePayloadData(JToken payloadData)
        {
            var payloadDataType = payloadData["type"].Value<string>();
            var payloadDataId = payloadData["id"].Value<string>();
            var payload = payloadData["payload"];

            if (payload.HasValues)
            {
                var datas = payload["data"].ToObject<JObject>();
                foreach (var dataPair in datas)
                {
                    var dataType = dataPair.Key;
                    var data = dataPair.Value;
                    switch (dataType)
                    {
                        case "adventure":
                            {
                                var id = data["id"];

                                var actionWindow = JArray.Parse(data["actionWindow"].Value<string>());
                                var undoneWindow = JArray.Parse(data["undoneWindow"].Value<string>());

                                List<AIDungeonAction> actions = new List<AIDungeonAction>();
                                foreach (var action in actionWindow)
                                    actions.Add(ParseAction(action));
                                foreach (var action in undoneWindow)
                                    actions.Add(ParseAction(action));

                                OnAdventureLoaded?.Invoke(actions);
                            }
                            break;
                        case "actionAdded":
                            {
                                List<AIDungeonAction> actions = new List<AIDungeonAction>();
                                actions.Add(ParseAction(data));
                                OnActionAdded?.Invoke(actions);
                            }
                            break;
                        case "actionUpdated":
                            {
                                List<AIDungeonAction> actions = new List<AIDungeonAction>();
                                actions.Add(ParseAction(data));
                                OnActionUpdated?.Invoke(actions);
                            }
                            break;
                    }
                }
            }
        }

        private AIDungeonAction ParseAction(JToken actionData)
        {
            var id = actionData["id"].Value<string>();
            var text = actionData["text"].Value<string>();
            var type = actionData["type"].Value<string>();
            var adventureId = actionData["adventureId"].Value<string>();
            var undoneAt = actionData["undoneAt"].Value<string>();
            var deletedAt = actionData["deletedAt"].Value<string>();
            var createdAt = actionData["createdAt"].Value<string>();

            //Value<DateTime>();

            return new AIDungeonAction() { id = id, text = text, type = type, adventureId = adventureId, undoneAt = undoneAt, deletedAt = deletedAt, createdAt = createdAt };
        }

        public void Dispose()
        {
            if (crawlingThread != null)
            {
                crawlingThread.Abort();
                crawlingThread = null;
            }
            if (driver != null)
            {
                driver.Close();
                driver.Quit();
                driver = null;
            }
        }
    }
}
