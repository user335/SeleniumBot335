using com.okitoki.wavhello;
using SeleniumBot.PageObjects;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Sockets;
//using System.Media;
using System.Text.RegularExpressions;
using System.Threading;
using TechTalk.SpecFlow;

namespace SeleniumBot
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
        string rootFolder = "SeleniumBot\\";
        string audioFolder = "Audio\\";
			
        string ip = "irc.chat.twitch.tv";
        int port = 6667;
        string _oauth => Environment.GetEnvironmentVariable("TwitchOAuth");
        string botUsername = "SeleniumBot335";
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
        static string _firstChatUser;
        static string _firstChatMessage;
        static bool _endTest = false;
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
                        _lastChatUser = twitchChatPage.allUserNames.ElementAt(lastUserElementNumber).Text;
                        lastUserElementNumber++;
                        if (twitchChatPage.allChatMessages.Count > lastTextElementNumber)
                        {
                            _lastChatMessage = twitchChatPage.allChatMessages.ElementAt(lastTextElementNumber).Text;
                            lastTextElementNumber++;
                        }
                        else _lastChatMessage = "";
						if (string.IsNullOrEmpty(_firstChatUser + _firstChatMessage)) //used in CheckIfBrowserChoppedOutOldChats
						{
							_firstChatUser = _lastChatUser;
							_firstChatMessage = _lastChatMessage;
						}
						if (string.IsNullOrEmpty(_lastChatUser) || _lastChatUser == "seleniumbot335" || _lastChatUser.ToLower() == "nightbot" || _lastChatMessage.StartsWith("/")) return;
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
                if (_endTest)
                {
                    Logger.Log("End of stream deyected.");
                    break;
                }
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
            Console.WriteLine("Parsing: " + input + " from: " + user);
            bool cameFromHost = string.Equals(user, "user335", StringComparison.OrdinalIgnoreCase);

            var sql = $"SELECT LastGreetTime FROM dbo.[LastGreetTimes] WHERE Name = '{user}'";
            object response = null;
            try
            {
                response = new SqlCommand(sql, _scenarioContext.Con()).ExecuteScalar();
            }
            catch (Exception e)
            {
                _scenarioContext.CloseSqlConn();
                throw e;
            }
            if (cameFromHost && input.StartsWith("!"))
            {
                if (ParseHostOnlyCommand(input)) //if ParseHostOnlyCommand returns true it means a host-only command was matched, which should cause early exit to prevent overlap
                    return;
            }
            if (response == null || string.IsNullOrEmpty(response.ToString()) || DateTime.Now.Subtract((DateTime)response) >= TimeSpan.FromHours(6))
            {
                sql = $"DELETE FROM LastGreetTimes WHERE [Name] = '{user}'";
                new SqlCommand(sql, _scenarioContext.Con()).ExecuteNonQuery();

                sql = $"INSERT LastGreetTimes (Name, LastGreetTime) values ('{user}','{DateTime.Now}')";
                int success = new SqlCommand(sql, _scenarioContext.Con()).ExecuteNonQuery();

                if (success != 1) throw new OperationCanceledException("Why did that insert fail? sql was: " + sql.ToString());
                else Console.WriteLine($"Greeted {user} at {DateTime.Now.ToShortTimeString()}");

                GreetUserInChat(user);
                GreetUserInAudio(user);				

                if (cameFromHost)
                {
                    PlayWhipBackToWork();
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
            else if (new Regex(@"back\sto\swork").IsMatch(input))
            {
                PlayUserGetBackToWork();
            }
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
            if (input.Contains("selenium") && input.Substring(input.IndexOf("selenium")).Contains("bot") && input.Contains("?"))
                PingToChat("Who, me? User335 made me with selenium, which he LOVES talking about to a ridiculous degree because test automation is the future of all QA - see full source code at github.com/user335/SeleniumBot335");
            else if (input.StartsWith("!discord", StringComparison.OrdinalIgnoreCase))
            {
                PingDiscord();
            }
            else if (input.StartsWith("!eott", StringComparison.OrdinalIgnoreCase))
            {
                PingToChat("user335 has been listening to $(lastfm user-335) for motivation - see last.fm/user/user-335 for latest stats!");
            }
            else if (input.StartsWith("!demo", StringComparison.OrdinalIgnoreCase) || input.StartsWith("!itch", StringComparison.OrdinalIgnoreCase) || input.StartsWith("!play", StringComparison.OrdinalIgnoreCase) || input.StartsWith("!rota", StringComparison.OrdinalIgnoreCase))
            {
                PingDemo();
            }
			else if (input.StartsWith("!wishlist", StringComparison.OrdinalIgnoreCase) || input.StartsWith("!steam", StringComparison.OrdinalIgnoreCase))
			{
                PingSteam();
			}
            else if (input.StartsWith("!github", StringComparison.OrdinalIgnoreCase))
            {
                PingToChat("My source code is available at github.com/user335/SeleniumBot335");
            }
		}

        void GreetUserInAudio(string user)
        {
			if (!CheckForCustomIntroPlay(user))
				PlayGenericEnterRoom();
		}

        void GreetUserInChat(string user)
        {
			if (!GreetUserFromDBIfFound(user)) 
                IntroduceUserToChatGenerically(user);
		}

        void PingDemo()
        {
			PingToChat("Try the game out here: user335.itch.io/rota - no install needed just download, extract, and run the exe. ALL feedback GREATLY appreciated!");
		}
        void PingDiscord()
        {
			PingToChat("Join user335's discord channel at: discord.gg/aH335JB");
		}
        void PingSteam()
        {
			PingToChat("user335 politely begs you to wishlist his game on the platform that forces all developers to do so: store.steampowered.com/app/1152260/Renditions_of_the_Awakening/");
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
            PingToChat("A strong whip gets automatically cracked across the back of user335. Get back to work, U!");
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
        public void PlayGenericEnterRoom()
        {
            wavePlayer.Play(_enterRoom);
        }
        List<string> clipsToRollFrom = new List<string>();
        /// <summary>
        /// returns true if custom intro is found and plays
        /// also checks the database for a greeting and chats it out if found!
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

		private bool GreetUserFromDBIfFound(string user)
		{
            var sql = $"SELECT [Greeting] FROM dbo.[CurrentGreetings] WHERE [Name] = '{user}'";
			var greeting = new SqlCommand(sql, _scenarioContext.Con()).ExecuteScalar();
            if (greeting != null)
			{
                PingToChat($"User335 recommends you check out {user}'s work: {greeting}");
                return true;
			}
            return false;
		}
        void IntroduceUserToChatGenerically(string user)
        {
            int roll = new Random().Next(63);
			switch (roll)
            {
                case 0: PingToChat($"{user} walks in"); break;
                case 1: PingToChat($"{user} strolls in"); break;
                case 2: PingToChat($"{user} makes an entrance"); break;
                case 3: PingToChat($"{user} appears"); break;
                case 4: PingToChat($"{user} suddenly appears"); break;
                case 5: PingToChat($"{user} pops out of a bush"); break;
                case 6: PingToChat($"{user} descends slowly from a tree"); break;
                case 7: PingToChat($"{user} climbs in through an unlocked window"); break;
                case 8: PingToChat($"{user} breaks an unlocked window and climbs in"); break;
                case 9: PingToChat($"{user} breaks an unlocked window and climbs in, cutting themselves on the glass"); break;
                case 10: PingToChat($"{user} is here"); break;
                case 11: PingToChat($"{user} emerges from a demon portal"); break;
                case 12: PingToChat($"{user} emerges from a demon portal, and is hungry"); break;
                case 13: PingToChat($"{user} emerges from a demon portal and immediately goes HAM"); break;
                case 14: PingToChat($"{user} defenestrates themselves into the stream"); break;
                case 15: PingToChat($"{user} reveals they have been lurking for quite some time and heard everything we said"); break;
                case 16: PingToChat($"{user} reveals they have been hiding under the sink but emerged due to audio issues"); break;
                case 17: PingToChat($"{user} emerges from a demon po- Oh god, why are they naked!?"); break;
                case 18: PingToChat($"{user} sneaks into the room wearing a disguise"); break;
                case 19: PingToChat($"{user} sneaks into the room by walking backwards"); break;
                case 20: PingToChat($"{user} is a spy."); break;
                case 21: PingToChat($"{user} infiltrates the chat"); break;
                case 22: PingToChat($"{user} arrives pretending to be one of us"); break;
                case 23: PingToChat($"{user} arrives late but plays it cool"); break;
                case 24: PingToChat($"{user} arrives uninvited but plays it off legit"); break;
                case 25: PingToChat($"{user} arrives acting surprised about the strange smell that has also arrived"); break;
                case 26: PingToChat($"{user} gets yeeted into the room by a giant ant demon"); break;
                case 27: PingToChat($"{user} arrives despite the restraining order"); break;
                case 28: PingToChat($"A wild {user} appears!"); break;
                case 29: PingToChat($"{user} drops in"); break;
                case 30: PingToChat($"{user} kicks down the door wearing full battle armor"); break;
                case 31: PingToChat($"{user} air assaults onto the lawn"); break;
                case 32: PingToChat($"{user} parachutes onto the roof then just stays up there, listening in"); break;
                case 33: PingToChat($"{user} bursts through the wall in a BattleMech"); break;
                case 34: PingToChat($"{user} has finally arrived"); break;
                case 35: PingToChat($"{user} arrived at precisely {DateTime.Now.ToShortTimeString()} (user time) and wants everyone to know so they can use us as an alibi"); break;
                case 36: PingToChat($"{user} stumbles in, sober as ever"); break;
                case 37: PingToChat($"{user} arrives, wondering where they can park their monster truck"); break;
                case 38: PingToChat($"{user} walks out of a demon portal dual-wielding two fully charged chaos meters"); break;
                case 39: PingToChat($"{user} arrives with an entourage of angry demons"); break;
                case 40: PingToChat($"{user} has arrived, which is strange because they were not meant to be here until the next chapter..."); break;
                case 41: PingToChat($"{user} emerges from a tunnel they dug underneath the house"); break;
                case 42: PingToChat($"{user} arrives carrying a large locked treasure chest"); break;
                case 43: PingToChat($"{user} arrives wearing jeans that reek of bleach"); break;
                case 44: PingToChat($"{user} chooses here to hide out from the police"); break;
                case 45: PingToChat($"{user} wanders into the room while searching for a specific sandwich"); break;
                case 46: PingToChat($"{user} arrives on horseback"); break;
                case 47: PingToChat($"{user} arrives riding a horse backwards"); break;
                case 48: PingToChat($"{user} arrives riding a bear without a saddle"); break;
                case 49: PingToChat($"{user} arrives riding a bear backwards"); break;
                case 50: PingToChat($"A bat flies into the room, lands on the couch, then transforms into {user}"); break;
                case 51: PingToChat($"A bat wearing sunglasses flies into the room, hangs from the ceiling fan and begins sipping from a tiny flask. It must be {user}"); break;
                case 52: PingToChat($"{user} materializes in the middle of the room with no memories at all"); break;
                case 53: PingToChat($"{user} appears to have emerged from thin air, but is actually a higher-dimensional being and simply walked here"); break;
                case 54: PingToChat($"Summoner's note: {user} is communing with us from the other side"); break;
                case 55: PingToChat($"{user} has crossed three planes of existence to be with us tonight"); break;
                case 56: PingToChat($"Summoner's note: {user} is hiding from Sasquatch. Please be an ally and do not report them"); break;
                case 57: PingToChat($"Summoner's note: {user} is hiding from the police. Please be an ally and do not report them"); break;
                case 58: PingToChat($"Summoner's note: {user} is hiding from the IRS. Please be an ally and do not report them"); break;
                case 59: PingToChat($"{user} ignores several previous warnings and shows up anyway"); break;
                case 60: PingToChat($"Summoner's note: {user} is originally from the future but is participating in this conversation from the past"); break;
                case 61: PingToChat($"Summoner's note: {user} is joining us again for the first time ever, having been recently re-reborn"); break;
                case 62: PingToChat($"{user} is choosing to spend time with us instead of any of the infinite other possibilities available"); break;
                default: PingToChat($"{user} suddenly barges in with unexpected intensity (due partially to an internal switch rolling outside of expected range: {roll})"); break;
            }
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
                switch (new Random().Next(3))
                {
                    case 0: PingToChat(user + " drinks a stinky potion and disappears");
                        break;
					case 1:
						PingToChat(user + " burrows into the earth, makes a dirt nest and gets cozy");
						break;
					case 2:
						PingToChat(user + " hides from a demon going HAM");
						break;
				}
				lurkers.Add(user);
				wavePlayer.Play(_burrow);
			}
		}

        public bool CanUserUnlurk(string user, string input)
        {
            return lurkers.Contains(user) || input.Contains("unlurk");
        }
        public void PlayAndUnlurkUser(string user)
        {
            lurkers.Remove(user);
            wavePlayer.Play(_unBurrow);
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
                {
                    PlayGenericEnterRoom();
                }
                CreateNewDefaultGreetingIfNoneFoundInDB(_lastRaider);
                PingToChat("Big thanks to " + _lastRaider + " for the raid! Hook them up with a follow if you can: twitch.tv/" + _lastRaider);
            }
        }

        /// <summary>
        /// stores FirstChat user and text on first call
        /// updates all indexes whenever FirstChat is not the same anymore due to twitch browser cropping out old chats
        /// </summary>
        public void CheckIfBrowserChoppedOutOldChats()
        {
            if (twitchChatPage.allUserNames.Count > 0 && !string.IsNullOrEmpty(_firstChatUser) &&
                    (twitchChatPage.allUserNames.First(o => o != null).Text != _firstChatUser
                    || twitchChatPage.allChatMessages.First().Text != _firstChatMessage))
            {
                RecalculateIndexes();

            }
            if (lastLineParsed >= 150)
                throw new OperationCanceledException("That's the max! Restart this bot or it's done!");
            //todo: fix this!
        }
        void RecalculateIndexes()
        {
            
        }
        bool ParseHostOnlyCommand(string input)
		{
            input = input.Trim(); //can't ToLower() because it breaks caps-sensitive links (youtube)
            if (input.StartsWith("!greet", StringComparison.OrdinalIgnoreCase)) //format for !greet is !greet <who> <greeting>, which will get played back as "User335 recommends you check out <who>'s work at <greeting>"
			{
                var userAndGreet = input.Substring(6).Trim();
                var user = userAndGreet.Substring(0, userAndGreet.IndexOf(' ')).Trim();
                var greet = Sterilize(userAndGreet.Substring(userAndGreet.IndexOf(' ')).Trim());

                var sql1 = $"INSERT OldGreetings (Name,Greeting,TimeAdded) values ('{user}','{greet}','{DateTime.Now}')";
                ExecuteNonQuery(sql1);
                var sql2 = $"DELETE FROM dbo.[CurrentGreetings] WHERE [Name] = '{user}'";
                ExecuteNonQuery(sql2);
                var sql3 = $"INSERT CurrentGreetings (Name,Greeting) values ('{user}','{greet}')";
                ExecuteNonQuery(sql3);
                PingToChat("Greeting for " + user + " saved: " + greet);
                return true;
			}
            else if (input.StartsWith("!regreet", StringComparison.OrdinalIgnoreCase))
            {
                if (input.StartsWith("!regreetnext", StringComparison.OrdinalIgnoreCase))
                {
					var user = input.Substring(8).Trim();
					var sql = $"DELETE FROM LastGreetTimes WHERE [Name] = '{user}'";
					ExecuteNonQuery(sql);
					PingToChat(user + " will be greeted again next time they chat");
				}
				else
                {
					var user = input.Substring(11).Trim();
					CheckForCustomIntroPlay(user);
					var sql = $"DELETE FROM LastGreetTimes WHERE [Name] = '{user}'";
					new SqlCommand(sql, _scenarioContext.Con()).ExecuteNonQuery();
					sql = $"INSERT LastGreetTimes (Name, LastGreetTime) values ('{user}','{DateTime.Now}')";
					new SqlCommand(sql, _scenarioContext.Con()).ExecuteNonQuery();
				}
				return true;
			}
			else if (input.StartsWith("!outro", StringComparison.OrdinalIgnoreCase))
            {
                PingDiscord();
                Thread.Sleep(1000);
                PingSteam();
				Thread.Sleep(1000);
                PingDemo();
                Thread.Sleep(1000);
                PingToChat("Have a good night everybody!");
                _endTest = true;
				return true;
			}
			return false;
		}
		void CreateNewDefaultGreetingIfNoneFoundInDB(string raider)
        {
            var sql = $"SELECT [Name] from CurrentGreetings WHERE [Name] = {raider}";
            var result = ExecuteScalar(sql);
            if (result == null)
            {
                Logger.Log("Creating new default twitch.tv greeting for " + raider);
                string greet = "twitch.tv/" + raider;
				var sql1 = $"INSERT OldGreetings (Name,Greeting,TimeAdded) values ('{raider}','{greet}','{DateTime.Now}')";
				ExecuteNonQuery(sql1);
				var sql2 = $"DELETE FROM dbo.[CurrentGreetings] WHERE [Name] = '{raider}'";
				ExecuteNonQuery(sql2);
				var sql3 = $"INSERT CurrentGreetings (Name,Greeting) values ('{raider}','{greet}')";
				ExecuteNonQuery(sql3);
                PingToChat($"Added a default greeting in the database recommending people check out twitch.tv/{raider} whenever {raider} is in the room - please let user know what you'd prefer your custom message to say!");
			}
        }
		string Sterilize(string input)
        {
            return input.Replace("\0", "").Replace("\'", "").Replace("\"", "").Replace("\b", "").Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace("\\Z", "").Replace("\\", "").Replace("\\%", "").Replace("\\_", "").Replace("%", "");
		}

        object ExecuteScalar(string sql)
		{
            try
            {
                var response = new SqlCommand(sql, _scenarioContext.Con()).ExecuteScalar();
                Logger.Log("Db returned " + response);
                return response;
            }
            catch (Exception e)
            {
				_scenarioContext.CloseSqlConn();
                throw e;
            }
        }

        int ExecuteNonQuery(string sql)
		{
            try
            {
                var response = new SqlCommand(sql, _scenarioContext.Con()).ExecuteNonQuery();
                Logger.Log("Db returned " + response);
                return response;
            }
            catch (Exception e)
            {
				_scenarioContext.CloseSqlConn();
				throw e;
            }
        }
        public void PingToChat(string message)
		{
            if (!_scenarioContext.TryGetValue("StreamWriter", out StreamWriter streamWriter))
			{
                streamWriter = InitializeStreamWriter();
			}
            try
            {
                streamWriter.WriteLine($"PRIVMSG #user335 :{message}");
            }
            catch (Exception e)
            {
                Logger.Log("Failed when writing message: " + message + "\n e was: " + e);
                Logger.Log("Flushing streamwriter and retrying");
                _scenarioContext.CloseStreamWriterConn();
				streamWriter = InitializeStreamWriter();
				streamWriter.WriteLine($"PRIVMSG #user335 :{message}");
			}
		}
        public StreamWriter InitializeStreamWriter()
		{
            Logger.Log("Initializing streamwriter...");
            var tcpClient = new TcpClient();
            tcpClient.Connect(ip, port);
            var streamReader = new StreamReader(tcpClient.GetStream());
            var streamWriter = new StreamWriter(tcpClient.GetStream()) { NewLine = "\r\n", AutoFlush = true };
            streamWriter.WriteLine($"PASS {_oauth}");
            streamWriter.WriteLine($"NICK {botUsername}");
            _scenarioContext.Add("StreamReader", streamReader);
            _scenarioContext.Add("StreamWriter", streamWriter);
            return streamWriter;
		}

	}
}
