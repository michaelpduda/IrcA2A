/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using UpbeatUI.ViewModel;

namespace IrcA2A.ViewModel
{
    public class MessageViewModel : BaseViewModel
    {
        public MessageViewModel(Parameters parameters) =>
            Message = parameters.Message;

        public string Message { get; }

        public class Parameters
        {
            public string Message { get; init; }
        }
    }
}
