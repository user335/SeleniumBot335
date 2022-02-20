using com.okitoki.wavhello;
using com.okitoki.wobblefm.client;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using PracticingSix.PageObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace PracticingSix
{
    public class EyeOfTheTiger
    {
        public EyeOfTheTiger(ScenarioContext injectedContext)
        {
            scenarioContext = injectedContext;
        }
        readonly ScenarioContext scenarioContext;
        Browser browser { get { return new Browser(scenarioContext); } }
        YouTubePlayPage youTubePlayPage { get { return new YouTubePlayPage(scenarioContext); } }
        IWebDriver _webDriver => browser._webDriver;
        WebDriverWait _wait => browser._wait;
        static bool _firstPlayGone;
        int spinnerEncounters = 0;
        static string _password => Setup.DecryptSecretKey(0);
        static string _apiKey => Setup.DecryptSecretKey(1);
        static string _apiSharedSecret => Setup.DecryptSecretKey(2);

        public void EyeMeEveryInterval(double time)
        {
            EyeMeEveryIntervalFromYoutube(time);
            //EyeMeEveryIntervalFromWobbleFM(time);
        }
        public void EyeMeEveryIntervalFromYoutube(double time)
        {
            if (_firstPlayGone) while (browser.IsVideoTitleCorrect() && IsSongStillPlaying()) 
                    Thread.Sleep(2500); //don't interrupt early even if the timing implies song should be over
            _webDriver.Navigate().GoToUrl("https://www.youtube.com/watch?v=btPJPFnesV4");
            browser.WaitForPageReady();
            youTubePlayPage.mainPlayer.SendKeys("M"); //mute
            _wait.Until(o => browser.IsElementDisplayed_ById(YouTubePlayPage.mainPlayerId));

            while (!browser.IsVideoTitleCorrect())
            {
                browser.RefreshPage();
            }
            youTubePlayPage.mainPlayer.SendKeys("M"); //unmute

            Console.WriteLine("Playing!");
            browser.WaitForPageReady();

            DateTime nextPlayTime = DateTime.Now.AddMinutes(time);
            while (DateTime.Now < nextPlayTime)
            {
                if (!_firstPlayGone)
                {
                    youTubePlayPage.mainPlayer.SendKeys(Keys.ArrowDown + Keys.ArrowDown + Keys.ArrowDown + Keys.ArrowDown + Keys.ArrowDown + Keys.ArrowDown + Keys.ArrowDown + Keys.ArrowDown + Keys.ArrowDown + Keys.ArrowDown);
                    WaitForConfirmPermissionsWindowAndLogIn();
                    _firstPlayGone = true;
                }
                try
                {
                    Thread.Sleep(250);
                    youTubePlayPage.mainPlayer.SendKeys(Keys.Shift + Keys.Tab);
                    if (youTubePlayPage.spinner.Displayed)
                    {
                        spinnerEncounters++;
                        _webDriver.FindElement(By.CssSelector("body")).SendKeys(Keys.F12);
                        Console.WriteLine("Tried to clear spinner, spinnerEncounters so far: " + spinnerEncounters);
                    }
                }
                catch (Exception)
                {
                }
            }

            //_wait.Until(o => Regex.IsMatch(youTubePlayPage.currentTimeDisplayer.Text, "4:0.", RegexOptions.IgnoreCase));
            //_webDriver.Quit();
            //_webDriver.Dispose();
            EyeMeEveryIntervalFromYoutube(time);
        }
        public void EyeMeEveryIntervalFromWobbleFM(double time)
        {
            PlayOneEyeOfTheTigerAndScrobbleAtEnd();
        }
        public void PlayOneEyeOfTheTigerAndScrobbleAtEnd()
        {
            //play eye of the tiger
            var file = new WaveFile("C:/Eye of the tiger.wav");
            var player = new WavePlayer();
            player.Play(file);
            ScrobbleOneEyeOfTheTiger(240);
        }
        public void ScrobbleOneEyeOfTheTiger(long timestampInSeconds) 
        {
            var client = new LastFMClient(_apiKey, _apiSharedSecret);
            client.AttemptAuthorization();
            client.Scrobble("Survivor", "Eye of the Tiger", timestampInSeconds);
            Console.WriteLine("Scrobbled.");
        }
        static int _loopCountForLoginWindow = -1;
        private void WaitForConfirmPermissionsWindowAndLogIn()
        {
            _loopCountForLoginWindow++;
            Console.WriteLine("Loop: " + _loopCountForLoginWindow);
            //wait for Confirm Permissions window to open
            //_wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
            browser.WaitForPageReady();
            _wait.Until(o => o.WindowHandles.Count() > 1);
            var handles = _webDriver.WindowHandles;
            foreach (var handle in handles)
            {
                _webDriver.SwitchTo().Window(handle);
                browser.WaitForPageReady();
                if (_webDriver.Url.Contains("scope_approval_dialog.html"))
                {
                    browser.WaitForPageReady();
                    Thread.Sleep(5000);
                    browser.WaitForElementDisplayed_ById("providerview"); //todo const this
                    Console.WriteLine("Switched to Confirm Permissions window");
                    //log in
                    //var gets = _webDriver.FindElements(By.XPath("//*[contains(text(),'')]"));
                    var windo = _webDriver.FindElements(By.XPath("//*"));
                    var view = _webDriver.FindElement(By.Id("providerview"));
                    view.SendKeys(Keys.Tab + Keys.Tab + Keys.Tab + Keys.Tab + Keys.Tab + Keys.Tab + Keys.Tab + Keys.Tab + Keys.Tab);
                    view.SendKeys("user-335");
                    view.SendKeys(Keys.Tab);
                    view.SendKeys(_password);
                    view.SendKeys(Keys.Enter);
                    browser.WaitForPageReady();
                    Thread.Sleep(2500);
                    view.SendKeys(Keys.Tab + Keys.Tab + Keys.Tab + Keys.Tab + Keys.Tab + Keys.Tab + Keys.Tab + Keys.Tab + Keys.Tab + Keys.Tab);
                    view.SendKeys(Keys.Enter);
                    browser.WaitForPageReady();
                    Console.WriteLine("Finished logging in");
                    _webDriver.SwitchTo().Window(_webDriver.WindowHandles.First());
                    return;
                }
            }
            WaitForConfirmPermissionsWindowAndLogIn();
        }
        public bool IsSongStillPlaying()
        {
            try
            {
                youTubePlayPage.currentTimeDisplayer.Click();
            }
            catch (Exception)
            {
                //guessing its an ad playing
                return false;
            }
            return !youTubePlayPage.currentTimeDisplayer.Text.StartsWith("4");
        }
    }
}
