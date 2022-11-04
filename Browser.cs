using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using PracticingSix.PageObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace PracticingSix
{
    public class Browser
    {
        public Browser(ScenarioContext injectedContext)
        {
            scenarioContext = injectedContext;
        }
        readonly ScenarioContext scenarioContext;
        public IWebDriver _webDriver => scenarioContext["webDriver"] as IWebDriver;
        public WebDriverWait _wait { get { return new WebDriverWait(_webDriver, defaultWaitTimeout); } }
        public TimeSpan defaultWaitTimeout = TimeSpan.FromSeconds(10);
        public void StartWebDriverAndStoreInContext()
        {
            Console.WriteLine("Starting the driver...");
            string path = Assembly.GetExecutingAssembly().Location;
            var cService = ChromeDriverService.CreateDefaultService($@"{path}\..");
            cService.HideCommandPromptWindow = true;
            ChromeOptions options = new ChromeOptions();
            //options.AddArguments("--disable-extensions");
            options.AddArguments("--disable-infobars");
            options.AddArguments("--ignore-cretificate-errors");
            //options.AddArguments("--incognito");
            options.AddArguments("--window-size=1200,800");
            //options.AddArguments("--start-maximized");
            //options.AddArguments("--headless");
            //options.AddExtensions(new string[1] {
            //    //@"C:\Users\user\Desktop\CRX files\cmedhionkhpnakcndndgjdbohmhepckk.crx",
            //    @"C:\Users\user\Desktop\CRX files\kjhnjfldmodoikafpfhfehngokaiegok.crx" } );
            var driver = new ChromeDriver(cService, options, TimeSpan.FromMinutes(3));
            //driver.Navigate().GoToUrl("chrome-extension://cmedhionkhpnakcndndgjdbohmhepckk/index.html");
            //driver.Navigate().GoToUrl("chrome-extension://kjhnjfldmodoikafpfhfehngokaiegok/index.html");
            driver.Navigate().GoToUrl("about:blank");
            foreach (var handle in driver.WindowHandles)
            {
                driver.SwitchTo().Window(handle);
                if (driver.Url.Contains("get.adblock"))
                {
                    driver.Close();
                }
            }
            scenarioContext.Add("webDriver", driver);
            //return driver;
        }
        public bool WebDriverIsLive()
        {
            try
            {
                var type = _webDriver.GetType();
                return !string.IsNullOrEmpty(_webDriver.CurrentWindowHandle);
            }
            catch (Exception)
            {
                return false;
            }
        }
        public void WaitForPageReady()
        {
            _wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
        }
        public void WaitForElementDisplayed_ByXpath(string xpath)
        {
            _wait.Until(d => d.FindElement(By.XPath(xpath)).Displayed);
        }
        public void WaitForElementDisplayed_ById(string id)
        {
            _wait.Until(d => d.FindElement(By.Id(id)).Displayed);
        }
        public void RefreshPage()
        {
            _webDriver.Navigate().GoToUrl(_webDriver.Url);
        }
        public bool IsElementDisplayed_ByXPath(string xpath)
        {
            try
            {
                _wait.Until(d => d.FindElement(By.XPath(xpath)).Displayed);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool IsElementDisplayed_ById(string id)
        {
            try
            {
                _wait.Until(d => d.FindElement(By.Id(id)).Displayed);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool IsVideoTitleCorrect()
        {
            //_webDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
            //_wait.Timeout = TimeSpan.FromSeconds(5);
            //var titleElement = _wait.Until(d => d.FindElement(By.XPath(YouTubePlayPage.videoTitleXPath)));
            //_webDriver.Manage().Timeouts().ImplicitWait = defaultWaitTimeout;
            var title = _webDriver.FindElement(By.XPath(YouTubePlayPage.videoTitleXPath));
            var text = title.GetAttribute("innerText");
            return text.ToLower().Contains("eye of the tiger");
        }
    }
}
