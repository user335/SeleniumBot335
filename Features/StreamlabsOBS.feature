Feature: Streamlabs OBS

@SeleniumJustCuz
Scenario: It's Twitch stream time
Given WinAppDriver is running
When I launch slobs and go live
Then my stream is live!