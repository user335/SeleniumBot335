using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using SeleniumBot.PageObjects;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using TechTalk.SpecFlow;

namespace SeleniumBot.Steps
{
	[Binding]
	public class WinApp
	{
		public WinApp(ScenarioContext injectedContext)
		{
			_scenarioContext = injectedContext;
		}
		private readonly ScenarioContext _scenarioContext;
		Browser browser => new Browser(_scenarioContext);
		TwitchVideoPage twitchVideoPage => new TwitchVideoPage(_scenarioContext);
		WindowsDriver<WindowsElement> rootDriver => _scenarioContext.TryGetValue("RootSession", out WindowsDriver<WindowsElement> session) ? session : LaunchRootSession();
		WindowsDriver<WindowsElement> slobsSession => _scenarioContext.TryGetValue("SlobsSession", out WindowsDriver<WindowsElement> session) ? session : LaunchSlobsSession();
		//WindowsDriver<WindowsElement> goLiveSession => _scenarioContext.TryGetValue("GoLiveSession", out WindowsDriver<WindowsElement> session) ? session : ConnectToSession("GoLive");

		WindowsDriver<WindowsElement> LaunchRootSession()
		{
			Logger.Log("Launching root session");
			DesiredCapabilities desktopCapabilities = new DesiredCapabilities();
			desktopCapabilities.SetCapability("app", "Root");
			desktopCapabilities.SetCapability("deviceName", "WindowsPC");
			var desktopSession = new WindowsDriver<WindowsElement>(new Uri("http://127.0.0.1:4723"), desktopCapabilities);
			desktopSession.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
			_scenarioContext.Add("RootSession", desktopSession);
			return desktopSession;
		}
		WindowsDriver<WindowsElement> LaunchSlobsSession()
		{
			Logger.Log("Launching slobs session");
			string appId = @"C:\Program Files\Streamlabs OBS\Streamlabs OBS.exe";
			DesiredCapabilities appCapabilities = new DesiredCapabilities();
			appCapabilities.SetCapability("app", appId);
			appCapabilities.SetCapability("deviceName", "WindowsPC");
			var session = new WindowsDriver<WindowsElement>(new Uri("http://127.0.0.1:4723"), appCapabilities);
			_scenarioContext.Add("SlobsSession", session);
			return session;
		}
		WindowsDriver<WindowsElement> ConnectToSession(string windowName)
		{
			var wait = DateTime.Now.AddSeconds(4);
			Logger.Log($"Connecting to window {windowName}...");
			while (true)
			{
				try
				{
					var windows = rootDriver.FindElementsByName(windowName);
					if (windows.Count > 0)
					{
						foreach (var window in windows)
						{
							var controlType = window.GetAttribute("LocalizedControlType");
							if (controlType == "pane")
							{
								var handle = window.GetAttribute("NativeWindowHandle");
								var parsedHandle = int.Parse(handle).ToString("x");
								var appCapabilities = new DesiredCapabilities();
								appCapabilities.SetCapability("appTopLevelWindow", parsedHandle);
								var session = new WindowsDriver<WindowsElement>(new Uri("http://127.0.0.1:4723"), appCapabilities);
								return session;
							}
						}
					}
				}
				catch
				{
					if (DateTime.Now > wait) throw new OperationCanceledException("Unable to switch to window " + windowName);
				}
			}
			//WindowsDriver<WindowsElement> UpdateGoLiveSession()
			//      {
			//	Logger.Log("Switching to new Go Live window...");

			//          LaunchGoLiveSession();
			//	//var window = rootDriver.FindElementByName("Go Live");
			//	//var handle = window.GetAttribute("NativeWindowHandle");
			//	//var parsedHandle = int.Parse(handle).ToString("x");
			// //         var newsession = goLiveSession;
			//	//newsession.SwitchTo().Window(parsedHandle);
			// //         _scenarioContext["GoLiveSession"] = newsession;

			//          try
			//          {
			//		var confirmAndGoLiveButton = goLiveSession.FindElementByName("Confirm & Go Live");
			//		confirmAndGoLiveButton.Click();

			//	}
			//	catch 
			//          {

			//          }

			//	return goLiveSession;
			//}
		}

		[When(@"I launch slobs and go live")]
		public void WhenILaunchSlobsAndGoLive()
		{
			//KillProcessesByName("Streamlabs OBS.exe");
			//KillProcessesByName("Streamlabs Desktop");
			//KillProcessesByName("WinAppDriver");
			//KillProcessesByName("WinAppDriver.exe");
			//CloseSlobsSession();

			//if (_scenarioContext.TryGetValue("WinAppDriver", out Process winappdriver))
			//	winappdriver.Kill();

			var quitTime = DateTime.Now.AddSeconds(60);
			while (true)
			{
				try
				{
					LaunchSlobsAndClickStartingSoon();
					if (GoLiveAndCheckForEncoding())
					{
						Logger.Log("We are live with encoding enabled");
						return;
					}
				}
				catch { }
				CloseSlobsSession();
				if (DateTime.Now > quitTime) throw new OperationCanceledException("Didn't work!");
			}
		}

		private void LaunchSlobsAndClickStartingSoon()
		{
			var quitTime = DateTime.Now.AddSeconds(20);
			while (true)
			{
				try
				{
					var startingSoonPane = slobsSession.FindElementByName("Starting Soon");
					startingSoonPane.Click();
					Logger.Log("startingSoonPane was clicked");
					break;
				}
				catch (Exception ex)
				{
					if (DateTime.Now > quitTime) throw new OperationCanceledException("Timeout failure while looking for Starting Soon pane");
					else Logger.Log("not yet... ");
				}
			}
		}

		[Then(@"my stream is live!")]
		public void ThenMyStreamIsLive()
		{
			twitchVideoPage.TryToMuteThePage();
			browser.OpenANewTab();
		}
		[Given(@"WinAppDriver is running")]
		public void GivenWinAppDriverIsRunning()
		{
			try
			{
				Logger.Log("rootDriver is running: " + rootDriver);
			}
			catch
			{
				Logger.Log("Guess not, attempting to launch winappdriver");
				var winAppDriver = Process.Start(@"C:\Program Files\Windows Application Driver\WinAppDriver.exe");
				_scenarioContext.Add("WinAppDriver", winAppDriver);
				if (rootDriver != null)
				{
					Logger.Log("It worked!");
				}
			}
		}
		[When(@"I launch Firefox")]
		public void WhenILaunchFirefox()
		{
			Process.Start("C:\\Program Files\\Mozilla Firefox\\firefox.exe");
		}

		bool GoLiveAndCheckForEncoding()
		{
			//if (_scenarioContext.ContainsKey("SlobsSession")) CloseSlobsSession();
			//if (_scenarioContext.ContainsKey("GoLiveSession")) CloseGoLiveSession();
			//LaunchSlobsAndClickStartingSoon();
			Logger.Log("Going Live and checking for encoding");
			var goLiveButton = slobsSession.FindElementByName("Go Live");
			goLiveButton.Click();
			var quitTime = DateTime.Now.AddSeconds(30);
			//LaunchGoLiveSession();
			//else UpdateGoLiveSession();
			while (true)
			{
				try
				{
					var goLiveSession = ConnectToSession("Go Live");
					var confirmAndGoLiveButton = goLiveSession.FindElementByName("Confirm & Go Live");
					confirmAndGoLiveButton.Click();
					Logger.Log("confirmAndGoLiveButton was clicked");
					break;
				}
				catch (Exception ex)
				{
					if (DateTime.Now > quitTime) throw new OperationCanceledException("Timeout failure while looking for Confirm & Go Live button");
					Logger.Log("not yet... ");
				}
			}
			if (!twitchVideoPage.LaunchBrowserAndCheckForEncoding())
			{
				var endStreamButton = slobsSession.FindElementByName("End Stream");
				endStreamButton.Click();
				return false;
			}
			else return true;
		}

		public void CloseSlobsSession()
		{
			CloseGoLiveSession();
			Logger.Log("Killing Slobs session");
			if (_scenarioContext.TryGetValue("SlobsSession", out WindowsDriver<WindowsElement> session))
			{
				try
				{
					session.Close();
				}
				catch
				{
				}
				try
				{
					session.Quit();
				}
				catch
				{
				}
				_scenarioContext.Remove("SlobsSession");
			}
			try
			{
				var sess = ConnectToSession("Streamlabs Desktop");
				sess.Close();
			}
			catch { }
		}

		private void KillProcessesByName(string v)
		{
			int count = 0;
			var p = Process.GetProcessesByName(v);
			foreach (var process in p)
			{
				count++;
				try
				{
					process.Kill();
				}
				catch { }
			}
			Logger.Log("Killed " + count + " " + v + " processes");
		}

		void CloseGoLiveSession()
		{
			Logger.Log("Killing GoLive session");
			if (_scenarioContext.TryGetValue("GoLiveSession", out WindowsDriver<WindowsElement> session))
			{
				try
				{
					session.Close();
				}
				catch
				{
				}
				try
				{
					session.Quit();
				}
				catch
				{
				}
				_scenarioContext.Remove("GoLiveSession");
			}
			KillProcessesByName("Go Live");
		}
	}
}
