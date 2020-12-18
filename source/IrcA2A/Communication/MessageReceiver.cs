/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using System.Collections.Concurrent;
using System.Threading;
using NetIrc2;
using NetIrc2.Events;

namespace IrcA2A.Communication
{
    public class MessageReceiver : IDisposable
    {
        private readonly IrcClient _ircClient;
        private readonly BlockingCollection<Received> _receivedMessages = new BlockingCollection<Received>();
        private Received _failedMessage;

        public MessageReceiver(IrcClient ircClient)
        {
            _ircClient = ircClient ?? throw new ArgumentNullException(nameof(ircClient));
            _ircClient.GotLeaveChannel += IrcClientGotLeaveChannel;
            _ircClient.GotMessage += IrcClientGotMessage;
            _ircClient.GotNameChange += IrcClientGotNameChange;
            _ircClient.GotUserKicked += IrcClientGotUserKick;
        }

        public void Dispose()
        {
            _ircClient.GotLeaveChannel -= IrcClientGotLeaveChannel;
            _ircClient.GotMessage -= IrcClientGotMessage;
            _ircClient.GotNameChange -= IrcClientGotNameChange;
            _ircClient.GotUserKicked -= IrcClientGotUserKick;
        }

        public void GetMessage(CancellationToken cancellationToken, Action<Received> processor)
        {
            Received received = null;
            try
            {
                received = _failedMessage ?? _receivedMessages.Take(cancellationToken);
            }
            catch (OperationCanceledException) { }
            _failedMessage = null;
            try
            {
                processor(received);
            }
            catch (Exception)
            {
                _failedMessage = received;
                throw;
            }
        }

        private void IrcClientGotMessage(object sender, ChatMessageEventArgs e) =>
            _receivedMessages.Add(new ReceivedMessage { Sender = e.Sender.Nickname, Message = e.Message });

        private void IrcClientGotNameChange(object sender, NameChangeEventArgs e) =>
            _receivedMessages.Add(new ReceivedNickChange { Sender = e.Identity.Nickname, NewNick = e.NewName });

        private void IrcClientGotLeaveChannel(object sender, JoinLeaveEventArgs e) =>
            _receivedMessages.Add(new ReceivedLeft { Sender = e.Identity.Nickname });

        private void IrcClientGotUserKick(object sender, KickEventArgs e) =>
            _receivedMessages.Add(new ReceivedLeft { Sender = e.Recipient });
    }
}
