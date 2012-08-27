using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using Zeta;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.CommonBot;
using Zeta.CommonBot.Profile;
using Zeta.CommonBot.Settings;
using Zeta.Internals;
using Zeta.Internals.Actors;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using UIElement = Zeta.Internals.UIElement;


namespace RadsProfileManager
{
    public class RadsProfileManager : IPlugin
    {
        // Stuff Db needs to make this a plugin
        public Version Version { get { return new Version(0, 9, 0); } }
        public string Author { get { return "Radonic"; } }
        public string Description { get { return "Restarting Radonics leveling profiles. Clicking OK on different buttons. Still in beta."; } }
        public string Name { get { return "RadsProfileManager beta"; } }
        public bool Equals(IPlugin other) { return (other.Name == Name) && (other.Version == Version); }

        // Config window
        #region configWindow

        private Button saveButton, defaultButton;
        private CheckBox checkRandomProfile, checkOKClicker;
        private Slider slideNP, slideLO;
        private TextBox npCount, loCount;

        private Window configWindow = null;
        public Window DisplayWindow
        {
            get
            {
                if (!File.Exists(pluginPath + "RadsProfileManager.xaml"))
                {
                    Log("Error: Can't find \"" + pluginPath + "RadsProfileManager.xaml\"");
                }
                try
                {
                    if (configWindow == null)
                    {
                        configWindow = new Window();
                    }
                    StreamReader xamlStream = new StreamReader(pluginPath + "RadsProfileManager.xaml");
                    DependencyObject xamlContent =
                        System.Windows.Markup.XamlReader.Load(xamlStream.BaseStream) as DependencyObject;
                    configWindow.Content = xamlContent;

                    // CheckBoxes
                    checkRandomProfile = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkRandomProfile") as CheckBox;
                    checkRandomProfile.Checked += new RoutedEventHandler(checkRandomProfile_check);
                    checkRandomProfile.Unchecked += new RoutedEventHandler(checkRandomProfile_uncheck);

                    checkOKClicker = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkOKClicker") as CheckBox;
                    checkOKClicker.Checked += new RoutedEventHandler(checkOKClicker_check);
                    checkOKClicker.Unchecked += new RoutedEventHandler(checkOKClicker_uncheck);

                    //Buttons
                    defaultButton = LogicalTreeHelper.FindLogicalNode(xamlContent, "buttonDefaults") as Button;
                    defaultButton.Click += new RoutedEventHandler(buttonDefaults_Click);
                    saveButton = LogicalTreeHelper.FindLogicalNode(xamlContent, "buttonSave") as Button;
                    saveButton.Click += new RoutedEventHandler(buttonSave_Click);

                    //Sliders
                    slideNP = LogicalTreeHelper.FindLogicalNode(xamlContent, "slideNP") as Slider;
                    slideNP.ValueChanged += new RoutedPropertyChangedEventHandler<double>(trackNextProfile_Scroll);
                    slideNP.SmallChange = 1;
                    slideNP.LargeChange = 1;
                    slideNP.TickFrequency = 1;
                    slideNP.IsSnapToTickEnabled = true;

                    slideLO = LogicalTreeHelper.FindLogicalNode(xamlContent, "slideLO") as Slider;
                    slideLO.ValueChanged += new RoutedPropertyChangedEventHandler<double>(trackLogout_Scroll);
                    slideLO.SmallChange = 1;
                    slideLO.LargeChange = 1;
                    slideLO.TickFrequency = 1;
                    slideLO.IsSnapToTickEnabled = true;

                    npCount = LogicalTreeHelper.FindLogicalNode(xamlContent, "npCount") as TextBox;
                    loCount = LogicalTreeHelper.FindLogicalNode(xamlContent, "loCount") as TextBox;

                    //Other stuff
                    UserControl mainControl =
                        LogicalTreeHelper.FindLogicalNode(xamlContent, "mainControl") as UserControl;
                    configWindow.Height = mainControl.Height + 30;
                    configWindow.Width = mainControl.Width;
                    configWindow.Title = "RadsProfileManager";

                    configWindow.Loaded += new RoutedEventHandler(configWindow_Loaded);
                    configWindow.Closed += configWindow_Closed;

                    configWindow.Content = xamlContent;
                }
                catch (System.Windows.Markup.XamlParseException ex)
                {
                    Log(ex.ToString());
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                }
                return configWindow;
            }
        }
        private void buttonDefaults_Click(object sender, RoutedEventArgs e)
        {
            bRandomProfile = true;
            checkRandomProfile.IsChecked = true;
            bOKClicker = false;
            checkOKClicker.IsChecked = false;
            slideNP.Value = 2;
            npCount.Text = "2";
            dtrip = 2;
            slideLO.Value = 3;
            loCount.Text = "3";
            dtripnxt = 3;

        }
        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveConfiguration();
            configWindow.Close();
        }
        private void configWindow_Loaded(object sender, EventArgs e)
        {
            checkRandomProfile.IsChecked = bRandomProfile;
            checkOKClicker.IsChecked = bOKClicker;
            slideNP.Value = dtrip;
            npCount.Text = dtrip.ToString();
            slideLO.Value = dtripnxt;
            loCount.Text = dtripnxt.ToString();
        }
        private void configWindow_Closed(object sender, EventArgs e)
        {
            configWindow = null;
        }
        private void checkRandomProfile_check(object sender, RoutedEventArgs e)
        {
            bRandomProfile = true;
        }
        private void checkRandomProfile_uncheck(object sender, RoutedEventArgs e)
        {
            bRandomProfile = false;
        }
        private void checkOKClicker_check(object sender, RoutedEventArgs e)
        {
            bOKClicker = true;
        }
        private void checkOKClicker_uncheck(object sender, RoutedEventArgs e)
        {
            bOKClicker = false;
        }
        private void trackNextProfile_Scroll(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            slideNP.Value = Math.Round(slideNP.Value);
            npCount.Text = slideNP.Value.ToString();
            dtrip = Math.Round(slideNP.Value);
        }
        private void trackLogout_Scroll(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            slideLO.Value = Math.Round(slideLO.Value);
            loCount.Text = slideLO.Value.ToString();
            dtripnxt = Math.Round(slideLO.Value);
        }
        #endregion
        // End of Config window

        // Custom logger to stamp logs with plugin name & version first
        public static void Log(string message)
        {
            Logging.Write(string.Format("[{0}] {1}", sName, message));
        }

        // All my variables used throughout the plugin
        private static DateTime lastlooked = DateTime.Now;
        private const string sName = "RadsProfileManager beta";
        public static int dcount = 0;
        public static double dtrip = 2;
        public static double dtripnxt = 3;
        public static string startpname = "";
        public static string leavepname = "";
        private static string databaseConnStr = "";
        private static DateTime lastDiedTime = DateTime.Today;
        private static bool bAlreadyHandledDeathPortal = true;
        private static bool bWasVendoringAfterDeath = false;
        public static bool bRandomProfile = true;
        public static bool bOKClicker = false;
        public static string pluginPath = "";
        public static string sConfigFile = "";
        private bool bSavingConfig = false;

        // If they enable the plugin
        void IPlugin.OnEnabled()
        {
            GameEvents.OnGameJoined += RadsOnGameJoined;
            GameEvents.OnGameLeft += RadsOnGameLeft;
            GameEvents.OnPlayerDied += RadsOnPlayerDied;
            BotMain.OnStop += RadsHandleBotStop;
            BotMain.OnStart += RadsHandleBotStart;
            Log("Enabled.");
            if (!Directory.Exists(pluginPath))
            {
                Log("WARNING! WARNING. INVALID PLUGIN PATH: " + pluginPath);
                Log("PLEASE CHECK THE PLUGIN PATH, AND THEN RESTART DEMONBUDDY.");
            }
            else
            {
                LoadConfiguration();
                Log("Config is now loaded.");
            }
        }

        // If they disable the plugin
        void IPlugin.OnDisabled()
        {
            GameEvents.OnGameJoined -= RadsOnGameJoined;
            GameEvents.OnGameLeft -= RadsOnGameLeft;
            GameEvents.OnPlayerDied -= RadsOnPlayerDied;
            BotMain.OnStop -= RadsHandleBotStop;
            BotMain.OnStart -= RadsHandleBotStart;
            Log("Disabled.");
        }


        void IPlugin.OnInitialize()
        {
            pluginPath = AppDomain.CurrentDomain.BaseDirectory + @"\Plugins\RadsProfileManager\";
            sConfigFile = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\Settings\RadsProfileManager.cfg";
        }

        void IPlugin.OnPulse()
        {
            // Check we don't spam onpulse too often - once every 5 seconds is enough
            if (DateTime.Now.Subtract(lastlooked).TotalSeconds > 5)
            {
                lastlooked = DateTime.Now;
                // Call the OKClicker
                if (bOKClicker)
                {
                    OkClicker();
                }
                // Check for death handling and town portalling
                if (!bAlreadyHandledDeathPortal && DateTime.Now.Subtract(lastDiedTime).TotalSeconds > 8)
                {
                    // Flag up if we were in the middle of vendoring
                    if (Zeta.CommonBot.Logic.BrainBehavior.IsVendoring)
                    {
                        bWasVendoringAfterDeath = true;
                    }
                    else if (bWasVendoringAfterDeath)
                    {
                        // If we reach here, then we WERE vendoring, but AREN'T vendoring any more
                        if (!ZetaDia.Me.IsInTown)
                        {
                            ZetaDia.Me.UsePower(SNOPower.UseStoneOfRecall, Vector3.Zero, ZetaDia.Me.WorldDynamicId, -1);
                        }
                        string lunstuckp = GlobalSettings.Instance.LastProfile;
                        ProfileManager.Load(lunstuckp);
                        bAlreadyHandledDeathPortal = true;
                        bWasVendoringAfterDeath = false;
                        Thread.Sleep(3000);
                    }
                    // Safety cancel this check after 2 minutes after death "Just incase"
                    if (DateTime.Now.Subtract(lastDiedTime).TotalSeconds > 120)
                    {
                        bAlreadyHandledDeathPortal = true;
                        bWasVendoringAfterDeath = false;
                    }
                }
            }
        }


        void IPlugin.OnShutdown()
        {
        }

        //Functions here

        private void LoadConfiguration()
        {
            if (!File.Exists(sConfigFile))
            {
                Log("No config file found, creating new" + sConfigFile);
                SaveConfiguration();
                return;
            }
            using (StreamReader configReader = new StreamReader(sConfigFile))
            {
                while (!configReader.EndOfStream)
                {
                    string[] config = configReader.ReadLine().Split('=');
                    if (config != null)
                    {
                        switch (config[0])
                        {
                            case "RandomProfile":
                                bRandomProfile = Convert.ToBoolean(config[1]);
                                break;
                            case "OKClicker":
                                bOKClicker = Convert.ToBoolean(config[1]);
                                break;
                            case "Tripwire":
                                dtrip = Convert.ToDouble(config[1]);
                                break;
                            case "TripwireLogout":
                                dtripnxt = Convert.ToDouble(config[1]);
                                break;
                        }
                    }
                }
            configReader.Close();
            }
        }

        private void SaveConfiguration()
        {
            if (bSavingConfig) return;
            bSavingConfig = true;
            FileStream configStream = File.Open(sConfigFile, FileMode.Create, FileAccess.Write, FileShare.Read);
            using (StreamWriter configWriter = new StreamWriter(configStream))
            {
                configWriter.WriteLine("RandomProfile=" + bRandomProfile.ToString());
                configWriter.WriteLine("OKClicker=" + bOKClicker.ToString());
                configWriter.WriteLine("Tripwire=" + dtrip.ToString());
                configWriter.WriteLine("TripwireLogout=" + dtripnxt.ToString());
            }
            configStream.Close();
            bSavingConfig = false;
        }

        public static void OkClicker()
        {
            UIElement warning = UIElement.FromHash(0xF9E7B8A635A4F725);
            if (warning.IsValid && warning.IsVisible)
            {
                UIElement button = UIElement.FromHash(0x891D21408238D18E);
                if (button.IsValid && button.IsVisible && button.IsEnabled)
                {
                    Log("Clicking OK.");
                    button.Click();
                    Thread.Sleep(3000);
                }
            }
        }

        static void RadsHandleBotStop(IBot bot)
        {
            startpname = "";
            string lastpname = Path.GetFileName(GlobalSettings.Instance.LastProfile);
            Log("Last profile used was " + lastpname + ".");
        }

        public static void RadsHandleBotStart(IBot bot)
        {
            startpname = GlobalSettings.Instance.LastProfile;
            leavepname = Path.GetFileName(startpname);
            Log("Starter profile is " + leavepname + ".");
        }

        static void RadsOnGameLeft(object sender, EventArgs e)
        {
            ProfileManager.Load(startpname);
        }

        static void RadsOnPlayerDied(object sender, EventArgs e)
        {
            // First of all check that DB isn't spamming the death event incase it causes bugs
            if (DateTime.Now.Subtract(lastDiedTime).TotalSeconds <= 4)
                return;
            // Now record this time of death
            lastDiedTime = DateTime.Now;
            bWasVendoringAfterDeath = false;
            bAlreadyHandledDeathPortal = false;
            // Now do stuff based on death counts
            string lastp = GlobalSettings.Instance.LastProfile;
            dcount = dcount + 1;
            if (dcount < dtrip)
            {
                Log("You died, reload profile so you can try again.");
                Log("Deathcount: " + dcount);
                ProfileManager.Load(lastp);
                Thread.Sleep(1000);
            }
            else if (dcount < dtripnxt)
            {
                StreamReader stRd = new StreamReader(lastp);
                XmlTextReader xmlRd = new XmlTextReader(stRd);
                XmlDocument rdXml = new XmlDocument();
                rdXml.Load(xmlRd);
                XmlNode connNode = rdXml.SelectSingleNode("Profile/Order/Continue");
                if (connNode != null && connNode.Attributes != null)
                {
                    databaseConnStr = connNode.Attributes.GetNamedItem("profile").Value;
                    string ppath = Path.GetDirectoryName(lastp);
                    string nxtp = ppath + "\\" + databaseConnStr;
                    if (databaseConnStr == leavepname)
                    {
                        ProfileManager.Load(startpname);
                        Thread.Sleep(1000);
                        ZetaDia.Service.Games.LeaveGame();
                        if (! ZetaDia.Me.IsInTown)
                        {
                            Thread.Sleep(10000);
                        }
                    }
                    else
                    {
                        ProfileManager.Load(nxtp);
                    }
                    Thread.Sleep(1000);
                    Log("Died twice, will move on to next profile " + nxtp);
                    Log("Deathcount: " + dcount);
                }
            }
            else
            {
                Log("You died, reload profile so you can try again.");
                ProfileManager.Load(startpname);
                Thread.Sleep(1000);
                ZetaDia.Service.Games.LeaveGame();
                if (!ZetaDia.Me.IsInTown)
                {
                    Thread.Sleep(10000);
                }
                Thread.Sleep(1000);
            }
        }

        private static void RadsOnGameJoined(object src, EventArgs mea)
        {
            dcount = 0;
            Log("Joined Game, reset death count to " + dcount);
        }
    }
}