using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zeta.Common;
using Zeta.CommonBot.Profile;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace RadsAtom.Functions
{
    class Logger
    {
        public static void Log(string msg)
        {
            Logging.Write("[{0}] - {1}", RadsAtom.PName, msg);
        }

        public static void ProfileLog(string msg)
        {
            Logging.Write("[{0}] - {1}", "Profile", msg);
        }
    }

    [XmlElement("Logmsg")]
    public class Logmsg : ProfileBehavior
    {
        private bool m_IsDone = false;
        public override bool IsDone
        {
            get { return m_IsDone; }
        }

        [XmlAttribute("message")]
        public string Message { get; set; }

        protected override Composite CreateBehavior()
        {
            return new Zeta.TreeSharp.Action(ret =>
            {
                Logger.ProfileLog(string.Format("{0}", Message));
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
