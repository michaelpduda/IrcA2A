/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using System.Collections.ObjectModel;

namespace IrcA2A.DataModel
{
    public class PlayedRound
    {
        public uint PlayedRoundId { get; set; }
        public DateTime Timestamp { get; set; }
        public virtual AdjectiveCard AdjectiveCard { get; set; }
        public virtual Player Judge { get; set; }
        public virtual Collection<Player> Players { get; set; }
        public virtual Collection<NounCard> PlayedCards { get; set; }
        public virtual NounCard WinningCard { get; set; }
        public virtual Player WinningPlayer { get; set; }
    }
}
