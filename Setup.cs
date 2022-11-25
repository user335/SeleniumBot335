using OpenQA.Selenium;
using System;
using System.IO;
using TechTalk.SpecFlow;

namespace SeleniumBot
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
		public static bool sharingWebDriverBetweenScenarios = true;
		public static string _nextStreamTitle = "Automating stream setup - normally i do game dev, 5+ years on my first title";
		public static string _streamTags = "Unity,Educational,Game Development,AMA,C#";
		static string[] _getAllSecureLines => File.ReadAllLines(@"C:\Passwords\lastfm.txt");

		//[BeforeTestRun]
		//static void BeforeTestRun()
		//{

		//}
		[BeforeScenario]
		static void BeforeScenario(ScenarioContext injectedContext)
		{
			if (sharingWebDriverBetweenScenarios)
			{
				if (scenarioContext != null && scenarioContext.TryGetValue("webDriver", out IWebDriver driver))
				{
					if (driver != null)
					{
						injectedContext.AddOrUpdate("webDriver", driver);
						Logger.Log("Incorporating previous scenario's webDriver because it seems like its still good");
					}
				}
			}
			scenarioContext = injectedContext;
			new TwitchBot(scenarioContext).InitializeStreamWriter();
		}
		[AfterTestRun]
		static void AfterTestRun()
		{
			scenarioContext.CloseSqlConn();
			scenarioContext.CloseStreamWriterConn();
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
				//answer = Eramake.eCryptography.Decrypt(_getAllSecureLines.ElementAt(k));
			}
			catch (Exception)
			{
				//answer = Eramake.eCryptography.Encrypt(_getAllSecureLines.ElementAt(k));
				Console.WriteLine("Couldn't decrypt line 1, returning back encrypted line " + k + " for you instead: " + answer);
			}
			return answer;
		}
	}
}
