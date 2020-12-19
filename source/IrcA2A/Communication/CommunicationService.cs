/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Meebey.SmartIrc4net;

namespace IrcA2A.Communication
{
    public class CommunicationService : IDisposable
    {
        private readonly Collection<string> _messages = new Collection<string>();
        private ManualResetEvent _ended = new ManualResetEvent(false);
        private IrcClient _ircClient;
        private int _messageWidth;

        public CommunicationService() =>
            Messages = new ReadOnlyCollection<string>(_messages);

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public string Channel { get; private set; }
        public string IrcServer { get; private set; }
        public bool IsConnected => _ircClient != null;
        public ReadOnlyCollection<string> Messages { get; }
        public uint? Port { get; private set; }
        public string Nick { get; private set; }

        public void Connect(string ircServer, uint port, string channelName, string nick)
        {
            if (_ircClient != null)
                throw new InvalidOperationException("Already connected");
            _messages.Clear();
            _ircClient = new IrcClient();
            _ircClient.OnAutoConnectError += IrcClientOnAutoConnectError;
            _ircClient.OnAway += IrcClientOnAway;
            _ircClient.OnBan += IrcClientOnBan;
            _ircClient.OnBanException += IrcClientOnBanException;
            _ircClient.OnBounce += IrcClientOnBounce;
            _ircClient.OnChannelAction += IrcClientOnChannelAction;
            _ircClient.OnChannelActiveSynced += IrcClientOnChannelActiveSynced;
            _ircClient.OnChannelAdmin += IrcClientOnChannelAdmin;
            _ircClient.OnChannelMessage += IrcClientOnChannelMessage;
            _ircClient.OnChannelModeChange += IrcClientOnChannelModeChange;
            _ircClient.OnChannelNotice += IrcClientOnChannelNotice;
            _ircClient.OnChannelPassiveSynced += IrcClientOnChannelPassiveSynced;
            _ircClient.OnConnected += IrcClientOnConnected;
            _ircClient.OnConnecting += IrcClientOnConnecting;
            _ircClient.OnConnectionError += IrcClientOnConnectionError;
            _ircClient.OnCtcpReply += IrcClientOnCtcpReply;
            _ircClient.OnCtcpRequest += IrcClientOnCtcpRequest;
            _ircClient.OnDeChannelAdmin += IrcClientOnDeChannelAdmin;
            _ircClient.OnDehalfop += IrcClientOnDehalfop;
            _ircClient.OnDeop += IrcClientOnDeop;
            _ircClient.OnDeowner += IrcClientOnDeowner;
            _ircClient.OnDevoice += IrcClientOnDevoice;
            _ircClient.OnDisconnected += IrcClientOnDisconnected;
            _ircClient.OnDisconnecting += IrcClientOnDisconnecting;
            _ircClient.OnError += IrcClientOnError;
            _ircClient.OnErrorMessage += IrcClientOnErrorMessage;
            _ircClient.OnHalfop += IrcClientOnHalfop;
            _ircClient.OnInvite += IrcClientOnInvite;
            _ircClient.OnInviteException += IrcClientOnInviteException;
            _ircClient.OnJoin += IrcClientOnJoin;
            _ircClient.OnKick += IrcClientOnKick;
            _ircClient.OnList += IrcClientOnList;
            _ircClient.OnModeChange += IrcClientOnModeChange;
            _ircClient.OnMotd += IrcClientOnMotd;
            _ircClient.OnNames += IrcClientOnNames;
            _ircClient.OnNickChange += IrcClientOnNickChange;
            _ircClient.OnNowAway += IrcClientOnNowAway;
            _ircClient.OnOp += IrcClientOnOp;
            _ircClient.OnOwner += IrcClientOnOwner;
            _ircClient.OnPart += IrcClientOnPart;
            _ircClient.OnPing += IrcClientOnPing;
            _ircClient.OnPong += IrcClientOnPong;
            _ircClient.OnQueryAction += IrcClientOnQueryAction;
            _ircClient.OnQueryMessage += IrcClientOnQueryMessage;
            _ircClient.OnQueryNotice += IrcClientOnQueryNotice;
            _ircClient.OnQuit += IrcClientOnQuit;
            _ircClient.OnRegistered += IrcClientOnRegistered;
            _ircClient.OnTopic += IrcClientOnTopic;
            _ircClient.OnTopicChange += IrcClientOnTopicChange;
            _ircClient.OnUnAway += IrcClientOnUnAway;
            _ircClient.OnUnBanException += IrcClientOnUnBanException;
            _ircClient.OnUnInviteException += IrcClientOnUnInviteException;
            _ircClient.OnUnban += IrcClientOnUnban;
            _ircClient.OnUserModeChange += IrcClientOnUserModeChange;
            _ircClient.OnVoice += IrcClientOnVoice;
            _ircClient.OnWho += IrcClientOnWho;
            _ircClient.OnWriteLine += IrcClientOnWriteLine;
            try
            {
                _ircClient.Connect(ircServer, (int)port);
                _ircClient.Login(nick, $"{nick} Bot");
                _ircClient.RfcJoin(channelName);
                ThreadPool.QueueUserWorkItem(
                    _ =>
                    {
                        try
                        {
                            _ircClient.Listen();
                        }
                        catch (Exception) { }
                        _ended.Set();
                    });
                IrcServer = ircServer;
                Port = port;
                Nick = nick;
                Channel = channelName;
                Connected?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (_ircClient != null)
            {
                _ircClient.RfcQuit();
                _ended.WaitOne();
                _ircClient.OnAutoConnectError -= IrcClientOnAutoConnectError;
                _ircClient.OnAway -= IrcClientOnAway;
                _ircClient.OnBan -= IrcClientOnBan;
                _ircClient.OnBanException -= IrcClientOnBanException;
                _ircClient.OnBounce -= IrcClientOnBounce;
                _ircClient.OnChannelAction -= IrcClientOnChannelAction;
                _ircClient.OnChannelActiveSynced -= IrcClientOnChannelActiveSynced;
                _ircClient.OnChannelAdmin -= IrcClientOnChannelAdmin;
                _ircClient.OnChannelMessage -= IrcClientOnChannelMessage;
                _ircClient.OnChannelModeChange -= IrcClientOnChannelModeChange;
                _ircClient.OnChannelNotice -= IrcClientOnChannelNotice;
                _ircClient.OnChannelPassiveSynced -= IrcClientOnChannelPassiveSynced;
                _ircClient.OnConnected -= IrcClientOnConnected;
                _ircClient.OnConnecting -= IrcClientOnConnecting;
                _ircClient.OnConnectionError -= IrcClientOnConnectionError;
                _ircClient.OnCtcpReply -= IrcClientOnCtcpReply;
                _ircClient.OnCtcpRequest -= IrcClientOnCtcpRequest;
                _ircClient.OnDeChannelAdmin -= IrcClientOnDeChannelAdmin;
                _ircClient.OnDehalfop -= IrcClientOnDehalfop;
                _ircClient.OnDeop -= IrcClientOnDeop;
                _ircClient.OnDeowner -= IrcClientOnDeowner;
                _ircClient.OnDevoice -= IrcClientOnDevoice;
                _ircClient.OnDisconnected -= IrcClientOnDisconnected;
                _ircClient.OnDisconnecting -= IrcClientOnDisconnecting;
                _ircClient.OnError -= IrcClientOnError;
                _ircClient.OnErrorMessage -= IrcClientOnErrorMessage;
                _ircClient.OnHalfop -= IrcClientOnHalfop;
                _ircClient.OnInvite -= IrcClientOnInvite;
                _ircClient.OnInviteException -= IrcClientOnInviteException;
                _ircClient.OnJoin -= IrcClientOnJoin;
                _ircClient.OnKick -= IrcClientOnKick;
                _ircClient.OnList -= IrcClientOnList;
                _ircClient.OnModeChange -= IrcClientOnModeChange;
                _ircClient.OnMotd -= IrcClientOnMotd;
                _ircClient.OnNames -= IrcClientOnNames;
                _ircClient.OnNickChange -= IrcClientOnNickChange;
                _ircClient.OnNowAway -= IrcClientOnNowAway;
                _ircClient.OnOp -= IrcClientOnOp;
                _ircClient.OnOwner -= IrcClientOnOwner;
                _ircClient.OnPart -= IrcClientOnPart;
                _ircClient.OnPing -= IrcClientOnPing;
                _ircClient.OnPong -= IrcClientOnPong;
                _ircClient.OnQueryAction -= IrcClientOnQueryAction;
                _ircClient.OnQueryMessage -= IrcClientOnQueryMessage;
                _ircClient.OnQueryNotice -= IrcClientOnQueryNotice;
                _ircClient.OnQuit -= IrcClientOnQuit;
                _ircClient.OnRegistered -= IrcClientOnRegistered;
                _ircClient.OnTopic -= IrcClientOnTopic;
                _ircClient.OnTopicChange -= IrcClientOnTopicChange;
                _ircClient.OnUnAway -= IrcClientOnUnAway;
                _ircClient.OnUnBanException -= IrcClientOnUnBanException;
                _ircClient.OnUnInviteException -= IrcClientOnUnInviteException;
                _ircClient.OnUnban -= IrcClientOnUnban;
                _ircClient.OnUserModeChange -= IrcClientOnUserModeChange;
                _ircClient.OnVoice -= IrcClientOnVoice;
                _ircClient.OnWho -= IrcClientOnWho;
                _ircClient.OnWriteLine -= IrcClientOnWriteLine;
                _ircClient = null;
            }
            _ended.Reset();
            IrcServer = null;
            Port = null;
            Nick = null;
            Channel = null;
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public MessageReceiver Open() =>
            new MessageReceiver(_ircClient ?? throw new InvalidOperationException($"{nameof(CommunicationService)} is not connected."));

        public void SendMessage(string message) =>
            _ircClient.SendMessage(SendType.Message, Channel, $"4●99,99 {message}");

        public void SendNotice(string recipient, string message) =>
            _ircClient.SendMessage(SendType.Notice, recipient, message);

        private void AddMessage(string message, IrcMessageData ircMessageData = null, [CallerMemberName] string messageType = "***")
        {
            _messageWidth = Math.Max(_messageWidth, message.Length);
            var fullMessage = $"{messageType.Replace("IrcClientOn", "").PadRight(20)} {message.PadRight(_messageWidth)} {ircMessageData?.ToRaw() ?? ""}";
            _messages.Add(fullMessage);
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs { Message = fullMessage });
        }

        private void IrcClientOnAutoConnectError(object sender, AutoConnectErrorEventArgs e) =>
            AddMessage($"{e.Exception.GetType()}, Port: {e.Port}, {e.Exception.Message}");

        private void IrcClientOnAway(object sender, AwayEventArgs e) =>
            AddMessage($"{e.Who} '{e.AwayMessage}'", e.Data);

        private void IrcClientOnBan(object sender, BanEventArgs e) =>
            AddMessage($"{e.Channel} {e.Who} -> {e.Hostmask}", e.Data);

        private void IrcClientOnBanException(object sender, BanEventArgs e) =>
            AddMessage($"{e.Channel} {e.Who} -> {e.Hostmask}", e.Data);

        private void IrcClientOnBounce(object sender, BounceEventArgs e) =>
            AddMessage($"{e.Server} {e.Port}", e.Data);

        private void IrcClientOnChannelAction(object sender, ActionEventArgs e) =>
            AddMessage($"{e.Data.Channel} '{e.Data.Nick} {e.ActionMessage}'", e.Data);

        private void IrcClientOnChannelActiveSynced(object sender, IrcEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnChannelAdmin(object sender, ChannelAdminEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnChannelMessage(object sender, IrcEventArgs e) =>
            AddMessage($"{e.Data.Channel} {e.Data.Nick} '{e.Data.Message}'", e.Data);

        private void IrcClientOnChannelModeChange(object sender, ChannelModeChangeEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnChannelNotice(object sender, IrcEventArgs e) =>
            AddMessage($"{e.Data.Channel} '{e.Data.Message}'", e.Data);

        private void IrcClientOnChannelPassiveSynced(object sender, IrcEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnConnected(object sender, EventArgs e) =>
            AddMessage($"Connected:");

        private void IrcClientOnConnecting(object sender, EventArgs e) =>
            AddMessage($"Connecting");

        private void IrcClientOnConnectionError(object sender, EventArgs e) =>
            AddMessage($"ConnectionError");

        private void IrcClientOnCtcpReply(object sender, IrcEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnCtcpRequest(object sender, IrcEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnDeChannelAdmin(object sender, DeChannelAdminEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnDehalfop(object sender, DehalfopEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnDeop(object sender, DeopEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnDeowner(object sender, DeownerEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnDevoice(object sender, DevoiceEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnDisconnected(object sender, EventArgs e) =>
            AddMessage($"");

        private void IrcClientOnDisconnecting(object sender, EventArgs e) =>
            AddMessage($"");

        private void IrcClientOnError(object sender, ErrorEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnErrorMessage(object sender, IrcEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnHalfop(object sender, HalfopEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnInvite(object sender, InviteEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnInviteException(object sender, IrcEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnJoin(object sender, JoinEventArgs e) =>
            AddMessage($"{e.Channel} {e.Who}", e.Data);

        private void IrcClientOnKick(object sender, KickEventArgs e) =>
            AddMessage($"{e.Channel} {e.Who} -> {e.Whom} '{e.KickReason}'", e.Data);

        private void IrcClientOnList(object sender, ListEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnModeChange(object sender, IrcEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnMotd(object sender, MotdEventArgs e) =>
            AddMessage($"{e.Data.Message}", e.Data);

        private void IrcClientOnNames(object sender, NamesEventArgs e) =>
            AddMessage($"{e.Channel} {string.Join(", ", e.UserList)}", e.Data);

        private void IrcClientOnNickChange(object sender, NickChangeEventArgs e) =>
            AddMessage($"{e.OldNickname} -> {e.NewNickname}", e.Data);

        private void IrcClientOnNowAway(object sender, IrcEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnOp(object sender, OpEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnOwner(object sender, OwnerEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnPart(object sender, PartEventArgs e) =>
            AddMessage($"{e.Channel} {e.Who} '{e.PartMessage}'", e.Data);

        private void IrcClientOnPing(object sender, PingEventArgs e) =>
            AddMessage($"{e.PingData}", e.Data);

        private void IrcClientOnPong(object sender, PongEventArgs e) =>
            AddMessage($"Lag: {e.Lag}", e.Data);

        private void IrcClientOnQueryAction(object sender, IrcEventArgs e) =>
            AddMessage($"{e.Data.Channel} {e.Data.Nick} '{e.Data.Message}'", e.Data);

        private void IrcClientOnQueryMessage(object sender, IrcEventArgs e) =>
            AddMessage($"{e.Data.Channel} {e.Data.Nick} {e.Data.Message}", e.Data);

        private void IrcClientOnQueryNotice(object sender, IrcEventArgs e) =>
            AddMessage($"{e.Data.Channel} {e.Data.Nick} {e.Data.Message}", e.Data);

        private void IrcClientOnQuit(object sender, QuitEventArgs e) =>
            AddMessage($"{e.Who} '{e.QuitMessage}'", e.Data);

        private void IrcClientOnRegistered(object sender, EventArgs e) =>
            AddMessage($"Registered");

        private void IrcClientOnTopic(object sender, TopicEventArgs e) =>
            AddMessage($"{e.Channel} '{e.Topic}'", e.Data);

        private void IrcClientOnTopicChange(object sender, TopicChangeEventArgs e) =>
            AddMessage($"{e.Channel} {e.Who} '{e.NewTopic}'", e.Data);

        private void IrcClientOnUnAway(object sender, IrcEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnUnBanException(object sender, IrcEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnUnInviteException(object sender, IrcEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnUnban(object sender, UnbanEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnUserModeChange(object sender, IrcEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnVoice(object sender, VoiceEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnWho(object sender, WhoEventArgs e) =>
            AddMessage($"", e.Data);

        private void IrcClientOnWriteLine(object sender, WriteLineEventArgs e) =>
            AddMessage($"{e.Line}");
    }
}
