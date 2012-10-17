using System.IO;
using Zeta.CommonBot.Profile;
using Zeta.CommonBot.Settings;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace RadsAtom.Functions
{
    [XmlElement("Keyrun")]
    public class Keyrun : ProfileBehavior
    {
        private bool m_IsDone = false;
        public override bool IsDone
        {
            get { return m_IsDone; }
        }

        [XmlAttribute("profile")]
        public string ProfileName { get; set; }

        protected override Composite CreateBehavior()
        {
            return new Zeta.TreeSharp.Action(ret =>
                                                 {
                                                     string p = Path.GetDirectoryName(GlobalSettings.Instance.LastProfile);
                                                     Settings.KeyrunProfile = p + "\\" + ProfileName;
                                                     Mrworker.KeyrunSwitch = true;
                                                     Logger.Log("Keyrun enabled. The keyrun profile is: "+ ProfileName);
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