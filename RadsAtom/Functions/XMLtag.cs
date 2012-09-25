using System;
using System.IO;
using System.Threading;
using Zeta;
using Zeta.Common;
using Zeta.CommonBot;
using Zeta.CommonBot.Profile;
using Zeta.CommonBot.Settings;
using Zeta.Internals.Actors;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace RadsAtom.Functions
{
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

        [XmlAttribute("backup")]
        public string BackUpProfile { get; set; }

        public bool BExitGame = false;
        private string newprofile = "";

        protected override Composite CreateBehavior()
        {
            return new Zeta.TreeSharp.Action(ret =>
                                                 {
                                                     if (ExitGame != null)
                                                     {
                                                         if (ExitGame == "true")
                                                         {
                                                             BExitGame = true;
                                                         }
                                                         else
                                                         {
                                                             BExitGame = false;
                                                         }
                                                     }
                                                     if (BackUpProfile != null)
                                                     {
                                                         string lastprofile = GlobalSettings.Instance.LastProfile;
                                                         string profilepath = Path.GetDirectoryName(lastprofile);
                                                         Settings.BackupProfile = profilepath + "\\" + BackUpProfile;
                                                     }
                                                     if (ProfileName != null)
                                                     {
                                                         char[] delimiters = new char[] { ',', ' ' };
                                                         string[] pArray = ProfileName.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
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
                                                         string lastprofile = GlobalSettings.Instance.LastProfile;
                                                         string profilepath = Path.GetDirectoryName(lastprofile);
                                                         string profile = profilepath + "\\" + newprofile;
                                                         Logger.Log("Loading next profile: " + profile);
                                                         ProfileManager.Load(profile);
                                                         Thread.Sleep(100);
                                                         Settings.DSinuse = false;
                                                         Death.DeathReset();
                                                         if (BExitGame)
                                                         {
                                                             if (!ZetaDia.Me.IsInTown)
                                                             {
                                                                 ZetaDia.Me.UsePower(SNOPower.UseStoneOfRecall, Vector3.Zero, ZetaDia.Me.WorldDynamicId, -1);
                                                                 Thread.Sleep(3000);
                                                             }
                                                             BExitGame = false;
                                                             Logger.Log("Leaving the game.");
                                                             Thread.Sleep(1000);
                                                             ZetaDia.Service.Games.LeaveGame();
                                                         }
                                                     }
                                                     else
                                                     {
                                                         Logger.Log("Cant find next profile, stopping the bot");
                                                         BotMain.Stop();
                                                     }
                                                     m_IsDone = true;
                                                 });
        }

        public override void ResetCachedDone()
        {
            m_IsDone = false;
            base.ResetCachedDone();
        }
    }
}
