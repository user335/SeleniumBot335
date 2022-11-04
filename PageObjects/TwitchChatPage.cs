using OpenQA.Selenium;
using System.Collections.Generic;
using TechTalk.SpecFlow;

namespace SeleniumBot.PageObjects
{
    public class TwitchChatPage
    {
        public TwitchChatPage(ScenarioContext injectedContext)
        {
            _scenarioContext = injectedContext;
        }
        public readonly ScenarioContext _scenarioContext;
        public Browser browser => new Browser(_scenarioContext);
        public const string recentChatsVariableXPath = "//*[contains(@class,'chat-scrollable-area__message-container')]//*[contains(@class,'text-fragment') and contains(text(),'%VAR%')]//ancestor::*[@class='chat-line__message']/following-sibling::div"; //replace %VAR% with text of last chat parsed
        public const string allChatLinesXPath = "//*[contains(@class,'chat-scrollable-area__message-container')]//*[@data-test-selector='chat-message-highlight' or @data-test-selector='user-notice-line']";
        public IReadOnlyCollection<IWebElement> allChatLines { get {  return browser._webDriver.FindElements(By.XPath(allChatLinesXPath)); } }
        public const string allChatsXPath = "//*[contains(@class,'chat-scrollable-area__message-container')]//*[contains(@data-test-selector,'chat-line-message-body')]";
        public IWebElement firstChat { get { return browser._webDriver.FindElement(By.XPath(allChatsXPath)); } }
        public IReadOnlyCollection<IWebElement> allChatMessages { get {  return browser._webDriver.FindElements(By.XPath(allChatsXPath)); } }
        public const string allUserNamesXPath = "//*[@class='chat-author__display-name']";
        public IReadOnlyCollection<IWebElement> allUserNames { get {  return browser._webDriver.FindElements(By.XPath(allUserNamesXPath)); } }
        public const string allRaidsXPath = "//img[contains(@class,'tw-image-avatar')]";
        public IReadOnlyCollection<IWebElement> allRaids { get { return browser._webDriver.FindElements(By.XPath(allRaidsXPath)); } }
    }
}
