using com.okitoki.wavhello;
using OpenQA.Selenium;
using Plugin.SimpleAudioPlayer;
using PracticingSix.PageObjects;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using System.Threading;
using TechTalk.SpecFlow;

namespace PracticingSix
{
    public class TwitchBot
    {
        public TwitchBot(ScenarioContext injectedContext)
        {
            _scenarioContext = injectedContext;
        }
        readonly ScenarioContext _scenarioContext;
        Browser browser => new Browser(_scenarioContext);
        string _path => ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath;
        string _pathPlusRoot => _path.Substring(0, _path.IndexOf(rootFolder) + rootFolder.Length);
        string rootFolder = "PracticingSix\\";
        string audioFolder= "Audio\\";
        WavePlayer wavePlayer => new WavePlayer();
        string _userCheckTheChat => _pathPlusRoot + audioFolder + "usercheckthechat.wav";
        string _whip => _pathPlusRoot + audioFolder + "351267__peterbullmusic__mixed-whip-crack-1.wav";
        string _userBackToWork => _pathPlusRoot + audioFolder + "usergetbacktowork.wav";
        string _goodnight => _pathPlusRoot + audioFolder + "goodnight.wav";
        string _sleepyTime => _pathPlusRoot + audioFolder + "sleepytime.wav";
        string _pop => _pathPlusRoot + audioFolder + "202230__deraj__pop-sound.wav";
        string _brb => _pathPlusRoot + audioFolder + "brb.wav";
        string _back => _pathPlusRoot + audioFolder + "back.wav";
        string _enterRoom => _pathPlusRoot + audioFolder + "249573__rivernile7__kocking-door-and-open-door.wav";
        string _burrow => _pathPlusRoot + audioFolder + "SCLURKERBURROW.wav";
        string _unBurrow => _pathPlusRoot + audioFolder + "SCUNBURROW.wav";
        TwitchChatPage twitchChatPage => new TwitchChatPage(_scenarioContext);
        List<string> namesGreetedThisSession; //todo: upgrade to Greetings.db, update all times of greetings when given and greet when hoursSinceLast >= 4
        List<string> lurkers;
        List<string> brbs;

        int lastLineParsed = 0;
        int lastUserElementNumber = 0;
        int lastTextElementNumber = 0;
        int lastRaidNumberParsed = 0;
        static string _lastLineParsedText;
        static string _lastChatUser;
        static string _lastChatMessage;
        static string _lastRaider;
        public void Go()
        {
            namesGreetedThisSession = new List<string>();
            lurkers = new List<string>();
            brbs = new List<string>();
            string nameToReGreet_Manually = "";
            while (true)
            {
                if (NewLineWasDetected())
                {
                    _lastLineParsedText = twitchChatPage.allChatLines.ElementAt(lastLineParsed).Text;
                    lastLineParsed++;
                    if (twitchChatPage.allUserNames.Count > lastUserElementNumber)
                    {
                        _lastChatUser = twitchChatPage.allUserNames.ElementAt(lastUserElementNumber).Text.ToLower();
                        lastUserElementNumber++;
                        if (twitchChatPage.allChatMessages.Count > lastTextElementNumber)
                        {
                            _lastChatMessage = twitchChatPage.allChatMessages.ElementAt(lastTextElementNumber).Text.ToLower();
                            lastTextElementNumber++;
                        }
                        else _lastChatMessage = "";
                        ParseForCommands(_lastChatMessage, _lastChatUser);
                    }
                    else CheckForNewRaidOrHost();
                }
                else CheckIfBrowserChoppedOutOldChats();
                if (!string.IsNullOrEmpty(nameToReGreet_Manually))
                {
                    namesGreetedThisSession.Remove(nameToReGreet_Manually);
                    nameToReGreet_Manually = "";
                }
                //Thread.Sleep(250); //not necessary
            }
        }
        bool NewLineWasDetected()
        {
            int count = twitchChatPage.allChatLines.Count;
            if (count > lastLineParsed)
            {
                Console.WriteLine("Line " + count);
                return true;
            }
            return false;
        }
        public void ParseForCommands(string input, string user)
        {
            if (string.IsNullOrEmpty(user) || user == "nightbot") return;
            Console.WriteLine("Parsing: " + input + " from: " + user);
            //parse user specific commands first
            if (!namesGreetedThisSession.Contains(user))
            {
                if (user == "user335" && (input.StartsWith("!") || input.StartsWith("/"))) return; //only play user's intro if user actually chats
                namesGreetedThisSession.Add(user);
                if (CheckForCustomIntroPlay(user) == false)
                    PlayEnterRoom();

                if (user == "user335")
                {
                    PlayWhipBackToWork();
                    //PlayUserGetBackToWork(); 
                }
            }
            if (CanUserUnlurk(user, input))
            {
                PlayAndUnlurkUser(user);
                if (input.ToLower().Contains("unlurk")) input = input.Substring(input.ToLower().IndexOf("unlurk") + 6);
            }

            input = input.ToLower();

            if (brbs.Contains(user))
                PlayBack(user);
            if (Regex.Match(input, @"lurk(s*)\b", RegexOptions.IgnoreCase).Success || input.ToLower().Contains("lurker") || input.ToLower().Contains("lurking"))
                PlayAndTrackLurker(user);
            else if (Regex.Match(input, @"brb\b", RegexOptions.IgnoreCase).Success) PlayBrb(user);

            if (input.Contains("pop")) PlayPop();
            else if (input.Contains("!whip")) PlayWhipBackToWork();
            else if (input.Contains("check") && input.Substring(input.IndexOf("check")).Contains("chat"))
                PlayUserCheckChat();
            else if (input.Contains("back")
                && input.Substring(input.IndexOf("back")).Contains("to")
                && input.Substring(input.IndexOf("to")).Contains("work"))
                PlayUserGetBackToWork();
            else if (input.Contains("goodnight") || input.Contains("bye") ||
                (input.Contains("going")
                && input.Substring(input.IndexOf("going")).Contains("to")
                && (input.Substring(input.IndexOf("to")).Contains("bed") || input.Substring(input.IndexOf("to")).Contains("sleep"))))
                PlayGoodnight();

            //this ones tricky, make sure sleep/rest comes after get/need before playing
            if ((input.Contains("get") || input.Contains("need"))
                && (input.Contains("sleep") || input.Contains("rest")))
            {
                int index = input.IndexOf("get");
                if (index < 0) index = input.IndexOf("need");
                if (index < 0) return;
                if (input.Substring(index).Contains("rest") || input.Substring(index).Contains("need"))
                    PlaySleepyTime();
            }
        }
        DateTime checkChatLockedOutUntilTime = DateTime.MinValue;
        public void PlayUserCheckChat()
        {
            if (DateTime.Now < checkChatLockedOutUntilTime) return;
            checkChatLockedOutUntilTime = DateTime.Now.AddSeconds(1.88f);
            wavePlayer.Play(_userCheckTheChat);
        }
        DateTime backToWorkLockedOutUntilTime = DateTime.MinValue;
        public void PlayUserGetBackToWork()
        {
            if (DateTime.Now < backToWorkLockedOutUntilTime) return;
            backToWorkLockedOutUntilTime = DateTime.Now.AddSeconds(2.15f);
            wavePlayer.Play(_userBackToWork);
        }
        public void PlayGoodnight()
        {
            //if (DateTime.Now < backToWorkLockedOutUntilTime) return;
            //backToWorkLockedOutUntilTime = DateTime.Now.AddSeconds(2.15f);
            wavePlayer.Play(_goodnight);
        }
        public void PlaySleepyTime()
        {
            wavePlayer.Play(_sleepyTime);
        }

        public void PlayWhipBackToWork()
        {
            wavePlayer.Play(_whip);
            backToWorkLockedOutUntilTime = DateTime.Now.AddSeconds(2.15f);
            wavePlayer.Play(_userBackToWork);
        }
        public void PlayPop()
        {
            wavePlayer.Play(_pop);
        }
        public void PlayBrb(string user)
        {
            wavePlayer.Play(_brb);
            brbs.Add(user);
        } 
        public void PlayBack(string user)
        {
            wavePlayer.Play(_back);
            brbs.Remove(user);
        }
        public void PlayEnterRoom()
        {
            wavePlayer.Play(_enterRoom);
        }
        List<string> clipsToRollFrom = new List<string>();
        /// <summary>
        /// returns true if custom intro is found and plays
        /// </summary>
        public bool CheckForCustomIntroPlay(string user)
        {
            string fullpath = _pathPlusRoot + audioFolder + "TwitchNames\\" + user + ".wav";
            if (!File.Exists(fullpath)) return false;
            Console.WriteLine("Eyy, its " + user + "!");
            if (File.Exists(fullpath.Replace(".wav", "1.wav")))
            {
                int i = 1;
                clipsToRollFrom.Clear();
                clipsToRollFrom.Add(fullpath);
                while (File.Exists(fullpath.Replace(".wav", $"{i}.wav")))
                {
                    clipsToRollFrom.Add(fullpath.Replace(".wav", $"{i}.wav"));
                    i++;
                }
                int rollo = new Random().Next(clipsToRollFrom.Count);
                Console.WriteLine("Found " + clipsToRollFrom.Count + " clips for user " + user + " and rolled clip #: " + rollo);
                fullpath = clipsToRollFrom.ElementAt(rollo);
            }
            float duration = 0;
            //try
            //{
            duration = TryToPlayFileFromPath(fullpath);
            //}
            //catch (Exception e)
            //{
            //    var sp = new SoundPlayer(fullpath);
            //    sp.Play();
            //    Console.WriteLine("clip played the old boring way, e was: " + e.Message);
            //    Thread.Sleep(user == "user335" ? 600 : 3000);
            //}
            //var sp = new SoundPlayer(fullpath);
            //sp.Play();
            Console.WriteLine("clip played the old boring way, duration was: " + duration);
            return true;
        }
        public float TryToPlayFileFromPath(string fullpath)
        {
            WavePlayer player = new WavePlayer();
            WaveFile file = player.Play(fullpath);
            if (file == null)
            {
                Console.WriteLine("file was null!");
                return -1;
            }
            Console.WriteLine("Duration of clip was " + file.LengthInSeconds() + ", thanks rabbit_xxxx you're the best!");
            return file.LengthInSeconds();
        }
        public void PlayAndTrackLurker(string user)
        {
            if (!lurkers.Contains(user))
            {
                lurkers.Add(user);
                wavePlayer.Play(_burrow);
                //Thread.Sleep(2000);
            }

        }

        //lol this
        //public int GetMaxMatchedChars(string stringA, string stringB)
        //{
        //    int matchLength = 0;
        //    for (int i = 0; i < stringA.Length; i++)
        //    {
        //        if (stringB.Contains(stringA.ElementAt(i)))
        //        {
        //            //get max chars to check by comparing length of remaining string in A vs same in B
        //            int charsToCheck = Math.Min(stringA.Substring(i).Length, stringB.Substring(stringB.IndexOf(stringA.ElementAt(i))).Length);
        //            while (!stringB.Contains(stringA.Substring(i, charsToCheck))) 
        //                charsToCheck--;
        //            if (charsToCheck > matchLength) matchLength = charsToCheck;
        //        }
        //    }
        //    //return longest length match found
        //    return matchLength;
        //}
        public bool CanUserUnlurk(string user, string input)
        {
            return lurkers.Contains(user) || input.Contains("unlurk");
        }
        public void PlayAndUnlurkUser(string user)
        {
            lurkers.Remove(user);
            wavePlayer.Play(_unBurrow);
            //Thread.Sleep(1000);
        }
        public void CheckForNewRaidOrHost()
        {
            if (twitchChatPage.allRaids.Count() > lastRaidNumberParsed)
            {
                var newRaid = twitchChatPage.allRaids.ElementAt(lastRaidNumberParsed);
                _lastRaider = newRaid.GetAttribute("alt");
                //_newRaid.Play(); //rely on streamlabs for this
                Thread.Sleep(1000);
                if (CheckForCustomIntroPlay(_lastRaider) == false)
                    PlayEnterRoom();
            }
        }

        /// <summary>
        /// stores FirstChat user and text on first call
        /// updates all indexes whenever FirstChat is not the same anymore due to twitch browser cropping out old chats
        /// </summary>
        public void CheckIfBrowserChoppedOutOldChats()
        {
            //if (twitchChatPage.allUserNames.Count > 0)
            //{
            //    if (string.IsNullOrEmpty(_firstChatUser) || (_firstChatUser != twitchChatPage.allUserNames.First().Text))
            //    {
            //        _firstChatUser = twitchChatPage.allUserNames.First().Text;
            //        _firstChatMessage = twitchChatPage.allChatMessages?.First()?.Text;
            //    }
            //    else if (twitchChatPage.allUserNames.First(o => o != null).Text != _firstChatUser || twitchChatPage.allChatMessages.First(o => o != null).Text != _firstChatMessage)
            //    {
            //        RecalculateIndexes();
            //    }
            //}
            //if (lastLineParsed >= 150)
            //    throw new OperationCanceledException("That's the max! Restart this bot or it's done!");
            ////todo: fix this!
        }
        void RecalculateIndexes()
        {
            int totalLinesChopped = 0;
            int linecount = twitchChatPage.allChatLines.Count;
            int usercount = twitchChatPage.allUserNames.Count;
            int messagecount = twitchChatPage.allChatMessages.Count;
            for (int i = 0; i < twitchChatPage.allChatLines.Count; i++)
            {
                if (twitchChatPage.allChatLines.ElementAt(i).Text == _lastLineParsedText)
                {
                    totalLinesChopped = lastLineParsed - i;
                    lastLineParsed = i;
                    break;
                }
            }
            //for (int i = lastUserElementNumber - (totalLinesChopped + 1); i < twitchChatPage.allUserNames.Count; i++)
            //{
            //    if (twitchChatPage.allUserNames.ElementAt(i).Text == lastuser)
            //        }
        }
    }
}
