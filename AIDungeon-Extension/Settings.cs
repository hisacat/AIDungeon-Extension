using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace AIDungeon_Extension
{
    public class Settings
    {
        private static Settings _instance = null;
        private static Settings Instance { get { if (_instance == null) _instance = new Settings(); return _instance; } }

        private IniFile ini = null;

        private Color bgColor = default;
        private Color textColor = default;
        private Color inputBoxColor = default;
        private Color inputTextColor = default;

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
            }
        }

        private Settings()
        {
            this.ini = new IniFile();
            var settingsIniPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Settings.ini");

            if (!System.IO.File.Exists(settingsIniPath))
            {
                //Create and initialize new settings.ini
                ini["Display"]["Font"] = "true";
                ini.Save(settingsIniPath);
            }else
            {
                ini.Load(settingsIniPath);
            }
        }
    }
}
