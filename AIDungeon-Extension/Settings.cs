using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace AIDungeon_Extension
{
    public class Settings
    {
        private static Settings _instance = null;
        private static Settings Instance { get { if (_instance == null) _instance = new Settings(); return _instance; } }

        private IniFile ini = null;
        private string iniPath = string.Empty;

        private bool showOriginalTexts = false;
        public static bool ShowOriginalTexts
        {
            get
            {
                return Instance.showOriginalTexts;
            }
            set
            {
                Instance.showOriginalTexts = value;
                Instance.ini["Game"]["showOriginalTexts"] = value;
                Instance.ini.Save(Instance.iniPath);
            }
        }
        private string font = string.Empty;
        public static string Font
        {
            get
            {
                return Instance.font;
            }
            set
            {
                Instance.font = value;
                Instance.ini["Display"]["Font"] = value.ToString();
                Instance.ini.Save(Instance.iniPath);
            }
        }
        private Color bgColor = default;
        public static Color BGColor
        {
            get
            {
                return Instance.bgColor;
            }
            set
            {
                Instance.bgColor = value;
                Instance.ini["Color"]["BackGround"] = value.ToString();
                Instance.ini.Save(Instance.iniPath);
            }
        }

        private Settings()
        {
            this.ini = new IniFile();
            this.iniPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Settings.ini");

            if (!System.IO.File.Exists(iniPath))
            {
                //Create and initialize new settings.ini
                Font = string.Empty;
                BGColor = ((SolidColorBrush)Application.Current.Resources["AID_Black"]).Color;
                ini.Save(iniPath);
            }else
            {
                ini.Load(iniPath);
            }
        }
    }
}
