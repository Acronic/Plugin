using System;
using System.IO;
using System.Threading;
using System.Xml;
using Zeta;
using Zeta.CommonBot;
using Zeta.CommonBot.Settings;

namespace RadsAtom.Functions
{
    class Death
    {
        public static int Count = 0;
        private static bool ownCount = false;
        public static DateTime whileDeadWait = DateTime.Today;
        public static DateTime lastCount = DateTime.Today;
        private static string isexitgame = "";
        private static string newprofiles = "";
        private static string newprofile = "";


        // Death reset event
        public static void DeathReset()
        {
            Settings.resetcount = true;
            if (Settings.resetcount)
            {
                Count = 0;
                if (Count == 0)
                {
                    Logger.Log("Resetting the deathcounter.");
                    Settings.resetcount = false;
                }
            }
        }


        // Count even, waiting for DB to return ownCount as True
        // It will then add counter and execute DeathEvent to reload profile
        public static void DeathCount()
        {
            if (ownCount && DateTime.Now.Subtract(lastCount).TotalSeconds > 5+Count)
            {
                ownCount = false;
                Count++;
                lastCount = DateTime.Now;
                Logger.Log("Released at: " + whileDeadWait);
                Logger.Log("Deathcount at: " + Count);
                if (Settings.DSinuse)
                {
                    DeathStatementFunc();
                }
                else
                {
                    DeathEvent();
                }
            }
        }

        private static void DeathEvent()
        {
            string lastprofile = GlobalSettings.Instance.LastProfile;
            if (Count < Settings.deathtrip)
            {
                Mrworker.MrProfile = lastprofile;
                Mrworker.LoadProfile = true;
                Logger.Log("Reload current profile.");
            }
            else if (Count < Settings.deathtrip2)
            {
                StreamReader streamReader = new StreamReader(lastprofile);
                XmlTextReader xmlTextReader = new XmlTextReader(streamReader);
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(xmlTextReader);
                XmlNode conNode = xmldoc.SelectSingleNode("Profile/Order/Continue");
                if (conNode != null && conNode.Attributes != null)
                {
                    var exitvar = conNode.Attributes.GetNamedItem("exitgame");
                    if (exitvar != null)
                    {
                        isexitgame = exitvar.Value;
                    }
                    newprofiles = conNode.Attributes.GetNamedItem("profile").Value;
                    string profilepath = Path.GetDirectoryName(lastprofile);
                    char[] delimiters = new char[] { ',', ' ' };
                    string[] pArray = newprofiles.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    int i = pArray.Length;
                    Random random = new Random(int.Parse(Guid.NewGuid().ToString().Substring(0, 8), System.Globalization.NumberStyles.HexNumber));
                    int count = random.Next(0, i);
                    if (Settings.UseSecurityRandomizer)
                    {
                        newprofile = pArray[count];
                    }
                    else
                    {
                        newprofile = pArray[0];
                    }
                    string profile = profilepath + "\\" + newprofile;
                    if (isexitgame == "")
                    {
                        Mrworker.MrProfile = profile;
                        Mrworker.LoadProfile = true;
                        Logger.Log("Loading next profile, died too much: " + profile);
                    }
                    else
                    {
                        Mrworker.MrProfile = Settings.BackupProfile;
                        Mrworker.LoadProfile = true;
                        Mrworker.MrLeaver = true;
                        Logger.Log("Leave game, last profile");
                        DeathReset();
                        isexitgame = "";
                    }
                }
            }
            else
            {
                Mrworker.MrProfile = Settings.BackupProfile;
                Mrworker.LoadProfile = true;
                Mrworker.MrLeaver = true;
                DeathReset();
                Logger.Log("Leave game, died too much.");
            }
            Mrworker.IsExecute = true;
        }


        // DBs OnDeath event
        public static void AtomOnDeath(object sender, EventArgs e)
        {
            whileDeadWait = DateTime.Now;
            ownCount = true;
        }

        public static void DeathStatementFunc()
        {
            if (Settings.DSinuse)
            {
                if (Count > Settings.DSdeathtrip)
                {
                    string lastprofile = GlobalSettings.Instance.LastProfile;
                    string path = Path.GetDirectoryName(lastprofile);
                    string DSprofile = path + Settings.DSBackupProfile;
                    DeathReset();
                    Settings.DSinuse = false;
                    if (Settings.DSaction == "nextprofile")
                    {
                        Mrworker.MrProfile = DSprofile;
                        Mrworker.LoadProfile = true;
                        Logger.Log("Loading next profile, died too much: " + DSprofile);
                        Mrworker.IsExecute = true;
                    }
                    else if (Settings.DSaction == "leave")
                    {
                        Mrworker.MrProfile = DSprofile;
                        Mrworker.LoadProfile = true;
                        Mrworker.MrLeaver = true;
                        Logger.Log("Leave game, died too much.");
                        Mrworker.IsExecute = true;
                        if (!ZetaDia.Me.IsInTown)
                        {
                            Thread.Sleep(10000);
                        }
                    }
                    else
                    {
                        Settings.DSinuse = false;
                    }
                }
            }
            else
            {
                DeathEvent();
            }
        }


    }
}
