Feature: Twitch Bot!

@SeleniumJustCuz
Scenario: Launch stream and go live
Given WinAppDriver is running
When I launch slobs and go live
Then my stream is live!
When I launch Firefox

@SeleniumJustCuz
Scenario: Run seleniumbot335
Given I want a Twitch Bot running in Selenium just for the heck of it
When I send a browser to snoop on my own chat channel
Then my Twitch Bot lives!


Scenario: Refresh a page until an element is clickable then alert
Given I 