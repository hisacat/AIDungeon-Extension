using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AIDungeon_Extension.DisplayAIDActionContainer;

namespace AIDungeon_Extension
{
    class ActionModel
    {
        public ObservableCollection<Action> Actions;

        public ActionModel()
        {
            Actions = new ObservableCollection<Action>();
        }
        public void Sort()
        {
            var tempNumberList = Actions.OrderBy(o => o.AIDAction.Action.createdAt).ToList();

            foreach (var temp in tempNumberList)
            {
                int oldIndex = Actions.IndexOf(temp);
                int newIndex = tempNumberList.IndexOf(temp);
                Actions.Move(oldIndex, newIndex);
            }
        }

        public class Action : INotifyPropertyChanged
        {
            public Action() { }

            public DisplayAIDAction AIDAction { get; private set; }
            public Action(DisplayAIDAction _AIDAction)
            {
                this.AIDAction = _AIDAction;
            }

            private string text;
            public string Text
            {
                get { return this.text; }
                set
                {
                    this.text = value;
                    OnPropertyChanged("text");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
            }
        }
    }
}
