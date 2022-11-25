# SeleniumBot335
Source code for the bot used at twitch.tv/user335

Requires VisualStudio + SpecFlow Extension to fully use - without the extension your .feature files will appear colorless
Also requires chromedriver nuget package to be updated or else the bot won't run at all. Chromedriver cannot be more than 1 version out of date from local installation
Build the project and run tests from within test explorer in VS or launch with CLI (or just update the paths inside TiwtchBot.bat and run that) using the following command:
<pathToExe>\SpecRun.exe run <pathToDll>
