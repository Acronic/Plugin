using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Zeta;
using Zeta.CommonBot;

namespace RadsAtom.Functions
{
    public static class Mrworker
    {
        // making this thread start the bot, load the profile and leave the game.
        public static void MrThread()
        {
            Logger.Log("MrWorker thread is starting.");
            while (!isClosed)
            {
                try
                {
                    // Set the breaktimer!
                    if (Settings.ResetBreakTimer && Settings.UseBreak && !ZetaDia.IsInGame && !ZetaDia.IsLoadingWorld)
                    {
                        Random random = new Random(int.Parse(Guid.NewGuid().ToString().Substring(0, 8), System.Globalization.NumberStyles.HexNumber));

                        Settings.ThisTime = DateTime.Now;

                        Settings.ThisBreak = random.Next(Settings.MinBreak, Settings.MaxBreak);
                        Settings.ThisBreak = Settings.ThisBreak * 60;

                        Settings.BreakTime = random.Next(Settings.BreakTimeMin, Settings.BreakTimeMax);
                        Settings.BreakTime = Settings.BreakTime * 60000;

                        Settings.ResetBreakTimer = false;
                        BreakSettingsSet = true;

                        Logger.Log(string.Format("{0} minutes to next break, the break will last for {1} minutes.", Settings.ThisBreak / 60, Settings.BreakTime / 60000));
                    }

                    // Trip the breaktimer!
                    if (BreakSettingsSet && !ZetaDia.IsInGame && !ZetaDia.IsLoadingWorld && DateTime.Now.Subtract(Settings.ThisTime).TotalSeconds > Settings.ThisBreak)
                    {
                        TakeABreakNOW = true;
                    }

                    // Start the bot after relogging!
                    if (RelogRestart)
                    {
                        RelogRestart = false;
                        BotMain.Stop();
                        Thread.Sleep(2000);
                        ProfileManager.Load(Settings.BackupProfile);
                        Thread.Sleep(2000);
                        BotMain.Start();
                    }
                    
                    // Execute XML stuff.
                    if (XMLdone)
                    {
                        Thread.Sleep(100);
                        if (LoadProfile)
                        {
                            LoadProfile = false;
                            ProfileManager.Load(MrProfile);
                        }

                        if (MrLeaver)
                        {
                            MrLeaver = false;
                            Thread.Sleep(1000);
                            BotMain.Stop();
                            Thread.Sleep(2000);
                            ZetaDia.Service.Games.LeaveGame();
                            Thread.Sleep(2000);
                            BotMain.Start();
                        }

                        XMLdone = false;
                    }

                    // Execute stuff from other stuff :D
                    if (IsExecute)
                    {
                        if (LoadProfile)
                        {
                            LoadProfile = false;
                            ProfileManager.Load(MrProfile);
                        }

                        if (MrLeaver)
                        {
                            MrLeaver = false;
                            Thread.Sleep(1000);
                            BotMain.Stop();
                            Thread.Sleep(2000);
                            ZetaDia.Service.Games.LeaveGame();
                            Thread.Sleep(2000);
                            BotMain.Start();
                        }

                        IsExecute = false;
                    }

                    // Take the break!
                    if (TakeABreakNOW)
                    {
                        TakeABreakNOW = false;
                        BotMain.Stop();
                        Logger.Log(string.Format("Will Stop the bot for {0} minutes.", Settings.BreakTime / 60000));
                        Thread.Sleep(Settings.BreakTime);
                        BotMain.Start();
                        Settings.ResetBreakTimer = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString());
                }
                Thread.Sleep(1000);
            }
            Logger.Log("MrWorker thead is Closing.");
        }

        private static bool isClosed
        {
            get
            {
                IntPtr h = Process.GetCurrentProcess().MainWindowHandle;
                return ((h == IntPtr.Zero || h == null));
            }
        }

        // Our bool
        public static volatile bool RelogRestart = false;
        public static volatile bool XMLdone = false;
        public static volatile bool LoadProfile = false;
        public static volatile bool MrLeaver = false;
        public static volatile bool IsExecute = false;
        public static volatile bool TakeABreakNOW = false;
        public static volatile bool BreakSettingsSet = false;


        public static string MrProfile = "";
        
    }
}
