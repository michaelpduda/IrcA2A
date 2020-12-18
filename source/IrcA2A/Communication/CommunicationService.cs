/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using System.Collections.ObjectModel;
using System.Threading;
using NetIrc2;
using NetIrc2.Events;

namespace IrcA2A.Communication
{
    public class CommunicationService : IDisposable
    {
        private IrcClient _ircClient;
        private Collection<string> _messages = new Collection<string>();

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
            try
            {
                _ircClient = new IrcClient();
                _ircClient.Closed += IrcClient_Closed;
                _ircClient.Connected += IrcClient_Connected;
                _ircClient.GotChannelListBegin += IrcClient_GotChannelListBegin;
                _ircClient.GotChannelListEnd += IrcClient_GotChannelListEnd;
                _ircClient.GotChannelListEntry += IrcClient_GotChannelListEntry;
                _ircClient.GotChannelTopicChange += IrcClient_GotChannelTopicChange;
                _ircClient.GotChatAction += IrcClient_GotChatAction;
                _ircClient.GotInvitation += IrcClient_GotInvitation;
                _ircClient.GotIrcError += IrcClient_GotIrcError;
                _ircClient.GotJoinChannel += IrcClient_GotJoinChannel;
                _ircClient.GotLeaveChannel += IrcClient_GotLeaveChannel;
                _ircClient.GotMessage += IrcClient_GotMessage;
                _ircClient.GotMode += IrcClient_GotMode;
                _ircClient.GotMotdBegin += IrcClient_GotMotdBegin;
                _ircClient.GotMotdEnd += IrcClient_GotMotdEnd;
                _ircClient.GotMotdText += IrcClient_GotMotdText;
                _ircClient.GotNameChange += IrcClient_GotNameChange;
                _ircClient.GotNameListEnd += IrcClient_GotNameListEnd;
                _ircClient.GotNameListReply += IrcClient_GotNameListReply;
                _ircClient.GotNotice += IrcClient_GotNotice;
                _ircClient.GotPingReply += IrcClient_GotPingReply;
                _ircClient.GotUserKicked += IrcClient_GotUserKicked;
                _ircClient.GotUserQuit += IrcClient_GotUserQuit;
                _ircClient.GotWelcomeMessage += IrcClient_GotWelcomeMessage;
                using (var x = new EventWaiter(h => _ircClient.Connected += h, h => _ircClient.Connected -= h))
                    ThreadPool.QueueUserWorkItem(_ => _ircClient.Connect(ircServer, (int)port));
                using (var x = new EventWaiter(h => _ircClient.GotMotdEnd += h, h => _ircClient.GotMotdEnd -= h))
                    ThreadPool.QueueUserWorkItem(_ => _ircClient.LogIn(nick, nick, nick));
                using (var x = new EventWaiter<JoinLeaveEventArgs>(h => _ircClient.GotJoinChannel += h, h => _ircClient.GotJoinChannel -= h))
                    _ircClient.Join(channelName);
                IrcServer = ircServer;
                Port = port;
                Nick = nick;
                Channel = channelName;
                Connected?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        public void SendMessage(string message) =>
            _ircClient.Message(Channel, $"4●99,99 {message}");

        public void Dispose()
        {
            if (_ircClient != null)
            {
                _ircClient.Closed -= IrcClient_Closed;
                _ircClient.Connected -= IrcClient_Connected;
                _ircClient.GotChannelListBegin -= IrcClient_GotChannelListBegin;
                _ircClient.GotChannelListEnd -= IrcClient_GotChannelListEnd;
                _ircClient.GotChannelListEntry -= IrcClient_GotChannelListEntry;
                _ircClient.GotChannelTopicChange -= IrcClient_GotChannelTopicChange;
                _ircClient.GotChatAction -= IrcClient_GotChatAction;
                _ircClient.GotInvitation -= IrcClient_GotInvitation;
                _ircClient.GotIrcError -= IrcClient_GotIrcError;
                _ircClient.GotJoinChannel -= IrcClient_GotJoinChannel;
                _ircClient.GotLeaveChannel -= IrcClient_GotLeaveChannel;
                _ircClient.GotMessage -= IrcClient_GotMessage;
                _ircClient.GotMode -= IrcClient_GotMode;
                _ircClient.GotMotdBegin -= IrcClient_GotMotdBegin;
                _ircClient.GotMotdEnd -= IrcClient_GotMotdEnd;
                _ircClient.GotMotdText -= IrcClient_GotMotdText;
                _ircClient.GotNameChange -= IrcClient_GotNameChange;
                _ircClient.GotNameListEnd -= IrcClient_GotNameListEnd;
                _ircClient.GotNameListReply -= IrcClient_GotNameListReply;
                _ircClient.GotNotice -= IrcClient_GotNotice;
                _ircClient.GotPingReply -= IrcClient_GotPingReply;
                _ircClient.GotUserKicked -= IrcClient_GotUserKicked;
                _ircClient.GotUserQuit -= IrcClient_GotUserQuit;
                _ircClient.GotWelcomeMessage -= IrcClient_GotWelcomeMessage;
                _ircClient.LogOut();
                _ircClient.Close();
                _ircClient = null;
            }
            IrcServer = null;
            Port = null;
            Nick = null;
            Channel = null;
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public MessageReceiver Open() =>
            new MessageReceiver(_ircClient ?? throw new InvalidOperationException($"{nameof(CommunicationService)} is not connected."));

        private void AddMessage(string message)
        {
            _messages.Add(message);
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs { Message = message });
        }

        public void SendNotice(string recipient, string message) =>
            _ircClient.Notice(recipient, message);

        private void IrcClient_Closed(object sender, EventArgs e) =>
            AddMessage($"Closed");
        
        private void IrcClient_Connected(object sender, EventArgs e) =>
            AddMessage($"Connected");
        
        private void IrcClient_GotChannelListBegin(object sender, EventArgs e) =>
            AddMessage($"GotChannelListBegin");

        private void IrcClient_GotChannelListEnd(object sender, EventArgs e) =>
            AddMessage($"GotChannelListEnd");

        private void IrcClient_GotChannelListEntry(object sender, ChannelListEntryEventArgs e) =>
            AddMessage($"GotChannelListEntry:   {e.Channel} {e.UserCount} {e.Topic}");

        private void IrcClient_GotChannelTopicChange(object sender, ChannelTopicChangeEventArgs e) =>
            AddMessage($"GotChannelTopicChange: {e.Channel} {e.NewTopic}");

        private void IrcClient_GotChatAction(object sender, ChatMessageEventArgs e) =>
            AddMessage($"GotChatAction:         {e.Sender.Nickname} {e.Recipient} '{e.Message}'");

        private void IrcClient_GotInvitation(object sender, InvitationEventArgs e) =>
            AddMessage($"GotInvitation:         {e.Sender}.Nickname {e.Recipient} {e.Channel}");

        private void IrcClient_GotIrcError(object sender, IrcErrorEventArgs e) =>
            AddMessage($"GotIrcError:           {e.Error} {e.Data}");

        private void IrcClient_GotJoinChannel(object sender, JoinLeaveEventArgs e) =>
            AddMessage($"GotJoinChannel:        {e.Identity.Nickname}");

        private void IrcClient_GotLeaveChannel(object sender, JoinLeaveEventArgs e) =>
            AddMessage($"GotLeaveChannel:       {e.Identity.Nickname}");

        private void IrcClient_GotMessage(object sender, ChatMessageEventArgs e) =>
            AddMessage($"GotMessage:            {e.Sender.Nickname} {e.Recipient} '{e.Message}'");

        private void IrcClient_GotMode(object sender, ModeEventArgs e) =>
            AddMessage($"GotMode:               {e.Sender.Nickname} {e.Recipient} {e.Command} {e.ParameterCount}");

        private void IrcClient_GotMotdBegin(object sender, EventArgs e) =>
            AddMessage($"GotMotdBegin");

        private void IrcClient_GotMotdEnd(object sender, EventArgs e) =>
            AddMessage($"GotMotdEnd");

        private void IrcClient_GotMotdText(object sender, SimpleMessageEventArgs e) =>
            AddMessage($"GotMotdText:           '{e.Message}'");

        private void IrcClient_GotNameChange(object sender, NameChangeEventArgs e) =>
            AddMessage($"GotNameChange:         {e.Identity.Nickname} {e.NewName}");

        private void IrcClient_GotNameListEnd(object sender, NameListEndEventArgs e) =>
            AddMessage($"GotNameListEnd:        {e.Channel}");

        private void IrcClient_GotNameListReply(object sender, NameListReplyEventArgs e) =>
            AddMessage($"GotNameListReply:      {e.Channel}");

        private void IrcClient_GotNotice(object sender, ChatMessageEventArgs e) =>
            AddMessage($"GotNotice:             {e.Sender.Nickname} {e.Recipient} {e.Message}");

        private void IrcClient_GotPingReply(object sender, PingReplyEventArgs e) =>
            AddMessage($"GotPingReply:          {e.Identity.Nickname} {e.Delay}");

        private void IrcClient_GotUserKicked(object sender, KickEventArgs e) =>
            AddMessage($"GotUserKicked:         {e.Sender.Nickname} {e.Recipient} {e.Channel} '{e.Reason}'");

        private void IrcClient_GotUserQuit(object sender, QuitEventArgs e) =>
            AddMessage($"GotUserQuit:           {e.Identity.Nickname} '{e.QuitMessage}'");

        private void IrcClient_GotWelcomeMessage(object sender, SimpleMessageEventArgs e) =>
            AddMessage($"GotWelcomeMessage:     '{e.Message}'");
    }
}
