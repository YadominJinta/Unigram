//
// Copyright Fela Ameghino 2015-2023
//
// Distributed under the GNU General Public License v3.0. (See accompanying
// file LICENSE or copy at https://www.gnu.org/licenses/gpl-3.0.txt)
//
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Common;
using Telegram.Navigation.Services;
using Telegram.Services;
using Telegram.Td.Api;
using Telegram.ViewModels.Delegates;
using Telegram.Views.Chats;
using Telegram.Views.Popups;
using Telegram.Views.Supergroups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Telegram.ViewModels.Supergroups
{
    public class SupergroupEditViewModel : TLViewModelBase
        , IDelegable<ISupergroupEditDelegate>
        , IHandle
    //, IHandle<UpdateChatPhoto>
    //, IHandle<UpdateSupergroup>
    //, IHandle<UpdateSupergroupFullInfo>
    //, IHandle<UpdateBasicGroup>
    //, IHandle<UpdateBasicGroupFullInfo>
    {
        public ISupergroupEditDelegate Delegate { get; set; }

        private readonly IProfilePhotoService _profilePhotoService;

        public SupergroupEditViewModel(IClientService clientService, ISettingsService settingsService, IEventAggregator aggregator, IProfilePhotoService profilePhotoService)
            : base(clientService, settingsService, aggregator)
        {
            _profilePhotoService = profilePhotoService;

            EditTypeCommand = new RelayCommand(EditTypeExecute);
            EditLinkedChatCommand = new RelayCommand(EditLinkedChatExecute);
            EditStickerSetCommand = new RelayCommand(EditStickerSetExecute);

            LinksCommand = new RelayCommand(LinksExecute);
            DeleteCommand = new RelayCommand(DeleteExecute);

            SendCommand = new RelayCommand(SendExecute);

            ReactionsCommand = new RelayCommand(ReactionsExecute);
            MembersCommand = new RelayCommand(MembersExecute);
            AdminsCommand = new RelayCommand(AdminsExecute);
            BannedCommand = new RelayCommand(BannedExecute);
            KickedCommand = new RelayCommand(KickedExecute);
        }

        protected Chat _chat;
        public Chat Chat
        {
            get => _chat;
            set => Set(ref _chat, value);
        }

        private string _title;
        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        private string _about;
        public string About
        {
            get => _about;
            set => Set(ref _about, value);
        }

        private bool _isSignatures;
        public bool IsSignatures
        {
            get => _isSignatures;
            set => Set(ref _isSignatures, value);
        }

        private bool _isAllHistoryAvailable;
        public int IsAllHistoryAvailable
        {
            get => Array.IndexOf(_allHistoryAvailableIndexer, _isAllHistoryAvailable);
            set
            {
                if (value >= 0 && value < _allHistoryAvailableIndexer.Length && _isAllHistoryAvailable != _allHistoryAvailableIndexer[value])
                {
                    _isAllHistoryAvailable = _allHistoryAvailableIndexer[value];
                    RaisePropertyChanged();
                }
            }
        }

        private readonly bool[] _allHistoryAvailableIndexer = new[]
        {
            true,
            false,
        };

        public List<SettingsOptionItem<bool>> AllHistoryAvailableOptions { get; } = new()
        {
            new SettingsOptionItem<bool>(true, Strings.Resources.ChatHistoryVisible),
            new SettingsOptionItem<bool>(false, Strings.Resources.ChatHistoryHidden)
        };

        #region Initialize

        protected override Task OnNavigatedToAsync(object parameter, NavigationMode mode, NavigationState state)
        {
            var chatId = (long)parameter;

            Chat = ClientService.GetChat(chatId);

            var chat = _chat;
            if (chat == null)
            {
                return Task.CompletedTask;
            }

            Delegate?.UpdateChat(chat);

            if (chat.Type is ChatTypeSupergroup super)
            {
                var item = ClientService.GetSupergroup(super.SupergroupId);
                var cache = ClientService.GetSupergroupFull(super.SupergroupId);

                Delegate?.UpdateSupergroup(chat, item);

                if (cache == null)
                {
                    ClientService.Send(new GetSupergroupFullInfo(super.SupergroupId));
                }
                else
                {
                    Delegate?.UpdateSupergroupFullInfo(chat, item, cache);
                }
            }
            else if (chat.Type is ChatTypeBasicGroup basic)
            {
                var item = ClientService.GetBasicGroup(basic.BasicGroupId);
                var cache = ClientService.GetBasicGroupFull(basic.BasicGroupId);

                Delegate?.UpdateBasicGroup(chat, item);

                if (cache == null)
                {
                    ClientService.Send(new GetBasicGroupFullInfo(basic.BasicGroupId));
                }
                else
                {
                    Delegate?.UpdateBasicGroupFullInfo(chat, item, cache);
                }
            }

            return Task.CompletedTask;
        }

        public override void Subscribe()
        {
            Aggregator.Subscribe<UpdateChatPhoto>(this, Handle)
                .Subscribe<UpdateSupergroup>(Handle)
                .Subscribe<UpdateSupergroupFullInfo>(Handle)
                .Subscribe<UpdateBasicGroup>(Handle)
                .Subscribe<UpdateBasicGroupFullInfo>(Handle);
        }

        public void Handle(UpdateChatPhoto update)
        {
            var chat = _chat;
            if (chat == null)
            {
                return;
            }

            if (chat.Id == update.ChatId)
            {
                BeginOnUIThread(() => Delegate?.UpdateChatPhoto(chat));
            }
        }

        public void Handle(UpdateSupergroup update)
        {
            var chat = _chat;
            if (chat == null)
            {
                return;
            }

            if (chat.Type is ChatTypeSupergroup super && super.SupergroupId == update.Supergroup.Id)
            {
                BeginOnUIThread(() => Delegate?.UpdateSupergroup(chat, update.Supergroup));
            }
        }

        public void Handle(UpdateSupergroupFullInfo update)
        {
            var chat = _chat;
            if (chat == null)
            {
                return;
            }

            if (chat.Type is ChatTypeSupergroup super && super.SupergroupId == update.SupergroupId)
            {
                BeginOnUIThread(() => Delegate?.UpdateSupergroupFullInfo(chat, ClientService.GetSupergroup(update.SupergroupId), update.SupergroupFullInfo));
            }
        }

        public void Handle(UpdateBasicGroup update)
        {
            var chat = _chat;
            if (chat == null)
            {
                return;
            }

            if (chat.Type is ChatTypeBasicGroup basic && basic.BasicGroupId == update.BasicGroup.Id)
            {
                BeginOnUIThread(() => Delegate?.UpdateBasicGroup(chat, update.BasicGroup));
            }
        }

        public void Handle(UpdateBasicGroupFullInfo update)
        {
            var chat = _chat;
            if (chat == null)
            {
                return;
            }

            if (chat.Type is ChatTypeBasicGroup basic && basic.BasicGroupId == update.BasicGroupId)
            {
                BeginOnUIThread(() => Delegate?.UpdateBasicGroupFullInfo(chat, ClientService.GetBasicGroup(update.BasicGroupId), update.BasicGroupFullInfo));
            }
        }

        #endregion

        public RelayCommand SendCommand { get; }
        private async void SendExecute()
        {
            var chat = _chat;
            if (chat == null)
            {
                return;
            }

            var about = _about.Format();
            var title = _title.Trim();
            string oldAbout = null;
            Supergroup supergroup = null;
            SupergroupFullInfo fullInfo = null;

            if (chat.Type is ChatTypeSupergroup)
            {
                var item = ClientService.GetSupergroup(chat);
                var cache = ClientService.GetSupergroupFull(chat);

                if (item == null || cache == null)
                {
                    return;
                }

                oldAbout = cache.Description;
                supergroup = item;
                fullInfo = cache;

                if (item.IsChannel && _isSignatures != item.SignMessages)
                {
                    var response = await ClientService.SendAsync(new ToggleSupergroupSignMessages(item.Id, _isSignatures));
                    if (response is Error)
                    {
                        // TODO:
                    }
                }
            }
            else if (chat.Type is ChatTypeBasicGroup basicGroup)
            {
                var item = ClientService.GetBasicGroup(basicGroup.BasicGroupId);
                var cache = ClientService.GetBasicGroupFull(basicGroup.BasicGroupId);

                oldAbout = cache.Description;
            }

            if (!string.Equals(title, chat.Title))
            {
                var response = await ClientService.SendAsync(new SetChatTitle(chat.Id, title));
                if (response is Error)
                {
                    // TODO:
                }
            }

            if (!string.Equals(about, oldAbout))
            {
                var response = await ClientService.SendAsync(new SetChatDescription(chat.Id, about));
                if (response is Error)
                {
                    // TODO:
                }
            }

            if (_isAllHistoryAvailable && chat.Type is ChatTypeBasicGroup)
            {
                var response = await ClientService.SendAsync(new UpgradeBasicGroupChatToSupergroupChat(chat.Id));
                if (response is Chat result && result.Type is ChatTypeSupergroup super)
                {
                    chat = result;
                    supergroup = await ClientService.SendAsync(new GetSupergroup(super.SupergroupId)) as Supergroup;
                    fullInfo = await ClientService.SendAsync(new GetSupergroupFullInfo(super.SupergroupId)) as SupergroupFullInfo;
                }
                else if (response is Error)
                {
                    // TODO:
                }
            }

            if (supergroup != null && fullInfo != null && _isAllHistoryAvailable != fullInfo.IsAllHistoryAvailable)
            {
                var response = await ClientService.SendAsync(new ToggleSupergroupIsAllHistoryAvailable(supergroup.Id, _isAllHistoryAvailable));
                if (response is Error)
                {
                    // TODO:
                }
            }

            NavigationService.GoBack();
        }

        public async void SetPhoto()
        {
            await _profilePhotoService.SetPhotoAsync(Chat.Id);
        }

        public async void CreatePhoto()
        {
            await _profilePhotoService.CreatePhotoAsync(NavigationService, Chat.Id);
        }

        public RelayCommand EditTypeCommand { get; }
        private void EditTypeExecute()
        {
            var chat = _chat;
            if (chat == null)
            {
                return;
            }

            NavigationService.Navigate(typeof(SupergroupEditTypePage), chat.Id);
        }

        public RelayCommand EditStickerSetCommand { get; }
        private void EditStickerSetExecute()
        {
            var chat = _chat;
            if (chat == null)
            {
                return;
            }

            NavigationService.Navigate(typeof(SupergroupEditStickerSetPage), chat.Id);
        }

        public RelayCommand EditLinkedChatCommand { get; }
        private void EditLinkedChatExecute()
        {
            var chat = _chat;
            if (chat == null)
            {
                return;
            }

            var supergroup = ClientService.GetSupergroup(chat);
            if (supergroup == null)
            {
                return;
            }

            NavigationService.Navigate(typeof(SupergroupEditLinkedChatPage), chat.Id);
        }

        public RelayCommand LinksCommand { get; }
        private void LinksExecute()
        {
            var chat = _chat;
            if (chat == null)
            {
                return;
            }

            NavigationService.Navigate(typeof(ChatInviteLinkPage), chat.Id);
        }

        public RelayCommand DeleteCommand { get; }
        private async void DeleteExecute()
        {
            var chat = _chat;
            if (chat == null)
            {
                return;
            }

            var updated = await ClientService.SendAsync(new GetChat(chat.Id)) as Chat ?? chat;
            var dialog = new DeleteChatPopup(ClientService, updated, null, false, true);

            var confirm = await ShowPopupAsync(dialog);
            if (confirm != ContentDialogResult.Primary)
            {
                return;
            }

            var response = await ClientService.SendAsync(new DeleteChat(chat.Id));
            if (response is Ok)
            {
                NavigationService.RemovePeerFromStack(chat.Id);
            }
            else if (response is Error error)
            {
                // TODO: ...
            }
        }

        #region Navigation

        public RelayCommand ReactionsCommand { get; }
        private void ReactionsExecute()
        {
            var chat = _chat;
            if (chat == null)
            {
                return;
            }

            NavigationService.Navigate(typeof(SupergroupReactionsPage), chat.Id);
        }

        public RelayCommand AdminsCommand { get; }
        private void AdminsExecute()
        {
            var chat = _chat;
            if (chat == null)
            {
                return;
            }

            NavigationService.Navigate(typeof(SupergroupAdministratorsPage), chat.Id);
        }

        public RelayCommand BannedCommand { get; }
        private void BannedExecute()
        {
            var chat = _chat;
            if (chat == null)
            {
                return;
            }

            NavigationService.Navigate(typeof(SupergroupBannedPage), chat.Id);
        }

        public RelayCommand KickedCommand { get; }
        private void KickedExecute()
        {
            var chat = _chat;
            if (chat == null)
            {
                return;
            }

            NavigationService.Navigate(typeof(SupergroupPermissionsPage), chat.Id);
        }

        public RelayCommand MembersCommand { get; }
        private void MembersExecute()
        {
            var chat = _chat;
            if (chat == null)
            {
                return;
            }

            NavigationService.Navigate(typeof(SupergroupMembersPage), chat.Id);
        }

        #endregion
    }
}
