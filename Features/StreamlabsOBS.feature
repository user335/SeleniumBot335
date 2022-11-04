Feature: Streamlabs OBS

@SeleniumJustCuz
Scenario: It's Twitch stream time
Given Appium is running
When I launch slobs and go live
Then my stream is live!