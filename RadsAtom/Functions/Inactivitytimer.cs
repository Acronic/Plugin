using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using Zeta;
using Zeta.Common;
using Zeta.CommonBot;
using Zeta.CommonBot.Logic;
using Zeta.CommonBot.Settings;
using Zeta.Internals;
using Zeta.Internals.Actors;


// Using some stuff from UnStuckMe
// Thanks Jorrit/Sinterlkaas

namespace RadsAtom.Functions
{
    class Inactivitytimer
    {
        private static DateTime lastlooked = DateTime.Now;
        private static DateTime lastcheck = DateTime.Now;
        private static Vector3 lastpos;
        private static bool Inactivty = false;

        public static bool isBusy()
        {
            try
            {
                if (UIElementTester.isValid(_UIElement.leavegame_cancel) || BrainBehavior.IsVendoring || ZetaDia.IsPlayingCutscene)
                    return true;

                if ((ZetaDia.Me != null && ZetaDia.Me.CommonData != null) && (
                    ZetaDia.Me.CommonData.AnimationState == AnimationState.Attacking ||
                    ZetaDia.Me.CommonData.AnimationState == AnimationState.Casting ||
                    ZetaDia.Me.CommonData.AnimationState == AnimationState.Channeling ||
                    ZetaDia.Me.CommonData.AnimationState == AnimationState.TakingDamage ||
                    ZetaDia.Me.IsDead))
                    return true;
            }
            catch (Exception ex)
            {
                Logger.LogDiag(ex.ToString());
            }
            return false;
        }

        public static void CheckMovement()
        {
            if (Settings.Inactrip > 0)
            {
                if (Inactivty)
                {
                    try
                    {
                        if (!ZetaDia.Me.IsInTown)
                        {
                            ZetaDia.Me.UseTownPortal();
                        }
                        string lastprofile = GlobalSettings.Instance.LastProfile;
                        string newprofiles = "";
                        string newprofile = "";
                        string isexitgame = "";
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
                                Inactivty = false;
                                Death.DeathReset();
                                ProfileManager.Load(profile);
                                Logger.Log("Loading next profile, stuck too long: " + profile);
                            }
                            else
                            {
                                Inactivty = false;
                                ProfileManager.Load(Settings.BackupProfile);
                                Logger.Log("Leave game, last profile, stuck too long");
                                Death.DeathReset();
                                Thread.Sleep(1000);
                                ZetaDia.Service.Games.LeaveGame();
                                if (!ZetaDia.Me.IsInTown)
                                    Thread.Sleep(10000);
                                Thread.Sleep(1000);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Inactivty = false;
                        Logger.LogDiag("Error: " + ex.ToString());
                    }
                }
                else
                {
                    if (!isBusy())
                    {
                        if (DateTime.Now.Subtract(lastlooked).TotalSeconds > 5)
                        {
                            lastlooked = DateTime.Now;
                            if (lastpos.Distance(ZetaDia.Me.Position) < 10)
                            {
                                int Inactivitytimer = Settings.Inactrip*60;
                                if (DateTime.Now.Subtract(lastcheck).TotalSeconds > Inactivitytimer)
                                {
                                    Inactivty = true;
                                    return;
                                }
                                return;
                            }
                            lastcheck = DateTime.Now;
                            lastpos = ZetaDia.Me.Position;
                        }
                    }
                }
            }
        }
    }

    #region ElementTester
    public static class _UIElement
    {

        public static ulong leavegame_cancel = 0x3B55BA1E41247F50,
        loginscreen_username = 0xDE8625FCCFFDFC28,
        loginscreen_password = 0xBA2D3316B4BB4104,
        loginscreen_loginbutton = 0x50893593B5DB22A9,
        startresume_button = 0x51A3923949DC80B7;
    }
    public static class UIElementTester
    {

        /// <summary>
        /// UIElement validation check
        /// </summary>
        /// <param name="hash">UIElement hash to check</param>
        /// <param name="isEnabled">should be enabled</param>
        /// <param name="isVisible">should be visible</param>
        /// <param name="isValid">should be a valid UIElement</param>
        /// <returns>true if all requirements are valid</returns>
        public static bool isValid(ulong hash, bool isEnabled = true, bool isVisible = true, bool bisValid = true)
        {
            try
            {
                if (!UIElement.IsValidElement(hash))
                    return false;
                else
                {
                    UIElement element = UIElement.FromHash(hash);

                    if ((isEnabled && !element.IsEnabled) || (!isEnabled && element.IsEnabled))
                        return false;
                    if ((isVisible && !element.IsVisible) || (!isVisible && element.IsVisible))
                        return false;
                    if ((bisValid && !element.IsValid) || (!bisValid && element.IsValid))
                        return false;

                }
            }
            catch (Exception ex)
            {
                Logger.LogDiag("Error: " + ex.ToString());
                return false;
            }
            return true;
        }
    }
    #endregion
}
