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

namespace AIDungeon_Extension
{
    public partial class MainWindow : Window
    {
        public const string VersionStr = "0.1b";
        private MainWindowViewModel vm = null;
        public static readonly RoutedUICommand Reset = new RoutedUICommand("Reset", "Reset", typeof(MainWindow));
        public static readonly RoutedUICommand Save = new RoutedUICommand("Save", "Save", typeof(MainWindow));
        public static readonly RoutedUICommand Exit = new RoutedUICommand("Exit", "Exit", typeof(MainWindow));

        private const string DefaultStatusText = "[Tips] Press 'Enter' to translate, 'Ctrl+Z' to revert to original text, 'Ctrl+Enter' to send, 'Shift+Enter' to newline,";

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

            //Settings.BGColor = Color.FromArgb(1, 1, 1, 1);
            Settings.Init();

            this.vm = new MainWindowViewModel();
            this.DataContext = this.vm;

            this.vm.TextColor = (SolidColorBrush)Application.Current.Resources["AID_White"];
            this.vm.BGColor = (SolidColorBrush)Application.Current.Resources["AID_Black"];
            this.vm.InputBoxColor = (SolidColorBrush)Application.Current.Resources["AID_Gray"];
            this.vm.InputTextColor = (SolidColorBrush)Application.Current.Resources["AID_White"];
            this.vm.VersionText = VersionStr;
            this.vm.StatusText = DefaultStatusText;
            this.vm.SideMenuVisibility = Visibility.Collapsed;
            this.vm.SideMenuButtonVisibility = Visibility.Visible;

            #region Init Controls
            //this.showOriginalTexts.IsChecked = Settings.ShowOriginalTexts;
            //this.detachNewlineTexts.IsChecked = Settings.DetachNewlineTexts;
            this.bgColorPicker.SelectedColor = this.vm.BGColor.Color;
            this.textColorPicker.SelectedColor = this.vm.TextColor.Color;
            this.inputBoxColorPicker.SelectedColor = this.vm.InputBoxColor.Color;
            this.inputTextColorPicker.SelectedColor = this.vm.InputTextColor.Color;
            #endregion

            //---
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
                while (
                translator == null || !translator.Ready ||
                hooker == null || !hooker.Ready)
                    Thread.Sleep(1);

                this.Dispatcher.Invoke(() =>
                {
                    this.vm.LoadingVisibility = Visibility.Collapsed;
                    this.Topmost = false;
                    this.Activate();
                });
            });

            //this.vm.LoadingText = Properties.Resources.LoadingText_Initializing;
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
                foreach (var action in actions)
                {
                    if (Settings.ShowOriginalTexts)
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
                            this.actionsTextBox.Text += "[번역중...]" + System.Environment.NewLine;
                            break;
                        case DisplayAIDActionContainer.DisplayAIDAction.TranslateStatusType.None:
                            this.actionsTextBox.Text += "[번역 준비중]" + System.Environment.NewLine;
                            break;
                    }

                    if (Settings.ShowOriginalTexts)
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
            if (this.currentAdventureId != adventure.id)
            {
                actionContainer.Clear();
                this.currentAdventureId = adventure.id;
            }
            actionContainer.AddRange(adventure.actionWindow);
        }
        private void OnAdventureUpdated(AIDungeonWrapper.Adventure adventure)
        {
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
                                translator.Translate(inputTextBox.Text, "ko", "en", (translated) =>
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
                                    Dispatcher.Invoke(() =>
                                    {
                                        SetStatusText(string.Format("Translate error : {0}", reason));
                                    });
                                },
                                finished: () =>
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        this.vm.TranslateLoadingVisibility = Visibility.Hidden;
                                        inputTextBox.IsReadOnly = false;
                                    });
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
                        {
                            var text = this.actionsTextBox.Text;

                            var saveFileDialog = new System.Windows.Forms.SaveFileDialog();
                            saveFileDialog.Title = "Save text";
                            saveFileDialog.FileName = "AIDungeon.txt";
                            saveFileDialog.Filter = "Text|*.txt";
                            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                                File.WriteAllText(saveFileDialog.FileName, text);
                        }
                        break;
                    case "Exit":
                        {
                            this.Close();
                        }
                        break;
                    case "Reset":
                        {
                            this.actionContainer.Clear();
                            this.actionsTextBox.Text = string.Empty;
                            this.hooker.Refresh();
                        }
                        break;
                }
            }
        }

        private void Help_CheckForUpdate(object sender, RoutedEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = @"https://github.com/hisacat/AIDungeon-Extension",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        private void Help_Developer(object sender, RoutedEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = @"https://twitter.com/ahisacat",
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void UpdateTranslateDictionary(object sender, RoutedEventArgs e)
        {
            if(this.translator != null)
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
        private void RestartHooker(object sender, RoutedEventArgs e)
        {
            if (this.hooker != null)
            {
                this.hooker.Dispose();
                this.hooker = null;
            }

            this.hooker = new AIDungeonHooker();
            this.hooker.Run();
        }
        private void RestartTranslator(object sender, RoutedEventArgs e)
        {
            if (this.translator != null)
            {
                this.translator.Dispose();
                this.hooker = null;
            }

            this.translator = new Translator();
            this.translator.Run();
        }
        private void OpenSideMenu(object sender, RoutedEventArgs e)
        {
            this.vm.SideMenuVisibility = Visibility.Visible;
            this.vm.SideMenuButtonVisibility = Visibility.Collapsed;
        }
        private void CloseSideMenu(object sender, RoutedEventArgs e)
        {
            this.vm.SideMenuVisibility = Visibility.Collapsed;
            this.vm.SideMenuButtonVisibility = Visibility.Visible;
        }
        private void ChangeFont(object sender, RoutedEventArgs e)
        {
            var fd = new System.Windows.Forms.FontDialog();
            var result = fd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Debug.WriteLine(fd.Font);

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
        private void SideMenu_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox != null)
            {
                switch (checkbox.Name)
                {
                    case "showOriginalTexts":
                        Settings.ShowOriginalTexts = true;
                        this.ActionContainer_OnActionsChanged(this.actionContainer.Actions);
                        break;
                    case "detachNewlineTexts":
                        Settings.DetachNewlineTexts = true;
                        this.ActionContainer_OnActionsChanged(this.actionContainer.Actions);
                        Reset.Execute(null, null);
                        break;
                }
            }
        }
        private void SideMenu_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            switch (checkbox.Name)
            {
                case "showOriginalTexts":
                    Settings.ShowOriginalTexts = false;
                    this.ActionContainer_OnActionsChanged(this.actionContainer.Actions);
                    break;
                case "detachNewlineTexts":
                    Settings.DetachNewlineTexts = false;
                    this.ActionContainer_OnActionsChanged(this.actionContainer.Actions);
                    Reset.Execute(null, null);
                    break;
            }
        }
        protected override void OnClosed(EventArgs e)
        {
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

    }
}
