using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
        public const bool LoggingDataType = true;
        public const bool LoggingDataJson = true;
        public const double LoginFormWaitTimeout = 10;

        public bool InputLoading { get; private set; }
        public delegate void OnInputLoadingChangedDelegate(bool isOn);
        public event OnInputLoadingChangedDelegate OnInputLoadingChanged = null;

        private Dictionary<string, List<string>> xpaths = null;
        public const string XPathKeyPrefix = ">";
        public const string XPathKey_Login_IDInputBox = "Login_IDInputBox";
        public const string XPathKey_Login_PWInputBox = "Login_PWInputBox";
        public const string XPathKey_Login_LoginButton = "Login_LoginButton";
        public const string XPathKey_InputBox = "InputBox";
        public const string XPathKey_AIPopupCloseButton = "AIPopupCloseButton";

        private const string LogType_Performance = "performance";
        private const string StartUpURL = "https://play.aidungeon.io/main/loginRegister";

        private ChromeDriver driver = null;
        private Thread crawlingThread = null;
        public bool Ready { get; private set; }

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
            this.xpaths = new Dictionary<string, List<string>>();
            this.LoadXPaths();

            SetInputLoading(false);
            this.OnInputLoadingChanged?.Invoke(this.InputLoading);

            this.Ready = false;
            crawlingThread = new Thread(Work);
            crawlingThread.Start();
        }
        private void LoadDefaultXPaths()
        {
            this.xpaths.Add(XPathKey_Login_IDInputBox, new List<string>() { "//*[@class=\"css-11aywtz r-snp9zz\" and @type=\"email\"]" });
            this.xpaths.Add(XPathKey_Login_PWInputBox, new List<string>() { "//*[@class=\"css-11aywtz r-snp9zz\" and @type=\"password\"]" });
            this.xpaths.Add(XPathKey_Login_LoginButton, new List<string>() { "//*[@aria-label=\"Login\"]/div" });
            this.xpaths.Add(XPathKey_InputBox, new List<string>() { "//*[@class=\"css-1dbjc4n r-1awozwy r-18u37iz r-16y2uox\"]/textarea[@placeholder]" });
            this.xpaths.Add(XPathKey_AIPopupCloseButton, new List<string>() { "//*[@class=\"css-18t94o4 css-1dbjc4n r-1loqt21 r-u8s1d r-zchlnj r-ipm5af r-1otgn73 r-1i6wzkk r-lrvibr\" and @aria-label=\"close\"]" });
        }
        private void LoadXPaths()
        {
            lock (this.xpaths)
            {
                this.xpaths.Clear();

                LoadDefaultXPaths();

                var filePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "XPaths.txt");
                if (System.IO.File.Exists(filePath))
                {
                    var currentKey = string.Empty;
                    var lines = System.IO.File.ReadAllLines(filePath);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        if (line.StartsWith(XPathKeyPrefix))
                        {
                            currentKey = line.Remove(0, XPathKeyPrefix.Length);
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(currentKey))
                            continue;

                        if (!this.xpaths.ContainsKey(currentKey))
                            this.xpaths.Add(currentKey, new List<string>());

                        if (!this.xpaths[currentKey].Contains(line))
                            this.xpaths[currentKey].Add(line);
                    }
                }
                SaveXPaths();
                return;
            }
        }
        private void SaveXPaths()
        {
            var text = string.Empty;
            foreach (var key in this.xpaths.Keys)
            {
                text += string.Format("{0}{1}", XPathKeyPrefix, key) + System.Environment.NewLine;
                text += string.Join(System.Environment.NewLine, this.xpaths[key]);
                text += System.Environment.NewLine + System.Environment.NewLine;
            }

            var filePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "XPaths.txt");
            System.IO.File.WriteAllText(filePath, text);
        }

        private void Work()
        {
            if (driver != null)
            {
                driver.Quit();
                driver = null;
            }

            var options = new ChromeOptions();
            options.SetLoggingPreference(LogType_Performance, LogLevel.All);
            options.AddArgument("disable-gpu");
            options.AddArgument("disable-infobars");
            options.AddArgument("--disable-extensions");
            //options.AddUserProfilePreference("profile.default_content_settings", 2);
            //options.AddUserProfilePreference("profile.default_content_setting_values", 2);

            //options.AddArgument("-homepage \"" + StartUpURL + "\"");
            options.AddArgument("user-agent=Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6)" +
                "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.100 Safari/537.36"); //Not bot

            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            try
            {
                driver = new ChromeDriver(service, options);
                driver.Navigate().GoToUrl(StartUpURL);
                this.Ready = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[Exception] Hooker-CrawlingScripts-LoadChromeDriver: " + e.Message);
                Process[] chromeDriverProcesses = Process.GetProcessesByName("chromedriver");
                foreach (var chromeDriverProcess in chromeDriverProcesses)
                {
                    try
                    {
                        if (chromeDriverProcess.MainModule.FileName.StartsWith(
                            System.AppDomain.CurrentDomain.BaseDirectory))
                        {
                            chromeDriverProcess.Kill();
                        }
                    }
                    catch (Exception _e)
                    {
                        Console.WriteLine("[Exception] Hooker-CrawlingScripts-Kill: " + _e.Message);
                    }
                }
                System.Environment.Exit(-1);
            }
            //Do login
            try
            {
                string account_id;
                if (SaveAccountWindow.GetSavedAccount_ID(out account_id))
                {
                    //Loading..?
                    IWebElement loginFormElement = null;
                    IWebElement idInput = null;
                    IWebElement pwInput = null;
                    var started = System.DateTime.Now;
                    do
                    {
                        loginFormElement = driver.FindElementByXPathOrNull("/html/body/div[2]");
                        idInput = FindElementWithXPathDict(XPathKey_Login_IDInputBox);
                        pwInput = FindElementWithXPathDict(XPathKey_Login_PWInputBox);
                        if ((System.DateTime.Now - started).TotalSeconds > LoginFormWaitTimeout)
                            throw new Exception("Timeout");

                    } while (loginFormElement == null || idInput == null || pwInput == null);
                    Console.WriteLine("[Log] Loginform wait completed: {0}", (System.DateTime.Now - started).TotalSeconds);

                    idInput.SendKeys(account_id);
                    account_id = null;

                    string account_pw;
                    if (SaveAccountWindow.GetSavedAccount_Password(out account_pw))
                    {
                        pwInput.SendKeys(account_pw);
                        account_pw = null;

                        var loginButton = FindElementWithXPathDict(XPathKey_Login_LoginButton);
                        if (loginButton != null)
                            loginButton.Click();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Exception] Hooker-AutoLogin: " + e.Message);
            }

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
                    Console.WriteLine("[Exception] Hooker-CrawlingScripts: " + e.Message);
                }
            }
        }

        private void ParsePayloadData(JToken payloadData)
        {
            try
            {
                JToken tempToken = null;
                tempToken = payloadData["type"]; if (tempToken == null) return;
                var payloadDataType = tempToken.Value<string>();

                tempToken = payloadData["id"]; if (tempToken == null) return;
                var payloadDataId = tempToken.Value<string>();

                tempToken = payloadData["payload"]; if (tempToken == null) return;
                var payload = tempToken;

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

                        if (LoggingDataType)
                        {
                            if (LoggingDataJson)
                                Console.WriteLine("[Log] Hooker-detect: {0}:\r\n{1}", dataType, json);
                            else
                                Console.WriteLine("[Log] Hooker-detect: {0}", dataType);

                        }
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
                            #region Action result callbacks
                            case "undoAction": //when Undo.
                            case "restoreAction": //when Restore or redo(case)
                            case "retryAction": //when Retry.
                            case "addAction": //when action added or error(AI cant reply)
                            case "editAction": //when Edit (Its not loading but just add for safety)
                                //case "updateAdventureMemory" from bottom.
                                SetInputLoading(false);
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
                                SetInputLoading(false);
                                break;
                            case "adventure":
                                OnAdventureCallback(data.ToObject<AIDungeonWrapper.Adventure>());
                                break;
                            case "adventureUpdated":
                                var testobj = data.ToObject<AIDungeonWrapper.Adventure>();
                                OnAdventureUpdatedCallback(data.ToObject<AIDungeonWrapper.Adventure>());
                                Console.WriteLine(testobj.allPlayers[0].isTyping);
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
                                    Console.WriteLine("[TRACE] Unknown dataType: {0}", dataType);
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Exception] Hooker-ParsePayloadData: " + e.Message);
            }
        }

        private IWebElement FindElementWithXPathDict(string xPathKey)
        {
            try
            {
                lock (xpaths)
                {
                    foreach (var xpath in this.xpaths[xPathKey])
                    {
                        var element = driver.FindElementByXPathOrNull(xpath);
                        if (element != null)
                            return element;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Exception] Hooker-FindElement FindElementWithXPathDict: Key {0} XPath {1}", xPathKey, e.Message);
            }
            return null;
        }

        public bool SendText(string text)
        {
            if (!this.Ready)
                return false;
            if (this.InputLoading)
                return false;

            try
            {
                var popupCloseButton = FindElementWithXPathDict(XPathKey_AIPopupCloseButton);
                if (popupCloseButton != null) popupCloseButton.Click();

                var inputBox = FindElementWithXPathDict(XPathKey_InputBox);
                if (inputBox == null) return false;
                if (!inputBox.Displayed) return false;

                inputBox.Clear();
                if (!string.IsNullOrEmpty(text))
                {
                    text = text.Replace("\r\n", "\r");
                    var lines = text.Split('\r');
                    int lineCount = lines.Length;

                    for (int i = 0; i < lineCount; i++)
                    {
                        inputBox.SendKeys(lines[i]);
                        if (i < lineCount - 1)
                            inputBox.SendKeys(Keys.Shift + Keys.Enter);
                    }
                }

                inputBox.SendKeys(Keys.Enter);
                SetInputLoading(true);
            }
            catch (Exception e)
            {
                Console.WriteLine("[Exception] Hooker-SendText: " + e.Message);
                return false;
            }

            return true;
        }
        public bool Command_Redo()
        {
            return SendText("/redo");
        }
        public bool Command_Undo()
        {
            return SendText("/undo");
        }
        public bool Command_Retry()
        {
            return SendText("/retry");
        }

        private void SetInputLoading(bool isOn)
        {
            if (this.InputLoading != isOn)
                this.InputLoading = isOn;

            OnInputLoadingChanged?.Invoke(this.InputLoading);
        }
        public void ForceSetInputLoading(bool isOn)
        {
            SetInputLoading(isOn);
        }

        public bool Refresh()
        {
            if (!this.Ready)
                return false;

            try
            {
                this.driver.Navigate().Refresh();
                this.SetInputLoading(false);
            }
            catch (Exception e)
            {
                Console.WriteLine("[Exception] Hooker-Refresh: " + e.Message);
                return false;
            }

            return true;
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
            //Basically, Action seems like guaranteed index order but i'm not sure.
            //Should sort by createdAt? ***id was not related with index***
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
            this.Ready = false;

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
