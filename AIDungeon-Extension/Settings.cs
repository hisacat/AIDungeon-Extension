﻿using System;
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
        private static Settings Instance { get; set; }

        private IniFile ini = null;
        private string iniPath = string.Empty;

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

        private Settings()
        {
            this.ini = new IniFile();
            this.iniPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Settings.ini");
        }
        public static void Init()
        {
            Instance = new Settings();

            if (!System.IO.File.Exists(Instance.iniPath))
            {
                //Create and initialize new settings.ini
                Font = Instance.font;
            }
            else
            {
                Instance.ini.Load(Instance.iniPath);
            }
        }
    }
}
