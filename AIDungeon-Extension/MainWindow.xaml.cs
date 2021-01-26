using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
using System.Windows.Media.Animation;
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
        //Key is publicId
        private Dictionary<string, ActionsModel> actionsModels = null;
        public static readonly RoutedUICommand Reset = new RoutedUICommand("Reset", "Reset", typeof(MainWindow));
        public static readonly RoutedUICommand Save = new RoutedUICommand("Save", "Save", typeof(MainWindow));
        public static readonly RoutedUICommand Exit = new RoutedUICommand("Exit", "Exit", typeof(MainWindow));

        public const string DefaultStatusText = "[Tips] Press 'Enter' to translate, 'Ctrl+Z' to revert to original text, 'Ctrl+Enter' to send, 'Shift+Enter' to newline,";

        public FontFamily actionFont { get; set; }

        private AIDungeonHooker hooker = null;
        private AIDAdventuresContainer actionContainer = null;

        private Translator actionTranslator = null;
        private Translator inputTranslator = null;

        private string currentAdventurePublicId = string.Empty;
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

            this.model.TranslateLanguage = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            UpdateColorPickerColorsFromViewModel();

            CloseSideMenu();

            this.actionsModels = new Dictionary<string, ActionsModel>();

            this.scenarioOptionModel = new ScenarioOptionModel();
            this.scenarioOptionsControl.ItemsSource = this.scenarioOptionModel.Options;

            //return;

            this.model.LoadingText = Properties.Resources.LoadingText_Initializing;
            this.model.ShowInputTranslateLoading = false;
            this.model.ShowInputLoading = false;

            UpdateWriteMode(WriteMode.Say);

            actionTranslator = new Translator();
            actionTranslator.Run();
            inputTranslator = new Translator();
            inputTranslator.Run();

            actionContainer = new AIDAdventuresContainer(actionTranslator);
            actionContainer.OnActionsChanged += ActionContainer_OnActionsChanged;

            StartHooker();
        }

        private void ActionContainer_OnActionsChanged(string publicId, List<AIDAdventuresContainer.AIDAction> actions)
        {
            //Update action model
            Dispatcher.Invoke(() =>
            {
                lock (this.actionsModels)
                {
                    if (!this.actionsModels.ContainsKey(publicId))
                        this.actionsModels.Add(publicId, new ActionsModel());

                    var actionsModel = this.actionsModels[publicId];

                    actions.Sort();
                    foreach (var action in actions)
                    {
                        if (!actionsModel.Actions.Any(x => x.AIDAction == action))
                            actionsModel.Actions.Add(new ActionsModel.Action(action));

                        var actionModel = actionsModel.Actions.First(x => x.AIDAction == action);
                        if (action.IsModified)
                        {
                            var actionText = action.Text;

                            actionModel.OriginText = action.Text;

                            if (actionModel.AIDAction.Action.type == "continue")
                            {
                                if (AIDAdventuresContainer.StartsWithNewLine(actionText))
                                    actionText = actionText.Remove(0, 1);
                            }
                            else
                            {
                                if (AIDAdventuresContainer.EndsWithNewLine(actionText))
                                    actionText = actionText.Remove(actionText.Length - 1, 1);
                            }

                            actionModel.OriginText = actionText;

                            if (actionModel.TranslateWork != null)
                            {
                                actionModel.TranslateWork.Abort();
                                actionModel.TranslateWork = null;
                            }

                            actionModel.OnTranslating = true;
                            actionModel.TranslatedText = "[번역중...]";
                            actionModel.TranslateWork = actionTranslator.Translate(actionModel.OriginText, "en", model.TranslateLanguage,
                                (translated) =>
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        var prevScrollableHeight = actionsScrollViewer.ScrollableHeight;
                                        actionModel.TranslatedText = translated;
                                        this.actionsScrollViewer.UpdateLayout();
                                        Console.WriteLine("Diff " + (prevScrollableHeight - actionsScrollViewer.ScrollableHeight));

                                        var newScrollOffset = actionsScrollViewer.VerticalOffset - (prevScrollableHeight - actionsScrollViewer.ScrollableHeight);
                                        //DoSmoothScroll(this.actionsScrollViewer, newScrollOffset, new TimeSpan(0, 0, 1));
                                        this.actionsScrollViewer.ScrollToVerticalOffset(newScrollOffset);
                                    });

                                }, failed: (reason) =>
                                {
                                    Dispatcher.Invoke(() => { actionModel.TranslatedText = "[번역 실패] " + reason; });
                                }, finished: () =>
                                {
                                    Dispatcher.Invoke(() => { actionModel.OnTranslating = false; });
                                });

                            action.IsModified = false;
                        }
                    }
                    actionsModel.Sort();

                    foreach (var head in actionsModel.Actions.ToArray())
                    {
                        if (!actions.Contains(head.AIDAction))
                        {
                            if (head.TranslateWork != null)
                            {
                                head.TranslateWork.Abort();
                                head.TranslateWork = null;
                            }
                            actionsModel.Actions.Remove(head);
                        }
                    }

                }
                UpdateDisplayAction();
            });
        }

        private void DoSmoothScroll(ScrollViewer scrollViewer, double to, TimeSpan duration)
        {
            DoubleAnimation verticalAnimation = new DoubleAnimation();

            verticalAnimation.From = scrollViewer.VerticalOffset;
            verticalAnimation.To = to;
            verticalAnimation.Duration = new Duration(duration);

            Storyboard storyboard = new Storyboard();

            storyboard.Children.Add(verticalAnimation);
            Storyboard.SetTarget(verticalAnimation, scrollViewer);
            Storyboard.SetTargetProperty(verticalAnimation, new PropertyPath(AniScrollViewer.CurrentVerticalOffsetProperty)); // Attached dependency property
            storyboard.Begin();
        }

        /// <summary>
        /// Update displayed game texts
        /// /// </summary>
        private void UpdateDisplayAction()
        {
            Dispatcher.Invoke(() =>
            {
                lock (this.actionsModels)
                {
                    if (!this.actionsModels.ContainsKey(this.currentAdventurePublicId))
                        this.actionsModels.Add(this.currentAdventurePublicId, new ActionsModel());

                    this.actionsControl.ItemsSource = this.actionsModels[this.currentAdventurePublicId].Actions;
                }
            });
        }

        #region AIDungeonHooker callbacks
        private void OnAdventurePublicIdChagned(string publicId)
        {
            this.currentAdventurePublicId = publicId;
            this.hooker.ForceSetInputLoading(false);

            UpdateDisplayAction();
        }
        private void Hooker_OnURLChanged(string url, AIDungeonHooker.URLType type)
        {
            this.hooker.ForceSetInputLoading(false);

            switch (type)
            {
                case AIDungeonHooker.URLType.Play:
                case AIDungeonHooker.URLType.AdventurePlay:
                    this.model.IsInGame = true;
                    break;
                default:
                    this.model.IsInGame = false;
                    break;
            }
        }
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
            actionContainer.UpdateFromAdventure(adventure);
            Dispatcher.Invoke(() => this.actionsScrollViewer.ScrollToBottom());
        }
        private void OnAdventureUpdated(AIDungeonWrapper.Adventure adventure)
        {
            actionContainer.UpdateFromAdventure(adventure);
            Dispatcher.Invoke(() => this.actionsScrollViewer.ScrollToBottom());
        }
        private void OnActionsUndone(List<AIDungeonWrapper.Action> actions)
        {
            foreach (var action in actions)
                this.actionContainer.Deleted(action);
        }
        private void OnActionAdded(AIDungeonWrapper.Action action)
        {
            this.actionContainer.Add(action);
            Dispatcher.Invoke(() => this.actionsScrollViewer.ScrollToBottom());
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
                            if (inputTranslator != null)
                            {
                                this.inputTranslateWork = inputTranslator.Translate(inputTextBox.Text, model.TranslateLanguage, "en", (translated) =>
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

        private bool SaveGameTexts()
        {
            var text = string.Empty;

            if (!actionsModels.ContainsKey(currentAdventurePublicId))
                return false;

            foreach (var t in this.actionsModels[currentAdventurePublicId].Actions)
            {
                if (this.model.ShowOriginTexts)
                    text += t.OriginText + System.Environment.NewLine + t.TranslatedText + System.Environment.NewLine;
                else
                    text += t.TranslatedText + System.Environment.NewLine;
            }
            var saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.Title = "Save text";
            saveFileDialog.FileName = "AIDungeon.txt";
            saveFileDialog.Filter = "Text|*.txt";

            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                File.WriteAllText(saveFileDialog.FileName, text);

            return true;
        }
        private void ResetHooker()
        {
            this.actionContainer.Clear();
            this.actionsModels.Clear();

            this.hooker.Refresh();
        }
        private void RestartHooker()
        {
            if (this.hooker != null)
            {
                this.hooker.Dispose();
                this.hooker = null;
            }

            this.actionContainer.Clear();
            this.actionsModels.Clear();

            StartHooker();
        }
        private void StartHooker()
        {
            hooker = new AIDungeonHooker();
            hooker.OnAdventurePublicIdChagned += OnAdventurePublicIdChagned;
            hooker.OnURLChanged += Hooker_OnURLChanged;
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

        private void OpenTranslateDictionary()
        {
            Translator.OpenDictionaryFile();
        }
        private void UpdateTranslateDictionary()
        {
            UpdateTranslateDictionary(this.actionTranslator);
            UpdateTranslateDictionary(this.inputTranslator);
        }
        private void UpdateTranslateDictionary(Translator translator)
        {
            if (translator != null)
            {
                this.model.ShowLoading = true;
                this.model.LoadingText = Properties.Resources.LoadingText_UpdateDictionary;

                Task.Run(() =>
                {
                    translator.LoadDictionary();

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
            if (actionTranslator != null)
            {
                actionTranslator.Dispose();
                actionTranslator = null;
            }
            if(inputTranslator != null)
            {
                inputTranslator.Dispose();
                inputTranslator = null;
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
    }
}