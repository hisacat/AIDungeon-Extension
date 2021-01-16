using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

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

        private Visibility sideMenuButtonVisibility;
        public Visibility SideMenuButtonVisibility
        {
            get { return this.sideMenuButtonVisibility; }
            set
            {
                this.sideMenuButtonVisibility = value;
                OnPropertyChanged("sideMenuButtonVisibility");
            }
        }
        private Visibility sideMenuVisibility;
        public Visibility SideMenuVisibility
        {
            get { return this.sideMenuVisibility; }
            set
            {
                this.sideMenuVisibility = value;
                OnPropertyChanged("sideMenuVisibility");
            }
        }
    }
}
