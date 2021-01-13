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

            //When https://play.aidungeon.io/main/scenarioPlay
            public const string ScenarioPlay_InputTextArea = "//*[@id=\"root\"]/div/div[1]/div[3]/div/div/div[1]/div[1]/div/div/div/div/div/div[4]/div[2]/div[2]/div/div/div/div[1]/div/div/div/div/div[2]/div[2]/div/div/textarea";
            public const string ScenarioPlay_SubmitButton = "//*[@id=\"root\"]/div/div[1]/div[3]/div/div/div[1]/div[1]/div/div/div/div/div/div[4]/div[2]/div[2]/div/div/div/div[1]/div/div/div/div/div[2]/div[2]/div/div/div";

            //When https://play.aidungeon.io/main/play
            public const string Play_InputTextArea = "//*[@id=\"root\"]/div/div[1]/div[3]/div/div/div[1]/div[1]/div/div/div/div/div/div[5]/div[2]/div[2]/div/div/div/div[1]/div/div/div[3]/div[3]/div/div/textarea";
            public const string Play_SubmitButton = "//*[@id=\"root\"]/div/div[1]/div[3]/div/div/div[1]/div[1]/div/div/div/div/div/div[5]/div[2]/div[2]/div/div/div/div[1]/div/div/div[3]/div[3]/div/div/div";

            //When https://play.aidungeon.io/main/adventurePlay
            public const string AdventurePlay_InputTextArea = "//*[@id=\"root\"]/div/div[1]/div[3]/div/div/div[1]/div[1]/div/div/div/div/div/div/div[2]/div[2]/div/div/div/div[1]/div/div/div[3]/div[3]/div/div/textarea";
            public const string AdventurePlay_SubmitButton = "//*[@id=\"root\"]/div/div[1]/div[3]/div/div/div[1]/div[1]/div/div/div/div/div/div/div[2]/div[2]/div/div/div/div[1]/div/div/div[3]/div[3]/div/div/div";
        }

        private const string LogType_Performance = "performance";
        private const string StartUpURL = "https://play.aidungeon.io/main/loginRegister";

        private ChromeDriver driver = null;

        private System.Threading.Thread crawlingThread = null;

        public delegate void AdventureDelegate(AIDungeonWrapper.Adventure adventure);
        public delegate void ScenarioDelegate(AIDungeonWrapper.Scenario scenario);
        public delegate void ActionsDelegate(List<AIDungeonWrapper.Action> actions);
        public delegate void ActionDelegate(AIDungeonWrapper.Action action);
        public event AdventureDelegate OnUpdateAdventureMemory = null;
        public event ScenarioDelegate OnScenario = null;
        public event AdventureDelegate OnAdventure = null;
        public event AdventureDelegate OnAdventureUpdated = null;
        public event ActionsDelegate OnActionsUndone = null;
        public event ActionDelegate OnActionAdded = null;
        public event ActionDelegate OnActionUpdated = null;
        public event ActionDelegate OnActionRestored = null;

        public void Run()
        {
            var options = new ChromeOptions();
            options.SetLoggingPreference(LogType_Performance, LogLevel.All);

            var service = ChromeDriverService.CreateDefaultService();
            //service.HideCommandPromptWindow = true;

            driver = new ChromeDriver(service, options);

            driver.Navigate().GoToUrl(StartUpURL);

            crawlingThread = new System.Threading.Thread(CrawlingScripts);
            crawlingThread.Start();
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

        public bool SendText(string text)
        {
            var inputTextAreaXPath = string.Empty;
            var SubmitButtonXPath = string.Empty;

            if (driver.Url.StartsWith("https://play.aidungeon.io/main/scenarioPlay"))
            {
                inputTextAreaXPath = XPaths.ScenarioPlay_InputTextArea;
                SubmitButtonXPath = XPaths.ScenarioPlay_SubmitButton;
            }
            else if (driver.Url.StartsWith("https://play.aidungeon.io/main/play"))
            {
                inputTextAreaXPath = XPaths.Play_InputTextArea;
                SubmitButtonXPath = XPaths.Play_SubmitButton;
            }
            else if (driver.Url.StartsWith("https://play.aidungeon.io/main/adventurePlay"))
            {
                inputTextAreaXPath = XPaths.AdventurePlay_InputTextArea;
                SubmitButtonXPath = XPaths.AdventurePlay_SubmitButton;
            }
            else
            {
                //Unknown
                return false;
            }

            try
            {
                driver.FindElementByXPath(inputTextAreaXPath).Clear();
                if (!string.IsNullOrEmpty(text))
                {
                    text = text.Replace("\r\n", "\r");
                    var lines = text.Split('\r');
                    int lineCount = lines.Length;

                    var inputTextBoxElement = driver.FindElementByXPath(inputTextAreaXPath);
                    for (int i = 0; i < lineCount; i++)
                    {
                        inputTextBoxElement.SendKeys(lines[i]);
                        if (i < lineCount - 1)
                            inputTextBoxElement.SendKeys(Keys.Shift + Keys.Enter);
                    }
                }

                driver.FindElementByXPath(SubmitButtonXPath).Click();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            return true;
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
                            OnAddDeviceTokenCallback(data.Value<bool>());
                            break;
                        case "markAsTyping":
                            OnMarkAsTypingCallback(data.Value<bool>());
                            break;
                        case "featureFlags":
                            OnFeatureFlagsCallback(data.ToObject<List<AIDungeonWrapper.FeatureFlag>>());
                            break;
                        #endregion
                        #region Users
                        case "createAnonymousAccount":
                            OnCreateAnonymousAccountCallback(data.ToObject<AIDungeonWrapper.User>());
                            break;
                        case "login":
                            OnLoginCallback(data.ToObject<AIDungeonWrapper.User>());
                            break;
                        case "updateUser":
                            OnUpdateUserCallback(data.ToObject<AIDungeonWrapper.User>());
                            break;
                        case "user":
                            OnUserCallback(data.ToObject<AIDungeonWrapper.User>());
                            break;
                        #endregion
                        #region Game
                        case "scenario":
                            OnScenarioCallback(data.ToObject<AIDungeonWrapper.Scenario>());
                            break;
                        case "updateAdventureMemory":
                            OnUpdateAdventureMemoryCallback(data.ToObject<AIDungeonWrapper.Adventure>());
                            break;
                        case "adventure":
                            OnAdventureCallback(data.ToObject<AIDungeonWrapper.Adventure>());
                            break;
                        case "adventureUpdated":
                            OnAdventureUpdatedCallback(data.ToObject<AIDungeonWrapper.Adventure>());
                            break;
                        case "actionsUndone":
                            OnActionsUndoneCallback(data.ToObject<List<AIDungeonWrapper.Action>>());
                            break;
                        case "actionAdded":
                            OnActionAddedCallback(data.ToObject<AIDungeonWrapper.Action>());
                            break;
                        case "actionUpdated":
                            OnActionUpdatedCallback(data.ToObject<AIDungeonWrapper.Action>());
                            break;
                        case "actionRestored":
                            OnActionRestoredCallback(data.ToObject<AIDungeonWrapper.Action>());
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

        #region Data callbacks
        #region Etc.
        public void OnAddDeviceTokenCallback(bool value)
        {
        }
        public void OnMarkAsTypingCallback(bool value)
        {
        }
        public void OnFeatureFlagsCallback(List<AIDungeonWrapper.FeatureFlag> featureFlags)
        {
        }
        #endregion
        #region Users
        //Create account for not logined user.
        public void OnCreateAnonymousAccountCallback(AIDungeonWrapper.User user)
        {
        }
        //Logined. (When failed. some fields empty)
        public void OnLoginCallback(AIDungeonWrapper.User user)
        {
        }
        //Current user info updated.
        public void OnUpdateUserCallback(AIDungeonWrapper.User user)
        {
        }
        //User info updated. *Dont know difference with 'updateUser'
        public void OnUserCallback(AIDungeonWrapper.User user)
        {
        }
        #endregion
        #region Game
        //Scenario. It seems like option system.
        public void OnScenarioCallback(AIDungeonWrapper.Scenario scenario)
        {
            //In scenario. only can select menu and typing option id.
            this.OnScenario?.Invoke(scenario);
        }
        //Memoty,Authors Note changed. (Pin, Remember)
        public void OnUpdateAdventureMemoryCallback(AIDungeonWrapper.Adventure adventure)
        {
            //memory, memory might be edited.
            this.OnUpdateAdventureMemory?.Invoke(adventure);
        }
        //When adventure start/inited.
        public void OnAdventureCallback(AIDungeonWrapper.Adventure adventure)
        {
            //It might be called when game started. (webpage opened)
            //It sometimes return empty values at last one. (only id and empty message aray exist)
            //Use adventure id for check the game was changed.
            //Basically, Action id seems like based on ascending.
            this.OnAdventure?.Invoke(adventure);
        }
        //Adventure updated.
        public void OnAdventureUpdatedCallback(AIDungeonWrapper.Adventure adventure)
        {
            this.OnAdventureUpdated?.Invoke(adventure);
        }
        //Action removed from actionWindow.
        public void OnActionsUndoneCallback(List<AIDungeonWrapper.Action> actions)
        {
            this.OnActionsUndone?.Invoke(actions);
        }
        //Single action added.
        public void OnActionAddedCallback(AIDungeonWrapper.Action action)
        {
            this.OnActionAdded?.Invoke(action);
        }
        //Single action updated. (Current action edited?)
        public void OnActionUpdatedCallback(AIDungeonWrapper.Action action)
        {
            this.OnActionUpdated?.Invoke(action);
        }
        //Single action restored. -Text might be changed.
        public void OnActionRestoredCallback(AIDungeonWrapper.Action action)
        {
            this.OnActionRestored?.Invoke(action);
        }
        #endregion
        #endregion

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
