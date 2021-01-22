﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using AIDungeon_Extension.Core;

//Todo : 번역 도중 esc로 취소하는 기능
//Todo : 원문에 빈 라인 있으면 번역에도 추가
//Todo : Adventure(Game)이 바뀌었는지 체크를 URL의 변경을 통해서도 하자
//Todo : 게임 리스트 불러오기.

//AI가 입력중입니다. 텍스트 띄우기 로딩중.
namespace AIDungeon_Extension
{
    public partial class MainWindow : Window
    {
        public const string VersionStr = "0.1b";
        private MainWindowViewModel model = null;
        private ScenarioOptionModel scenarioOptionModel = null;
        private ActionModel actionModel = null;
        public static readonly RoutedUICommand Reset = new RoutedUICommand("Reset", "Reset", typeof(MainWindow));
        public static readonly RoutedUICommand Save = new RoutedUICommand("Save", "Save", typeof(MainWindow));
        public static readonly RoutedUICommand Exit = new RoutedUICommand("Exit", "Exit", typeof(MainWindow));

        public const string DefaultStatusText = "[Tips] Press 'Enter' to translate, 'Ctrl+Z' to revert to original text, 'Ctrl+Enter' to send, 'Shift+Enter' to newline,";

        public FontFamily actionFont { get; set; }

        private AIDungeonHooker hooker = null;
        private DisplayAIDActionContainer actionContainer = null;

        private Translator translator = null;

        private string currentAdventureId = string.Empty;
        public enum WriteMode : int
        {
            Say = 0,
            Do = 1,
            Story = 2,
        }
        private WriteMode writeMode = default;

        public MainWindow()
        {
            InitializeComponent();

            var chromeDriverPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "chromedriver.exe");
            if (!System.IO.File.Exists(chromeDriverPath))
            {
                var chromeVersion = string.Empty;
                {
                    const string suffix = @"Google\Chrome\Application\chrome.exe";
                    var prefixes = new List<string> { Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) };
                    var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    var programFilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                    if (programFilesx86 != programFiles)
                    {
                        prefixes.Add(programFiles);
                    }
                    else
                    {
                        var programFilesDirFromReg = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion", "ProgramW6432Dir", null) as string;
                        if (programFilesDirFromReg != null) prefixes.Add(programFilesDirFromReg);
                    }

                    prefixes.Add(programFilesx86);
                    var path = prefixes.Distinct().Select(prefix => System.IO.Path.Combine(prefix, suffix)).FirstOrDefault(File.Exists);

                    if (!string.IsNullOrEmpty(path))
                    {
                        chromeVersion = FileVersionInfo.GetVersionInfo(path.ToString()).FileVersion;
                    }
                }

                if (MessageBox.Show(string.Format(Properties.Resources.MessageBox_ChromeDriverMissing_Text, chromeVersion),
                    Properties.Resources.MessageBox_ChromeDriverMissing_Caption, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    OpenURL(@"https://chromedriver.chromium.org/downloads");
                }
                System.Environment.Exit(-1);
            }

            this.model = new MainWindowViewModel();
            this.DataContext = this.model;

            UpdateColorPickerColorsFromViewModel();

            CloseSideMenu();

            this.actionModel = new ActionModel();
            this.actionsControl.ItemsSource = this.actionModel.Actions;

            this.scenarioOptionModel = new ScenarioOptionModel();
            this.scenarioOptionsControl.ItemsSource = this.scenarioOptionModel.Options;


            //return;

            this.model.LoadingText = Properties.Resources.LoadingText_Initializing;
            this.model.ShowInputTranslateLoading = false;
            this.model.ShowInputLoading = false;

            //this.actionsTextBox.Text = string.Empty;

            UpdateWriteMode(WriteMode.Say);

            translator = new Translator();
            translator.Run();

            actionContainer = new DisplayAIDActionContainer(translator);
            actionContainer.OnActionsChanged += ActionContainer_OnActionsChanged;
            actionContainer.OnTranslated += ActionContainer_OnTranslated;

            StartHooker();
        }

        private void ActionContainer_OnActionsChanged(List<DisplayAIDActionContainer.DisplayAIDAction> actions)
        {
            UpdateActionText(actions);
        }
        private void ActionContainer_OnTranslated(List<DisplayAIDActionContainer.DisplayAIDAction> actions, DisplayAIDActionContainer.DisplayAIDAction translated)
        {
            UpdateActionText(actions);
        }
        private void UpdateActionText(List<DisplayAIDActionContainer.DisplayAIDAction> actions, bool forceUpdatee = false)
        {
            //여러 윈도우가 뎁스로 쌓여있어서 뒤 윈도우에 값을 전달할때가 있음. 이경우 꼬여버림.
            //해결책이 필요
            Dispatcher.Invoke(() =>
            {
                var actionsClone = actions.ToArray();

                foreach (var action in actionsClone)
                {
                    if (!this.actionModel.Actions.Any(x => x.AIDAction == action))
                        this.actionModel.Actions.Add(new ActionModel.Action(action));

                    var actionModelNode = this.actionModel.Actions.First(x => x.AIDAction == action);

                    if(action.IsModified || forceUpdatee)
                    {
                        actionModelNode.Text = string.Empty;

                        if (this.model.ShowOriginTexts)
                            actionModelNode.Text = action.Text + System.Environment.NewLine;

                        switch (action.TranslateStatus)
                        {
                            case DisplayAIDActionContainer.DisplayAIDAction.TranslateStatusType.Abort:
                                actionModelNode.Text += "[번역 취소됨]" + System.Environment.NewLine;
                                break;
                            case DisplayAIDActionContainer.DisplayAIDAction.TranslateStatusType.Failed:
                                actionModelNode.Text += "[번역 실패!]:" + action.TranslateFailedReason;
                                break;
                            case DisplayAIDActionContainer.DisplayAIDAction.TranslateStatusType.Success:
                                actionModelNode.Text += action.Translated + System.Environment.NewLine;
                                break;
                            case DisplayAIDActionContainer.DisplayAIDAction.TranslateStatusType.Working:
                                if (this.model.ShowOriginTexts)
                                    actionModelNode.Text += "[번역중...]" + System.Environment.NewLine;
                                else
                                    actionModelNode.Text += action.Text + System.Environment.NewLine;
                                break;
                            case DisplayAIDActionContainer.DisplayAIDAction.TranslateStatusType.None:
                                actionModelNode.Text += "[번역 준비중]" + System.Environment.NewLine;
                                break;
                        }

                        if (!this.model.ShowOriginTexts)
                            actionModelNode.Text += System.Environment.NewLine;
                        action.IsModified = false;
                    }
                }
                this.actionModel.Sort();

                foreach (var actionModelNode in this.actionModel.Actions.ToArray())
                {
                    if (!actionsClone.Contains(actionModelNode.AIDAction))
                        this.actionModel.Actions.Remove(actionModelNode);
                }

                //this.actionModel.Actions.

            });
            /*
            Dispatcher.Invoke(() =>
            {
                this.actionsTextBox.Text = string.Empty;
                //게임 시작하고 난 뒤에 윈도우끄면 끄면 여기서 무한루프됨. 이유 알아보자.
                foreach (var action in actions.ToArray())
                {
                    if (this.model.ShowOriginTexts)
                        this.actionsTextBox.Text += action.Text + System.Environment.NewLine;

                    switch (action.TranslateStatus)
                    {
                        case DisplayAIDActionContainer.DisplayAIDAction.TranslateStatusType.Abort:
                            this.actionsTextBox.Text += "[번역 취소됨]" + System.Environment.NewLine;
                            break;
                        case DisplayAIDActionContainer.DisplayAIDAction.TranslateStatusType.Failed:
                            this.actionsTextBox.Text += "[번역 실패!]:" + action.TranslateFailedReason;
                            break;
                        case DisplayAIDActionContainer.DisplayAIDAction.TranslateStatusType.Success:
                            this.actionsTextBox.Text += action.Translated + System.Environment.NewLine;
                            break;
                        case DisplayAIDActionContainer.DisplayAIDAction.TranslateStatusType.Working:
                            if (this.model.ShowOriginTexts)
                                this.actionsTextBox.Text += "[번역중...]" + System.Environment.NewLine;
                            else
                                this.actionsTextBox.Text += action.Text + System.Environment.NewLine;
                            break;
                        case DisplayAIDActionContainer.DisplayAIDAction.TranslateStatusType.None:
                            this.actionsTextBox.Text += "[번역 준비중]" + System.Environment.NewLine;
                            break;
                    }

                    if (!this.model.ShowOriginTexts)
                        this.actionsTextBox.Text += System.Environment.NewLine;
                }
                this.actionsTextBox.ScrollToEnd();
            });
            */
        }

        private void AdventureChangedCallback(AIDungeonWrapper.Adventure adventure)
        {
            actionContainer.Clear();
            this.currentAdventureId = adventure.id;

            if (this.hooker != null)
                this.hooker.ForceSetInputLoading(false);
        }

        #region AIDungeonHooker callbacks
        private void OnScenario(AIDungeonWrapper.Scenario scenario)
        {
            /*
            this.Dispatcher.Invoke(() =>
            {
                this.model.PromptText = scenario.prompt;
                this.scenarioOptionModel.Options.Clear();
                if (scenario.options != null && scenario.options.Count >= 0)
                {
                    int optionCount = scenario.options.Count;
                    for (int i = 0; i < optionCount; i++)
                    {
                        //Order is index+1 in normal case.
                        this.scenarioOptionModel.Options.Add(
                            new ScenarioOptionModel.Option() { OrderText = (i + 1).ToString(), Text = scenario.options[i].title });
                    }
                }
            });
            */
        }
        private void OnUpdateAdventureMemory(AIDungeonWrapper.Adventure adventure)
        {
            var memory = adventure.memory;
            var authorsNote = adventure.authorsNote;
        }
        private void OnAdventure(AIDungeonWrapper.Adventure adventure)
        {
            if (adventure == null)
                return;

            if (this.currentAdventureId != adventure.id)
                AdventureChangedCallback(adventure);

            actionContainer.AddRange(adventure.actionWindow);
        }
        private void OnAdventureUpdated(AIDungeonWrapper.Adventure adventure)
        {
            if (adventure == null)
                return;

            if (this.currentAdventureId != adventure.id)
                AdventureChangedCallback(adventure);

            actionContainer.AddRange(adventure.actionWindow);
        }
        private void OnActionsUndone(List<AIDungeonWrapper.Action> actions)
        {
            foreach (var action in actions)
                this.actionContainer.Deleted(action);
        }
        private void OnActionAdded(AIDungeonWrapper.Action action)
        {
            this.actionContainer.Add(action);
        }
        private void OnActionUpdated(AIDungeonWrapper.Action action)
        {
            if (string.IsNullOrEmpty(action.deletedAt))
            {
                this.actionContainer.Edited(action);
            }
            else
            {
                this.actionContainer.Deleted(action);
            }
        }
        private void OnActionRestored(AIDungeonWrapper.Action action)
        {
            this.actionContainer.Deleted(action);
            this.actionContainer.Add(action);
        }
        #endregion

        private bool CheckItCommandText(string input, string command)
        {
            if (input.StartsWith(command, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(input.Remove(0, command.Length)))
                    return true;
                else
                    return false;
            }
            return false;
        }

        private Translator.TranslateWorker inputTranslateWork = null;
        private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            /*
             * Will support.
             * [Done] Say
             * [Done] Do
             * [Done] Story (only in adventure)
             * Undo (텍스트 직접보냄)
             * Redo (텍스트 직접보냄)
             * Retry (대답 다시하기)
             * Alter (마지막 대화문 수정)
             * Remember (기억하기 / Style hint descriptive?)
             * 
             * ---Report
             * ---Restore
             * ---World info
             */

            if (inputTextBox.IsReadOnly)
            {
                e.Handled = false;
                return;
            }

            if (e.Key == Key.Enter)
            {
                switch (Keyboard.Modifiers)
                {
                    case ModifierKeys.Shift: //New line
                        {
                            e.Handled = false;
                            return;
                        }
                    case ModifierKeys.Control: //Send
                        {
                            e.Handled = true;

                            if (this.hooker != null && this.hooker.Ready)
                            {
                                #region Check it command
                                if (inputTextBox.Text.StartsWith("/"))
                                {
                                    if (CheckItCommandText(inputTextBox.Text, "/redo"))
                                    {
                                        inputTextBox.Text = string.Empty;
                                        this.hooker.Command_Redo();
                                        return;
                                    }
                                    if (CheckItCommandText(inputTextBox.Text, "/undo"))
                                    {
                                        inputTextBox.Text = string.Empty;
                                        this.hooker.Command_Undo();
                                        return;
                                    }
                                    if (CheckItCommandText(inputTextBox.Text, "/retry"))
                                    {
                                        inputTextBox.Text = string.Empty;
                                        this.hooker.Command_Retry();
                                        return;
                                    }
                                }
                                #endregion

                                var sendText = string.Format("/{0} {1}", this.writeMode, inputTextBox.Text);

                                if (this.hooker != null)
                                {
                                    if (this.hooker.SendText(sendText))
                                    {
                                        inputTextBox.Text = string.Empty;
                                    }
                                    else
                                    {
                                        this.SetStatusText("Cannot send text. hooker is busy");
                                    }
                                }
                            }
                            return;
                        }
                    case ModifierKeys.None: //Translate
                        {
                            e.Handled = true;

                            inputTextBox.IsReadOnly = true;
                            this.model.ShowInputTranslateLoading = true;
                            SetStatusText("Translating...");
                            if (translator != null)
                            {
                                this.inputTranslateWork = translator.Translate(inputTextBox.Text, "ko", "en", (translated) =>
                                {
                                    Dispatcher.Invoke(() =>
                                   {
                                       inputTextBox.Text = translated;
                                       inputTextBox.CaretIndex = inputTextBox.Text.Length;
                                       SetStatusText(null);
                                   });
                                },
                                failed: (reason) =>
                                {
                                    if (!inputTranslateWork.isAborted)
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            SetStatusText(string.Format("Translate error : {0}", reason));
                                        });
                                    }
                                },
                                finished: () =>
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        this.model.ShowInputTranslateLoading = false;
                                        inputTextBox.IsReadOnly = false;
                                    });
                                    this.inputTranslateWork = null;
                                });
                            }
                            return;
                        }
                }
                return;
            }

            SetStatusText(null);
            if (e.Key == Key.Space)
            {
                if (inputTextBox.Text.StartsWith("/"))
                {
                    if (inputTextBox.Text.Equals("/say", StringComparison.OrdinalIgnoreCase))
                    {
                        e.Handled = true;
                        inputTextBox.Text = string.Empty;
                        UpdateWriteMode(WriteMode.Say);
                    }
                    else if (inputTextBox.Text.Equals("/do", StringComparison.OrdinalIgnoreCase))
                    {
                        e.Handled = true;
                        inputTextBox.Text = string.Empty;
                        UpdateWriteMode(WriteMode.Do);
                    }
                    else if (inputTextBox.Text.Equals("/story", StringComparison.OrdinalIgnoreCase))
                    {
                        e.Handled = true;
                        inputTextBox.Text = string.Empty;
                        UpdateWriteMode(WriteMode.Story);
                    }
                }
            }
        }

        #region Functions
        public void UpdateWriteMode(WriteMode mode)
        {
            this.writeMode = mode;
            switch (mode)
            {
                case WriteMode.Say:
                    placeHolderTextBlock.Text = "What do you say?";
                    break;
                case WriteMode.Do:
                    placeHolderTextBlock.Text = "What do you do?";
                    break;
                case WriteMode.Story:
                    placeHolderTextBlock.Text = "What happens next?";
                    break;
            }
        }
        public void SetStatusText(string text)
        {
            this.model.StatusText = string.IsNullOrEmpty(text) ? DefaultStatusText : text;
        }

        private void SaveGameTexts()
        {
            //var text = this.actionsTextBox.Text;

            var saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.Title = "Save text";
            saveFileDialog.FileName = "AIDungeon.txt";
            saveFileDialog.Filter = "Text|*.txt";
            //if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //    File.WriteAllText(saveFileDialog.FileName, text);
        }
        private void ResetHooker()
        {
            this.actionContainer.Clear();
            //this.actionsTextBox.Text = string.Empty;
            this.hooker.Refresh();
        }
        private void StartHooker()
        {
            hooker = new AIDungeonHooker();
            hooker.OnScenario += OnScenario;
            hooker.OnUpdateAdventureMemory += OnUpdateAdventureMemory;
            hooker.OnAdventure += OnAdventure;
            hooker.OnAdventureUpdated += OnAdventureUpdated;
            hooker.OnActionsUndone += OnActionsUndone;
            hooker.OnActionAdded += OnActionAdded;
            hooker.OnActionUpdated += OnActionUpdated;
            hooker.OnActionRestored += OnActionRestored;
            hooker.OnInputLoadingChanged += OnInputLoadingChanged;
            hooker.Run();

            this.Topmost = true;
            this.model.ShowLoading = true;
            Task.Run(() =>
            {
                while (hooker != null && !hooker.Ready)
                    Thread.Sleep(1);

                if (hooker == null)
                {
                    Console.WriteLine("[ERROR] Hooker missing at StartHooker");
                    System.Environment.Exit(-1);
                    return;
                }

                this.Dispatcher.Invoke(() =>
                {
                    this.model.ShowLoading = false;
                    this.Topmost = false;
                    this.Activate();
                });
            });
        }

        private void OnInputLoadingChanged(bool isOn)
        {
            this.model.ShowInputLoading = isOn;
        }

        private void RestartHooker()
        {
            if (this.hooker != null)
            {
                this.hooker.Dispose();
                this.hooker = null;
            }

            StartHooker();
        }
        private void OpenTranslateDictionary()
        {
            Translator.OpenDictionaryFile();
        }
        private void UpdateTranslateDictionary()
        {
            if (this.translator != null)
            {
                this.model.ShowLoading = true;
                this.model.LoadingText = Properties.Resources.LoadingText_UpdateDictionary;

                Task.Run(() =>
                {
                    this.translator.LoadDictionary();

                    this.Dispatcher.Invoke(() =>
                    {
                        this.model.ShowLoading = false;
                    });
                });
            }
        }
        private void OpenSideMenu()
        {
            this.model.ShowSideMenu = true;
            this.model.ShowSideMenuButton = !this.model.ShowSideMenu;
        }
        private void CloseSideMenu()
        {
            this.model.ShowSideMenu = false;
            this.model.ShowSideMenuButton = !this.model.ShowSideMenu;
        }
        private void OpenChangeFontDialog()
        {
            var fd = new System.Windows.Forms.FontDialog();

            //Select current font
            if (this.model.FontFamily != null)
            {
                var isBold = this.model.FontWeight == FontWeights.Bold ? true : false;
                var isItalic = this.model.FontStyle == FontStyles.Italic ? true : false;
                var fdFontSize = (float)((this.model.FontSize * 72.0) / 96.0);
                var fdFontStyle = System.Drawing.FontStyle.Regular;
                if (isBold) fdFontStyle = fdFontStyle | System.Drawing.FontStyle.Bold;
                if (isItalic) fdFontStyle = fdFontStyle | System.Drawing.FontStyle.Italic;

                if (fdFontSize <= 0)
                    fdFontSize = 9;

                string fdFontName = this.model.FontFamily == null ? null : this.model.FontFamily.Source;

                var fdFont = new System.Drawing.Font(fdFontName, fdFontSize, fdFontStyle);

                fd.Font = fdFont;
            }

            var result = fd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var fontFamily = new FontFamily(fd.Font.Name);
                var fontSize = fd.Font.Size * 96.0 / 72.0;
                var fontWeight = fd.Font.Bold ? FontWeights.Bold : FontWeights.Regular;
                var fontStyle = fd.Font.Italic ? FontStyles.Italic : FontStyles.Normal;

                TextDecorationCollection tdc = new TextDecorationCollection();
                if (fd.Font.Underline) tdc.Add(TextDecorations.Underline);
                if (fd.Font.Strikeout) tdc.Add(TextDecorations.Strikethrough);
                var textDecorations = tdc;

                this.model.FontFamily = fontFamily;
                this.model.FontSize = fontSize;
                this.model.FontWeight = fontWeight;
                this.model.FontStyle = fontStyle;
                this.model.TextDecorations = textDecorations;
            }
        }
        private void OpenSelectBGImageDialog()
        {
            var openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Title = "Open image";
            openFileDialog.DefaultExt = "jpg";
            openFileDialog.Filter = "Images Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg;*.jpeg;*.gif;*.bmp;*.png";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.model.BGImage = openFileDialog.FileName;
            }
        }
        private void ClearBGImage()
        {
            this.model.BGImage = null;
        }
        private void ResetColorToDefault()
        {
            this.model.TextColor = (SolidColorBrush)Application.Current.Resources["AID_White"];
            this.model.BGColor = (SolidColorBrush)Application.Current.Resources["AID_Black"];
            this.model.InputBoxColor = (SolidColorBrush)Application.Current.Resources["AID_Gray"];
            this.model.InputTextColor = (SolidColorBrush)Application.Current.Resources["AID_White"];

            UpdateColorPickerColorsFromViewModel();
        }
        private void UpdateColorPickerColorsFromViewModel()
        {
            this.bgColorPicker.SelectedColor = this.model.BGColor.Color;
            this.textColorPicker.SelectedColor = this.model.TextColor.Color;
            this.inputBoxColorPicker.SelectedColor = this.model.InputBoxColor.Color;
            this.inputTextColorPicker.SelectedColor = this.model.InputTextColor.Color;
        }
        private void CancelInputTranslate()
        {
            if (this.inputTranslateWork != null)
            {
                Dispatcher.Invoke(() =>
                {
                    this.model.ShowInputTranslateLoading = false;
                    inputTextBox.IsReadOnly = false;
                });
                this.inputTranslateWork.Abort();
            }
        }
        private void OpenURL(string url)
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void OnShownOriginTextsChanged()
        {
            if (this.actionContainer != null)
                UpdateActionText(this.actionContainer.Actions, true);
        }
        private void OnDetachNewLineTextsChanged()
        {
            if (this.actionContainer != null)
                this.actionContainer.SetForceNewLine(this.model.DetachNewlineTexts);
        }
        #endregion

        private void ControlMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.hooker == null || !this.hooker.Ready || this.hooker.InputLoading)
                return;

            var button = sender as AIDMenuButtonControl;
            if (button == null) return;
            switch (button.Tag)
            {
                case "redo":
                    this.hooker.Command_Redo();
                    break;
                case "undo":
                    this.hooker.Command_Undo();
                    break;
                case "retry":
                    this.hooker.Command_Retry();
                    break;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    CancelInputTranslate();
                    break;
            }
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = string.Empty;
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                item = menuItem.Header.ToString();
            }
            else
            {
                var executedRoutedEventArga = e as ExecutedRoutedEventArgs;
                if (executedRoutedEventArga != null)
                {
                    item = (executedRoutedEventArga.Command as RoutedUICommand).Text;
                }
            }
            if (!string.IsNullOrEmpty(item))
            {
                switch (item)
                {
                    case "Save":
                        SaveGameTexts();
                        break;
                    case "Exit":
                        this.Close();
                        break;
                    case "Reset":
                        ResetHooker();
                        break;
                }
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            //Dispose...

            if (hooker != null)
            {
                hooker.Dispose();
                hooker = null;
            }
            if (translator != null)
            {
                translator.Dispose();
                translator = null;
            }

            Process[] chromeDriverProcesses = Process.GetProcessesByName("chromedriver");
            foreach (var chromeDriverProcess in chromeDriverProcesses)
            {
                if (chromeDriverProcess.MainModule.FileName.StartsWith(
                    System.AppDomain.CurrentDomain.BaseDirectory))
                {
                    chromeDriverProcess.Kill();
                }
            }

            System.Environment.Exit(0);
        }

        #region ColorPicker callbacks
        private void textColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
                this.model.TextColor = new SolidColorBrush(e.NewValue.Value);
        }
        private void bgColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
                this.model.BGColor = new SolidColorBrush(e.NewValue.Value);
        }
        private void inputBoxColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
                this.model.InputBoxColor = new SolidColorBrush(e.NewValue.Value);
        }
        private void inputTextColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
                this.model.InputTextColor = new SolidColorBrush(e.NewValue.Value);
        }
        #endregion
        #region Button callbacks
        #region Menu
        private void Help_CheckForUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            OpenURL(@"https://github.com/hisacat/AIDungeon-Extension");
        }
        private void Help_DeveloperButton_Click(object sender, RoutedEventArgs e)
        {
            OpenURL(@"https://twitter.com/ahisacat");
        }
        private void SaveAccountMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new SaveAccountWindow();
            window.Left = this.Left;
            window.Top = this.Top;
            window.Show();
        }
        private void ClearAccountMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveAccountWindow.RemoveSavedAccount();
        }
        #endregion
        private void OpenSideMenuButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSideMenu();
        }
        private void CloseSideMenuButton_Click(object sender, RoutedEventArgs e)
        {
            CloseSideMenu();
        }
        #region SideMenu
        private void SideMenu_ChangeFontButton_Click(object sender, RoutedEventArgs e)
        {
            OpenChangeFontDialog();
        }
        private void SideMenu_ColorResetToDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            ResetColorToDefault();
        }
        private void SideMenu_SetBGImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectBGImageDialog();
        }
        private void SideMenu_ClearBGImageButton_Click(object sender, RoutedEventArgs e)
        {
            ClearBGImage();
        }
        private void SideMenu_OpenDictionaryButton_Click(object sender, RoutedEventArgs e)
        {
            OpenTranslateDictionary();
        }
        private void SideMenu_UpdateTranslateDictionaryButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateTranslateDictionary();
        }
        private void SideMenu_ResetHookerButton_Click(object sender, RoutedEventArgs e)
        {
            ResetHooker();
        }
        private void SideMenu_RestartHookerButton_Click(object sender, RoutedEventArgs e)
        {
            RestartHooker();
        }
        #endregion
        #endregion
        #region Checkbox callbacks
        private void SideMenu_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SideMenu_CheckBox_IsCheckedChanged(sender, e);
        }
        private void SideMenu_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SideMenu_CheckBox_IsCheckedChanged(sender, e);
        }
        private void SideMenu_CheckBox_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            switch (cb.Tag)
            {
                case "ShowOriginTexts":
                    OnShownOriginTextsChanged();
                    break;
                case "DetachNewlineTexts":
                    OnDetachNewLineTextsChanged();
                    break;
            }
        }
        #endregion

    }
}