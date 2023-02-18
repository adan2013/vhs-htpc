using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.ComponentModel.Design.Serialization;
using System.Configuration.Internal;
using System.Windows;

namespace RemoteWindowsController
{
    public struct ProfileItem
    {
        public string name;
        public string value;

        public ProfileItem(string name, string value)
        {
            this.name = name;
            this.value = value;
        }
    }

    public struct Profile
    {
        public string name;
        public List<ProfileItem> items;

        public Profile(string name, List<ProfileItem> items)
        {
            this.name = name;
            this.items = items;
        }
    }

    public struct AppShortcut
    {
        public string name;
        public string path;
        public string args;

        public AppShortcut(string name, string path,string args)
        {
            this.name = name;
            this.path = path;
            this.args = args;
        }
    }

    public class ConfigStorage
    {
        private const string FILE_NAME = "config-rwc.xml";
        private const int MAX_PROFILES = 6;
        private const int MAX_APPS = 6;

        private List<Profile> profiles;
        private List<AppShortcut> appShortcuts;

        public int selectedProfileIndex = 0;

        public ConfigStorage()
        {
            profiles = new List<Profile>() { generateInitialProfile() };
            appShortcuts = new List<AppShortcut> { generateInitialAppShortcut() };
            if (System.IO.File.Exists(FILE_NAME))
            {
                readConfigFile();
            } else {
                writeConfigFile();
            }
        }

        private Profile generateInitialProfile()
        {
            List<ProfileItem> items = new List<ProfileItem>();
            for (int i = 1; i < 7; i++) items.Add(new ProfileItem("Slot " + i, "Slot value 1"));
            return new Profile("New profile", items);
        }

        private AppShortcut generateInitialAppShortcut()
        {
            return new AppShortcut("New app", "calc.exe", "");
        }

        private void readConfigFile()
        {
            profiles = new List<Profile>();
            appShortcuts = new List<AppShortcut>();

            XmlDocument xml = new XmlDocument();
            xml.Load(FILE_NAME);

            XmlNodeList profileNodes = xml.GetElementsByTagName("Profile");
            foreach (XmlNode profileNode in profileNodes)
            {
                List<ProfileItem> items = new List<ProfileItem>();
                foreach(XmlNode itemNode in profileNode.ChildNodes)
                {
                    if (itemNode is XmlElement el) items.Add(new ProfileItem(el.GetAttribute("name"), el.GetAttribute("value")));
                }
                if (profileNode is XmlElement profileElement) profiles.Add(new Profile(profileElement.GetAttribute("name"), items));
            }

            XmlNodeList appNodes = xml.GetElementsByTagName("App");
            foreach (XmlNode appNode in appNodes)
            {
                if (appNode is XmlElement appElement)
                {
                    string name = appElement.GetAttribute("name");
                    string path = appElement.GetAttribute("path");
                    string args = appElement.GetAttribute("args");
                    appShortcuts.Add(new AppShortcut(name, path, args));
                }
            }
        }

        private void writeConfigFile()
        {
            XmlDocument xml = new XmlDocument();
            xml.AppendChild(xml.CreateComment("Config file of the Remote Windows Controller app"));
            XmlNode root = xml.CreateElement("Config");

            XmlNode profileRoot = xml.CreateElement("Profiles");
            foreach (Profile profile in profiles)
            {
                XmlNode profileNode = xml.CreateElement("Profile");
                XmlAttribute nameAttribute = xml.CreateAttribute("name");
                nameAttribute.Value = profile.name;
                profileNode.Attributes?.Append(nameAttribute);
                foreach(ProfileItem item in profile.items)
                {
                    XmlNode itemNode = xml.CreateElement("ProfileItem");
                    XmlAttribute itemNameAttribute = xml.CreateAttribute("name");
                    XmlAttribute itemValueAttribute = xml.CreateAttribute("value");
                    itemNameAttribute.Value = item.name;
                    itemValueAttribute.Value = item.value;
                    itemNode.Attributes?.Append(itemNameAttribute);
                    itemNode.Attributes?.Append(itemValueAttribute);
                    profileNode.AppendChild(itemNode);
                }
                profileRoot.AppendChild(profileNode);
            }
            root.AppendChild(profileRoot);

            XmlNode appShortcutsRoot = xml.CreateElement("Apps");
            foreach (AppShortcut appShortcut in appShortcuts)
            {
                XmlNode appShortcutNode = xml.CreateElement("App");
                XmlAttribute nameAttribute = xml.CreateAttribute("name");
                XmlAttribute pathAttribute = xml.CreateAttribute("path");
                XmlAttribute argsAttribute = xml.CreateAttribute("args");
                nameAttribute.Value = appShortcut.name;
                pathAttribute.Value = appShortcut.path;
                argsAttribute.Value = appShortcut.args;
                appShortcutNode.Attributes?.Append(nameAttribute);
                appShortcutNode.Attributes?.Append(pathAttribute);
                appShortcutNode.Attributes?.Append(argsAttribute);
                appShortcutsRoot.AppendChild(appShortcutNode);
            }
            root.AppendChild(appShortcutsRoot);

            xml.AppendChild(root);
            xml.Save(FILE_NAME);
        }

        private void showError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private bool swapElements<T>(IList<T> list, int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= list.Count) return false;
            if (indexB < 0 || indexB >= list.Count) return false;
            T tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
            return true;
        }

        public List<Profile> getProfiles() => profiles;

        public bool addProfile()
        {
            if(profiles.Count >= MAX_PROFILES)
            {
                showError("You can have max " + MAX_PROFILES + " saved profiles!");
                return false;
            }
            profiles.Add(generateInitialProfile());
            writeConfigFile();
            return true;
        }

        public bool removeProfile(int idx)
        {
            if(idx < profiles.Count && idx >= 0)
            {
                if(profiles.Count == 1)
                {
                    showError("You can't remove the last saved profile!");
                    return false;
                }
                profiles.RemoveAt(idx);
                writeConfigFile();
                return true;
            }
            return false;
        }

        public bool updateProfile(int idx, Profile profile)
        {
            if (idx < profiles.Count && idx >= 0)
            {
                profiles[idx] = profile;
                writeConfigFile();
                return true;
            }
            return false;
        }

        public bool moveProfile(int idx, int dir)
        {
            if(swapElements<Profile>(profiles, idx, idx + dir))
            {
                writeConfigFile();
                return true;
            }
            return false;
        }

        public List<AppShortcut> getAppShortcuts() => appShortcuts;

        public bool addAppShortcut()
        {
            if (appShortcuts.Count >= MAX_APPS)
            {
                showError("You can have max " + MAX_APPS + " saved apps!");
                return false;
            }
            appShortcuts.Add(generateInitialAppShortcut());
            writeConfigFile();
            return true;
        }

        public bool removeAppShortcut(int idx)
        {
            if (idx < appShortcuts.Count && idx >= 0)
            {
                if (appShortcuts.Count == 1)
                {
                    showError("You can't remove the last saved app shortcut!");
                    return false;
                }
                appShortcuts.RemoveAt(idx);
                writeConfigFile();
                return true;
            }
            return false;
        }

        public bool updateAppShortcut(int idx, AppShortcut appShortcut)
        {
            if (idx < appShortcuts.Count && idx >= 0)
            {
                appShortcuts[idx] = appShortcut;
                writeConfigFile();
                return true;
            }
            return false;
        }

        public bool moveAppShortcut(int idx, int dir)
        {
            if (swapElements<AppShortcut>(appShortcuts, idx, idx + dir))
            {
                writeConfigFile();
                return true;
            }
            return false;
        }

        public string getSlotValue(int slotNumber)
        {
            try
            {
                Profile profile = profiles[selectedProfileIndex];
                return profile.items[slotNumber - 1].value;
            }
            catch
            {
                return "";
            }
        }
    }
}
