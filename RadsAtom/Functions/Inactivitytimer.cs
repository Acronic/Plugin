using System;
using System.Diagnostics;
using System.IO;
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
    public static class Inactivitytimer
    {
        // Varibles
        private static DateTime lastlooked = DateTime.Now;
        private static DateTime lastcheck = DateTime.Now;
        private static Vector3 lastpos;
        private static volatile bool Inactivty = false;
        private static volatile bool TownInactivty = false;
        private static volatile bool Activated = false;
        private static volatile bool isRunning = false;

        // The inactivity thread, which is executing the check void.
        public static void InactivityThread()
        {
            Logger.Log("Inactivty thread is starting.");
            while (!isClosed)
            {
                try
                {
                    if (ZetaDia.Me != null)
                    {
                        CheckMovement();
                    }
                }
                catch(Exception ex)
                {
                    Logger.LogDiag(ex.ToString());
                }
                Thread.Sleep(1000);
            }
            Logger.Log("Inactivty thead is Closing.");
        }

        // Checking if DB is alive or not, making the thread safely close if it is not.
        private static bool isClosed
        {
            get
            {
                IntPtr h = Process.GetCurrentProcess().MainWindowHandle;
                return ((h == IntPtr.Zero || h == null));
            }
        }

        // Checking if different KEY ui elements is present, and if the char is dead or is fighting.
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

        // This is the big one.
        public static void CheckMovement()
        {
            // Only check if the setting is over 0
            if (Settings.Inactrip > 0 && BotMain.IsRunning && !BotMain.IsPaused && !BotMain.IsPausedForStateExecution)
            {
                // Resetting the position and datetime if we are not in a game, if we are loading the world or if we are playing a cutscene.
                if (!ZetaDia.IsInGame || ZetaDia.IsLoadingWorld || ZetaDia.IsPlayingCutscene)
                {
                    lastpos = Vector3.Zero;
                    lastcheck = DateTime.Now;
                }
                //This is if the bot is shown to be inactive. This will TP to town, load next profile and if its the last profile in the line of profiles. It will leave the game.
                else if (Inactivty && !Activated && !isRunning && ZetaDia.IsInGame && !ZetaDia.IsLoadingWorld)
                {
                    try
                    {
                        isRunning = true;
                        Activated = true;
                        if (!ZetaDia.Me.IsInTown)
                        {
                            ZetaDia.Me.UseTownPortal();
                            Thread.Sleep(5000);
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
                                Mrworker.MrProfile = profile;
                                Mrworker.LoadProfile = true;
                                Logger.Log("Loading next profile, stuck too long: " + profile);
                            }
                            else
                            {
                                Inactivty = false;
                                Mrworker.MrProfile = Settings.BackupProfile;
                                Mrworker.LoadProfile = true;
                                Mrworker.MrLeaver = true;
                                Logger.Log("Leave game, last profile, stuck too long");
                                Death.DeathReset();
                            }
                            Mrworker.IsExecute = true;
                            isRunning = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        isRunning = false;
                        Inactivty = false;
                        Logger.LogDiag("Error: " + ex.ToString());
                    }
                }
                // This is the same as above, but if we are inactive in town. Then it will just reload the profile. In a hope to recover.
                else if (TownInactivty && !Activated && !isRunning && ZetaDia.IsInGame && !ZetaDia.IsLoadingWorld)
                {
                    try
                    {
                        isRunning = true;
                        Activated = true;
                        string lastprofile = GlobalSettings.Instance.LastProfile;
                        TownInactivty = false;
                        Death.DeathReset();
                        Mrworker.MrProfile = lastprofile;
                        Mrworker.LoadProfile = true;
                        Logger.Log("Reloading profile, stuck in town too long: " + lastprofile);
                        Mrworker.IsExecute = true;
                        isRunning = false;
                    }
                    catch (Exception ex)
                    {
                        isRunning = false;
                        TownInactivty = false;
                        Logger.LogDiag("Error: " + ex);
                    }
                }
                // This is if we are in town, it will check you pos and the time to gather info about if we want to trip the activator.
                else if (ZetaDia.IsInGame && !ZetaDia.IsLoadingWorld && ZetaDia.Me.IsInTown)
                {
                    if (ZetaDia.Me.IsInTown)
                    {
                        if(!isBusy())
                        {
                            if (lastpos.Distance(ZetaDia.Me.Position) < 10 && !Activated)
                            {
                                int i;
                                if (Settings.Inactrip > 1)
                                {
                                    i = Settings.Inactrip / 2;
                                }
                                else
                                {
                                    i = Settings.Inactrip;
                                }
                                int Inactivitytimer = i * 60;
                                if (DateTime.Now.Subtract(lastcheck).TotalSeconds > Inactivitytimer)
                                {
                                    TownInactivty = true;
                                    return;
                                }
                                return;
                            }
                            lastcheck = DateTime.Now;
                            lastpos = ZetaDia.Me.Position;
                            Activated = false;
                        }
                        else
                        {
                            lastpos = Vector3.Zero;
                            lastcheck = DateTime.Now;
                        }
                    }
                }
                // This is if we are in out in the wild and discovering! This will check if we are moving or not, and by how much.
                else if (ZetaDia.IsInGame && !ZetaDia.IsLoadingWorld && ZetaDia.Me != null)
                {
                    if (ZetaDia.IsInGame && !ZetaDia.IsLoadingWorld)
                    {
                        if (!isBusy() && !isRunning && DateTime.Now.Subtract(lastlooked).TotalSeconds > 5)
                        {
                            lastlooked = DateTime.Now;
                            if (lastpos.Distance(ZetaDia.Me.Position) < 10 && !Activated)
                            {
                                int Inactivitytimer = Settings.Inactrip * 60;
                                if (DateTime.Now.Subtract(lastcheck).TotalSeconds > Inactivitytimer)
                                {
                                    Inactivty = true;
                                    return;
                                }
                                return;
                            }
                            lastcheck = DateTime.Now;
                            lastpos = ZetaDia.Me.Position;
                            Activated = false;
                        } 
                    }
                }
            }
        }
    }

    // Sinterlkaas ElementTester
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
