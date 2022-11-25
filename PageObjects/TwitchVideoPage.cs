using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Configuration;
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

		public const string chatButtonXPath = "//p[text()='Chat']";
		public IWebElement chatButton => browser._webDriver.FindElement(By.XPath(chatButtonXPath));
		public const string settingsGearXPath = "//button[contains(@class,'ScCoreButton-') and @aria-label='Settings']";
		public IWebElement settingsGear => browser._webDriver.FindElement(By.XPath(settingsGearXPath));
		public const string settings_qualityBarXPath = "//div[text()='Quality']";
		public IWebElement settings_qualityBar => browser._webDriver.FindElement(By.XPath(settings_qualityBarXPath));
		public const string settings_quality_480pXPath = "//div[text()='480p']";
		public IWebElement settings_quality_480p => browser._webDriver.FindElement(By.XPath(settings_quality_480pXPath));
		public const string toggleMuteButtonXPath = "//button[@data-a-target='player-mute-unmute-button']";
		public IWebElement toggleMuteButton => browser._webDriver.FindElement(By.XPath(toggleMuteButtonXPath));
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

		public bool TryToMuteThePage()
		{
			int count = 0;
			string foundText = "";
			try
			{
				var els = browser._webDriver.FindElements(By.XPath(toggleMuteButtonXPath));
				foreach (var el in els)
				{
					var text = el.Text;
					if (text.StartsWith("Mute"))
					{
						toggleMuteButton.Click();
						Logger.Log("Toggled mute on the page");
						return true;
					}
					else foundText += text + "\n";
					count++;
				}
				Logger.Log("No mute found yet... " + count + " elements checked");
				var deepPath = toggleMuteButtonXPath + "//*";
				var els2 = browser._webDriver.FindElements(By.XPath(deepPath));
				foreach (var el2 in els2)
				{
					var text = el2.Text;
					if (text.StartsWith("Mute"))
					{
						toggleMuteButton.Click();
						Logger.Log("Toggled mute on the page, found after " + count + " elements checked");
						return true;
					}
					else foundText += text + "\n";
				}
			}
			catch (Exception e)
			{
				Logger.Log("Couldn't mute the page, exception was: " + e);
			}
			Logger.Log("No mute found in " + count + " elements checked, all text found was:\n" + foundText);
			return false;
		}
	}
}
