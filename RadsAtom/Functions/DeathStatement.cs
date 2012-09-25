using Zeta.CommonBot.Profile;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace RadsAtom.Functions
{
    [XmlElement("DeathStatement")]
    public class DeathStatement : ProfileBehavior
    {
        private bool m_IsDone = false;
        public override bool IsDone
        {
            get { return m_IsDone; }
        }

        [XmlAttribute("action")]
        private string action { get; set; }

        [XmlAttribute("amount")]
        private string amount { get; set; }

        [XmlAttribute("profile")]
        private string profile { get; set; }

        protected override Composite CreateBehavior()
        {
            return new Zeta.TreeSharp.Action(ret =>
                                                 {
                                                     Settings.DSinuse = true;
                                                     Settings.DSaction = action;
                                                     Settings.DSdeathtrip = int.Parse(amount);
                                                     Settings.DSBackupProfile = profile;
                                                     Logger.Log(string.Format("Death Statement activated."));
                                                     Logger.Log(string.Format("Statement settings are:"));
                                                     Logger.Log(string.Format("Action: {0}", Settings.DSaction));
                                                     Logger.Log(string.Format("Ammount: {0}", Settings.DSdeathtrip));
                                                     Logger.Log(string.Format("Profile: {0}", Settings.DSBackupProfile));
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
