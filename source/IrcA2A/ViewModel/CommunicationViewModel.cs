/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using IrcA2A.Communication;
using UpbeatUI.ViewModel;

namespace IrcA2A.ViewModel
{
    public class CommunicationViewModel : IrcA2AViewModel, IDisposable
    {
        private readonly CommunicationService _communicationService;
        private readonly ObservableCollection<string> _messages;
        private string _channel;
        private string _ircServer;
        private string _nick;
        private uint _port;

        public CommunicationViewModel(IUpbeatService upbeatService, CommunicationService communicationService)
            : base(upbeatService)
        {
            _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));

            _messages = new ObservableCollection<string>(_communicationService.Messages);
            Messages = new ReadOnlyObservableCollection<string>(_messages);

            _communicationService.MessageReceived += MessageReceived;

            ConnectCommand = new DelegateCommand(ConnectAsync, () => !_communicationService.IsConnected, ShowError);
            DisconnectCommand = new DelegateCommand(DisconnectAsync, () => _communicationService.IsConnected, ShowError);
        }

        private void MessageReceived(object sender, MessageReceivedEventArgs e) =>
            Application.Current.Dispatcher.Invoke(() => _messages.Add(e.Message));

        public string Channel { get => _channel; private set => SetProperty(ref _channel, value); }
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public string IrcServer { get => _ircServer; private set => SetProperty(ref _ircServer, value); }
        public INotifyCollectionChanged Messages { get; }
        public string Nick { get => _nick; private set => SetProperty(ref _nick, value); }
        public uint Port { get => _port; private set => SetProperty(ref _port, value); }

        public void Dispose() =>
            _communicationService.MessageReceived -= MessageReceived;

        private async Task ConnectAsync()
        {
            var parameters = new ConnectViewModel.Parameters();
            await _upbeatService.OpenViewModelAsync(parameters);
            if (!parameters.IsSet)
                return;
            _messages.Clear();
            await Task.Run(() => _communicationService.Connect(parameters.IrcServerAddress, parameters.Port, parameters.ChannelName, parameters.Nickname));
            Synchronize();
            CommandManager.InvalidateRequerySuggested();
        }

        private async Task DisconnectAsync()
        {
            await Task.Run(() => _communicationService.Dispose());
            Synchronize();
            CommandManager.InvalidateRequerySuggested();
        }

        private void Synchronize()
        {
            Channel = _communicationService.Channel;
            IrcServer = _communicationService.IrcServer;
            Nick = _communicationService.Nick;
            Port = _communicationService.Port.GetValueOrDefault();
        }
    }
}
