//
// Copyright Fela Ameghino 2015-2023
//
// Distributed under the GNU General Public License v3.0. (See accompanying
// file LICENSE or copy at https://www.gnu.org/licenses/gpl-3.0.txt)
//
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Collections;
using Telegram.Common;
using Telegram.Controls;
using Telegram.Converters;
using Telegram.Services;
using Telegram.Td.Api;
using Windows.UI.Xaml.Controls;

namespace Telegram.ViewModels
{
    public class CallsViewModel : TLViewModelBase
    {
        public CallsViewModel(IClientService clientService, ISettingsService settingsService, IEventAggregator aggregator)
            : base(clientService, settingsService, aggregator)
        {
            Items = new ItemsCollection(clientService);

            CallDeleteCommand = new RelayCommand<TLCallGroup>(CallDeleteExecute);
        }

        public ItemsCollection Items { get; private set; }

        public class ItemsCollection : IncrementalCollection<TLCallGroup>
        {
            private readonly IClientService _clientService;

            private string _nextOffset;
            private bool _hasMoreItems = true;

            public ItemsCollection(IClientService clientService)
            {
                _clientService = clientService;
            }

            public override async Task<IList<TLCallGroup>> LoadDataAsync()
            {
                var response = await _clientService.SendAsync(new SearchCallMessages(_nextOffset, 50, false)); //(new TLInputPeerEmpty(), null, null, new TLInputMessagesFilterPhoneCalls(), 0, 0, 0, _lastMaxId, 50);
                if (response is FoundMessages messages)
                {
                    _nextOffset = messages.NextOffset;
                    _hasMoreItems = messages.NextOffset.Length > 0;

                    List<TLCallGroup> groups = new List<TLCallGroup>();
                    List<Message> currentMessages = null;
                    Chat currentChat = null;
                    User currentPeer = null;
                    bool currentFailed = false;
                    DateTime? currentTime = null;

                    foreach (var message in messages.Messages)
                    {
                        var chat = _clientService.GetChat(message.ChatId);
                        if (chat == null)
                        {
                            continue;
                        }

                        var call = message.Content as MessageCall;
                        if (call == null)
                        {
                            continue;
                        }

                        var peer = _clientService.GetUser(chat);
                        if (peer == null)
                        {
                            continue;
                        }

                        var outgoing = message.IsOutgoing;
                        var missed = call.DiscardReason is CallDiscardReasonMissed or CallDiscardReasonDeclined;
                        var failed = !outgoing && missed;
                        var time = Converter.DateTime(message.Date);

                        if (currentPeer != null)
                        {
                            if (currentPeer.Id == peer.Id && currentFailed == failed && currentTime.Value.Date == time.Date)
                            {
                                currentMessages.Add(message);
                                continue;
                            }
                            else
                            {
                                groups.Add(new TLCallGroup(currentMessages, currentChat.Id, currentPeer, currentFailed));
                            }
                        }

                        currentChat = chat;
                        currentPeer = peer;
                        currentMessages = new List<Message> { message };
                        currentFailed = failed;
                        currentTime = time;
                    }

                    if (currentMessages?.Count > 0)
                    {
                        groups.Add(new TLCallGroup(currentMessages, currentChat.Id, currentPeer, currentFailed));
                    }

                    return groups;
                }

                return new TLCallGroup[0];
            }

            protected override bool GetHasMoreItems()
            {
                return _hasMoreItems;
            }
        }

        #region Context menu

        public RelayCommand<TLCallGroup> CallDeleteCommand { get; }
        private async void CallDeleteExecute(TLCallGroup group)
        {
            var popup = new MessagePopup
            {
                Title = Strings.Resources.DeleteCalls,
                Message = Strings.Resources.DeleteSelectedCallsText,
                PrimaryButtonText = Strings.Resources.Delete,
                SecondaryButtonText = Strings.Resources.Cancel,
                CheckBoxLabel = Strings.Resources.DeleteCallsForEveryone
            };

            var confirm = await ShowPopupAsync(popup);
            if (confirm != ContentDialogResult.Primary)
            {
                return;
            }

            var response = await ClientService.SendAsync(new DeleteMessages(group.ChatId, group.Items.Select(x => x.Id).ToArray(), popup.IsChecked == true));
            if (response is Ok)
            {
                Items.Remove(group);
            }
        }

        #endregion
    }

    public class TLCallGroup
    {
        public TLCallGroup(IEnumerable<Message> messages, long chatId, User peer, bool failed)
        {
            Items = new ObservableCollection<Message>(messages);
            ChatId = chatId;
            Peer = peer;
            IsFailed = failed;
        }

        public ObservableCollection<Message> Items { get; private set; }

        public User Peer { get; private set; }

        public long ChatId { get; private set; }

        public bool IsFailed { get; private set; }

        public override string ToString()
        {
            if (Items.Count > 1)
            {
                return string.Format("{0} ({1}) - {2}", Peer.FullName(), Items.Count, DisplayType);
            }

            return string.Format("{0} - {1}", Peer.FullName(), DisplayType);
        }

        private string _displayType;
        public string DisplayType
        {
            get
            {
                if (_displayType == null)
                {
                    _displayType = GetDisplayType();
                }

                return _displayType;
            }
        }

        public Message Message => Items.FirstOrDefault();

        private enum TLCallDisplayType
        {
            Outgoing,
            Incoming,
            Cancelled,
            Missed
        }

        private string GetDisplayType()
        {
            if (IsFailed)
            {
                return Strings.Resources.CallMessageIncomingMissed;
            }

            var finalType = string.Empty;
            var types = new List<TLCallDisplayType>();
            foreach (var message in Items)
            {
                var call = message.Content as MessageCall;
                if (call == null)
                {
                    continue;
                }

                var outgoing = message.IsOutgoing;
                var missed = call.DiscardReason is CallDiscardReasonMissed or CallDiscardReasonDeclined;

                var type = missed ? (outgoing ? TLCallDisplayType.Cancelled : TLCallDisplayType.Missed) : (outgoing ? TLCallDisplayType.Outgoing : TLCallDisplayType.Incoming);

                if (types.Contains(type)) { }
                else
                {
                    types.Add(type);
                }
            }

            if (types.Count > 1)
            {
                while (types.Contains(TLCallDisplayType.Cancelled))
                {
                    types.Remove(TLCallDisplayType.Cancelled);
                }
            }

            var typesArray = types.OrderBy(x => (int)x);
            foreach (var typeValue in typesArray)
            {
                var type = StringForDisplayType(typeValue);
                if (finalType.Length == 0)
                {
                    finalType = type;
                }
                else
                {
                    finalType += $", {type}";
                }
            }

            if (Items.Count == 1)
            {
                var message = Items[0];

                var call = message.Content as MessageCall;
                if (call == null)
                {
                    return string.Empty;
                }

                var missed = call.DiscardReason is CallDiscardReasonMissed or CallDiscardReasonDeclined;

                var duration = missed || call.Duration < 1 ? null : Locale.FormatCallDuration(call.Duration);
                finalType = duration != null ? string.Format("{0} ({1})", finalType, duration) : finalType;
            }

            return finalType;
        }

        private string StringForDisplayType(TLCallDisplayType type)
        {
            switch (type)
            {
                case TLCallDisplayType.Outgoing:
                    return Strings.Resources.CallMessageOutgoing;
                case TLCallDisplayType.Incoming:
                    return Strings.Resources.CallMessageIncoming;
                case TLCallDisplayType.Cancelled:
                    return Strings.Resources.CallMessageOutgoingMissed;
                case TLCallDisplayType.Missed:
                    return Strings.Resources.CallMessageIncomingMissed;
                default:
                    return null;
            }
        }
    }
}
