using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
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

        protected override void OnStartup(StartupEventArgs e)
        {
            //AllocConsole();
            base.OnStartup(e);
        }
        protected override void OnExit(ExitEventArgs e)
        {
            //FreeConsole();
            base.OnExit(e);
        }
    }
}
