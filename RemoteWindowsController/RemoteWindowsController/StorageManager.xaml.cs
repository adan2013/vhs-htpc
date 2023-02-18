using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RemoteWindowsController
{

    public partial class StorageManager : Window
    {
        ConfigStorage config;
        TextBox[] nameFieldsRef = new TextBox[6];
        TextBox[] valueFieldsRef = new TextBox[6];

        bool profileModified = false;
        bool appModified = false;

        public StorageManager()
        {
            config = ((App)Application.Current).config;
            InitializeComponent();
            refreshListsOfProfilesAndApps();
        }

        void refreshListsOfProfilesAndApps()
        {
            int selectedProfile = profilesList.SelectedIndex;
            int selectedApp = appsList.SelectedIndex;
            profilesList.SelectionChanged -= list_SelectionChanged;
            appsList.SelectionChanged -= list_SelectionChanged;
            profilesList.Items.Clear();
            appsList.Items.Clear();

            List<Profile> profiles = config.getProfiles();
            List<AppShortcut> appShortcuts = config.getAppShortcuts();

            for (int i = 0; i < profiles.Count; i++)
            {
                profilesList.Items.Add("(" + (i + 1) + "/" + profiles.Count + ") " + profiles[i].name);
            }
            for (int i = 0; i < appShortcuts.Count; i++)
            {
                appsList.Items.Add("(" + (i + 1) + "/" + appShortcuts.Count + ") " + appShortcuts[i].name);
            }

            profilesList.SelectedIndex = selectedProfile < 0 || selectedProfile >= profiles.Count ? 0 : selectedProfile;
            appsList.SelectedIndex = selectedApp < 0 || selectedApp >= appShortcuts.Count ? 0 : selectedApp;
            refreshProfileFields();
            refreshAppFields();
            profilesList.SelectionChanged += list_SelectionChanged;
            appsList.SelectionChanged += list_SelectionChanged;
        }

        private void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            refreshListsOfProfilesAndApps();
        }

        void refreshProfileFields()
        {
            Profile currentProfile = config.getProfiles()[profilesList.SelectedIndex];
            profileName.TextChanged -= profileDataChanged;
            profileName.Text = currentProfile.name;
            profileName.TextChanged += profileDataChanged;
            profileItemsContainer.Children.Clear();
            for (int i = 0; i < 6; i++)
            {
                ProfileItem item = currentProfile.items[i];
                Label lbl = new Label() {
                    Margin = new Thickness(5, 10, 5, 5),
                    Content = "- Item " + (i + 1).ToString() + " -",
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                };
                TextBox nameInput = new TextBox()
                {
                    Margin = new Thickness(5),
                    MaxLength = 20,
                    Text = item.name,
                };
                TextBox valueInput = new TextBox()
                {
                    Margin = new Thickness(5),
                    MaxLength = 150,
                    Text = item.value,
                };
                nameInput.TextChanged += profileDataChanged;
                valueInput.TextChanged += profileDataChanged;
                nameFieldsRef[i] = nameInput;
                valueFieldsRef[i] = valueInput;
                profileItemsContainer.Children.Add(lbl);
                profileItemsContainer.Children.Add(nameInput);
                profileItemsContainer.Children.Add(valueInput);
            }
            refreshProfileButtons(false);
        }

        private void profileDataChanged(object sender, TextChangedEventArgs e)
        {
            refreshProfileButtons(true);
        }

        private void refreshProfileButtons(bool s)
        {
            profileModified = s;
            saveProfile.IsEnabled = s;
            restoreProfile.IsEnabled = s;
            newProfile.IsEnabled = !s;
            deleteProfile.IsEnabled = !s;
            moveUpProfile.IsEnabled = !s;
            moveDownProfile.IsEnabled = !s;
        }

        void refreshAppFields()
        {
            AppShortcut currentAppShortcut = config.getAppShortcuts()[appsList.SelectedIndex];
            appName.TextChanged -= appDataChanged;
            appPath.TextChanged -= appDataChanged;
            appArgs.TextChanged -= appDataChanged;
            appName.Text = currentAppShortcut.name;
            appPath.Text = currentAppShortcut.path;
            appArgs.Text = currentAppShortcut.args;
            appName.TextChanged += appDataChanged;
            appPath.TextChanged += appDataChanged;
            appArgs.TextChanged += appDataChanged;
            refreshAppButtons(false);
        }

        private void appDataChanged(object sender, TextChangedEventArgs e)
        {
            refreshAppButtons(true);
        }

        private void refreshAppButtons(bool s)
        {
            appModified = s;
            saveApp.IsEnabled = s;
            restoreApp.IsEnabled = s;
            newApp.IsEnabled = !s;
            deleteApp.IsEnabled = !s;
            moveUpApp.IsEnabled = !s;
            moveDownApp.IsEnabled = !s;
        }

        private void saveProfile_Click(object sender, RoutedEventArgs e)
        {
            List<ProfileItem> items = new List<ProfileItem>();
            for(int i = 0; i < nameFieldsRef.Length; i++)
            {
                items.Add(new ProfileItem(nameFieldsRef[i].Text.Trim(), valueFieldsRef[i].Text.Trim()));
            }
            config.updateProfile(profilesList.SelectedIndex, new Profile(profileName.Text.Trim(), items));
            refreshListsOfProfilesAndApps();
        }

        private void restoreProfile_Click(object sender, RoutedEventArgs e)
        {
            refreshListsOfProfilesAndApps();
        }

        private void newProfile_Click(object sender, RoutedEventArgs e)
        {
            if(config.addProfile())
            {
                refreshListsOfProfilesAndApps();
                profilesList.SelectedIndex = profilesList.Items.Count - 1;
            }
        }

        private void deleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (config.removeProfile(profilesList.SelectedIndex))
            {
                refreshListsOfProfilesAndApps();
            }
        }

        private void moveUpProfile_Click(object sender, RoutedEventArgs e)
        {
            if (config.moveProfile(profilesList.SelectedIndex, -1))
            {
                refreshListsOfProfilesAndApps();
                profilesList.SelectedIndex -= 1;
            }
        }

        private void moveDownProfile_Click(object sender, RoutedEventArgs e)
        {
            if (config.moveProfile(profilesList.SelectedIndex, 1))
            {
                refreshListsOfProfilesAndApps();
                profilesList.SelectedIndex += 1;
            }
        }

        private void saveApp_Click(object sender, RoutedEventArgs e)
        {
            config.updateAppShortcut(appsList.SelectedIndex, new AppShortcut(appName.Text.Trim(), appPath.Text.Trim(), appArgs.Text.Trim()));
            refreshListsOfProfilesAndApps();
        }

        private void restoreApp_Click(object sender, RoutedEventArgs e)
        {
            refreshListsOfProfilesAndApps();
        }

        private void newApp_Click(object sender, RoutedEventArgs e)
        {
            if (config.addAppShortcut())
            {
                refreshListsOfProfilesAndApps();
                appsList.SelectedIndex = appsList.Items.Count - 1;
            }
        }

        private void deleteApp_Click(object sender, RoutedEventArgs e)
        {
            if (config.removeAppShortcut(appsList.SelectedIndex))
            {
                refreshListsOfProfilesAndApps();
            }
        }

        private void moveUpApp_Click(object sender, RoutedEventArgs e)
        {
            if (config.moveAppShortcut(appsList.SelectedIndex, -1))
            {
                refreshListsOfProfilesAndApps();
                appsList.SelectedIndex -= 1;
            }
        }

        private void moveDownApp_Click(object sender, RoutedEventArgs e)
        {
            if (config.moveAppShortcut(appsList.SelectedIndex, 1))
            {
                refreshListsOfProfilesAndApps();
                appsList.SelectedIndex += 1;
            }
        }

        private void TestTheCommand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(appPath.Text.Trim(), appArgs.Text.Trim());
            }
            catch
            {
                MessageBox.Show("Action failed!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
