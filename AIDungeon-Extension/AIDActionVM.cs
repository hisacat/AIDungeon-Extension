using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDungeon_Extension
{
    public class AIDActionVM
    {
        private string originText;
        public string OriginText
        {
            get { return this.originText; }
            set
            {
                if (this.originText == value) return;
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
                if (this.translatedText == value) return;
                this.translatedText = value;
                OnPropertyChanged("translatedText");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
        }
    }
}
