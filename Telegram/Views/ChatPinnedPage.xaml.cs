//
// Copyright Fela Ameghino 2015-2023
//
// Distributed under the GNU General Public License v3.0. (See accompanying
// file LICENSE or copy at https://www.gnu.org/licenses/gpl-3.0.txt)
//
using Telegram.Navigation;
using Telegram.ViewModels;
using Telegram.ViewModels.Delegates;
using Windows.UI.Xaml.Navigation;

namespace Telegram.Views
{
    public sealed partial class ChatPinnedPage : HostedPage, INavigablePage, ISearchablePage, IActivablePage
    {
        public DialogPinnedViewModel ViewModel => DataContext as DialogPinnedViewModel;

        public ChatView View => Content as ChatView;

        public ChatPinnedPage()
        {
            InitializeComponent();

            Content = new ChatView(CreateViewModel, SetTitle);
            Header = View.Header;
            NavigationCacheMode = NavigationCacheMode.Required;
        }

        private DialogViewModel CreateViewModel(IDialogDelegate delegato, int sessionId)
        {
            var viewModel = TLContainer.Current.Resolve<DialogPinnedViewModel, IDialogDelegate>(delegato, sessionId);
            DataContext = viewModel;

            return viewModel;
        }

        private void SetTitle(string title)
        {
            Title = title;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            View.OnNavigatingFrom(e.SourcePageType);
        }

        public void OnBackRequested(BackRequestedRoutedEventArgs args)
        {
            View.OnBackRequested(args);
        }

        public void Search()
        {
            View.Search();
        }

        public void Deactivate(bool navigation)
        {
            View.Deactivate(navigation);

            if (navigation)
            {
                return;
            }

            DataContext = new object();
        }

        public void Activate(int sessionId)
        {
            View.Activate(sessionId);
        }

        public void PopupOpened()
        {
            View.PopupOpened();
        }

        public void PopupClosed()
        {
            View.PopupClosed();
        }
    }
}
