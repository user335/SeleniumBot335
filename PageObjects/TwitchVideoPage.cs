using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace SeleniumBot.PageObjects
{
	public class TwitchVideoPage
	{
		readonly ScenarioContext _scenarioContext;
		public TwitchVideoPage(ScenarioContext injectedContext)
		{
			_scenarioContext = injectedContext;
		}
		public Browser browser => new Browser(_scenarioContext);

		const string mainUrl = "https://twitch.tv/user335";
		//const string mainUrl = "https://www.twitch.tv/dalbasudo";
		const string chatButtonXPath = "//p[text()='Chat']";
		IWebElement chatButton => browser._webDriver.FindElement(By.XPath(chatButtonXPath));
		const string settingsGearXPath = "//button[contains(@class,'ScCoreButton-') and @aria-label='Settings']";
		IWebElement settingsGear => browser._webDriver.FindElement(By.XPath(settingsGearXPath));
		const string settings_qualityBarXPath = "//div[text()='Quality']";
		IWebElement settings_qualityBar => browser._webDriver.FindElement(By.XPath(settings_qualityBarXPath));
		const string settings_quality_480pXPath = "//div[text()='480p']";
		IWebElement settings_quality_480p => browser._webDriver.FindElement(By.XPath(settings_quality_480pXPath));
		public bool LaunchBrowserAndCheckForEncoding()
		{
			browser.GoTo(mainUrl);
			try
			{
				chatButton.Click();
			}
			catch 
			{

			}
			browser.WaitForPageReady();
			try
			{
				browser.SuperClick(settingsGearXPath);
			}
			catch (Exception ex)
			{
				Logger.Log("Failed to get settings gear by xpath, ex: " + ex);
			}
			browser.WaitForPageReady();
			try
			{
				settings_qualityBar.Click();
			}
			catch (Exception ex)
			{
				Logger.Log("Failed to get settings_qualityBar by xpath, ex: " + ex);
			}

			return browser._webDriver.FindElements(By.XPath(settings_quality_480pXPath)).Count > 0;
			//return browser.IsElementDisplayed_ByXPath(settings_quality_480pXPath);
		}
	}
}
