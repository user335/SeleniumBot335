using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace PracticingSix.PageObjects
{
    public class TwitchChatPage
    {
        public TwitchChatPage(ScenarioContext injectedContext)
        {
            _scenarioContext = injectedContext;
        }
        public readonly ScenarioContext _scenarioContext;
        public static string recentChatsVariableXPath = "//*[contains(@class,'chat-scrollable-area__message-container')]//*[contains(@class,'text-fragment') and contains(text(),'%VAR%')]//ancestor::*[@class='chat-line__message']/following-sibling::div"; //replace %VAR% with text of last chat parsed
        public static string allChatLinesXPath = "//*[contains(@class,'chat-scrollable-area__message-container')]//*[@data-test-selector='chat-message-highlight' or @data-test-selector='user-notice-line']";
        public IReadOnlyCollection<IWebElement> allChatLines { get {  return Browser._webDriver.FindElements(By.XPath(allChatLinesXPath)); } }
        public static string allChatsXPath = "//*[contains(@class,'chat-scrollable-area__message-container')]//*[contains(@data-test-selector,'chat-line-message-body')]";
        public IWebElement firstChat { get { return Browser._webDriver.FindElement(By.XPath(allChatsXPath)); } }
        public IReadOnlyCollection<IWebElement> allChatMessages { get {  return Browser._webDriver.FindElements(By.XPath(allChatsXPath)); } }
        public static string allUserNamesXPath = "//*[@class='chat-author__display-name']";
        public IReadOnlyCollection<IWebElement> allUserNames { get {  return Browser._webDriver.FindElements(By.XPath(allUserNamesXPath)); } }
        public static string allRaidsXPath = "//img[contains(@class,'tw-image-avatar')]";
        public IReadOnlyCollection<IWebElement> allRaids { get { return Browser._webDriver.FindElements(By.XPath(allRaidsXPath)); } }
    }
}
