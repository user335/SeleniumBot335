using Microsoft.Win32;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumBot.PageObjects;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml.Linq;
using TechTalk.SpecFlow;

namespace SeleniumBot
{
	public class Browser
    {
        public Browser(ScenarioContext injectedContext)
        {
            scenarioContext = injectedContext;
        }
        readonly ScenarioContext scenarioContext;
        public IWebDriver _webDriver => scenarioContext["webDriver"] as IWebDriver;
        public WebDriverWait _wait { get { return new WebDriverWait(_webDriver, defaultWaitTimeout); } }
        public TimeSpan defaultWaitTimeout = TimeSpan.FromSeconds(10);
        public void StartWebDriverAndStoreInContext()
        {
            Console.WriteLine("Starting the driver...");
            //string path = Assembly.GetExecutingAssembly().Location;
            //var cService = ChromeDriverService.CreateDefaultService($@"{path}\..");
            //cService.HideCommandPromptWindow = true;
            string path = DownloadLatestVersionOfChromeDriver();
            ChromeOptions options = new ChromeOptions();
            //options.AddArguments("--disable-extensions");
            options.AddArguments("--disable-infobars");
            options.AddArguments("--ignore-cretificate-errors");
            //options.AddArguments("--incognito");
            options.AddArguments("--window-size=1600,1000");
            var driver = new ChromeDriver(path, options, TimeSpan.FromMinutes(3));
            driver.Navigate().GoToUrl("about:blank");
            foreach (var handle in driver.WindowHandles)
            {
                driver.SwitchTo().Window(handle);
                if (driver.Url.Contains("get.adblock"))
                {
                    driver.Close();
                }
            }
            scenarioContext.Add("webDriver", driver);
            //return driver;
        }
        public bool WebDriverIsLive()
        {
            try
            {
                var type = _webDriver.GetType();
                return !string.IsNullOrEmpty(_webDriver.CurrentWindowHandle);
            }
            catch (Exception)
            {
                return false;
            }
        }
        public void WaitForPageReady()
        {
            _wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
        }
        public void WaitForElementDisplayed_ByXpath(string xpath)
        {
            _wait.Until(d => d.FindElement(By.XPath(xpath)).Displayed);
        }
        public void WaitForElementDisplayed_ById(string id)
        {
            _wait.Until(d => d.FindElement(By.Id(id)).Displayed);
        }
        public void RefreshPage()
        {
            _webDriver.Navigate().GoToUrl(_webDriver.Url);
        }
        public bool IsElementDisplayed_ByXPath(string xpath)
        {
            try
            {
                _wait.Until(d => d.FindElement(By.XPath(xpath)).Displayed);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool IsElementDisplayed_ById(string id)
        {
            try
            {
                _wait.Until(d => d.FindElement(By.Id(id)).Displayed);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool IsVideoTitleCorrect()
        {
            var title = _webDriver.FindElement(By.XPath(YouTubePlayPage.videoTitleXPath));
            var text = title.GetAttribute("innerText");
            return text.ToLower().Contains("eye of the tiger");
        }
		public string DownloadLatestVersionOfChromeDriver()
		{
			string path = DownloadLatestVersionOfChromeDriverGetVersionPath();
			var version = DownloadLatestVersionOfChromeDriverGetChromeVersion(path);
			var urlToDownload = DownloadLatestVersionOfChromeDriverGetURLToDownload(version);
			DownloadLatestVersionOfChromeDriverKillAllChromeDriverProcesses();
			return DownloadLatestVersionOfChromeDriverDownloadNewVersionOfChrome(urlToDownload);
		}

		public string DownloadLatestVersionOfChromeDriverGetVersionPath()
		{
			//Path originates from here: https://chromedriver.chromium.org/downloads/version-selection            
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\chrome.exe"))
			{
				if (key != null)
				{
					Object o = key.GetValue("");
					if (!String.IsNullOrEmpty(o.ToString()))
					{
						return o.ToString();
					}
					else
					{
						throw new ArgumentException("Unable to get version because chrome registry value was null");
					}
				}
				else
				{
					throw new ArgumentException("Unable to get version because chrome registry path was null");
				}
			}
		}

		public string DownloadLatestVersionOfChromeDriverGetChromeVersion(string productVersionPath)
		{
			if (String.IsNullOrEmpty(productVersionPath))
			{
				throw new ArgumentException("Unable to get version because path is empty");
			}

			if (!File.Exists(productVersionPath))
			{
				throw new FileNotFoundException("Unable to get version because path specifies a file that does not exists");
			}

			var versionInfo = FileVersionInfo.GetVersionInfo(productVersionPath);
			if (versionInfo != null && !String.IsNullOrEmpty(versionInfo.FileVersion))
			{
				Console.WriteLine("Fetching chromedriver version: " + versionInfo.FileVersion);
				return versionInfo.FileVersion;
			}
			else
			{
				throw new ArgumentException("Unable to get version from path because the version is either null or empty: " + productVersionPath);
			}
		}

		public string DownloadLatestVersionOfChromeDriverGetURLToDownload(string version)
		{
			if (String.IsNullOrEmpty(version))
			{
				throw new ArgumentException("Unable to get url because version is empty");
			}

			//URL's originates from here: https://chromedriver.chromium.org/downloads/version-selection
			string html = string.Empty;
			string urlToPathLocation = @"https://chromedriver.storage.googleapis.com/LATEST_RELEASE_" + String.Join(".", version.Split('.').Take(3));

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlToPathLocation);
			request.AutomaticDecompression = DecompressionMethods.GZip;

			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
			using (Stream stream = response.GetResponseStream())
			using (StreamReader reader = new StreamReader(stream))
			{
				html = reader.ReadToEnd();
			}

			if (String.IsNullOrEmpty(html))
			{
				throw new WebException("Unable to get version path from website");
			}

			return "https://chromedriver.storage.googleapis.com/" + html + "/chromedriver_win32.zip";
		}

		public void DownloadLatestVersionOfChromeDriverKillAllChromeDriverProcesses()
		{
			//It's important to kill all processes before attempting to replace the chrome driver, because if you do not you may still have file locks left over
			var processes = Process.GetProcessesByName("chromedriver");
			foreach (var process in processes)
			{
				try
				{
					process.Kill();
				}
				catch
				{
					//We do our best here but if another user account is running the chrome driver we may not be able to kill it unless we run from a elevated user account + various other reasons we don't care about
				}
			}
		}

		public string DownloadLatestVersionOfChromeDriverDownloadNewVersionOfChrome(string urlToDownload)
		{
			if (String.IsNullOrEmpty(urlToDownload))
			{
				throw new ArgumentException("Unable to get url because urlToDownload is empty");
			}
			//string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			//Downloaded files always come as a zip, we need to do a bit of switching around to get everything in the right place
			using (var client = new WebClient())
			{
				try
				{
					if (File.Exists(path + "\\chromedriver.zip"))
					{
						File.Delete(path + "\\chromedriver.zip");
					}

					client.DownloadFile(urlToDownload, path + "\\chromedriver.zip");

					if (File.Exists(path + "\\chromedriver.zip") && File.Exists(path + "\\chromedriver.exe"))
					{
						File.Delete(path + "\\chromedriver.exe");
					}

					if (File.Exists(path + "\\chromedriver.zip"))
					{
						System.IO.Compression.ZipFile.ExtractToDirectory(path + "\\chromedriver.zip", path);
					}
					else throw new OperationCanceledException("Chromedriver Download failed! file:\\\\" + path);
				}
				catch (Exception e)
				{
					Logger.Log("DownloadLatestVersionOfChromeDriverDownloadNewVersionOfChrome hit exception: " + e);
				}
			}
			return path;
		}

		public void GoTo(string url)
		{
			if (!WebDriverIsLive()) StartWebDriverAndStoreInContext();
			_webDriver.Navigate().GoToUrl(url);
			WaitForPageReady();
		}

		public void SuperClick(string elementXPath)
		{
			var element = _webDriver.FindElement(By.XPath(elementXPath));
			IJavaScriptExecutor executor = (IJavaScriptExecutor)_webDriver;
			executor.ExecuteScript("arguments[0].click();", element);
		}
		public void SuperClick(IWebElement element)
		{
			IJavaScriptExecutor executor = (IJavaScriptExecutor)_webDriver;
			executor.ExecuteScript("arguments[0].click();", element);
		}
	}
}
