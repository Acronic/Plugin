using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Zeta;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.CommonBot;
using Zeta.CommonBot.Logic;
using Zeta.CommonBot.Profile;
using Zeta.CommonBot.Settings;
using Zeta.Internals;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using UIElement = Zeta.Internals.UIElement;


namespace RadsProfileManager
{
    public class RadsProfileManager : IPlugin
    {
        // Stuff Db needs to make this a plugin
        public Version Version { get { return new Version(0, 4); } }
        public string Author { get { return "Radonic"; } }
        public string Description { get { return "Restarting Radonics leveling profiles. Clicking OK on different buttons. Still in beta."; } }
        public string Name { get { return "RadsProfileManager beta"; } }
        public bool Equals(IPlugin other) { return (other.Name == Name) && (other.Version == Version); }
        public Window DisplayWindow { get { return null; } }

        // Custom logger to stamp logs with plugin name & version first
        public static void Log(string message)
        {
            Logging.Write(string.Format("[{0}] {1}", iName, message));
        }

        // All my variables used throughout the plugin
        public static DateTime _lastLooked = DateTime.Now;
        public static string iName = "RadsProfileManager beta";
        private static bool isLeaving;
        private static bool gameStarted;
        public static int dcount;
        public const int dtrip = 3;
        public static string startpname = "";
        // If they enable the plugin
        void IPlugin.OnEnabled()
        {
            GameEvents.OnGameJoined += OnGameJoined;
            GameEvents.OnGameLeft += OnGameLeft;
            GameEvents.OnPlayerDied += OnPlayerDied;
            BotMain.OnStop += HandleBotStop;
            BotMain.OnStart += HandleBotStart;
            Log("Enabled.");
        }

        // If they disable the plugin
        void IPlugin.OnDisabled()
        {
            GameEvents.OnGameJoined -= OnGameJoined;
            GameEvents.OnGameLeft -= OnGameLeft;
            GameEvents.OnPlayerDied -= OnPlayerDied;
            BotMain.OnStop -= HandleBotStop;
            BotMain.OnStart -= HandleBotStart;
            Log("Disabled.");
        }


        void IPlugin.OnInitialize()
        {
        }

        void IPlugin.OnPulse()
        {
            OkClicker();
        }

        public static void OkClicker()
        {
            if (DateTime.Now.Subtract(_lastLooked).TotalSeconds > 3)
            {
                _lastLooked = DateTime.Now;
                UIElement Warning = UIElement.FromHash(0xF9E7B8A635A4F725);
                if (Warning.IsValid && Warning.IsVisible)
                {
                    UIElement Button = UIElement.FromHash(0x891D21408238D18E);
                    if (Button.IsValid && Button.IsVisible && Button.IsEnabled)
                    {
                        Log("Clicking OK.");
                        Button.Click();
                    }
                }  
            }
        }

        void IPlugin.OnShutdown()
        {
        }

        static void HandleBotStop(IBot bot)
        {
            string lastpname = Path.GetFileName(GlobalSettings.Instance.LastProfile);
            Log("Last profile used was " + lastpname + ".");
        }

        public static void HandleBotStart(IBot bot)
        {
            startpname = GlobalSettings.Instance.LastProfile;
            Log("Starter profile is " + startpname + ".");
        }

        static void OnGameLeft(object sender, EventArgs e)
        {
        }

        static void OnPlayerDied(object sender, EventArgs e)
        {
            string lastp = GlobalSettings.Instance.LastProfile;
            if(dcount != dtrip)
            {
                Log("You died, reload profile so you can try again.");
                ProfileManager.Load(lastp);
                dcount = dcount + 1;
                Log("Deathcount: " + dcount);
            }
            if(dcount == dtrip)
            {
                Log("You died, reload profile so you can try again.");
                ProfileManager.Load(startpname);
                ZetaDia.Service.Games.LeaveGame();
            }
        }

        private static void OnGameJoined(object src, EventArgs mea)
        {
            Log("Joined Game.");
            dcount = dcount - dcount;
        }

        [XmlElement("Continue")]
        public class ContinueTag : ProfileBehavior
        {
            private bool m_IsDone = false;

            public override bool IsDone
            {
                get { return m_IsDone; }
            }

            [XmlAttribute("profile")]
            public string ProfileName { get; set; }

            [XmlAttribute("exitgame")]
            public string ExitGame { get; set; }


            protected override Zeta.TreeSharp.Composite CreateBehavior()
            {
                return new Zeta.TreeSharp.Action((ret) =>
                {
                    string lastp = GlobalSettings.Instance.LastProfile;
                    string ppath = Path.GetDirectoryName(lastp);
                    string nextProfile = ppath + "\\" + ProfileName;
                    if (ProfileName != null)
                    {
                        if (ExitGame != null)
                        {
                            Log("Been asked to load a new profile, which is " + ProfileName);
                            ProfileManager.Load(nextProfile);
                            BotMain.PauseWhile(() => ZetaDia.IsInGame || ZetaDia.IsLoadingWorld, 8);
                            ZetaDia.Service.Games.LeaveGame();  
                        }
                        else
                        {
                            Log("Been asked to load a new profile, which is " + ProfileName);
                            dcount = dcount - dcount;
                            ProfileManager.Load(nextProfile);
                        }
                    }
                    else
                    {
                        Log("No profile selected, stopping the bot.");
                        BotMain.Stop();
                    }
                    m_IsDone = true;
                });
            }

            public override void ResetCachedDone()
            {
                m_IsDone = false;
                base.ResetCachedDone();
            } // End of ResetCachedDone function

        } // End of ContinueTag

    }
}