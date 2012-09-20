using System.ComponentModel;
using System.IO;
using Zeta.Common.Xml;
using Zeta.XmlEngine;

namespace RadsAtom.Functions
{
    [XmlElement("AuthenticatorSettings")]
    public sealed class AuthenticatorSettings : XmlSettings
    {
        #region singleton
        static readonly AuthenticatorSettings instance = new AuthenticatorSettings();

        static AuthenticatorSettings()
        {
        }

        AuthenticatorSettings() :
            base(Path.Combine(Path.Combine(SettingsDirectory, "RadsAtom"), "AuthenticatorSettings.xml"))
        {

        }

        public static AuthenticatorSettings Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion

        [XmlElement("AuthenticatorAssembly")]
        [DefaultValue(@"\Plugins\RadsAtom\Authenticator.dll")]
        public string AuthenticatorAssembly { get; set; }

        [XmlElement("Serial")]
        public string Serial { get; set; }
        [XmlElement("SecretKey")]
        public string SecretKey { get; set; }

        [XmlElement("BnetUsername")]
        public string BnetUsername { get; set; }
        [XmlElement("BnetPassword")]
        public string BnetPassword { get; set; }
    }
}
