using System;
using System.Threading;
using Zeta;
using Zeta.Internals;

namespace RadsAtom.Functions
{
    class PortalStoneHelper
    {
        private static DateTime lastlooked = DateTime.Now;

        public static void PortalStone()
        {
            if (DateTime.Now.Subtract(lastlooked).TotalSeconds > 3)
            {
                if (ZetaDia.Me != null)
                {
                    if (ZetaDia.IsPlayingCutscene)
                    {
                        ZetaDia.Me.SkipCutscene();
                        Logger.Log("Canceling cutscene.");
                    }
                }

                UIElement warning = UIElement.FromHash(0xF9E7B8A635A4F725);
                if (warning.IsValid && warning.IsVisible)
                {
                    UIElement button = UIElement.FromHash(0x891D21408238D18E);
                    if (button.IsValid && button.IsVisible && button.IsEnabled)
                    {
                        Logger.Log("Clicking OK.");
                        button.Click();
                        Thread.Sleep(3000);
                    }
                }
            }
        }
    }
}
