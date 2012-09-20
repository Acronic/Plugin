using System;
using System.Threading;
using Zeta;
using Zeta.Common;
using Zeta.CommonBot;
using Zeta.CommonBot.Settings;
using Zeta.Internals.Actors;

namespace RadsAtom.Functions
{
    class AntiTownStuck
    {
        private static DateTime lastlooked = DateTime.Now;

        public static void TownStuck()
        {
            if (DateTime.Now.Subtract(lastlooked).TotalSeconds > 5)
            {
                lastlooked = DateTime.Now;
                if (!Settings.AlreadyHandledDeathPortal && DateTime.Now.Subtract(Death.whileDeadWait).TotalSeconds > 8)
                {
                    if (Zeta.CommonBot.Logic.BrainBehavior.IsVendoring)
                    {
                        Settings.WasVendoringAfterDeath = true;
                    }
                    else if (Settings.WasVendoringAfterDeath)
                    {
                        if (!ZetaDia.Me.IsInTown)
                        {
                            ZetaDia.Me.UsePower(SNOPower.UseStoneOfRecall, Vector3.Zero, ZetaDia.Me.WorldDynamicId, -1);
                        }
                        string unstuckprofile = GlobalSettings.Instance.LastProfile;
                        ProfileManager.Load(unstuckprofile);
                        Settings.AlreadyHandledDeathPortal = true;
                        Settings.WasVendoringAfterDeath = false;
                        Thread.Sleep(3000);
                    }
                    // Safety cancel this check after 2 minutes after death "Just incase"
                    if (DateTime.Now.Subtract(Death.whileDeadWait).TotalSeconds > 120)
                    {
                        Settings.AlreadyHandledDeathPortal = true;
                        Settings.WasVendoringAfterDeath = false;
                    }
                }
            }
        }
    }
}
