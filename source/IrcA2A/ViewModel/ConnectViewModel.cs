/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System.Windows.Input;
using UpbeatUI.ViewModel;

namespace IrcA2A.ViewModel
{
    public class ConnectViewModel : IrcA2AViewModel
    {
        private string _channelName = "#";
        private string _ircServerAddress = "irc.";
        private string _nickname = "A2ABot";
        private uint _port = 6667;

        public ConnectViewModel(IUpbeatService upbeatService, Parameters parameters)
            : base(upbeatService)
        {
            StartCommand = new DelegateCommand(
                () =>
                {
                    parameters.IsSet = true;
                    parameters.ChannelName = ChannelName;
                    parameters.IrcServerAddress = IrcServerAddress;
                    parameters.Nickname = Nickname;
                    parameters.Port = Port;
                    _upbeatService.Close();
                },
                () => !string.IsNullOrWhiteSpace(ChannelName)
                    && !string.IsNullOrWhiteSpace(IrcServerAddress)
                    && !string.IsNullOrWhiteSpace(Nickname)
                    && ChannelName != "#",
                ShowError);
        }

        public string ChannelName { get => _channelName; set => SetProperty(ref _channelName, value); }
        public string IrcServerAddress { get => _ircServerAddress; set => SetProperty(ref _ircServerAddress, value); }
        public string Nickname { get => _nickname; set => SetProperty(ref _nickname, value); }
        public uint Port { get => _port; set => SetProperty(ref _port, value); }

        public ICommand StartCommand { get; }

        public class Parameters
        {
            public string ChannelName { get; internal set; }
            public string IrcServerAddress { get; internal set; }
            public bool IsSet { get; internal set; }
            public string Nickname { get; internal set; }
            public uint Port { get; internal set; }
        }
    }
}
