//
// Copyright Fela Ameghino 2015-2023
//
// Distributed under the GNU General Public License v3.0. (See accompanying
// file LICENSE or copy at https://www.gnu.org/licenses/gpl-3.0.txt)
//
using Telegram.Common;
using Telegram.ViewModels.Settings;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Controls;

namespace Telegram.Views.Settings
{
    public sealed partial class SettingsAdvancedPage : HostedPage
    {
        public SettingsAdvancedViewModel ViewModel => DataContext as SettingsAdvancedViewModel;

        public SettingsAdvancedPage()
        {
            InitializeComponent();
            Title = Strings.Resources.PrivacyAdvanced;

            if (ApiInfo.IsPackagedRelease)
            {
                FindName(nameof(UpdatePanel));
            }

#if !DESKTOP_BRIDGE
            if (ApiInformation.IsTypePresent("Windows.ApplicationModel.FullTrustProcessLauncher"))
            {
                FindName(nameof(TraySwitch));
            }
#endif
        }

        private void Shortcuts_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsShortcutsPage));
        }
    }
}
