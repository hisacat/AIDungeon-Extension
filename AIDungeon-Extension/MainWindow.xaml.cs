using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        private MainWindowViewModel vm = null;
        public static readonly RoutedUICommand Reset = new RoutedUICommand("Reset", "Reset", typeof(MainWindow));
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
            this.vm = new MainWindowViewModel();
            this.DataContext = this.vm;

            this.actionsTextBox.Text = string.Empty;
            this.vm.StatusText = DefaultStatusText;

            this.vm.LoadingVisibility = Visibility.Hidden;
            this.vm.TranslateLoadingVisibility = Visibility.Hidden;
            this.vm.InputLoadingVisibility = Visibility.Hidden;

            //---
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
        }

        /*
         * Will support.
         * Say
         * Do
         * Story (only in adventure)
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

        private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
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

                            var sendText = string.Format("/{0} {1}", this.writeMode, inputTextBox.Text);
                            inputTextBox.Text = string.Empty;

                            if (hooker != null)
                                hooker.SendText(sendText);
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

        public void SetStatusText(string text)
        {
            this.vm.StatusText = string.IsNullOrEmpty(text) ? DefaultStatusText : text;
        }

        private void ChangeFontButton_Click(object sender, RoutedEventArgs e)
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

        private void MenuItem_RestartHooker(object sender, RoutedEventArgs e)
        {

        }

        private void StackPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        private void SideMenuButton_Click(object sender, RoutedEventArgs e)
        {
            this.vm.SideMenuVisibility = Visibility.Visible;
            this.vm.SideMenuButtonVisibility = Visibility.Collapsed;
        }
        private void SideMenuCloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.vm.SideMenuVisibility = Visibility.Collapsed;
            this.vm.SideMenuButtonVisibility = Visibility.Visible;
        }
    }
}
