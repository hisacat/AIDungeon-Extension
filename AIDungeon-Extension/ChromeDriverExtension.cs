using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDungeon_Extension
{
    public static class ChromeDriverExtension
    {
        public static IWebElement FindElementByXPathOrNull(this ChromeDriver driver, string xpath)
        {
            var elements = driver.FindElementsByXPath(xpath);
            return elements != null ? (elements.Count > 0 ? elements[0] : null) : null;
        }
    }
}
