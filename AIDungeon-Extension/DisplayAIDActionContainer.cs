using AIDungeon_Extension.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDungeon_Extension
{
    class DisplayAIDActionContainer
    {
        bool forceNewline = true;
        bool doTranslate = true;

        public class DisplayAIDAction : IComparer<DisplayAIDAction>, IComparable<DisplayAIDAction>
        {
            public AIDungeonWrapper.Action Action { get; set; }
            public List<AIDungeonWrapper.Action> InnerActions { get; set; }

            private DisplayAIDActionContainer container = null;
            private Translator.TranslateWorker translateWork = null;

            public string Text { get; private set; }
            public string Translated { get; private set; }

            public enum TranslateStatusType : int
            {
                Abort = -2,
                Failed = -1,
                None = 0,
                Working = 1,
                Success = 2,
            }
            public TranslateStatusType TranslateStatus { get; private set; }
            public string TranslateFailedReason { get; private set; }

            public DisplayAIDAction(DisplayAIDActionContainer container, AIDungeonWrapper.Action action)
            {
                this.Action = action;
                this.InnerActions = new List<AIDungeonWrapper.Action>();

                this.container = container;
                this.translateWork = null;
                this.TranslateStatus = TranslateStatusType.None;
            }
            public void UpdatedCallback()
            {
                this.InnerActions.Sort();

                this.Text = this.Action.text;
                foreach (var action in this.InnerActions)
                    this.Text += action.text;

                if (container.forceNewline && StartsWithNewLine(this.Action.text))
                {
                    if (this.Text.StartsWith("\n"))
                        this.Text.Remove(0, 1);
                    else if (this.Text.StartsWith("\r\n"))
                        this.Text.Remove(0, 2);
                }

                if (translateWork != null)
                {
                    translateWork.Abort();
                    translateWork = null;
                }

                //Translate.
                if (container.doTranslate)
                {
                    this.TranslateStatus = TranslateStatusType.Working;
                    translateWork = container.translator.Translate(this.Text, "en", "ko", (r) =>
                    {
                        this.Translated = r;
                        this.TranslateStatus = TranslateStatusType.Success;
                    },
                    failed: (reason) =>
                    {
                        this.TranslateFailedReason = reason;
                        this.TranslateStatus = TranslateStatusType.Failed;
                    },
                    finished: () =>
                    {
                        container.TranslatedCallback(this);
                    });
                }
            }
            public void Dispose()
            {
                if (translateWork != null)
                {
                    translateWork.Abort();
                    translateWork = null;
                }
            }
            public int CompareTo(DisplayAIDAction other)
            {
                if (other == null || other.Action == null)
                    return 0;

                return Action.CompareTo(other.Action);
            }

            public int Compare(DisplayAIDAction x, DisplayAIDAction y)
            {
                return x.CompareTo(y);
            }
        }
        public List<DisplayAIDAction> Actions { get; private set; }
        private Translator translator = null;

        public delegate void ActionsChangedDelegate(List<DisplayAIDAction> actions);
        public delegate void TranslatedDelegate(List<DisplayAIDAction> actions, DisplayAIDAction translated);
        public event ActionsChangedDelegate OnActionsChanged;
        public event TranslatedDelegate OnTranslated;

        public DisplayAIDActionContainer(Translator translator)
        {
            this.Actions = new List<DisplayAIDAction>();
            this.translator = translator;
        }

        public void Clear()
        {
            foreach (var action in this.Actions)
                action.Dispose();

            this.Actions.Clear();
            OnActionsChanged?.Invoke(this.Actions);
            return;
        }

        public void AddRange(List<AIDungeonWrapper.Action> actions)
        {
            if (actions == null) return;
            actions.Sort();

            int count = actions.Count;
            for (int i = 0; i < count; i++)
            {
                var head = this.Actions.FirstOrDefault(x => x.Action.id == actions[i].id);
                if (head == null)
                {
                    head = new DisplayAIDAction(this, actions[i]);
                    this.Actions.Add(head);
                    this.Actions.Sort();
                }

                for (int idx = i + 1; idx < count; idx++)
                {
                    var targetAction = actions[idx];
                    if (targetAction.type != "continue") break;
                    //In force-newline option. make new head when text started with newLine
                    if (forceNewline && !StartsWithNewLine(targetAction.text)) break;
                    head.InnerActions.RemoveAll(x => x.id == targetAction.id);
                    head.InnerActions.Add(targetAction);

                    i = idx;
                }

                head.UpdatedCallback();
            }
            OnActionsChanged?.Invoke(this.Actions);
            return;
        }
        public void Add(AIDungeonWrapper.Action action)
        {
            var target = this.Actions.FirstOrDefault(x => x.Action.id == action.id);
            if (target == null)
            {
                target = new DisplayAIDAction(this, action);

                if (target.Action.type == "continue")
                {
                    bool makeNewHead = false;
                    //In force-newline option. starts with newline action is another head.
                    if (forceNewline)
                    {
                        if (StartsWithNewLine(target.Action.text))
                            makeNewHead = true;
                    }

                    if (this.Actions.IndexOf(target) == 0) //First action is always new head.
                        makeNewHead = true;

                    if (makeNewHead) //Make new head.
                    {
                        this.Actions.Add(target);
                        this.Actions.Sort();
                        target.UpdatedCallback();
                    }
                    else //Attach to parent's inner actions.
                    {
                        this.Actions.Add(target); //Add temp action for get index with sort
                        this.Actions.Sort();
                        {
                            var head = this.Actions[this.Actions.IndexOf(target) - 1]; //Parent head(-1 index)
                            head.InnerActions.RemoveAll(x => x.id == target.Action.id);
                            head.InnerActions.Add(target.Action);
                            head.UpdatedCallback();
                        }
                        this.Actions.Remove(target); //Remove temp action
                    }
                }
                else
                {
                    this.Actions.Add(target);
                    this.Actions.Sort();
                    target.UpdatedCallback();
                }

                OnActionsChanged?.Invoke(this.Actions);
                return;
            }
            else
            {
                //Action already exist. do edit.
                Edited(action);
            }
        }
        public void Deleted(AIDungeonWrapper.Action action)
        {
            var target = this.Actions.FirstOrDefault(x => x.Action.id == action.id);
            if (target != null)
            {
                var innerActions = target.InnerActions;
                int idx = this.Actions.IndexOf(target);
                this.Actions.Remove(target);
                target.Dispose();

                //Detach innerAction and make to head
                {
                    if (innerActions.Count > 0)
                    {
                        //Make new head
                        if (idx <= 0 | //If first idx is always newline.
                            (forceNewline && StartsWithNewLine(target.Action.text)))
                        {
                            //Newline: Make new head.
                            var head = new DisplayAIDAction(this, innerActions[0]);
                            innerActions.RemoveAt(0);
                            head.InnerActions = innerActions;

                            head.UpdatedCallback();

                            this.Actions.Add(head);
                            this.Actions.Sort();
                        }
                        //Attach innerActions to parent's head
                        else
                        {
                            var head = this.Actions[idx - 1];
                            foreach (var innerAction in innerActions)
                                head.InnerActions.Add(innerAction);

                            head.UpdatedCallback();
                        }
                    }
                }

                OnActionsChanged?.Invoke(this.Actions);
                return;
            }
            else
            {
                foreach (var head in this.Actions)
                {
                    var innerAction = head.InnerActions.FirstOrDefault(x => x.id == action.id);
                    if (innerAction != null)
                    {
                        head.InnerActions.Remove(innerAction);
                        head.UpdatedCallback();

                        OnActionsChanged?.Invoke(this.Actions);
                        return;
                    }
                }
            }
        }
        public void Edited(AIDungeonWrapper.Action action)
        {
            var target = this.Actions.FirstOrDefault(x => x.Action.id == action.id);
            if (target != null)
            {
                target.Action = action;
                target.UpdatedCallback();

                OnActionsChanged?.Invoke(this.Actions);
                return;
            }
            else
            {
                bool finded = false;
                foreach (var head in this.Actions)
                {
                    var innerAction = head.InnerActions.FirstOrDefault(x => x.id == action.id);
                    if (innerAction != null)
                    {
                        head.InnerActions.Remove(innerAction);
                        head.InnerActions.Add(action);
                        head.UpdatedCallback();

                        finded = true;

                    }
                }
                if(finded)
                {
                    OnActionsChanged?.Invoke(this.Actions);
                    return;
                }
                else
                {
                    //Cannot find action. do add.
                    Add(action);
                }
            }
        }

        private static bool StartsWithNewLine(string str)
        {
            return str.StartsWith("\r\n") || str.StartsWith("\n");
        }

        private void TranslatedCallback(DisplayAIDAction action)
        {
            OnTranslated?.Invoke(this.Actions, action);
        }

        public void Dispose()
        {
            foreach (var action in this.Actions)
                action.Dispose();

            this.Actions = null;
        }
    }
}
