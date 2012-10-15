using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using RadsAtom.Functions;
using RadsAtom.Gui;
using Zeta;
using Zeta.Common;
using Zeta.Common.Plugins;
using System.Windows;
using Zeta.CommonBot;
using UIElement = Zeta.Internals.UIElement;

namespace RadsAtom
{
    public class RadsAtom : IPlugin
    {

        #region IPluginInfo

        // IPlugin information
        public Version Version
        {
            get { return new Version(1, 4, 2); }
        }

        public string Author
        {
            get { return "Radonic"; }
        }

        public string Description
        {
            get { return "RadsAtom the core of profile management."; }
        }

        public string Name
        {
            get { return "RadsAtom"; }
        }

        public bool Equals(IPlugin other)
        {
            return (other.Name == Name) && (other.Version == Version);
        }

        public Thread threadMrworker;
        public Thread threadInactivty;
        public const string PName = "RadsAtom";
        public static string DBpath = AppDomain.CurrentDomain.BaseDirectory;

        #endregion

        #region ConfigWindow

        // Settings window
        public Window DisplayWindow
        {
            get 
            {
                return ConfigWindow.Instance.configWindow;
            }
        }

        #endregion

        #region IPluginEvents

        public void OnDisabled()
        {
            // Disable all DB Events
            GameEvents.OnPlayerDied -= Death.AtomOnDeath;
            GameEvents.OnGameJoined -= Settings.AtomOnGameJoined;
            GameEvents.OnGameLeft -= Settings.AtomOnGameLeft;
        }

        

        public void OnEnabled()
        {
            // Enable all DB Events + Loading settings
            GameEvents.OnPlayerDied += Death.AtomOnDeath;
            GameEvents.OnGameJoined += Settings.AtomOnGameJoined;
            GameEvents.OnGameLeft += Settings.AtomOnGameLeft;
            Settings.LoadSettings();


            if (Settings.UseRelogger)
            {
                // Load Auth
                Auth = new Authenticator(AuthenticatorSettings.Instance.AuthenticatorAssembly);
                Auth.Serial = (AuthenticatorSettings.Instance.Serial != null ? AuthenticatorSettings.Instance.Serial : null);
                Auth.SecretKey = (AuthenticatorSettings.Instance.SecretKey != null ? Authenticator.StringToByteArray(AuthenticatorSettings.Instance.SecretKey) : null);

                // Start Thread
                threadWorker = new Thread(new ThreadStart(Worker));
                threadWorker.IsBackground = true;
                threadWorker.Start(); 
            }

            threadInactivty = new Thread(new ThreadStart(Inactivitytimer.InactivityThread));
            threadInactivty.IsBackground = true;
            threadInactivty.Start();

            threadMrworker = new Thread(new ThreadStart(Mrworker.MrThread));
            threadMrworker.IsBackground = true;
            threadMrworker.Start();
        }

        public void OnInitialize()
        {
            // Creating all paths needed
            string SettingsDirectory = Path.Combine(DBpath, "Settings");
            ConfigWindow.pluginPath = DBpath + @"Plugins\RadsAtom\";
            Settings.settingsFile = Path.Combine(Path.Combine(SettingsDirectory, "RadsAtom"), "RadsAtom.cfg");
            if (File.Exists(Path.Combine(DBpath, "Input.txt")))
            {
                Settings.EnableAggregator = true;
                File.Create(Path.Combine(DBpath, "Output.txt"));
            }
            if (!File.Exists(AuthenticatorSettings.Instance.AuthenticatorAssembly))
            {
                try
                {
                    string winauthpath = ConfigWindow.pluginPath + @"WinAuth\WinAuth.exe";
                    AuthenticatorSettings.Instance.AuthenticatorAssembly = winauthpath;
                    AuthenticatorSettings.Instance.Save();
                }
                catch (Exception ex)
                {
                    Logger.LogDiag(ex.Message);
                }
            }
        }

        public void OnShutdown()
        {
        }

        #endregion

        #region OnPulse

        public void OnPulse()
        {
            // Check if ingame and check if its not in a Loadingscreen.
            if (ZetaDia.IsInGame && !ZetaDia.IsLoadingWorld)
            {
                Death.DeathCount();
                AntiTownStuck.TownStuck();
                PortalStoneHelper.PortalStone();
            }
        }

        #endregion

        #region Relogger

        public static Authenticator Auth;
        private UIElement AuthTextbox, AuthButton, LoginUsername, LoginPassword, LoginButton;
        private Thread threadWorker;
        private bool StartProfile = false;

        public void Worker()
        {
            Logger.Log("Relogger thread is starting.");
            bool isSynced = false;
            while (!isClosing)
            {
                try
                {
                    if (Auth != null && Auth.isValid)
                    {
                        // Check for error 3 (login information incorrect...)
                        if (ErrorDialog.IsVisible)
                        {
                            if (ErrorDialog.ErrorCode == 3)
                            {
                                Logger.Log("Login information incorrect");
                                isSynced = false;
                            }
                            Logger.Log("Closing error dialog: " + ErrorDialog.ErrorCode);
                            Thread.Sleep(1000);
                            ErrorDialog.Click();
                        }

                        if (!isSynced)
                        {
                            isSynced = true;
                            Logger.Log("Syncing..");
                            Auth.Sync();
                            Logger.Log("Synced: offset = " + TimeSpan.FromTicks(Auth.ServerTimeDiff).TotalSeconds);
                        }

                        // Check if we are logging in
                        while (isLogging)
                        {
                            Logger.Log("Waiting to get logged in");
                            Thread.Sleep(1000);
                        }

                        // Check if we need to login
                        if (shouldLogin)
                        {
                            // We are at login screen 
                            string bnetaccount = CommandLine.Arguments.Single("bnetaccount");
                            string bnetpassword = CommandLine.Arguments.Single("bnetpassword");
                            if ((!string.IsNullOrEmpty(bnetaccount) && bnetaccount != "") &&
                                (!string.IsNullOrEmpty(bnetpassword) && bnetpassword != ""))
                            {
                                Logger.Log("Using Bnet Account + Password from commandline");
                                LoginUsername.SetText(bnetaccount);
                                LoginPassword.SetText(bnetpassword);
                                Thread.Sleep(1000);
                                LoginButton.Click();
                            }
                            else if ((!string.IsNullOrEmpty(AuthenticatorSettings.Instance.BnetPassword) &&
                                      AuthenticatorSettings.Instance.BnetUsername != "") &&
                                     (!string.IsNullOrEmpty(AuthenticatorSettings.Instance.BnetPassword) &&
                                      AuthenticatorSettings.Instance.BnetPassword != ""))
                            {
                                Logger.Log("Using Bnet Account + Password from Settings");
                                LoginUsername.SetText(AuthenticatorSettings.Instance.BnetUsername);
                                LoginPassword.SetText(AuthenticatorSettings.Instance.BnetPassword);
                                Thread.Sleep(1000);
                                LoginButton.Click();
                            }
                            StartProfile = true;
                            Thread.Sleep(1000);
                            continue;
                        }

                        // Check if we need to authenticate
                        if (shouldAuthenticate)
                        {
                            Logger.Log("Authenticate");
                            if (Auth.timeLeft < 2)
                            {
                                Logger.Log("Only 2 seconds left on current authentication code");
                                Logger.Log("Waiting 4 seconds to use the next code");
                                Thread.Sleep(4000);
                            }
                            AuthTextbox.SetText(Auth.CurrentCode);
                            Thread.Sleep(100);
                            AuthButton.Click();
                        }

                        // Check if we need to start the profile after we logged in.
                        if (StartProfile)
                        {
                            Mrworker.RelogRestart = true;
                            StartProfile = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString());
                }
                Thread.Sleep(1000);
            }
            Logger.Log("Thread shutdown");
        }

        private bool shouldLogin
        {
            get
            {
                try
                {
                    // Diablo 3: login
                    // username = 0xDE8625FCCFFDFC28,
                    // password = 0xBA2D3316B4BB4104,
                    // loginbutton = 0x50893593B5DB22A9,
                    if (UIElement.IsValidElement(0xDE8625FCCFFDFC28) && UIElement.IsValidElement(0xBA2D3316B4BB4104) &&
                        UIElement.IsValidElement(0x50893593B5DB22A9))
                    {
                        LoginUsername = UIElement.FromHash(0xDE8625FCCFFDFC28);
                        LoginPassword = UIElement.FromHash(0xBA2D3316B4BB4104);
                        LoginButton = UIElement.FromHash(0x50893593B5DB22A9);

                        if ((LoginUsername.IsValid && LoginUsername.IsVisible && LoginUsername.IsEnabled) &&
                            (LoginPassword.IsValid && LoginPassword.IsVisible && LoginPassword.IsEnabled) &&
                            (LoginButton.IsValid && LoginButton.IsVisible))
                            return true;
                    }
                }
                catch
                {
                }
                return false;
            }
        }

        private bool shouldAuthenticate
        {
            get
            {
                try
                {

                    // Diablo 3: Authenticate
                    // Textbox: 0x94D9581BE416FCD6
                    // Button: 0xC0D8651455FC1DA2
                    if (UIElement.IsValidElement(0x94D9581BE416FCD6) && UIElement.IsValidElement(0xC0D8651455FC1DA2))
                    {
                        AuthTextbox = UIElement.FromHash(0x94D9581BE416FCD6);
                        AuthButton = UIElement.FromHash(0xC0D8651455FC1DA2);
                    }
                    if ((AuthTextbox.IsValid && AuthTextbox.IsEnabled && AuthTextbox.IsVisible) &&
                        (AuthButton.IsVisible && AuthButton.IsVisible))
                        return true;
                }
                catch
                {
                }
                return false;
            }
        }

        private bool isLogging
        {
            get
            {
                try
                {
                    // Diablo 3 Login popup
                    // Box: 0xCBDF9A8EEE61B0C0
                    // Cancel: 0xFCC3DCA83337A824
                    if (UIElement.IsValidElement(0xCBDF9A8EEE61B0C0) && UIElement.IsValidElement(0xFCC3DCA83337A824))
                    {
                        UIElement Box = UIElement.FromHash(0xCBDF9A8EEE61B0C0);
                        UIElement Cancel = UIElement.FromHash(0xFCC3DCA83337A824);

                        if ((Box.IsValid && Box.IsVisible) &&
                            (Cancel.IsValid && Cancel.IsVisible))
                            return true;
                    }
                }
                catch
                {
                }
                return false;
            }
        }

        public bool isClosing
        {
            get
            {
                IntPtr h = Process.GetCurrentProcess().MainWindowHandle;
                return ((h == IntPtr.Zero || h == null));
            }
        }

        #endregion

        #region RnR Aggregator


        #endregion
    }
}