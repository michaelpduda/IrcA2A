/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;

namespace IrcA2A.GameEngine
{
    public class ExceptionEventArgs : EventArgs
    {
        public Exception Exception { get; init; }
    }
}
