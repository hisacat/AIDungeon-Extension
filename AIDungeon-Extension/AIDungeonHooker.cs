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
        public static class XPaths
        {
            public const string Login_IdInput = "//*[@id=\"root\"]/div/div[1]/div[3]/div/div/div[1]/div[1]/div/div/div/div/div/div/div[2]/div[2]/div/div/div/div[1]/div/div/div/div/div[3]/input";
            public const string Login_PasswordInput = "//*[@id=\"root\"]/div/div[1]/div[3]/div/div/div[1]/div[1]/div/div/div/div/div/div/div[2]/div[2]/div/div/div/div[1]/div/div/div/div/div[4]/input";
            public const string Login_Button = "//*[@id=\"root\"]/div/div[1]/div[3]/div/div/div[1]/div[1]/div/div/div/div/div/div/div[2]/div[2]/div/div/div/div[1]/div/div/div/div/div[5]/div/div";

            public const string Game_InputTextArea = "//*[@id=\"root\"]/div/div[1]/div[3]/div/div/div[1]/div[1]/div/div/div/div/div/div[3]/div[2]/div[2]/div/div/div/div[1]/div/div/div/div/div[2]/div[2]/div/div/textarea";
            public const string Game_SubmitButton = "//*[@id=\"root\"]/div/div[1]/div[3]/div/div/div[1]/div[1]/div/div/div/div/div/div[3]/div[2]/div[2]/div/div/div/div[1]/div/div/div/div/div[2]/div[2]/div/div/div";
        }

        private const string LogType_Performance = "performance";
        private const string AIDungeonURL = "https://play.aidungeon.io/main/loginRegister";

        private ChromeDriver driver = null;

        private System.Threading.Thread crawlingThread = null;

        public delegate void ActionUpdatedDelegate(List<AIDungeonAction> actions);
        public event ActionUpdatedDelegate OnAdventureLoaded = null;
        public event ActionUpdatedDelegate OnActionAdded = null;
        public event ActionUpdatedDelegate OnActionUpdated = null;

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

        public class AIDungeonAction : IComparer<AIDungeonAction>, IComparable<AIDungeonAction>
        {
            public string id { get; set; }
            public string text { get; set; }
            public string type { get; set; }
            public string adventureId { get; set; }
            public string undoneAt { get; set; }
            public string deletedAt { get; set; }
            public string createdAt { get; set; }

            public int CompareTo(AIDungeonAction other)
            {
                if (this.id.Length == other.id.Length)
                    return this.id.CompareTo(id);
                else
                    return this.id.Length.CompareTo(other.id.Length);
            }

            public int Compare(AIDungeonAction x, AIDungeonAction y)
            {
                return x.CompareTo(y);
            }
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

                    var json = data.ToString();

                    //Ignore list - for debug.
                    bool skip = false;
                    var ignoreList = new string[] { "sendEvent", "sendExperimentEvent" };
                    foreach (var ignore in ignoreList)
                    {
                        if (dataType.Equals(ignore))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip) continue;

                    switch (dataType)
                    {
                        #region Others
                        case "search":
                        case "refreshSearchIndex":
                        case "saveContent":
                        case "updateAdventureWorldInfo":
                        case "scenarioLeaderboard":
                        case "price":
                            break;
                        #endregion
                        #region Bottom control button callbacks
                        case "editAction": //when Edit.
                        case "undoAction": //when Undo.
                        case "retryAction": //when Retry.
                        case "restoreAction": //when Restore.
                            break;
                        #endregion
                        #region Etc.
                        case "addDeviceToken":
                            {
                                var value = data.Value<bool>();
                            }
                            break;
                        case "markAsTyping":
                            {
                                var value = data.Value<bool>();
                            }
                            break;
                        case "featureFlags":
                            {
                                foreach (var child in data.Children())
                                {
                                    var featureFlag = child.ToObject<AIDungeonWrapper.FeatureFlag>();
                                }
                            }
                            break;
                        #endregion
                        #region Users
                        case "createAnonymousAccount": //Create account for not logined user.
                            {
                                var user = data.ToObject<AIDungeonWrapper.User>();
                            }
                            break;
                        case "login": //Logined. (When failed. some fields empty)
                            {
                                var user = data.ToObject<AIDungeonWrapper.User>();
                            }
                            break;
                        case "updateUser": //Current user info updated.
                            {
                                var user = data.ToObject<AIDungeonWrapper.User>();
                            }
                            break;
                        case "user": //User info updated. *Dont know difference with 'updateUser'
                            {
                                var user = data.ToObject<AIDungeonWrapper.User>();
                            }
                            break;
                        #endregion
                        #region Game
                        case "updateAdventureMemory": //Memoty,Authors Note changed. (Pin, Remember)
                            {
                                //memory, memory might be edited.
                                var adventure = data.ToObject<AIDungeonWrapper.Adventure>();
                            }
                            break;
                        case "scenario": //Scenario. It seems like option system.
                            {
                                //In scenario. only can select menu and typing option id.
                                var scenario = data.ToObject<AIDungeonWrapper.Scenario>();
                            }
                            break;
                        case "adventure": //When adventure start/inited.
                            {
                                //It might be called when game started. (webpage opened)
                                //It sometimes return empty values at last one. (only id and empty message aray exist)
                                //Use adventure id for check the game was changed.
                                //Basically, Action id seems like based on ascending.
                                var adventure = data.ToObject<AIDungeonWrapper.Adventure>();
                            }
                            break;
                        case "adventureUpdated": //Adventure updated.
                            {
                                var adventure = data.ToObject<AIDungeonWrapper.Adventure>();
                            }
                            break;
                        case "actionsUndone": //Remove action from actionWindow.
                            {
                                var actions = data.ToObject<List<AIDungeonWrapper.Action>>();
                            }
                            break;
                        case "actionAdded": //Single action added.
                            {
                                var action = data.ToObject<AIDungeonWrapper.Action>();
                            }
                            break;
                        case "actionUpdated": //Single action updated. (Current action edited?)
                            {
                                var action = data.ToObject<AIDungeonWrapper.Action>();
                            }
                            break;
                        case "actionRestored": //Single action restored. -Text might be changed.
                            {
                                var action = data.ToObject<AIDungeonWrapper.Action>();
                            }
                            break;
                        #endregion
                        default:
                            {
                                Console.WriteLine("[TRACE] Unknown dataType : {0}", dataType);
                            }
                            break;
                    }
                }
            }
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
                driver.Quit();
                driver = null;
            }
        }
    }
}
