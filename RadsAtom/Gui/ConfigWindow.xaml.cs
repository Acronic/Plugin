using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RadsAtom.Functions;
using Zeta.Common;
using Zeta.Common.Plugins;

namespace RadsAtom.Gui
{
    public class ConfigWindow
    {


        #region singleton
        static readonly ConfigWindow instance=new ConfigWindow();

        static ConfigWindow()
        {
        }

        ConfigWindow()
        {
        }

        public static ConfigWindow Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion

        Authenticator newAuth;
        public static string pluginPath = "";
        private const string xamlFile = "Gui\\ConfigWindow.xaml";
        private const string enrollFile = "Gui\\EnrollWindow.xaml";
        private const string restoreFile = "Gui\\RestoreWindow.xaml";

        #region MainConfigWindow

        private Window confwindow;
        public Window configWindow
        {
            get
            {
                confwindow = new Window();
                CreateWindow();
                return confwindow;
            }
        }



        // ---------------------------------------
        // --------------- Tools -----------------
        //----------------------------------------
        private Thread threadWorker;
        private CheckBox checkBox_enablerelogger, checkBox_securityrandomization;
        private TextBox textBox_bnusername, textBox_bnpassword, textBox_serial, textBox_code, textBox_nextprofile, textBox_leavegame, textBox_inactivtytime;
        private ProgressBar progressBar1;
        private Button button_enroll, button_restore;


        public void CreateWindow()
        {
            StreamReader xamlSteam = new StreamReader(pluginPath + xamlFile);
            DependencyObject xamlContent = XamlReader.Load(xamlSteam.BaseStream) as DependencyObject;
            UserControl mainControl = LogicalTreeHelper.FindLogicalNode(xamlContent, "mainControl") as UserControl;
            confwindow.Width = mainControl.Width + 30;
            confwindow.Height = mainControl.Height + 30;
            confwindow.Title = "RadsAtom Settings";
            confwindow.Content = xamlContent;
            confwindow.Closed += ConfigWindow_Closed;
            confwindow.Loaded += ConfigWindow_Loaded;

            // ProgressBar
            progressBar1 = LogicalTreeHelper.FindLogicalNode(xamlContent, "progressBar1") as ProgressBar;
            progressBar1.Maximum = 30;


            // CheckBoxes
            checkBox_enablerelogger = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkBox_enablerelogger") as CheckBox;
            checkBox_enablerelogger.Checked += checkBox_enablerelogger_check;
            checkBox_enablerelogger.Unchecked += checkBox_enablerelogger_uncheck;
            checkBox_securityrandomization = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkBox_securityrandomization") as CheckBox;
            checkBox_securityrandomization.Checked += checkBox_securityrandomization_Checked;
            checkBox_securityrandomization.Unchecked += checkBox_securityrandomization_Unchecked;


            // TextBoxes
            textBox_bnusername = LogicalTreeHelper.FindLogicalNode(xamlContent, "textBox_bnusername") as TextBox;
            textBox_serial = LogicalTreeHelper.FindLogicalNode(xamlContent, "textBox_serial") as TextBox;
            textBox_code = LogicalTreeHelper.FindLogicalNode(xamlContent, "textBox_code") as TextBox;
            textBox_bnpassword = LogicalTreeHelper.FindLogicalNode(xamlContent, "textBox_bnpassword") as TextBox;
            textBox_nextprofile = LogicalTreeHelper.FindLogicalNode(xamlContent, "textBox_nextprofile") as TextBox;
            textBox_leavegame = LogicalTreeHelper.FindLogicalNode(xamlContent, "textBox_leavegame") as TextBox;
            textBox_inactivtytime = LogicalTreeHelper.FindLogicalNode(xamlContent, "textBox_inactivtytime") as TextBox;


            // Button
            button_enroll = LogicalTreeHelper.FindLogicalNode(xamlContent, "button_enroll") as Button;
            button_enroll.Click += button_enroll_Click;
            button_restore = LogicalTreeHelper.FindLogicalNode(xamlContent, "button_restore") as Button;
            button_restore.Click += button_restore_Click;


        }

        void checkBox_securityrandomization_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.UseSecurityRandomizer = false;
        }

        void checkBox_securityrandomization_Checked(object sender, RoutedEventArgs e)
        {
            Settings.UseSecurityRandomizer = true;
        }

        private void button_enroll_Click(object sender, RoutedEventArgs e)
        {
            showEnroll();
        }

        private void button_restore_Click(object sender, RoutedEventArgs e)
        {
            showRestore();
        }

        private void checkBox_enablerelogger_check(object sender, RoutedEventArgs e)
        {
            Settings.UseRelogger = true;
        }

        private void checkBox_enablerelogger_uncheck(object sender, RoutedEventArgs e)
        {
            Settings.UseRelogger = false;
        }

        private void ConfigWindow_Loaded(object sender, EventArgs e)
        {
            
            checkBox_enablerelogger.IsChecked = Settings.UseRelogger;
            checkBox_securityrandomization.IsChecked = Settings.UseSecurityRandomizer;
            textBox_bnusername.Text = Settings.BNetUser;
            textBox_bnpassword.Text = Settings.BNetPass;
            textBox_nextprofile.Text = Settings.deathtrip.ToString();
            textBox_leavegame.Text = Settings.deathtrip2.ToString();
            textBox_inactivtytime.Text = Settings.Inactrip.ToString();
            threadWorker = new Thread(new ThreadStart(Worker));
            threadWorker.SetApartmentState(ApartmentState.STA);
            threadWorker.Start();

        }

        private void ConfigWindow_Closed(object sender, EventArgs e)
        {
            string np = textBox_nextprofile.Text;
            string lg = textBox_leavegame.Text;
            string it = textBox_inactivtytime.Text;
            Settings.deathtrip = int.Parse(np);
            Settings.deathtrip2 = int.Parse(lg);
            Settings.Inactrip = int.Parse(it);
            Settings.BNetUser = textBox_bnusername.Text;
            Settings.BNetPass = textBox_bnpassword.Text;
            AuthenticatorSettings.Instance.BnetUsername = Settings.BNetUser;
            AuthenticatorSettings.Instance.BnetPassword = Settings.BNetPass;
            AuthenticatorSettings.Instance.Save();
            Settings.SaveSettings();

            try
            {
                WorkStop();
            }
            catch{ }
        }

        private volatile bool work = true;

        public void Worker()
        {
            while (work)
            {
                try
                {
                    if (RadsAtom.Auth != null && RadsAtom.Auth.isValid)
                    {
                        confwindow.Dispatcher.Invoke(
                             new Action(
                                delegate()
                                {
                                    textBox_serial.Text = RadsAtom.Auth.Serial;
                                    textBox_code.Text = RadsAtom.Auth.CurrentCode;
                                    progressBar1.Value = (30 - RadsAtom.Auth.timeLeft);
                                }));
                    }
                }
                catch (Exception ex)
                {
                    Logging.Write(ex.ToString());
                }

                Thread.Sleep(1000);
            }
            work = true;
            Logger.Log("Enroll Thread stopped!");
        }

        public void WorkStop()
        {
            work = false;
        }

        #endregion

        #region EnrollWindow


        // ---------------------------------------
        // --------------- Tools -----------------
        //----------------------------------------
        private TextBox textBox_enrollserial, textBox_enrollcode, textBox_enrollrestore;
        private Window enrollwindow;
        private void showEnroll()
        {
            enrollwindow= new Window();
            StreamReader xamlSteam = new StreamReader(pluginPath + enrollFile);
            DependencyObject xamlContent = XamlReader.Load(xamlSteam.BaseStream) as DependencyObject;
            UserControl mainControl = LogicalTreeHelper.FindLogicalNode(xamlContent, "mainControl") as UserControl;
            enrollwindow.Content = xamlContent;
            enrollwindow.Title = "Enroll new Authenticator";
            enrollwindow.Height = mainControl.Height + 30;
            enrollwindow.Width = mainControl.Width + 30;
            enrollwindow.Closed += enrollwindow_Closed;
            enrollwindow.Loaded += enrollwindow_Loaded;


            // TextBox
            textBox_enrollserial = LogicalTreeHelper.FindLogicalNode(xamlContent, "textBox_enrollserial") as TextBox;
            textBox_enrollcode = LogicalTreeHelper.FindLogicalNode(xamlContent, "textBox_enrollcode") as TextBox;
            textBox_enrollrestore = LogicalTreeHelper.FindLogicalNode(xamlContent, "textBox_enrollrestore") as TextBox;



            enrollwindow.Show();
        }

        void enrollwindow_Loaded(object sender, RoutedEventArgs e)
        {
            newAuth = new Authenticator(AuthenticatorSettings.Instance.AuthenticatorAssembly);

            textBox_enrollserial.Text = textBox_enrollcode.Text = textBox_enrollrestore.Text = "Loading ...";
            newAuth.Enroll();
            textBox_enrollserial.Text = newAuth.Serial;
            textBox_enrollcode.Text = newAuth.CurrentCode;
            textBox_enrollrestore.Text = newAuth.RestoreCode;
        }

        void enrollwindow_Closed(object sender, EventArgs e)
        {
            RadsAtom.Auth = newAuth;
            AuthenticatorSettings.Instance.Serial = newAuth.Serial;
            AuthenticatorSettings.Instance.SecretKey = Authenticator.ByteArrayToString(newAuth.SecretKey);
            AuthenticatorSettings.Instance.Save();
        }

        #endregion

        #region RestoreWindow


        // ---------------------------------------
        // --------------- Tools -----------------
        //----------------------------------------
        private TextBox textBox_restoreserial, textBox_restorecode;
        private Button button_restorerestore;
        private Window restoreWindow;
        private void showRestore()
        {
            restoreWindow = new Window();
            StreamReader xamlStream = new StreamReader(pluginPath + restoreFile);
            DependencyObject xamlContent = XamlReader.Load(xamlStream.BaseStream) as DependencyObject;
            UserControl mainControl = LogicalTreeHelper.FindLogicalNode(xamlContent, "mainControl") as UserControl;
            restoreWindow.Content = xamlContent;
            restoreWindow.Height = mainControl.Height + 30;
            restoreWindow.Width = mainControl.Width + 30;
            restoreWindow.Title = "Restore Authenticator";

            // TextBox
            textBox_restoreserial = LogicalTreeHelper.FindLogicalNode(xamlContent, "textBox_restoreserial") as TextBox;
            textBox_restorecode = LogicalTreeHelper.FindLogicalNode(xamlContent, "textBox_restorecode") as TextBox;

            // Button
            button_restorerestore = LogicalTreeHelper.FindLogicalNode(xamlContent, "button_restorerestore") as Button;
            button_restorerestore.Click += button_restorerestore_Click;



            restoreWindow.Show();
        }

        void button_restorerestore_Click(object sender, RoutedEventArgs e)
        {
            string serial = textBox_restoreserial.Text.Replace("-", "");
            string code = textBox_restorecode.Text;
            if (serial.Length == 14 && code.Length == 10)
            {
                try
                {

                    newAuth = new Authenticator(AuthenticatorSettings.Instance.AuthenticatorAssembly);
                    newAuth.Restore(serial, code);
                    RadsAtom.Auth = newAuth;
                    AuthenticatorSettings.Instance.Serial = newAuth.Serial;
                    AuthenticatorSettings.Instance.SecretKey = Authenticator.ByteArrayToString(newAuth.SecretKey);
                    AuthenticatorSettings.Instance.Save();
                    restoreWindow.Close();
                }
                catch
                {
                    MessageBox.Show("Failed to restore authenticator", "Restore Failed!", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
                MessageBox.Show("Incorrect restore information given", "Restore Failed!", MessageBoxButton.OK, MessageBoxImage.Warning);
        }


        #endregion


    }
}
