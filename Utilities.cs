using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System;

namespace SeleniumBot
{
	public static class Utilities
	{
		public static string DownloadLatestVersionOfChromeDriver()
		{
			string path = DownloadLatestVersionOfChromeDriverGetVersionPath();
			var version = DownloadLatestVersionOfChromeDriverGetChromeVersion(path);
			var urlToDownload = DownloadLatestVersionOfChromeDriverGetURLToDownload(version);
			DownloadLatestVersionOfChromeDriverKillAllChromeDriverProcesses();
			return DownloadLatestVersionOfChromeDriverDownloadNewVersionOfChrome(urlToDownload);
		}

		public static string DownloadLatestVersionOfChromeDriverGetVersionPath()
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

		public static string DownloadLatestVersionOfChromeDriverGetChromeVersion(string productVersionPath)
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

		public static string DownloadLatestVersionOfChromeDriverGetURLToDownload(string version)
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

		public static void DownloadLatestVersionOfChromeDriverKillAllChromeDriverProcesses()
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

		public static string DownloadLatestVersionOfChromeDriverDownloadNewVersionOfChrome(string urlToDownload)
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
	}
}