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
        private SolidColorBrush textColor = (SolidColorBrush)Application.Current.Resources["AID_White"];
        public SolidColorBrush TextColor
        {
            get { return this.textColor; }
            set
            {
                this.textColor = value;
                OnPropertyChanged("textColor");
            }
        }
        private SolidColorBrush bgColor = (SolidColorBrush)Application.Current.Resources["AID_Black"];
        public SolidColorBrush BGColor
        {
            get { return this.bgColor; }
            set
            {
                this.bgColor = value;
                OnPropertyChanged("bgColor");
            }
        }
        private SolidColorBrush inputBoxColor = (SolidColorBrush)Application.Current.Resources["AID_Gray"];
        public SolidColorBrush InputBoxColor
        {
            get { return this.inputBoxColor; }
            set
            {
                this.inputBoxColor = value;
                OnPropertyChanged("inputBoxColor");
            }
        }
        private SolidColorBrush inputTextColor = (SolidColorBrush)Application.Current.Resources["AID_White"];
        public SolidColorBrush InputTextColor
        {
            get { return this.inputTextColor; }
            set
            {
                this.inputTextColor = value;
                OnPropertyChanged("inputTextColor");
            }
        }
        private string bgImage;
        public string BGImage
        {
            get { return this.bgImage; }
            set
            {
                this.bgImage = value;
                OnPropertyChanged("bgImage");
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
        private string loadingText = "loadingText";
        public string LoadingText
        {
            get { return this.loadingText; }
            set
            {
                this.loadingText = value;
                OnPropertyChanged("loadingText");
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
        private bool showOriginTexts;
        public bool ShowOriginTexts
        {
            get { return this.showOriginTexts; }
            set
            {
                this.showOriginTexts = value;
                OnPropertyChanged("showOriginTexts");
            }
        }
        private bool detachNewlineTexts;
        public bool DetachNewlineTexts
        {
            get { return this.detachNewlineTexts; }
            set
            {
                this.detachNewlineTexts = value;
                OnPropertyChanged("detachNewlineTexts");
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
        }

        private string statusText = "status";
        public string StatusText
        {
            get { return this.statusText; }
            set
            {
                this.statusText = value;
                OnPropertyChanged("statusText");
            }
        }
        private string versionText = "version";
        public string VersionText
        {
            get { return this.versionText; }
            set
            {
                this.versionText = value;
                OnPropertyChanged("versionText");
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
