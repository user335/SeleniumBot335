using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace PracticingSix
{
    [Binding]
    public class Setup
    {
        //public Setup(ScenarioContext injectedContext)
        //{
        //    scenarioContext = injectedContext;
        //}
        //readonly ScenarioContext scenarioContext;
        public static ScenarioContext scenarioContext;

        static string[] _getAllSecureLines => File.ReadAllLines(@"C:\Passwords\lastfm.txt");

        //[BeforeTestRun]
        //static void BeforeTestRun()
        //{
        //    Browser.StartWebDriverAndStoreInContext();
        //}
        [BeforeScenario]
        static void BeforeScenario(ScenarioContext injecteContext)
        {
            scenarioContext = injecteContext;
        }
        [AfterTestRun]
        static void AfterTestRun()
        {
            if (scenarioContext.TryGetValue("webDriver", out IWebDriver driver))
            {
                try
                {
                    driver.Quit();
                }
                catch (Exception)
                {

                }
                try
                {
                    driver.Dispose();
                }
                catch (Exception)
                {

                }
                try
                {
                    driver.Quit();
                }
                catch (Exception)
                {

                }
            }
        }
        public static string DecryptSecretKey(int k)
        {
            var answer = "";
            try
            {
                answer = Eramake.eCryptography.Decrypt(_getAllSecureLines.ElementAt(k));
            }
            catch (Exception)
            {
                answer = Eramake.eCryptography.Encrypt(_getAllSecureLines.ElementAt(k));
                Console.WriteLine("Couldn't decrypt line 1, returning back encrypted line " + k + " for you instead: " + answer);
            }
            return answer;
        }
    }
}
