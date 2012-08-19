using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using Zeta;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.CommonBot;
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
        public Version Version { get { return new Version(0, 5, 1); } }
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
        private static DateTime _lastLooked = DateTime.Now;
        private static string iName = "RadsProfileManager beta";
        private static int dcount = 0;
        private const int dtrip = 2;
        private const int dtripnxt = 3;
        private static string startpname = "";
        private static string databaseConnStr = "";

        // If they enable the plugin
        void IPlugin.OnEnabled()
        {
            GameEvents.OnGameJoined += RadsOnGameJoined;
            GameEvents.OnGameLeft += RadsOnGameLeft;
            GameEvents.OnPlayerDied += RadsOnPlayerDied;
            BotMain.OnStop += RadsHandleBotStop;
            BotMain.OnStart += RadsHandleBotStart;
            Log("Enabled.");
        }

        // If they disable the plugin
        void IPlugin.OnDisabled()
        {
            GameEvents.OnGameJoined -= RadsOnGameJoined;
            GameEvents.OnGameLeft -= RadsOnGameLeft;
            GameEvents.OnPlayerDied -= RadsOnPlayerDied;
            BotMain.OnStop -= RadsHandleBotStop;
            BotMain.OnStart -= RadsHandleBotStart;
            Log("Disabled.");
        }


        void IPlugin.OnInitialize()
        {
        }

        void IPlugin.OnPulse()
        {
            OkClicker();
        }

        void IPlugin.OnShutdown()
        {
        }

        //Functions here

        public static void OkClicker()
        {
            if (DateTime.Now.Subtract(_lastLooked).TotalSeconds > 5)
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
                        Thread.Sleep(3000);
                    }
                }
            }
        }

        static void RadsHandleBotStop(IBot bot)
        {
            string lastpname = Path.GetFileName(GlobalSettings.Instance.LastProfile);
            Log("Last profile used was " + lastpname + ".");
        }

        public static void RadsHandleBotStart(IBot bot)
        {
            startpname = GlobalSettings.Instance.LastProfile;
            Log("Starter profile is " + startpname + ".");
        }

        static void RadsOnGameLeft(object sender, EventArgs e)
        {
        }

        static void RadsOnPlayerDied(object sender, EventArgs e)
        {
            string lastp = GlobalSettings.Instance.LastProfile;
            dcount = dcount + 1;
            if (dcount < dtrip)
            {
                Log("You died, reload profile so you can try again.");
                Log("Deathcount: " + dcount);
                ProfileManager.Load(lastp);
                Thread.Sleep(1000);
            }
            else if (dcount < dtripnxt)
            {
                StreamReader stRd = new StreamReader(lastp);
                XmlTextReader xmlRd = new XmlTextReader(stRd);
                XmlDocument rdXml = new XmlDocument();
                rdXml.Load(xmlRd);
                XmlNode connNode = rdXml.SelectSingleNode("Profile/Order/Continue");
                if (connNode != null && connNode.Attributes != null)
                {
                    databaseConnStr = connNode.Attributes.GetNamedItem("profile").Value;
                    string ppath = Path.GetDirectoryName(lastp);
                    string nxtp = ppath + "\\" + databaseConnStr;
                    ProfileManager.Load(nxtp);
                    Thread.Sleep(1000);
                    Log("Died twice, will move on to next profile " + nxtp);
                    Log("Deathcount: " + dcount);
                }
            }
            else
            {
                Log("You died, reload profile so you can try again.");
                ProfileManager.Load(startpname);
                Thread.Sleep(1000);
                ZetaDia.Service.Games.LeaveGame();
                Thread.Sleep(1000);
            }
        }

        private static void RadsOnGameJoined(object src, EventArgs mea)
        {
            dcount = 0;
            Log("Joined Game, reset death count to " + dcount);
        }

        //XmlElement here

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


            protected override Composite CreateBehavior()
            {
                return new Zeta.TreeSharp.Action((ret) =>
                {
                    string lastp = GlobalSettings.Instance.LastProfile;
                    string ppath = Path.GetDirectoryName(lastp);
                    string nxtp = ppath + "\\" + ProfileName;
                    if (ProfileName != null)
                    {
                        if (ExitGame != null)
                        {
                            Log("Been asked to load a new profile, which is " + ProfileName);
                            ProfileManager.Load(nxtp);
                            BotMain.PauseWhile(() => ZetaDia.IsInGame || ZetaDia.IsLoadingWorld, 8);
                            ZetaDia.Service.Games.LeaveGame();  
                        }
                        else
                        {
                            Log("Been asked to load a new profile, which is " + ProfileName);
                            dcount = 0;
                            Log("Reset death count to " + dcount);
                            ProfileManager.Load(nxtp);
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