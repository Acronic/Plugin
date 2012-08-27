using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Zeta;
using Zeta.CommonBot;
using Zeta.CommonBot.Profile;
using Zeta.CommonBot.Settings;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace RadsProfileManager
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

        [XmlAttribute("profileB")]
        public string ProfileNameB { get; set; }

        protected override Composite CreateBehavior()
        {
            return new Zeta.TreeSharp.Action((ret) =>
            {
                if (RadsProfileManager.bRandomProfile)
                {
                    if (ProfileName != null)
                    {
                        if (ProfileNameB != null)
                        {
                            string lastp = GlobalSettings.Instance.LastProfile;
                            string ppath = Path.GetDirectoryName(lastp);
                            string nxtp = ppath + "\\" + ProfileName;
                            string nxtpB = ppath + "\\" + ProfileNameB;
                            Random random = new Random(int.Parse(Guid.NewGuid().ToString().Substring(0, 8), System.Globalization.NumberStyles.HexNumber));
                            int ircount = random.Next(1, 10);
                            if (ircount < 5)
                            {
                                RadsProfileManager.Log("Been asked to load a new profile, which is " + ProfileName);
                                ProfileManager.Load(nxtp);
                                RadsProfileManager.dcount = 0;
                                RadsProfileManager.Log("Reset death count to " + RadsProfileManager.dcount + ".");
                                Thread.Sleep(1000);
                            }
                            else if (ircount >= 5)
                            {
                                RadsProfileManager.Log("Been asked to load a new profile, which is " + ProfileNameB);
                                ProfileManager.Load(nxtpB);
                                RadsProfileManager.dcount = 0;
                                RadsProfileManager.Log("Reset death count to " + RadsProfileManager.dcount + ".");
                                Thread.Sleep(1000);
                            }
                            else
                            {
                                RadsProfileManager.Log("MEGA ERROR, RADONIC YOU SUCK!");
                            }
                            m_IsDone = true;
                        }
                        else
                        {
                            string lastp = GlobalSettings.Instance.LastProfile;
                            string ppath = Path.GetDirectoryName(lastp);
                            string nxtp = ppath + "\\" + ProfileName;
                            if (ProfileName != null)
                            {
                                RadsProfileManager.Log("Been asked to load a new profile, which is " + ProfileName);
                                ProfileManager.Load(nxtp);
                                RadsProfileManager.dcount = 0;
                                RadsProfileManager.Log("Reset death count to " + RadsProfileManager.dcount + ".");
                                Thread.Sleep(1000);
                            }
                        }
                    }
                    else
                    {
                        RadsProfileManager.Log("DEBUG: No next profile selected in the profile, stopping the bot.");
                        BotMain.Stop();
                    }
                }
                else
                {
                    string lastp = GlobalSettings.Instance.LastProfile;
                    string ppath = Path.GetDirectoryName(lastp);
                    string nxtp = ppath + "\\" + ProfileName;
                    if (ProfileName != null)
                    {
                        RadsProfileManager.Log("Been asked to load a new profile, which is " + ProfileName);
                        ProfileManager.Load(nxtp);
                        RadsProfileManager.dcount = 0;
                        RadsProfileManager.Log("Reset death count to " + RadsProfileManager.dcount + ".");
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        RadsProfileManager.Log("DEBUG: No next profile selected in the profile, stopping the bot.");
                        BotMain.Stop();
                    }
                    m_IsDone = true;
                }
            });
        }
        public override void ResetCachedDone()
        {
            m_IsDone = false;
            base.ResetCachedDone();
        }
    }
}
