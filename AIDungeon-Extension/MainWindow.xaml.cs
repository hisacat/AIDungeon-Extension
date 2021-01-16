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
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private double fontSize;
        public double FontSize
        {
            get { return this.fontSize; }
            set
            {
                this.fontSize = value;
                OnPropertyChanged("fontSize");
            }
        }

        private FontFamily fontFamily;
        public FontFamily FontFamily
        {
            get { return this.fontFamily; }
            set
            {
                this.fontFamily = value;
                OnPropertyChanged("fontFamily");
            }
        }
        private FontWeight fontWeight;
        public FontWeight FontWeight
        {
            get { return this.fontWeight; }
            set
            {
                this.fontWeight = value;
                OnPropertyChanged("fontWeight");
            }
        }
        private FontStyle fontStyle;
        public FontStyle FontStyle
        {
            get { return this.fontStyle; }
            set
            {
                this.fontStyle = value;
                OnPropertyChanged("fontStyle");
            }
        }
        private TextDecorationCollection textDecorations;
        public TextDecorationCollection TextDecorations
        {
            get { return this.textDecorations; }
            set
            {
                this.textDecorations = value;
                OnPropertyChanged("textDecorationCollection");
            }
        }


        private Visibility loadingVisibility = Visibility.Hidden;
        public Visibility LoadingVisibility
        {
            get { return this.loadingVisibility; }
            set
            {
                this.loadingVisibility = value;
                OnPropertyChanged("loadingVisibility");
            }
        }

        private Visibility translateLoadingVisibility = Visibility.Hidden;
        public Visibility TranslateLoadingVisibility
        {
            get { return this.translateLoadingVisibility; }
            set
            {
                this.translateLoadingVisibility = value;
                OnPropertyChanged("translateLoadingVisibility");
            }
        }
        private Visibility inputLoadingVisibility = Visibility.Hidden;
        public Visibility InputLoadingVisibility
        {
            get { return this.inputLoadingVisibility; }
            set
            {
                this.inputLoadingVisibility = value;
                OnPropertyChanged("inputLoadingVisibility");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
        }

        private string statusText;
        public string StatusText
        {
            get { return this.statusText; }
            set
            {
                this.statusText = value;
                OnPropertyChanged("statusText");
            }
        }
    }

    public partial class MainWindow : Window
    {
        private MainWindowViewModel viewModel = null;
        public static readonly RoutedUICommand Reset = new RoutedUICommand("Reset", "Reset", typeof(MainWindow));
        private const string DefaultStatusText = "[Tips] Press 'Enter' to translate, 'Ctrl+Z' to revert to original text, 'Ctrl+Enter' to send, 'Shift+Enter' to newline,";

        public FontFamily actionFont { get; set; }

        private AIDungeonHooker hooker = null;
        private DisplayAIDActionContainer actionContainer = null;

        private Translator translator = null;
        //private LiveTranslator inputTextTranslator = null;
        //private WebBrowserTranslator webBrowserTranslator = null;

        private string currentAdventureId = string.Empty;

        private System.Threading.Thread updateThread = null;

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
            this.viewModel = new MainWindowViewModel();
            this.DataContext = this.viewModel;

            this.actionsTextBox.Text = string.Empty;
            this.viewModel.StatusText = DefaultStatusText;

            this.viewModel.LoadingVisibility = Visibility.Hidden;
            this.viewModel.TranslateLoadingVisibility = Visibility.Hidden;
            this.viewModel.InputLoadingVisibility = Visibility.Hidden;

            //---
            UpdateMode(WriteMode.Say);

            updateThread = new System.Threading.Thread(Update);
            updateThread.Start();

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
                    //this.actionsTextBox.Text += action.Translated + System.Environment.NewLine;
                }
                this.actionsTextBox.ScrollToEnd();
            });
        }

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
            this.actionContainer.Edited(action);
        }
        private void OnActionRestored(AIDungeonWrapper.Action action)
        {
            this.actionContainer.Deleted(action);
            this.actionContainer.Add(action);
        }

        /*
        private void UpdateActionFromAdventure(AIDungeonWrapper.Adventure adventure)
        {
            lock (lockObj)
            {
                if (currentAdventureId != adventure.id)
                {
                    currentAdventureId = adventure.id;
                    currentActions.Clear();
                }

                if (adventure.actionWindow == null)
                    return;

                adventure.actionWindow.Sort();
                int count = adventure.actionWindow.Count;
                for (int i = 0; i < count; i++)
                {
                    var action = adventure.actionWindow[i];
                    DisplayAIDAction displayAction = new DisplayAIDAction(action);
                    //displayAction.Updated = true;
                    if (currentActions.Exists(x => x.Id == action.id))
                    {
                        var originAction = currentActions.First(x => x.Id == action.id);
                        if (originAction.Action.text != action.text)
                        {
                            currentActions.RemoveAll(x => x.Id == action.id);
                            currentActions.Add(displayAction);
                        }
                    }
                    else
                    {
                        currentActions.Add(displayAction);
                    }

                    //Attach to head If next actions are continue action.
                    {
                        bool continueActionAdded = false;
                        for (int j = i + 1; j < count; j++)
                        {
                            var continueAction = adventure.actionWindow[j];

                            if (continueAction.type == "continue")
                            {
                                displayAction.ContinueActions.RemoveAll(x => x.id == continueAction.id);
                                displayAction.ContinueActions.Add(continueAction);
                                continueActionAdded = true;

                                i = j;
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (continueActionAdded)
                        {
                            displayAction.ContinueActions.Sort();
                            //displayAction.Updated = true;
                        }

                        displayAction.Update(this, translator);
                    }
                }

                //this.actionUpdated = true;
            }
        }
        private void UpdateAction_Removed(AIDungeonWrapper.Action action)
        {
            var origin = this.currentActions.FirstOrDefault(x => x.Id == action.id);
            if (origin != null)
            {
                var continueActions = origin.ContinueActions;
                this.currentActions.Remove(origin);

                //Create new HEAD if inner continueAction exist.
                if (continueActions.Count > 0)
                {
                    var head = new DisplayAIDAction(continueActions[0]);
                    //head.Updated = true;

                    continueActions.RemoveAt(0);

                    //Add remains continueActions to HEAD.
                    if (continueActions.Count > 0)
                    {
                        foreach (var continueAction in continueActions)
                            head.ContinueActions.Add(continueAction);
                        head.ContinueActions.Sort();

                        //head.Updated = true;
                        head.Update(this, translator);
                    }

                    this.currentActions.Add(head);
                    this.currentActions.Sort();
                }
            }
            else
            {
                int count = this.currentActions.Count;
                for (int i = 0; i < count; i++)
                {
                    var head = this.currentActions[i];
                    if (head.ContinueActions.Exists(x => x.id == action.id))
                    {
                        head.ContinueActions.RemoveAll(x => x.id == action.id);
                        head.ContinueActions.Sort();

                        //head.Updated = true;
                        head.Update(this, translator);
                        break;
                    }
                }
            }

            this.actionUpdated = true;
        }
        private void UpdateAction_Added(AIDungeonWrapper.Action action)
        {
            //Add action at last always
            this.currentActions.RemoveAll(x => x.Id == action.id);

            if (this.currentActions.Count > 0 && action.type == "continue")
            {
                var head = this.currentActions[this.currentActions.Count - 1];
                head.ContinueActions.Add(action);

                //head.Updated = true;
                head.Update(this, translator);
            }
            else
            {
                var head = new DisplayAIDAction(action);
                this.currentActions.Add(head);
                this.currentActions.Sort();

                //head.Updated = true;
                head.Update(this, translator);
            }

            this.actionUpdated = true;
        }
        private void UpdateAction_Edited(AIDungeonWrapper.Action action)
        {
            var origin = this.currentActions.FirstOrDefault(x => x.Id == action.id);
            if (origin != null)
            {
                var continueActions = origin.ContinueActions;
                this.currentActions.Remove(origin);
                var newAction = new DisplayAIDAction(action);
                newAction.ContinueActions = continueActions;
                this.currentActions.Add(newAction);

                //newAction.Updated = true;
                newAction.Update(this, translator);
                this.currentActions.Sort();
            }
            else
            {
                int count = this.currentActions.Count;
                for (int i = 0; i < count; i++)
                {
                    var head = this.currentActions[i];
                    if (head.ContinueActions.Exists(x => x.id == action.id))
                    {
                        head.ContinueActions.RemoveAll(x => x.id == action.id);
                        head.ContinueActions.Add(action);
                        head.ContinueActions.Sort();

                        //head.Updated = true;
                        head.Update(this, translator);
                        break;
                    }
                }
            }

            this.actionUpdated = true;
        }
        */

        private void Update()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(1);
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
            if (updateThread != null)
            {
                updateThread.Abort();
                updateThread = null;
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

        public void UpdateMode(WriteMode mode)
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
                            this.viewModel.TranslateLoadingVisibility = Visibility.Visible;
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
                                        this.viewModel.TranslateLoadingVisibility = Visibility.Hidden;
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
                        UpdateMode(WriteMode.Say);
                    }
                    else if (inputTextBox.Text.Equals("/do", StringComparison.OrdinalIgnoreCase))
                    {
                        e.Handled = true;
                        inputTextBox.Text = string.Empty;
                        UpdateMode(WriteMode.Do);
                    }
                    else if (inputTextBox.Text.Equals("/story", StringComparison.OrdinalIgnoreCase))
                    {
                        e.Handled = true;
                        inputTextBox.Text = string.Empty;
                        UpdateMode(WriteMode.Story);
                    }
                }
            }
        }

        public void SetStatusText(string text)
        {
            this.viewModel.StatusText = string.IsNullOrEmpty(text) ? DefaultStatusText : text;
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

                this.viewModel.FontFamily = fontFamily;
                this.viewModel.FontSize = fontSize;
                this.viewModel.FontWeight = fontWeight;
                this.viewModel.FontStyle = fontStyle;
                this.viewModel.TextDecorations = textDecorations;
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
            //Do
        }

        private void StackPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }
    }
}
