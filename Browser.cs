using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumBot.PageObjects;
using System;
using System.Linq;
using TechTalk.SpecFlow;

namespace SeleniumBot
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
            //string path = Assembly.GetExecutingAssembly().Location;
            //var cService = ChromeDriverService.CreateDefaultService($@"{path}\..");
            //cService.HideCommandPromptWindow = true;
            string path = Utilities.DownloadLatestVersionOfChromeDriver();
            ChromeOptions options = new ChromeOptions();
            //options.AddArguments("--disable-extensions");
            options.AddArguments("--disable-infobars");
            options.AddArguments("--ignore-cretificate-errors");
            //options.AddArguments("--incognito");
            options.AddArguments("--window-size=1600,1000");
            var driver = new ChromeDriver(path, options, TimeSpan.FromMinutes(3));
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
			if (Setup.sharingWebDriverBetweenScenarios)
				Setup.scenarioContext = scenarioContext;
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
            var title = _webDriver.FindElement(By.XPath(YouTubePlayPage.videoTitleXPath));
            var text = title.GetAttribute("innerText");
            return text.ToLower().Contains("eye of the tiger");
        }


		public void GoTo(string url)
		{
			if (!WebDriverIsLive()) StartWebDriverAndStoreInContext();
			_webDriver.Navigate().GoToUrl(url);
			WaitForPageReady();
		}

		public void SuperClick(string elementXPath)
		{
			var element = _webDriver.FindElement(By.XPath(elementXPath));
			IJavaScriptExecutor executor = (IJavaScriptExecutor)_webDriver;
			executor.ExecuteScript("arguments[0].click();", element);
		}
		public void SuperClick(IWebElement element)
		{
			IJavaScriptExecutor executor = (IJavaScriptExecutor)_webDriver;
			executor.ExecuteScript("arguments[0].click();", element);
		}

		public void OpenANewTab()
		{
			((IJavaScriptExecutor)_webDriver).ExecuteScript("window.open();");
			_webDriver.SwitchTo().Window(_webDriver.WindowHandles.Last());
		}
	}
}
