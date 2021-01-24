using AIDungeon_Extension.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDungeon_Extension
{
    class AIDAdventuresContainer
    {
        private bool forceNewline = true;

        public class AIDAction : IComparer<AIDAction>, IComparable<AIDAction>
        {
            public AIDungeonWrapper.Action Action { get; set; }
            public List<AIDungeonWrapper.Action> InnerActions { get; set; }

            private AIDAdventuresContainer container = null;

            public string Text { get; private set; }
            public bool IsModified { get; set; }

            public AIDAction(AIDAdventuresContainer container, AIDungeonWrapper.Action action)
            {
                this.Action = action;
                this.InnerActions = new List<AIDungeonWrapper.Action>();

                this.container = container;
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

                this.IsModified = true;
            }
            public void Dispose()
            {
            }
            public int CompareTo(AIDAction other)
            {
                if (other == null || other.Action == null)
                    return 0;

                return Action.CompareTo(other.Action);
            }

            public int Compare(AIDAction x, AIDAction y)
            {
                return x.CompareTo(y);
            }
        }
        public class AIDAdventrue
        {
            public readonly string Id = string.Empty;
            public readonly string PublicId = string.Empty;
            public List<AIDAction> Actions { get; private set; }

            public AIDAdventrue(string id, string publicId)
            {
                this.Id = id;
                this.PublicId = publicId;
                this.Actions = new List<AIDAction>();
            }
        }
        //Key is adventure id
        //public List<DisplayAIDAction> Actions { get; private set; }
        public Dictionary<string, AIDAdventrue> Adventures { get; private set; }
        private Translator translator = null;

        public delegate void ActionsChangedDelegate(string publicId, List<AIDAction> actions);
        public event ActionsChangedDelegate OnActionsChanged;

        public AIDAdventuresContainer(Translator translator)
        {
            this.Adventures = new Dictionary<string, AIDAdventrue>();
            this.translator = translator;
        }

        public void SetForceNewLine(bool isOn)
        {
            if (this.forceNewline == isOn)
                return;

            this.forceNewline = isOn;

            foreach (var adventure in this.Adventures.Values)
            {
                var actions = new List<AIDungeonWrapper.Action>();
                foreach (var head in adventure.Actions)
                {
                    actions.Add(head.Action);
                    foreach (var innerAction in head.InnerActions)
                        actions.Add(innerAction);
                }
                adventure.Actions.Clear();

                UpdateFromAdventure(adventure.Id, adventure.PublicId, actions);
            }
        }

        public void Clear()
        {
            foreach (var adventure in this.Adventures.Values)
            {
                lock(adventure.Actions)
                {
                    foreach (var action in adventure.Actions)
                        action.Dispose();
                    adventure.Actions.Clear();
                }

                OnActionsChanged?.Invoke(adventure.PublicId, adventure.Actions);
            }
            this.Adventures.Clear();
        }

        private AIDAdventrue GetAdventureByID(string id)
        {
            if (this.Adventures.ContainsKey(id))
                return this.Adventures[id];
            else
            {
                throw new Exception("[ActionContainer] Cannot find adventure ID " + id);
            }
        }

        private void UpdateFromAdventure(string adventureId, string adventurePublicId, List<AIDungeonWrapper.Action> actions)
        {
            if (!this.Adventures.ContainsKey(adventureId))
                this.Adventures.Add(adventureId, new AIDAdventrue(adventureId, adventurePublicId));

            var adventure = this.Adventures[adventureId];

            if (actions == null)
                return;

            actions.Sort();

            lock (adventure.Actions)
            {
                int count = actions.Count;
                for (int i = 0; i < count; i++)
                {
                    var head = adventure.Actions.FirstOrDefault(x => x.Action.id == actions[i].id);
                    if (head == null)
                    {
                        head = new AIDAction(this, actions[i]);
                        adventure.Actions.Add(head);
                        adventure.Actions.Sort();
                    }

                    for (int idx = i + 1; idx < count; idx++)
                    {
                        var targetAction = actions[idx];
                        if (targetAction.type != "continue") break;

                        //In force-newline option. make new head when parent text started with newLine
                        if (forceNewline)
                        {
                            if (EndsWithNewLine(actions[idx - 1].text) ||
                                StartsWithNewLine(actions[idx].text))
                                break;
                        }

                        head.InnerActions.RemoveAll(x => x.id == targetAction.id);
                        head.InnerActions.Add(targetAction);

                        i = idx;
                    }

                    head.UpdatedCallback();
                }
            }
            OnActionsChanged?.Invoke(adventure.PublicId, adventure.Actions);
            return;
        }
        public void UpdateFromAdventure(AIDungeonWrapper.Adventure adventureData)
        {
            UpdateFromAdventure(adventureData.id, adventureData.publicId, adventureData.actionWindow);
        }
        public void Add(AIDungeonWrapper.Action action)
        {
            var adventure = GetAdventureByID(action.adventureId);

            var target = adventure.Actions.FirstOrDefault(x => x.Action.id == action.id);
            if (target == null)
            {
                lock (adventure.Actions)
                {
                    target = new AIDAction(this, action);
                    adventure.Actions.Add(target);
                    adventure.Actions.Sort();

                    if (target.Action.type == "continue")
                    {
                        var actionIndex = adventure.Actions.IndexOf(target);

                        bool makeNewHead = false;
                        //In force-newline option. starts with newline action is another head.
                        if (forceNewline)
                        {
                            if (actionIndex > 0)
                            {
                                if (EndsWithNewLine(adventure.Actions[actionIndex - 1].Action.text) ||
                                    StartsWithNewLine(adventure.Actions[actionIndex].Action.text))
                                    makeNewHead = true;
                            }
                        }

                        if (actionIndex == 0) //First action is always new head.
                            makeNewHead = true;

                        if (makeNewHead) //Make new head.
                        {
                            target.UpdatedCallback();
                        }
                        else //Attach to parent's inner actions.
                        {
                            var head = adventure.Actions[actionIndex - 1]; //Parent head(-1 index)
                            head.InnerActions.RemoveAll(x => x.id == target.Action.id);
                            head.InnerActions.Add(target.Action);
                            head.UpdatedCallback();

                            adventure.Actions.Remove(target); //Remove temp action
                        }
                    }
                    else
                    {
                        target.UpdatedCallback();
                    }
                }

                OnActionsChanged?.Invoke(adventure.PublicId, adventure.Actions);
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
            var adventure = GetAdventureByID(action.adventureId);

            var target = adventure.Actions.FirstOrDefault(x => x.Action.id == action.id);
            if (target != null)
            {
                lock (adventure.Actions)
                {
                    var innerActions = target.InnerActions;
                    int idx = adventure.Actions.IndexOf(target);
                    adventure.Actions.Remove(target);
                    target.Dispose();

                    //Detach innerAction and make to head
                    {
                        if (innerActions.Count > 0)
                        {
                            //Make new head
                            if (idx <= 0 || //If first idx is always newline.
                                (forceNewline && (EndsWithNewLine(adventure.Actions[idx - 1].Action.text) || StartsWithNewLine(adventure.Actions[idx].Action.text))))
                            {
                                //Newline: Make new head.
                                var head = new AIDAction(this, innerActions[0]);
                                innerActions.RemoveAt(0);
                                head.InnerActions = innerActions;

                                head.UpdatedCallback();

                                adventure.Actions.Add(head);
                                adventure.Actions.Sort();
                            }
                            //Attach innerActions to parent's head
                            else
                            {
                                var head = adventure.Actions[idx - 1];
                                foreach (var innerAction in innerActions)
                                    head.InnerActions.Add(innerAction);

                                head.UpdatedCallback();
                            }
                        }
                    }
                }
                OnActionsChanged?.Invoke(adventure.PublicId, adventure.Actions);
                return;
            }
            else
            {
                foreach (var head in adventure.Actions)
                {
                    var innerAction = head.InnerActions.FirstOrDefault(x => x.id == action.id);
                    if (innerAction != null)
                    {
                        lock (adventure.Actions)
                        {
                            head.InnerActions.Remove(innerAction);
                            head.UpdatedCallback();
                        }
                        OnActionsChanged?.Invoke(adventure.PublicId, adventure.Actions);
                        return;
                    }
                }
            }
        }
        public void Edited(AIDungeonWrapper.Action action)
        {
            var adventure = GetAdventureByID(action.adventureId);

            var target = adventure.Actions.FirstOrDefault(x => x.Action.id == action.id);
            if (target != null)
            {
                lock (adventure.Actions)
                {
                    target.Action = action;
                    target.UpdatedCallback();
                }
                OnActionsChanged?.Invoke(adventure.PublicId, adventure.Actions);
                return;
            }
            else
            {
                bool finded = false;
                foreach (var head in adventure.Actions)
                {
                    var innerAction = head.InnerActions.FirstOrDefault(x => x.id == action.id);
                    if (innerAction != null)
                    {
                        lock (adventure.Actions)
                        {
                            head.InnerActions.Remove(innerAction);
                            head.InnerActions.Add(action);
                            head.UpdatedCallback();

                            finded = true;
                        }
                    }
                }
                if (finded)
                {
                    OnActionsChanged?.Invoke(adventure.PublicId, adventure.Actions);
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
        private static bool EndsWithNewLine(string str)
        {
            return str.EndsWith("\r\n") || str.EndsWith("\n");
        }

        public void Dispose()
        {
            this.Clear();
            this.Adventures.Clear();
        }
    }
}
