/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using System.Collections.ObjectModel;

namespace IrcA2A.DataModel
{
    public class PlayerLinkage
    {
        public Guid PlayerLinkageId { get; set; }
        public virtual Collection<Player> LinkedPlayers { get; set; }
    }
}
