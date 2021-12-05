using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace PracticingSix.Steps
{
    [Binding]
    public class TwitchBotSteps
    {
        public TwitchBotSteps(ScenarioContext injectedContext)
        {
            _scenarioContext = injectedContext;
        }
        private readonly ScenarioContext _scenarioContext;
        Browser browser => new Browser(_scenarioContext);
        TwitchBot twitchBot => new TwitchBot(_scenarioContext);

        [Given(@"I want a Twitch Bot running in Selenium just for the heck of it")]
        public void GivenIWantATwitchBotRunningInSeleniumJustForTheHeckOfIt()
        {
            //Browser.StartWebDriver(); framework starts this, no need to do anything here
        }

        [When(@"I send a browser to snoop on my own chat channel")]
        public void WhenISendABrowserToSnoopOnMyOwnChatChannel()
        {
            if (!Browser.WebDriverIsLive()) Browser.StartWebDriver();
            Browser._webDriver.Navigate().GoToUrl("https://twitch.tv/user335/chat");
            browser.WaitForPageReady();
        }

        [Then(@"my Twitch Bot lives!")]
        public void ThenMyTwitchBotLives()
        {
            twitchBot.Go();
        }

    }
}
