using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace PracticingSix.Steps
{
    [Binding]
    public class StreamlabsOBSSteps
    {
        public StreamlabsOBSSteps(ScenarioContext injectedContext)
        {
            _scenarioContext = injectedContext;
        }
        private readonly ScenarioContext _scenarioContext;
        Browser browser => new Browser(_scenarioContext);
        AppiumDriver<WindowsElement> appiumDriver;
        [When(@"I launch slobs and go live")]
        public void WhenILaunchSlobsAndGoLive()
        {
            var appiumOptions = new AppiumOptions();
            appiumOptions.AddAdditionalCapability("newCommandTimeout", 10000);
            appiumOptions.AddAdditionalCapability("app", @"C:\Program Files\Streamlabs OBS\Streamlabs OBS.exe");
            appiumOptions.AddAdditionalCapability("deviceName", "WindowsPC");
			try
			{
                appiumDriver = new WindowsDriver<WindowsElement>(new Uri("http://localhost:4723/wd/hub"), appiumOptions);
			}
			catch (OpenQA.Selenium.WebDriverException e)
			{
				throw new OpenQA.Selenium.WebDriverException("You forgot to launch appium!\n" + e.Message);
			}
            var el1 = appiumDriver.FindElementByName("Streamlabs Desktop");
            var now = DateTime.Now;
            while (DateTime.Now < now.AddSeconds(30))
			{
				try
				{
                    var el2 = appiumDriver.FindElementByName("Go Live");
                    el2.Click();
                    Logger.Log("Go Live button was clicked");
                    break;
				}
				catch (Exception)
				{

				}
			}
            now = DateTime.Now;
            while (DateTime.Now < now.AddSeconds(600))
			{
				try
				{
                    var handles = browser._webDriver.WindowHandles;
                    //var elx = appiumDriver.FindElementByName("Confirm & Go Live");
                    appiumDriver.SwitchTo().Alert();
                    var el4 = appiumDriver.FindElementsByClassName("Chrome_RenderWidgetHostHWND");
					var el2 = appiumDriver.FindElementByName("Go Live - 2 running windows");
                    var el3 = appiumDriver.FindElementByName("Confirm & Go Live");
                    //el2.Click();npm 
                    break;
				}
				catch (Exception e)
				{

				}
			}

		}

        [Then(@"my stream is live!")]
        public void ThenMyStreamIsLive()
        {
            _scenarioContext.Pending();
        }
        [Given(@"Appium is running")]
        public void GivenAppiumIsRunning()
        {
            var appiumOptions = new AppiumOptions();
            appiumOptions.AddAdditionalCapability("app", @"C:\Program Files (x86)\Chatty\Chatty.exe");
            appiumOptions.AddAdditionalCapability("deviceName", "WindowsPC");
            try
            {
                appiumDriver = new WindowsDriver<WindowsElement>(new Uri("http://localhost:4723/wd/hub"), appiumOptions);
            }
            catch (OpenQA.Selenium.WebDriverException e)
            {
                throw new OpenQA.Selenium.WebDriverException("You forgot to launch appium!\n" + e.Message);
            }
            if (!browser.WebDriverIsLive()) browser.StartWebDriverAndStoreInContext();
        }


    }
}
