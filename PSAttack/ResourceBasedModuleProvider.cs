using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using PSAttack.Utils;

namespace PSAttack
{
    internal class ResourceBasedModuleProvider : IModuleProvider
    {
        static ResourceBasedModuleProvider()
        {
            Instance = new ResourceBasedModuleProvider();
        }

        private ResourceBasedModuleProvider()
        {
            _decryptionKey = Properties.Settings.Default.encryptionKey;
        }

        public string GetModule(string moduleName)
        {
            string result;
            _resourcesByName.TryGetValue(moduleName, out result);
            return result;
        }

        public string GetProperty(string propertyName)
        {
            if (null == _propertiesByName) {
                _propertiesByName = new Dictionary<string, string>();
                Assembly assembly = Assembly.GetExecutingAssembly();
                Stream valueStream = assembly.GetManifestResourceStream(
                    "PSAttack.Resources." + Properties.Settings.Default.valueStore);
                string decryptionKey = Properties.Settings.Default.encryptionKey;
                MemoryStream valueStore = valueStream.Decrypt(decryptionKey);
                string valueStoreStr = Encoding.Unicode.GetString(valueStore.ToArray());

                foreach (string value in valueStoreStr.Replace("\r", "").Split('\n')) {
                    if (string.IsNullOrEmpty(value)) { continue; }
                    string[] entry = value.Split('|');
                    _propertiesByName.Add(entry[0], entry[1]);
                }
            }
            string result;
            _propertiesByName.TryGetValue(propertyName, out result);
            return result;
        }

        public IEnumerable<string> ResourceNames()
        {
            if (null == _resourcesByName) {
                _resourcesByName = new Dictionary<string, string>();
                Assembly assembly = Assembly.GetExecutingAssembly();
                foreach (string resource in assembly.GetManifestResourceNames()) {
                    if (!resource.Contains(Globals.ModulePrefix)) { continue; }
                    string encryptedModuleName = resource.Replace(Globals.ModulePrefix, string.Empty);
                    byte[] rawModuleName = encryptedModuleName.Decrypt(_decryptionKey).ToArray();
                    string trueModuleName = Encoding.UTF32.GetString(rawModuleName);
                    // ImportModule(assembly.GetManifestResourceStream(resource));
                    string[] allResources = assembly.GetManifestResourceNames();
                    Stream encryptedModuleContent =
                        assembly.GetManifestResourceStream(Globals.ModulePrefix + encryptedModuleName);
                    MemoryStream rawModuleContent =
                        encryptedModuleContent.Decrypt(_decryptionKey);
                    string trueModuleContent =
                        Encoding.Unicode.GetString(rawModuleContent.ToArray());
                    _resourcesByName.Add(trueModuleName, trueModuleContent);
                }
            }
            foreach(string moduleName in _resourcesByName.Keys) {
                yield return moduleName;
            }
            yield break;
        }

        internal static ResourceBasedModuleProvider Instance { get; private set; }
        private string _decryptionKey;
        private Dictionary<string, string> _propertiesByName = null;
        private Dictionary<string, string> _resourcesByName = null;
    }
}
