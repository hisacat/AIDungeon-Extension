using System;
using System.Collections;
using System.Collections.Generic;
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
        public static readonly RoutedUICommand Reset = new RoutedUICommand("Reset", "Reset", typeof(MainWindow));
        private bool Loading
        {
            get { return this.loadingIndicator.Visibility == Visibility.Visible; }
            set { this.loadingIndicator.Visibility = value ? Visibility.Visible : Visibility.Hidden; }
        }

        private AIDungeonHooker hooker = null;
        private Translator translator = null;
        //private LiveTranslator inputTextTranslator = null;
        //private WebBrowserTranslator webBrowserTranslator = null;

        private string currentAdventureId = string.Empty;

        public class DisplayAIDAction : IComparer<DisplayAIDAction>, IComparable<DisplayAIDAction>
        {
            public string Id { get; set; }
            public AIDungeonWrapper.Action Action { get; set; }
            public List<AIDungeonWrapper.Action> ContinueActions { get; set; }

            public bool OriginalTextChanged { get; set; }
            public string OriginalText { get; set; }
            public string TranslatedText { get; set; }

            public int CompareTo(DisplayAIDAction other)
            {
                if (other == null) return 0;
                return Action.CompareTo(other.Action);
            }
            public int Compare(DisplayAIDAction x, DisplayAIDAction y)
            {
                if (x == null) return 0;
                return x.CompareTo(y);
            }

            public DisplayAIDAction(AIDungeonWrapper.Action action)
            {
                this.Id = action.id;
                this.Action = action;

                this.ContinueActions = new List<AIDungeonWrapper.Action>();
                this.OriginalTextChanged = true;
                //this.DisplayText = string.Empty;
            }
        }
        private List<DisplayAIDAction> currentActions = null;
        private bool actionUpdated = false;
        private object lockObj = new object();

        private System.Threading.Thread updateThread = null;

        public enum WriteMode : int
        {
            Say = 0,
            Do = 1,
            Story = 2,
        }
        private WriteMode writeMode = default;

        private const string DefaultStatusText = "[Tips] Press 'Enter' to translate, 'Ctrl+Z' to revert to original text, 'Ctrl+Enter' to send, 'Shift+Enter' to newline,";

        public MainWindow()
        {
            InitializeComponent();

            this.Loading = false;
            this.translateLoadingGrid.Visibility = Visibility.Hidden;
            this.statusTextBlock.Text = DefaultStatusText;

            this.currentActions = new List<DisplayAIDAction>();

            UpdateMode(WriteMode.Say);

            updateThread = new System.Threading.Thread(Update);
            updateThread.Start();

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

            translator = new Translator();
            translator.Run();

            /*
            inputTextTranslator = new LiveTranslator();
            inputTextTranslator.Run();
            inputTextTranslator.Translated += OnSendTextTranslated;
            */
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
            UpdateActionFromAdventure(adventure);
        }
        private void OnAdventureUpdated(AIDungeonWrapper.Adventure adventure)
        {
            UpdateActionFromAdventure(adventure);
        }

        private void OnActionsUndone(List<AIDungeonWrapper.Action> actions)
        {
            lock (lockObj)
            {
                foreach (var action in actions)
                    UpdateAction_Removed(action);

                this.actionUpdated = true;
            }
        }
        private void OnActionAdded(AIDungeonWrapper.Action action)
        {
            lock (lockObj)
            {
                UpdateAction_Added(action);

                this.actionUpdated = true;
            }
        }
        private void OnActionUpdated(AIDungeonWrapper.Action action)
        {
            lock (lockObj)
            {
                UpdateAction_Edited(action);

                this.actionUpdated = true;
            }
        }
        private void OnActionRestored(AIDungeonWrapper.Action action)
        {
            lock (lockObj)
            {
                UpdateAction_Removed(action);
                UpdateAction_Added(action);

                this.actionUpdated = true;
            }
        }

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
                    displayAction.OriginalTextChanged = true;
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
                            displayAction.OriginalTextChanged = true;
                            displayAction.ContinueActions.Sort();
                            displayAction.OriginalTextChanged = true;
                        }
                    }
                }

                this.actionUpdated = true;
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
                    head.OriginalTextChanged = true;

                    continueActions.RemoveAt(0);

                    //Add remains continueActions to HEAD.
                    if (continueActions.Count > 0)
                    {
                        foreach (var continueAction in continueActions)
                            head.ContinueActions.Add(continueAction);
                        head.ContinueActions.Sort();
                        head.OriginalTextChanged = true;
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

                        head.OriginalTextChanged = true;
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
                head.OriginalTextChanged = true;
            }
            else
            {
                this.currentActions.Add(new DisplayAIDAction(action));
                this.currentActions.Sort();
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

                newAction.OriginalTextChanged = true;
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

                        head.OriginalTextChanged = true;
                        break;
                    }
                }
            }

            this.actionUpdated = true;
        }

        private void Update()
        {
            while (true)
            {
                if (actionUpdated)
                {
                    actionUpdated = false;

                    //innertexts. marger.
                    //영어도 일단 먼저 보여주긴 해야하니까, Translate는 나중에 업데이트되는식으로 뭔가 구상을해야할듯.
                    //Control 이용할까?
                    List<string> displayTexts = new List<string>();
                    lock (lockObj)
                    {
                        foreach (var action in currentActions)
                        {
                            var text = action.Action.text;
                            foreach (var continueAction in action.ContinueActions)
                            {
                                if (continueAction.text.StartsWith("\n") || continueAction.text.StartsWith("\r\n"))
                                {
                                    //Line에 관해서 : Continue일떄 다 붙이는게 아니라, 개행이 아닐떄만 붙이기 ?
                                    //고민해보자.
                                }
                                text += continueAction.text;
                            }
                            displayTexts.Add(text);
                        }
                    }

                    {
                        int count = displayTexts.Count;
                        /*
                        for (int i = 0; i < count; i++)
                            displayTexts[i] += Environment.NewLine + translator.DoTranslate(displayTexts[i]);
                        */

                        Dispatcher.Invoke(() =>
                        {
                            this.actionsTextBox.Text = string.Empty;
                            for (int i = 0; i < count; i++)
                                this.actionsTextBox.Text += displayTexts[i];

                            this.actionsTextBox.ScrollToEnd();
                        });
                    }
                }

                /*
                if (lastInputTranslated)
                    return;

                if ((DateTime.Now - lastInputTime).TotalSeconds > 0.5f)
                {
                    //inputTextTranslator.Translate(inputTextBox.Text);
                    lastInputTranslated = true;
                }
                */

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

        private void OnSendTextTranslated(string text)
        {
            translatedInputTextBox.Text = text;
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

                            hooker.SendText(sendText);
                            return;
                        }
                    case ModifierKeys.None: //Translate
                        {
                            e.Handled = true;

                            inputTextBox.IsReadOnly = true;
                            translateLoadingGrid.Visibility = Visibility.Visible;
                            SetStatusText("Translating...");
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
                                    translateLoadingGrid.Visibility = Visibility.Hidden;
                                    inputTextBox.IsReadOnly = false;
                                });
                            });
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
            this.statusTextBlock.Text = string.IsNullOrEmpty(text) ? DefaultStatusText : text;
        }

        DateTime lastInputTime = default;
        bool lastInputTranslated = false;
        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            lastInputTime = DateTime.Now;
            lastInputTranslated = false;
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
                            this.currentActions.Clear();
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

    }
}
