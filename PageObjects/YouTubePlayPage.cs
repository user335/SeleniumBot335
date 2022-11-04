using OpenQA.Selenium;
using TechTalk.SpecFlow;

namespace SeleniumBot.PageObjects
{
    public class YouTubePlayPage
    {
        public YouTubePlayPage(ScenarioContext injectedContext)
        {
            scenarioContext = injectedContext;
        }
        ScenarioContext scenarioContext;
        Browser browser => new Browser(scenarioContext);

        public static string mainPlayerId = "movie_player";
        public IWebElement mainPlayer { get { return browser._webDriver.FindElement(By.Id(mainPlayerId)); } }
        public static string adPreviewButtonXPath = "//*[contains(@id,'ad-preview')]";
        public static string videoTitleXPath = "//*[contains(@class,'ytp-title-link')]";
        public IWebElement videoTitleElement { get { return browser._webDriver.FindElement(By.XPath(videoTitleXPath)); } }
        public static string currentTimeXPath = "//span[@class='ytp-time-current']";
        public IWebElement currentTimeDisplayer { get { return browser._webDriver.FindElement(By.XPath(currentTimeXPath)); } }
        public static string spinnerXPath = "//div[@class='ytp-spinner-rotator']";
        public IWebElement spinner { get { return browser._webDriver.FindElement(By.XPath(spinnerXPath)); } }
    }
}