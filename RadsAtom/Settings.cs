using System;
using System.IO;
using RadsAtom.Functions;
using Zeta;
using Zeta.CommonBot;

namespace RadsAtom
{
    class Settings
    {
        public static bool resetcount = false;
        public static double deathtrip = 2;
        public static double deathtrip2 = 3;
        public static string BackupProfile = "";
        public static string KeyrunProfile = "";
        public static string settingsFile = "";
        public static bool ingamecheck = true;
        private static bool SavingSettings = false;
        public static bool UseRelogger = false;
        public static bool UseSecurityRandomizer = true;
        public static string BNetUser = "";
        public static string BNetPass = "";
        public static string NextProfile = "";
        public static bool EnableAggregator = false;
        public static bool AlreadyHandledDeathPortal = true;
        public static bool WasVendoringAfterDeath = false;
        public static int Inactrip = 0;
        public static volatile bool ResetBreakTimer = true;
        public static DateTime ThisTime = DateTime.Now;
        public static bool UseBreak = false;
        public static int MinBreak = 120;
        public static int MaxBreak = 240;
        public static int ThisBreak;
        public static int BreakTimeMin = 5;
        public static int BreakTimeMax = 30;
        public static int BreakTime;



        // Death Statement Tag
        public static bool DSinuse = false;
        public static string DSBackupProfile = "";
        public static int DSdeathtrip = 0;
        public static string DSaction = "";
        // Death Statement Tag

        public static void AtomOnGameJoined(object src, EventArgs mea)
        {
        }

        public static void AtomOnGameLeft(object src, EventArgs mea)
        {
        }

        public static void SaveSettings()
        {
            if (SavingSettings) return;
            SavingSettings = true;
            FileStream settingsStream = File.Open(settingsFile, FileMode.Create, FileAccess.Write, FileShare.Read);
            using (StreamWriter settingsWriter = new StreamWriter(settingsStream))
            {
                settingsWriter.WriteLine("Deathtrip " + deathtrip.ToString());
                settingsWriter.WriteLine("Deathtrip2 " + deathtrip2.ToString());
                settingsWriter.WriteLine("UseRelogger " + UseRelogger.ToString());
                settingsWriter.WriteLine("BNetUser " + BNetUser);
                settingsWriter.WriteLine("BNetPass " + BNetPass);
                settingsWriter.WriteLine("UseSecurityRandomizer " + UseSecurityRandomizer.ToString());
                settingsWriter.WriteLine("InactivityTime " + Inactrip.ToString());
                settingsWriter.WriteLine("UseBreak " + UseBreak.ToString());
                settingsWriter.WriteLine("BreakDurationMin " + BreakTimeMin.ToString());
                settingsWriter.WriteLine("BreakDurationMax " + BreakTimeMax.ToString());
                settingsWriter.WriteLine("UntilBreakMin " + MinBreak.ToString());
                settingsWriter.WriteLine("UntilBreakMax " + MaxBreak.ToString());
            }
            settingsStream.Close();
            SavingSettings = false;
        }

        public static void LoadSettings()
        {
            if (!File.Exists(settingsFile))
            {
                Logger.Log("Creating new settings file.");
                SaveSettings();
                return;
            }
            using (StreamReader settingsReader = new StreamReader(settingsFile))
            {
                while (!settingsReader.EndOfStream)
                {
                    string[] settings = settingsReader.ReadLine().Split(' ');
                    if (settings != null)
                    {
                        switch (settings[0])
                        {
                            case "Deathtrip":
                                deathtrip = Convert.ToDouble(settings[1]);
                                break;
                            case "Deathtrip2":
                                deathtrip2 = Convert.ToDouble(settings[1]);
                                break;
                            case "UseRelogger":
                                UseRelogger = Convert.ToBoolean(settings[1]);
                                break;
                            case "BNetUser":
                                BNetUser = Convert.ToString(settings[1]);
                                break;
                            case "BNetPass":
                                BNetPass = Convert.ToString(settings[1]);
                                break;
                            case "UseSecurityRandomizer":
                                UseSecurityRandomizer = Convert.ToBoolean(settings[1]);
                                break;
                            case "InactivityTime":
                                Inactrip = Convert.ToInt32(settings[1]);
                                break;
                            case "UseBreak":
                                UseBreak = Convert.ToBoolean(settings[1]);
                                break;
                            case "BreakDurationMin":
                                BreakTimeMin = Convert.ToInt32(settings[1]);
                                break;
                            case "BreakDurationMax":
                                BreakTimeMax = Convert.ToInt32(settings[1]);
                                break;
                            case "UntilBreakMin":
                                MinBreak = Convert.ToInt32(settings[1]);
                                break;
                            case "UntilBreakMax":
                                MaxBreak = Convert.ToInt32(settings[1]);
                                break;
                        }
                    }
                }
            settingsReader.Close();
            }
        }
    }
}
