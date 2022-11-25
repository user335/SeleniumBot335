using TechTalk.SpecFlow;

namespace SeleniumBot.Steps
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
            browser.GoTo("https://twitch.tv/user335/chat");
        }

        [Then(@"my Twitch Bot lives!")]
        public void ThenMyTwitchBotLives()
        {
            twitchBot.Go();
        }

    }
}
