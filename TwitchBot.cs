using com.okitoki.wavhello;
using PracticingSix.PageObjects;
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
using System.Windows;
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
        public void Go()
        {
            namesGreetedThisSession = new List<string>();
            lurkers = new List<string>();
            brbs = new List<string>();
            string nameToReGreet_Manually = "";
            while (true && browser._webDriver.Url.Contains("335"))
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
            if (string.IsNullOrEmpty(user) || user == "seleniumbot335" || user.ToLower() == "nightbot" || input.StartsWith("/")) return;
            Console.WriteLine("Parsing: " + input + " from: " + user);

            bool cameFromHost = string.Equals(user, "user335", StringComparison.OrdinalIgnoreCase);
            //parse user specific commands first
            //if (!namesGreetedThisSession.Contains(user))
            var sql = $"SELECT LastGreetTime FROM dbo.[LastGreetTimes] WHERE Name = '{user}'";
            object response = null;
            try
            {
                response = new SqlCommand(sql, _scenarioContext.Con()).ExecuteScalar();
            }
            catch (Exception e)
            {
                _scenarioContext.CloseCon();
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

                if (CheckForCustomIntroPlay(user) == false)
                    PlayEnterRoom();

                if (cameFromHost)
                {
                    PlayWhipBackToWork();
                }
            }
            //         else
            //{
            //             _scenarioContext.CloseCon();
            //}
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
            //else if (input.Contains("back ")
            //    && input.Substring(input.IndexOf("back ")).Contains(" to ")
            //    && input.Substring(input.IndexOf("back ")).Substring(input.IndexOf(" to ")).Contains(" work"))
            //PlayUserGetBackToWork();
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
                PingToChat("Who, me? User335 made me with selenium, which he LOVES talking about to a ridiculous degree because test automation is the future of all QA so feel free to distract him further with more questions!");
            if (input.StartsWith("!discord", StringComparison.OrdinalIgnoreCase))
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
        public void PlayEnterRoom()
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
            GreetUserFromDBIfFound(user);
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

		private void GreetUserFromDBIfFound(string user)
		{
            var sql = $"SELECT [Greeting] FROM dbo.[CurrentGreetings] WHERE [Name] = '{user}'";
			var greeting = new SqlCommand(sql, _scenarioContext.Con()).ExecuteScalar();
            if (greeting != null)
			{
                PingToChat($"User335 recommends you check out {user}'s work: {greeting}");
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
                lurkers.Add(user);
                wavePlayer.Play(_burrow);
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
                {
                    PlayEnterRoom();
                }
                PingToChat("Big thanks to " + _lastRaider + " for the raid! Hook them up with a follow if you can: twitch.tv/" + _lastRaider);
                CreateNewDefaultGreetingIfNoneFoundInDB(_lastRaider);
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
                if (input.StartsWith("!regreetnow", StringComparison.OrdinalIgnoreCase))
                {
					var user = input.Substring(11).Trim();
					CheckForCustomIntroPlay(user);
				}
                else
                {
                    var user = input.Substring(8).Trim();
                    var sql = $"DELETE FROM LastGreetTimes WHERE [Name] = '{user}'";
                    ExecuteNonQuery(sql);
                    PingToChat(user + " will be greeted again next time they chat");
                    return true;
                }
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
				_scenarioContext.CloseCon();
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
				_scenarioContext.CloseCon();
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
            catch (Exception)
            {
				var writer = _scenarioContext.Get<StreamWriter>("StreamWriter");
				var reader = _scenarioContext.Get<StreamReader>("StreamReader");
                writer.Flush();
                writer.Close();
                reader.Close();
                _scenarioContext.Remove("StreamWriter");
                _scenarioContext.Remove("StreamReader");
				writer = InitializeStreamWriter();
				writer.WriteLine($"PRIVMSG #user335 :{message}");
			}
		}
        public StreamWriter InitializeStreamWriter()
		{
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
