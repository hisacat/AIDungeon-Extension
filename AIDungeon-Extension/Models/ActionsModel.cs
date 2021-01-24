using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AIDungeon_Extension.AIDAdventuresContainer;

namespace AIDungeon_Extension
{
    class ActionsModel
    {
        public ObservableCollection<Action> Actions;

        public ActionsModel()
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

            public AIDAction AIDAction { get; private set; }
            public Action(AIDAction _AIDAction)
            {
                this.AIDAction = _AIDAction;
            }

            private string originText;
            public string OriginText
            {
                get { return this.originText; }
                set
                {
                    this.originText = value;
                    OnPropertyChanged("originText");
                }
            }
            private string translatedText;
            public string TranslatedText
            {
                get { return this.translatedText; }
                set
                {
                    this.translatedText = value;
                    OnPropertyChanged("translatedText");
                }
            }
            private bool onTranslating;
            public bool OnTranslating
            {
                get { return this.onTranslating; }
                set
                {
                    this.onTranslating = value;
                    OnPropertyChanged("onTranslating");
                }
            }
            private Translator.TranslateWorker translateWork;
            public Translator.TranslateWorker TranslateWork
            {
                get { return this.translateWork; }
                set
                {
                    this.translateWork = value;
                    OnPropertyChanged("translateWork");
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
