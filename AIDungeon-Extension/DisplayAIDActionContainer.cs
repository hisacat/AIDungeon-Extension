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
        bool forceNewline = false;
        bool doTranslate = false;

        public class DisplayAIDAction
        {
            public string Id { get; set; }
            public AIDungeonWrapper.Action Action { get; set; }
            public List<AIDungeonWrapper.Action> InnerActions { get; set; }

            private DisplayAIDActionContainer container = null;
            private Translator.TranslateWorker translateWork = null;

            public string Text { get; private set; }
            public string Translated { get; private set; }

            public DisplayAIDAction(DisplayAIDActionContainer container, AIDungeonWrapper.Action action)
            {
                this.container = container;
                this.Action = action;
            }
            public void UpdatedCallback()
            {
                this.InnerActions.Sort();

                this.Text = this.Action.text;
                foreach (var action in this.InnerActions)
                    this.Text += action.text;

                //Translate.
                if (translateWork != null)
                {
                    translateWork.Abort();
                    translateWork = null;
                }

                translateWork = container.translator.Translate(this.Text, "en", "ko", (r) => this.Translated = r,
                    failed: (reason) =>
                    {
                        this.Translated = string.Format("[Error: Translate failed] {0}", reason);
                    },
                    finished: () =>
                    {
                        container.TranslatedCallback(this);
                    });
            }
            public void Dispose()
            {
                if (translateWork != null)
                {
                    translateWork.Abort();
                    translateWork = null;
                }
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
        }

        public void AddRange(List<AIDungeonWrapper.Action> actions)
        {
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
                    head.InnerActions.Add(targetAction);

                    i = idx;
                }

                head.UpdatedCallback();
            }
            OnActionsChanged?.Invoke(this.Actions);
        }
        public void Add(AIDungeonWrapper.Action action)
        {
            var target = this.Actions.FirstOrDefault(x => x.Action.id == action.id);
            if (target == null)
            {
                target = new DisplayAIDAction(this, action);
                this.Actions.Add(target);
                this.Actions.Sort();

                //Find parrent and add to inner action when action type is continue.
                if (target.Action.type == "continue")
                {
                    //In force-newline option. starts with newline action is another head.
                    if (forceNewline && !StartsWithNewLine(target.Action.text))
                    {
                        var idx = this.Actions.IndexOf(target) - 1;
                        if (idx > 0)
                        {
                            var head = this.Actions[idx];
                            head.InnerActions.Add(target.Action);
                            this.Actions.Remove(target);

                            head.UpdatedCallback();
                        }
                    }
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
        public void Delete(AIDungeonWrapper.Action action)
        {
            var target = this.Actions.FirstOrDefault(x => x.Action.id == action.id);
            if (target != null)
            {
                var innerActions = target.InnerActions;
                int idx = this.Actions.IndexOf(target);
                this.Actions.RemoveAll(x => x.Id == action.id);

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

                        break;
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
                        break;
                    }
                }

                if (!finded)
                {
                    //Cannot find action. do add.
                    Add(action);
                }
            }
        }

        private bool StartsWithNewLine(string str)
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
