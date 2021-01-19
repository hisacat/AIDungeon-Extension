using System;
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
        private MainWindowViewModel vm = null;
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

            this.vm = new MainWindowViewModel();
            this.DataContext = this.vm;

            UpdateColorPickerColorsFromViewModel();

            this.vm.SideMenuVisibility = Visibility.Collapsed;
            this.vm.SideMenuButtonVisibility = Visibility.Visible;

            //return;

            this.vm.LoadingVisibility = Visibility.Visible;
            this.vm.LoadingText = Properties.Resources.LoadingText_Initializing;
            this.vm.TranslateLoadingVisibility = Visibility.Hidden;
            this.vm.InputLoadingVisibility = Visibility.Hidden;

            this.actionsTextBox.Text = string.Empty;

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
        private void UpdateActionText(List<DisplayAIDActionContainer.DisplayAIDAction> actions)
        {
            Dispatcher.Invoke(() =>
            {
                this.actionsTextBox.Text = string.Empty;
                foreach (var action in actions.ToArray())
                {
                    if (this.vm.ShowOriginTexts)
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
                            if (this.vm.ShowOriginTexts)
                                this.actionsTextBox.Text += "[번역중...]" + System.Environment.NewLine;
                            else
                                this.actionsTextBox.Text += action.Text + System.Environment.NewLine;
                            break;
                        case DisplayAIDActionContainer.DisplayAIDAction.TranslateStatusType.None:
                            this.actionsTextBox.Text += "[번역 준비중]" + System.Environment.NewLine;
                            break;
                    }

                    if (!this.vm.ShowOriginTexts)
                        this.actionsTextBox.Text += System.Environment.NewLine;
                }
                this.actionsTextBox.ScrollToEnd();
            });
        }

        #region AIDungeonHooker callbacks
        private void OnScenario(AIDungeonWrapper.Scenario scenario)
        {
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
            {
                actionContainer.Clear();
                this.currentAdventureId = adventure.id;
            }
            actionContainer.AddRange(adventure.actionWindow);
        }
        private void OnAdventureUpdated(AIDungeonWrapper.Adventure adventure)
        {
            if (adventure == null)
                return;

            if (this.currentAdventureId != adventure.id)
            {
                actionContainer.Clear();
                this.currentAdventureId = adventure.id;
            }
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

                            if (hooker.Ready)
                            {
                                var sendText = string.Format("/{0} {1}", this.writeMode, inputTextBox.Text);
                                inputTextBox.Text = string.Empty;

                                if (hooker != null)
                                    hooker.SendText(sendText);
                            }
                            return;
                        }
                    case ModifierKeys.None: //Translate
                        {
                            e.Handled = true;

                            inputTextBox.IsReadOnly = true;
                            this.vm.TranslateLoadingVisibility = Visibility.Visible;
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
                                        this.vm.TranslateLoadingVisibility = Visibility.Hidden;
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
            this.vm.StatusText = string.IsNullOrEmpty(text) ? DefaultStatusText : text;
        }

        private void SaveGameTexts()
        {
            var text = this.actionsTextBox.Text;

            var saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.Title = "Save text";
            saveFileDialog.FileName = "AIDungeon.txt";
            saveFileDialog.Filter = "Text|*.txt";
            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                File.WriteAllText(saveFileDialog.FileName, text);
        }
        private void ResetHooker()
        {
            this.actionContainer.Clear();
            this.actionsTextBox.Text = string.Empty;
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
            hooker.Run();

            this.Topmost = true;
            Task.Run(() =>
            {
                while (!hooker.Ready)
                    Thread.Sleep(1);

                this.Dispatcher.Invoke(() =>
                {
                    this.vm.LoadingVisibility = Visibility.Collapsed;
                    this.Topmost = false;
                    this.Activate();
                });
            });
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
            Translator.OpenDictionary();
        }
        private void UpdateTranslateDictionary()
        {
            if (this.translator != null)
            {
                this.vm.LoadingVisibility = Visibility.Visible;
                this.vm.LoadingText = Properties.Resources.LoadingText_UpdateDictionary;

                Task.Run(() =>
                {
                    this.translator.LoadDictionary();

                    this.Dispatcher.Invoke(() =>
                    {
                        this.vm.LoadingVisibility = Visibility.Collapsed;
                    });
                });
            }
        }
        private void OpenSideMenu()
        {
            this.vm.SideMenuVisibility = Visibility.Visible;
            this.vm.SideMenuButtonVisibility = Visibility.Collapsed;
        }
        private void CloseSideMenu()
        {
            this.vm.SideMenuVisibility = Visibility.Collapsed;
            this.vm.SideMenuButtonVisibility = Visibility.Visible;
        }
        private void OpenChangeFontDialog()
        {
            var fd = new System.Windows.Forms.FontDialog();

            //Select current font
            if (this.vm.FontFamily != null)
            {
                var isBold = this.vm.FontWeight == FontWeights.Bold ? true : false;
                var isItalic = this.vm.FontStyle == FontStyles.Italic ? true : false;
                var fdFontSize = (float)((this.vm.FontSize * 72.0) / 96.0);
                var fdFontStyle = System.Drawing.FontStyle.Regular;
                if (isBold) fdFontStyle = fdFontStyle | System.Drawing.FontStyle.Bold;
                if (isItalic) fdFontStyle = fdFontStyle | System.Drawing.FontStyle.Italic;

                if (fdFontSize <= 0)
                    fdFontSize = 9;

                string fdFontName = this.vm.FontFamily == null ? null : this.vm.FontFamily.Source;

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

                this.vm.FontFamily = fontFamily;
                this.vm.FontSize = fontSize;
                this.vm.FontWeight = fontWeight;
                this.vm.FontStyle = fontStyle;
                this.vm.TextDecorations = textDecorations;
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
                this.vm.BGImage = openFileDialog.FileName;
            }
        }
        private void ClearBGImage()
        {
            this.vm.BGImage = null;
        }
        private void ResetColorToDefault()
        {
            this.vm.TextColor = (SolidColorBrush)Application.Current.Resources["AID_White"];
            this.vm.BGColor = (SolidColorBrush)Application.Current.Resources["AID_Black"];
            this.vm.InputBoxColor = (SolidColorBrush)Application.Current.Resources["AID_Gray"];
            this.vm.InputTextColor = (SolidColorBrush)Application.Current.Resources["AID_White"];

            UpdateColorPickerColorsFromViewModel();
        }
        private void UpdateColorPickerColorsFromViewModel()
        {
            this.bgColorPicker.SelectedColor = this.vm.BGColor.Color;
            this.textColorPicker.SelectedColor = this.vm.TextColor.Color;
            this.inputBoxColorPicker.SelectedColor = this.vm.InputBoxColor.Color;
            this.inputTextColorPicker.SelectedColor = this.vm.InputTextColor.Color;
        }
        private void CancelInputTranslate()
        {
            if (this.inputTranslateWork != null)
            {
                Dispatcher.Invoke(() =>
                {
                    this.vm.TranslateLoadingVisibility = Visibility.Hidden;
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
                UpdateActionText(this.actionContainer.Actions);
        }
        private void OnDetachNewLineTextsChanged()
        {
            if (this.actionContainer != null)
                this.actionContainer.SetForceNewLine(this.vm.DetachNewlineTexts);
        }
        #endregion

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
                this.vm.TextColor = new SolidColorBrush(e.NewValue.Value);
        }
        private void bgColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
                this.vm.BGColor = new SolidColorBrush(e.NewValue.Value);
        }
        private void inputBoxColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
                this.vm.InputBoxColor = new SolidColorBrush(e.NewValue.Value);
        }
        private void inputTextColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
                this.vm.InputTextColor = new SolidColorBrush(e.NewValue.Value);
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