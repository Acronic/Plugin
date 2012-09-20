using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;


// Thanks Jorrit/sinterklaas
// All credit to him for this part of the plugin
// I only made it work with RadsAtom

namespace RadsAtom.Functions
{
    public class Authenticator : ICloneable
    {
        private Type typeAuthenticator;
        private object _authenticator;

        private PropertyInfo _secretkey;
        private PropertyInfo _serial;
        private PropertyInfo _servertimediff;
        private PropertyInfo _servertime;
        private PropertyInfo _codeinterval;
        private PropertyInfo _currentcode;
        private PropertyInfo _restorecode;

        public Authenticator(string filename)
        {
            Assembly asm = Assembly.LoadFile(filename);
            typeAuthenticator = asm.GetType("WindowsAuthenticator.Authenticator");

            _authenticator = Activator.CreateInstance(typeAuthenticator);
            _secretkey = typeAuthenticator.GetProperty("SecretKey");
            _serial = typeAuthenticator.GetProperty("Serial");
            _servertimediff = typeAuthenticator.GetProperty("ServerTimeDiff");
            _servertime = typeAuthenticator.GetProperty("ServerTime");
            _codeinterval = typeAuthenticator.GetProperty("CodeInterval");
            _currentcode = typeAuthenticator.GetProperty("CurrentCode");
            _restorecode = typeAuthenticator.GetProperty("RestoreCode");
        }

        public bool isValid
        {
            get
            {
                return (SecretKey != null && Serial != null);
            }
        }

        /// <summary>
        /// Secret key used for Authenticator
        /// </summary>
        public byte[] SecretKey
        {
            get
            {
                return _secretkey.GetValue(_authenticator, null) as byte[];
            }
            set
            {
                _secretkey.SetValue(_authenticator, value, null);
            }
        }

        /// <summary>
        /// Serial number of authenticator
        /// </summary>
        public string Serial
        {
            get
            {
                return _serial.GetValue(_authenticator, null) as string;
            }
            set
            {
                _serial.SetValue(_authenticator, value, null);
            }
        }

        /// <summary>
        /// Time difference in milliseconds of our machine and server
        /// </summary>
        public long ServerTimeDiff
        {
            get
            {
                return (long)_servertimediff.GetValue(_authenticator, null);
            }
            set
            {
                _servertimediff.SetValue(_authenticator, value, null);
            }
        }
        /// <summary>
        /// Get the server time since 1/1/70
        /// </summary>
        public long ServerTime
        {
            get
            {
                return (long)_servertime.GetValue(_authenticator, null);
            }
            set
            {
                _servertime.SetValue(_authenticator, value, null);
            }
        }
        /// <summary>
        /// Calculate the code interval based on the calculated server time
        /// </summary>
        public long CodeInterval
        {
            get
            {
                return (long)_codeinterval.GetValue(_authenticator, null);
            }
        }
        /// <summary>
        /// Get the current code for the authenticator.
        /// </summary>
        /// <returns>authenticator code</returns>
        public string CurrentCode
        {
            get
            {
                return _currentcode.GetValue(_authenticator, null) as string;
            }
        }

        /// <summary>
        /// Synchorise this authenticator's time with server time. We update our data record with the difference from our UTC time.
        /// </summary>
        public void Sync()
        {
            typeAuthenticator.InvokeMember("Sync", BindingFlags.InvokeMethod, null, _authenticator, null);
        }

        /// <summary>
        /// Restore an authenticator from the serial number and restore code.
        /// </summary>
        /// <param name="serial">serial code, e.g. US-1234-5678-1234</param>
        /// <param name="restoreCode">restore code given on enroll, 10 chars.</param>
        public void Restore(string serial, string restoreCode)
        {
            object[] param = new object[] { serial, restoreCode };
            typeAuthenticator.InvokeMember("Restore", BindingFlags.InvokeMethod, null, _authenticator, param);
        }


        /// <summary>
        /// Convert a hex string into a byte array. E.g. "001f406a" -> byte[] {0x00, 0x1f, 0x40, 0x6a}
        /// </summary>
        /// <param name="hex">hex string to convert</param>
        /// <returns>byte[] of hex string</returns>
        public static byte[] StringToByteArray(string hex)
        {
            int len = hex.Length;
            byte[] bytes = new byte[len / 2];
            for (int i = 0; i < len; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        /// <summary>
        /// Convert a byte array into a ascii hex string, e.g. byte[]{0x00,0x1f,0x40,ox6a} -> "001f406a"
        /// </summary>
        /// <param name="bytes">byte array to convert</param>
        /// <returns>string version of byte array</returns>
        public static string ByteArrayToString(byte[] bytes)
        {
            // Use BitConverter, but it sticks dashes in the string
            return BitConverter.ToString(bytes).Replace("-", string.Empty);
        }

        /// <summary>
        /// Enroll this authenticator with the server.
        /// </summary>
        public void Enroll()
        {
            Enroll(string.Empty);
        }

        /// <summary>
        /// Enroll the authenticator with the server. We can pass an optional country code from http://en.wikipedia.org/wiki/ISO_3166-1_alpha-2
        /// but the server uses GEOIP to determine the region anyway
        /// </summary>
        /// <param name="countryCode">optional 2 letter country code</param>
        public void Enroll(string countryCode)
        {
            object[] param = new object[] { countryCode };
            typeAuthenticator.InvokeMember("Enroll", BindingFlags.InvokeMethod, null, _authenticator, param);
        }

        /// <summary>
        /// Load a new Authenticator from a root xml node
        /// </summary>
        /// <param name="rootnode">Root node holding authenticator data</param>
        /// <param name="password"></param>
        public void Load(XmlNode rootnode, string password, decimal version)
        {
            object[] param = new object[] { rootnode, password, version };
            typeAuthenticator.InvokeMember("Load", BindingFlags.InvokeMethod, null, _authenticator, param);
        }

        /// <summary>
        /// Get the restore code for an authenticator used to recover a lost authenticator along with the serial number.
        /// </summary>
        /// <returns>restore code (10 chars)</returns>
        public string RestoreCode
        {
            get
            {
                return _restorecode.GetValue(_authenticator, null) as string;
            }
        }

        public int timeLeft
        {
            get
            {
                return (int)(30 - ((ServerTime % 30000L) / 1000L));
            }
        }

        #region ICloneable

        /// <summary>
        /// Clone the current object
        /// </summary>
        /// <returns>return clone</returns>
        public object Clone()
        {
            // we only need to do shallow copy
            return this.MemberwiseClone();
        }

        #endregion
    }
}
