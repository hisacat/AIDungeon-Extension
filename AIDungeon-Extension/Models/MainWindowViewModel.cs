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

        private bool showLoading;
        public bool ShowLoading
        {
            get { return this.showLoading; }
            set
            {
                this.showLoading = value;
                OnPropertyChanged("showLoading");
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

        private bool showInputTranslateLoading;
        public bool ShowInputTranslateLoading
        {
            get { return this.showInputTranslateLoading; }
            set
            {
                this.showInputTranslateLoading = value;
                OnPropertyChanged("showInputTranslateLoading");
            }
        }
        private bool showInputLoading;
        public bool ShowInputLoading
        {
            get { return this.showInputLoading; }
            set
            {
                this.showInputLoading = value;
                OnPropertyChanged("showInputLoading");
            }
        }
        private bool showOriginTexts = false;
        public bool ShowOriginTexts
        {
            get { return this.showOriginTexts; }
            set
            {
                this.showOriginTexts = value;
                OnPropertyChanged("showOriginTexts");
            }
        }
        private bool detachNewlineTexts = true;
        public bool DetachNewlineTexts
        {
            get { return this.detachNewlineTexts; }
            set
            {
                this.detachNewlineTexts = value;
                OnPropertyChanged("detachNewlineTexts");
            }
        }

        private string statusText = MainWindow.DefaultStatusText;
        public string StatusText
        {
            get { return this.statusText; }
            set
            {
                this.statusText = value;
                OnPropertyChanged("statusText");
            }
        }
        private string versionText = MainWindow.VersionStr;
        public string VersionText
        {
            get { return this.versionText; }
            set
            {
                this.versionText = value;
                OnPropertyChanged("versionText");
            }
        }

        private bool showSideMenuButton = true;
        public bool ShowSideMenuButton
        {
            get { return this.showSideMenuButton; }
            set
            {
                this.showSideMenuButton = value;
                OnPropertyChanged("showSideMenuButton");
            }
        }
        private bool showSideMenu = true;
        public bool ShowSideMenu
        {
            get { return this.showSideMenu; }
            set
            {
                this.showSideMenu = value;
                OnPropertyChanged("showSideMenu");
            }
        }
        private bool showControlMenus = true;
        public bool ShowControlMenus
        {
            get { return this.showControlMenus; }
            set
            {
                this.showControlMenus = value;
                OnPropertyChanged("showControlMenus");
            }
        }
        private string promptText = "prompts here";
        public string PromptText
        {
            get { return this.promptText; }
            set
            {
                this.promptText = value;
                OnPropertyChanged("promptText");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

            switch(propertyName)
            {
                case "fontFamily":
                    ini["Font"]["fontFamily"] = this.fontFamily == null ? null : this.fontFamily.Source;
                    break;
                case "fontWeight":
                    ini["Font"]["fontWeight"] = this.fontWeight.ToString();
                    break;
                case "fontStyle":
                    ini["Font"]["fontStyle"] = this.fontStyle.ToString();
                    break;
                case "textDecorations":
                    ini["Font"]["textDecorations"] = this.textDecorations.ToString();
                    break;

                case "textColor":
                    ini["Color"]["textColor"] = this.textColor.ToString();
                    break;
                case "bgColor":
                    ini["Color"]["bgColor"] = this.bgColor.ToString();
                    break;
                case "inputBoxColor":
                    ini["Color"]["inputBoxColor"] = this.InputBoxColor.ToString();
                    break;
                case "inputTextColor":
                    ini["Color"]["inputTextColor"] = this.inputTextColor.ToString();
                    break;

                case "bgImage":
                    ini["Image"]["bgImage"] = this.bgImage;
                    break;

                case "showOriginTexts":
                    ini["Option"]["showOriginTexts"] = this.showOriginTexts;
                    break;
                case "detachNewlineTexts":
                    ini["Option"]["detachNewlineTexts"] = this.detachNewlineTexts;
                    break;
            }

            ini.Save(this.iniPath);
        }

        private IniFile ini = null;
        private string iniPath = string.Empty;
        public MainWindowViewModel()
        {
            this.ini = new IniFile();
            this.iniPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Settings.ini");

            if (System.IO.File.Exists(this.iniPath))
            {
                this.ini.Load(this.iniPath);

                string font;
                font = this.ini["Font"]["fontFamily"].ToString();
                if (!string.IsNullOrEmpty(font)) this.fontFamily = new FontFamily(font);
                
                int weight;
                if (this.ini["Font"]["fontWeight"].TryConvertInt(out weight))
                    this.fontWeight = System.Windows.FontWeight.FromOpenTypeWeight(weight);

                this.ini["Font"]["fontStyle"].ToString();
                this.ini["Font"]["textDecorations"].ToString();

                Color color;
                {
                    if (this.ini["Color"]["textColor"].TryConvertColor(out color))
                        this.textColor = new SolidColorBrush(color);
                    if (this.ini["Color"]["bgColor"].TryConvertColor(out color))
                        this.bgColor = new SolidColorBrush(color);
                    if (this.ini["Color"]["InputBoxColor"].TryConvertColor(out color))
                        this.InputBoxColor = new SolidColorBrush(color);
                    if (this.ini["Color"]["inputTextColor"].TryConvertColor(out color))
                        this.inputTextColor = new SolidColorBrush(color);
                }

                this.bgImage = this.ini["Image"]["bgimage"].ToString();

                bool isOn;
                {
                    if (this.ini["Option"]["showOriginTexts"].TryConvertBool(out isOn))
                        this.showOriginTexts = isOn;
                    if (this.ini["Option"]["detachNewlineTexts"].TryConvertBool(out isOn))
                        this.detachNewlineTexts = isOn;
                }
            }
        }
    }
}
