using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.HtmlControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace AIDungeon_Extension
{
    class WebBrowserTranslator
    {
        private const string inputElementXPath = "//*[@id=\"yDmH0d\"]/c-wiz/div/div[2]/c-wiz/div[2]/c-wiz/div[1]/div[2]/div[2]/c-wiz[1]/span/span/div/textarea";
        private const string translatedElementXPath = "//*[@id=\"yDmH0d\"]/c-wiz/div/div[2]/c-wiz/div[2]/c-wiz/div[1]/div[2]/div[2]/c-wiz[2]/div[5]/div/div[3]";

        private Grid grid = null;

        public WebBrowserTranslator(Grid grid)
        {
            this.grid = grid;
        }

        public class TranlateWorker
        {
            private Grid grid = null;
            private string text = null;
            private Action<string> onTranslated = null;

            private WebBrowser webBrowser = null;

            public TranlateWorker(Grid grid, string text, Action<string> onTranslated)
            {
                if (string.IsNullOrEmpty(text))
                {
                    onTranslated?.Invoke(null);
                    return;
                }

                this.grid = grid;
                this.text = text;
                this.onTranslated = onTranslated;
            }

            public void Start()
            {
                this.webBrowser = new WebBrowser();
                this.webBrowser.LoadCompleted += loadCompleted;
                this.grid.Children.Add(webBrowser);

                string sUrl = "https://translate.google.co.kr/?hl=ko&tab=TT#en/ko/";
                sUrl = sUrl + HttpUtility.UrlPathEncode(text);

                webBrowser.Navigate(sUrl);
            }

            private void loadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
            {
                Console.WriteLine("loadCompleted");

                Task.Run(() =>
                {
                    while (true)
                    {
                        string html = string.Empty;
                        this.grid.Dispatcher.Invoke(() =>
                        {
                            var document = this.webBrowser.Document as mshtml.HTMLDocument;

                            html = document.documentElement.outerHTML;
                        });

                        while (string.IsNullOrEmpty(html))
                        {

                        }

                        var doc = new HtmlAgilityPack.HtmlDocument();
                        doc.LoadHtml(html);

                        try
                        {
                            var nodeCollection = doc.DocumentNode.SelectNodes(translatedElementXPath);
                            if (nodeCollection == null) continue;
                            var node = nodeCollection.LastOrDefault();
                            if (node == null) continue;
                            var attributes = node.Attributes["data-text"];
                            if (attributes == null) continue;

                            var translated = attributes.Value;

                            this.grid.Dispatcher.Invoke(() =>
                            {
                                onTranslated?.Invoke(translated);
                            });
                            break;
                        }
                        catch (Exception exception) { }
                    }
                });
            }

            public void Dispose()
            {
                this.webBrowser.LoadCompleted -= loadCompleted;
                this.grid.Children.Remove(webBrowser);
            }
        }

        public void Translate(string text, System.Action<string> onSuccess, System.Action onFailed)
        {
            var worker = new TranlateWorker(this.grid, text, onSuccess);
            worker.Start();
        }
        public void Dispose()
        {

        }
    }
}