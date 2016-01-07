#region

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using Renci.SshNet;

#endregion

namespace Sshfs
{
    internal static class Utilities
    {
        private static readonly DirectoryInfo datadir = Directory.CreateDirectory(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\WinSshFS"
            );

        public static void Load<T>(this List<T> list, string file) where T : ISerializable
        {
            string filepath = datadir.FullName+"\\"+file;
            if (!File.Exists(filepath)) return;

            var xmlSerializer = new DataContractSerializer(typeof(IEnumerable<T>));
            using (
                var stream = File.Open(filepath, FileMode.OpenOrCreate,
                                                            FileAccess.Read))
            {
                list.Clear();
               
                list.AddRange(xmlSerializer.ReadObject(stream) as IEnumerable<T>);
            }
        }

        public static T Load<T>(this T obj, string file) where T : ISerializable
        {
            string filepath = datadir.FullName + "\\" + file;
            if (!File.Exists(filepath)) return default(T);

            var xmlSerializer = new DataContractSerializer(typeof(IEnumerable<T>));
            
            using (
                var stream = File.Open(filepath, FileMode.OpenOrCreate,
                                                            FileAccess.Read))
            {
                return (T)xmlSerializer.ReadObject(stream);
            }
        }

        public static void Persist<T>(this List<T> list, string file, bool delete = false) where T : ISerializable

        {
            string filepath = datadir.FullName + "\\" + file;
            if (delete)
            {
                File.Delete(filepath);
            }
            else
            {
                var xmlSerializer = new DataContractSerializer(typeof (List<T>));
                using (
                    var stream = File.Open(filepath, FileMode.Create,
                                                                FileAccess.Write))
                {
                    xmlSerializer.WriteObject(stream, list);
                }
            }
        }


        public static void Persist<T>(this T obj, string file, bool delete = false) where T : ISerializable
        {
            string filepath = datadir.FullName + "\\" + file;
            if (delete)
            {
                File.Delete(filepath);
            }
            else
            {
                var xmlSerializer = new DataContractSerializer(typeof(List<T>));
                using (
                    var stream = File.Open(filepath, FileMode.Create,
                                                                FileAccess.Write))
                {
                    xmlSerializer.WriteObject(stream, obj);
                }
            }
        }


        public static string ProtectString(string stringToProtect)
        {
            return stringToProtect != null
                       ? Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(stringToProtect), null,
                                                                      DataProtectionScope.CurrentUser))
                       : null;
        }

        public static string UnprotectString(string stringToUnprotect)
        {
            try
            {
                return stringToUnprotect != null
                           ? Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(stringToUnprotect),
                                                                             null,
                                                                             DataProtectionScope.CurrentUser))
                           : null;
            }
            catch
            {
                //in case of migration of config.xml between hosts - passwords cannot and shoud not work
                return null;
            }
        }

        public static IEnumerable<char> GetAvailableDrives()
        {
            return Enumerable.Range('D', 23).Select(value => (char) value).Except(
                Directory.GetLogicalDrives().Select(drive => drive[0]));
        }


        public static void RegisterForStartup()
        {
            Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true).SetValue(Application.ProductName,
                                                                                     Application.ExecutablePath);
        }

        public static void UnregisterForStarup()
        {
            Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true).DeleteValue(Application.ProductName);
        }


        public static bool IsAppRegistredForStarup()
        {
            return (Registry.CurrentUser.OpenSubKey
                        ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false).GetValue(Application.ProductName)
                    as string) == Application.ExecutablePath;
        }


        public static bool IsValidUnixPath(string value)
        {
            return !string.IsNullOrEmpty(value) && (value[0] == '.' || value[0] == '/') && (value.IndexOf('\\') == -1);
        }

       /* public static bool IsValidPrivateKey(string path)
        {
            try
            {
                new PrivateKeyFile(path);
                return true;
            }
            catch
            {
                return false;
            }
        }*/

        public static void SetNetworkDriveName(string connection,string name)
        {
           var drive= Registry.CurrentUser.OpenSubKey
               ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MountPoints2", false).OpenSubKey(connection.Replace("\\","#"),true);
            if(drive!=null)
            {
                drive.SetValue("_LabelFromReg", name);
            }
        }
    }
}