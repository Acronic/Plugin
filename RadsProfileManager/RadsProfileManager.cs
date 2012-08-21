using System;
using System.IO;
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
using Zeta.Internals.Actors;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using UIElement = Zeta.Internals.UIElement;


namespace RadsProfileManager
{
    public class RadsProfileManager : IPlugin
    {
        // Stuff Db needs to make this a plugin
        public Version Version { get { return new Version(0, 6, 2); } }
        public string Author { get { return "Radonic"; } }
        public string Description { get { return "Restarting Radonics leveling profiles. Clicking OK on different buttons. Still in beta."; } }
        public string Name { get { return "RadsProfileManager beta"; } }
        public bool Equals(IPlugin other) { return (other.Name == Name) && (other.Version == Version); }
        public Window DisplayWindow
        {
            get
            {
                return null;
            }
        }
        // Custom logger to stamp logs with plugin name & version first
        public static void Log(string message)
        {
            Logging.Write(string.Format("[{0}] {1}", iName, message));
        }

        // All my variables used throughout the plugin
        private static DateTime _lastLooked = DateTime.Now;
        private const string iName = "RadsProfileManager beta";
        private static int dcount = 0;
        private const int dtrip = 2;
        private const int dtripnxt = 3;
        private static string startpname = "";
        private static string joinpname = "";
        private static string databaseConnStr = "";
        private static DateTime lastDiedTime = DateTime.Today;
        private static bool bAlreadyHandledDeathPortal = true;
        private static bool bWasVendoringAfterDeath = false;

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
            // Check we don't spam onpulse too often - once every 5 seconds is enough
            if (DateTime.Now.Subtract(_lastLooked).TotalSeconds > 5)
            {
                _lastLooked = DateTime.Now;
                // Call the OKClicker
                OkClicker();
                // Check for death handling and town portalling
                if (!bAlreadyHandledDeathPortal && DateTime.Now.Subtract(lastDiedTime).TotalSeconds > 8)
                {
                    // Flag up if we were in the middle of vendoring
                    if (Zeta.CommonBot.Logic.BrainBehavior.IsVendoring)
                    {
                        bWasVendoringAfterDeath = true;
                    }
                    else if (bWasVendoringAfterDeath)
                    {
                        // If we reach here, then we WERE vendoring, but AREN'T vendoring any more
                        if (!ZetaDia.Me.IsInTown)
                        {
                            ZetaDia.Me.UsePower(SNOPower.UseStoneOfRecall, Vector3.Zero, ZetaDia.Me.WorldDynamicId, -1);
                        }
                        string lunstuckp = GlobalSettings.Instance.LastProfile;
                        ProfileManager.Load(lunstuckp);
                        bAlreadyHandledDeathPortal = true;
                        bWasVendoringAfterDeath = false;
                        Thread.Sleep(3000);
                    }
                    // Safety cancel this check after 2 minutes after death "Just incase"
                    if (DateTime.Now.Subtract(lastDiedTime).TotalSeconds > 120)
                    {
                        bAlreadyHandledDeathPortal = true;
                        bWasVendoringAfterDeath = false;
                    }
                }
            }
        }


        void IPlugin.OnShutdown()
        {
        }

        //Functions here

        public static void OkClicker()
        {
            UIElement warning = UIElement.FromHash(0xF9E7B8A635A4F725);
            if (warning.IsValid && warning.IsVisible)
            {
                UIElement button = UIElement.FromHash(0x891D21408238D18E);
                if (button.IsValid && button.IsVisible && button.IsEnabled)
                {
                    Log("Clicking OK.");
                    button.Click();
                    Thread.Sleep(3000);
                }
            }
        }

        static void RadsHandleBotStop(IBot bot)
        {
            startpname = "";
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
            // First of all check that DB isn't spamming the death event incase it causes bugs
            if (DateTime.Now.Subtract(lastDiedTime).TotalSeconds <= 4)
                return;
            // Now record this time of death
            lastDiedTime = DateTime.Now;
            bWasVendoringAfterDeath = false;
            bAlreadyHandledDeathPortal = false;
            // Now do stuff based on death counts
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
                    if (databaseConnStr == joinpname)
                    {
                        ProfileManager.Load(joinpname);
                        Thread.Sleep(1000);
                        ZetaDia.Service.Games.LeaveGame();
                    }
                    else
                    {
                        ProfileManager.Load(nxtp);
                    }
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
            joinpname = Path.GetFileName(GlobalSettings.Instance.LastProfile);
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
                        Log("Been asked to load a new profile, which is " + ProfileName);
                        ProfileManager.Load(nxtp);
                        dcount = 0;
                        Log("Reset death count to " + dcount + ".");
                        if (ProfileName == joinpname)
                        {
                            ZetaDia.Service.Games.LeaveGame();
                            Log("Run is over, leaving game.");
                        }
                        Thread.Sleep(1000);
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