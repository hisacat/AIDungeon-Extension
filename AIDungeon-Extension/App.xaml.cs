using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using AIDungeonExt.Core;

namespace AIDungeon_Extension
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        [DllImport("Kernel32")]
        public static extern void AllocConsole();

        [DllImport("Kernel32")]
        public static extern void FreeConsole();

        public static AIDungeonHooker Hooker = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            //Hooker = new AIDungeonHooker();

            //AllocConsole();
            base.OnStartup(e);
        }
        protected override void OnExit(ExitEventArgs e)
        {
            if(Hooker != null)
            {
                Hooker.Dispose();
                Hooker = null;
            }

            //FreeConsole();
            base.OnExit(e);
        }
    }
}
