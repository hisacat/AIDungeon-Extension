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
        //private bool forceNewline = true;

        public class AIDAction : IComparer<AIDAction>, IComparable<AIDAction>
        {
            public AIDungeonWrapper.Action Action { get; set; }
            public List<AIDungeonWrapper.Action> InnerActions { get; set; }

            private AIDAdventuresContainer container = null;

            public string Text { get; private set; }
            public bool IsModified { get; set; } //It will be lagacy. use LastUpdatedAt
            public DateTime LastUpdatedAt { get; private set; }

            public AIDAction(AIDAdventuresContainer container, AIDungeonWrapper.Action action)
            {
                this.Action = action;
                this.InnerActions = new List<AIDungeonWrapper.Action>();

                this.container = container;
            }
            public void UpdatedCallback()
            {
                this.InnerActions.Sort();

                var originText = this.Text;

                this.Text = this.Action.text;
                foreach (var action in this.InnerActions)
                    this.Text += action.text;

                if (this.Text != originText)
                {
                    this.IsModified = true;
                    this.LastUpdatedAt = DateTime.Now;
                }
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
        public class AIDAdventure
        {
            public readonly string Id = string.Empty;
            public readonly string PublicId = string.Empty;
            public List<AIDAction> Actions { get; private set; }
            public List<AIDungeonWrapper.Action> MetaActions { get; private set; }
            public AIDAdventure(string id, string publicId)
            {
                this.Id = id;
                this.PublicId = publicId;
                this.Actions = new List<AIDAction>();
                this.MetaActions = new List<AIDungeonWrapper.Action>();
            }
        }
        //Key is adventure id
        public Dictionary<string, AIDAdventure> Adventures { get; private set; }
        private Translator translator = null;

        public delegate void ActionsChangedDelegate(string publicId, List<AIDAction> actions);
        public event ActionsChangedDelegate OnActionsChanged;

        public AIDAdventuresContainer(Translator translator)
        {
            this.Adventures = new Dictionary<string, AIDAdventure>();
            this.translator = translator;
        }

        public void Clear()
        {
            foreach (var adventure in this.Adventures.Values)
            {
                lock (adventure.Actions)
                {
                    foreach (var action in adventure.Actions)
                        action.Dispose();
                    adventure.Actions.Clear();
                }

                OnActionsChanged?.Invoke(adventure.PublicId, adventure.Actions);
            }
            this.Adventures.Clear();
        }
        private AIDAdventure GetAdventureByID(string id)
        {
            if (this.Adventures.ContainsKey(id))
                return this.Adventures[id];
            else
            {
                throw new Exception("[ActionContainer] Cannot find adventure ID " + id);
            }
        }

        public void UpdateFromAdventure(AIDungeonWrapper.Adventure adventureData)
        {
            if (!this.Adventures.ContainsKey(adventureData.id))
                this.Adventures.Add(adventureData.id, new AIDAdventure(adventureData.id, adventureData.publicId));

            var adventure = this.Adventures[adventureData.id];

            var actions = adventureData.actionWindow.ToList();
            adventure.MetaActions.Clear();
            if (actions == null) return;

            adventure.MetaActions.AddRange(actions);

            UpdateFromMetaAction(adventure);
        }
        private void UpdateFromMetaAction(AIDAdventure adventure)
        {
            var actions = adventure.MetaActions;
            actions.Sort();

            lock (adventure.Actions)
            {
                var removedHeads = new List<AIDAction>(adventure.Actions);
                int count = actions.Count;
                for (int i = 0; i < count; i++)
                {
                    var head = adventure.Actions.FirstOrDefault(x => x.Action.id == actions[i].id);
                    if (head == null)
                    {
                        head = new AIDAction(this, actions[i]);
                        adventure.Actions.Add(head);
                        adventure.Actions.Sort();

                        head.UpdatedCallback();
                    }
                    else
                    {
                        head.UpdatedCallback();
                        removedHeads.Remove(head);
                    }

                    //merge action if they are one-line texts
                    var innerActions = new List<AIDungeonWrapper.Action>();
                    for (; i + 1 < count; i++)
                    {
                        if (!EndsWithNewLine(actions[i].text) && !StartsWithNewLine(actions[i + 1].text))
                            innerActions.Add(actions[i + 1]);
                        else
                            break;
                    }

                    var added = innerActions.Except(head.InnerActions);
                    var removed = head.InnerActions.Except(innerActions);
                    var innerActionChanged = (removed.Count() + added.Count()) > 0;
                    if (innerActionChanged)
                    {
                        head.InnerActions.Clear();
                        head.InnerActions.AddRange(innerActions);
                        head.InnerActions.Sort();
                        head.UpdatedCallback();
                    }
                }
                foreach (var removedHead in removedHeads)
                    adventure.Actions.Remove(removedHead);

            }
            OnActionsChanged?.Invoke(adventure.PublicId, adventure.Actions);
        }
        public void Add(AIDungeonWrapper.Action action)
        {
            var adventure = GetAdventureByID(action.adventureId);

            var origin = adventure.MetaActions.FirstOrDefault(x => x.id == action.id);
            if (origin != null)
            {
                //Already exist. do edit
                Edited(action);
            }
            else
            {
                adventure.MetaActions.Add(action);
                UpdateFromMetaAction(adventure);
            }
        }
        public void Deleted(AIDungeonWrapper.Action action)
        {
            var adventure = GetAdventureByID(action.adventureId);

            var origin = adventure.MetaActions.FirstOrDefault(x => x.id == action.id);
            if (origin != null)
                adventure.MetaActions.Remove(origin);

            UpdateFromMetaAction(adventure);
        }
        public void Edited(AIDungeonWrapper.Action action)
        {
            var adventure = GetAdventureByID(action.adventureId);

            var origin = adventure.MetaActions.FirstOrDefault(x => x.id == action.id);
            if (origin != null)
                adventure.MetaActions[adventure.MetaActions.IndexOf(origin)] = action;

            UpdateFromMetaAction(adventure);
        }

        public static bool StartsWithNewLine(string str)
        {
            return str.StartsWith("\r\n") || str.StartsWith("\n");
        }
        public static bool EndsWithNewLine(string str)
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
