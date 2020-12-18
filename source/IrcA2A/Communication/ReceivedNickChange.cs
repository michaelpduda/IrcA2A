/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
namespace IrcA2A.Communication
{
    public class ReceivedNickChange : Received
    {
        public string NewNick { get; init; }
    }
}
