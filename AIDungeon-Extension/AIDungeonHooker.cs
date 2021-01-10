using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace AIDungeonExt.Core
{
    public class AIDungeonHooker : IDisposable
    {
        private const string LogType_Performance = "performance";
        private const string AIDungeonURL = "https://play.aidungeon.io/";

        private ChromeDriver driver = null;
        private bool disposedValue;

        public AIDungeonHooker()
        {
            //Open console first.
            //AllocConsole();

            //Check chrome version.

            var options = new ChromeOptions();
            //options.AddArgument("headless");
            options.SetLoggingPreference(LogType_Performance, LogLevel.All);

            var service = ChromeDriverService.CreateDefaultService();
            //service.HideCommandPromptWindow = true;

            driver = new ChromeDriver(service, options);

            //If failed.
            //C:\Program Files\Google\Chrome\Application\

            driver.Navigate().GoToUrl(AIDungeonURL);

            //FreeConsole();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    #region Dispose managed resources.
                    if (driver != null)
                    {
                        driver.Close();
                        driver.Quit();
                        driver = null;
                    }
                    #endregion
                }
                #region Dispose unmanaged resources.
                #endregion

                disposedValue = true;
            }
        }

        ~AIDungeonHooker()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
