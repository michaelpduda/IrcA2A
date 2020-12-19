/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System.Linq;
using Meebey.SmartIrc4net;

namespace IrcA2A.Communication
{
    internal static class Extensions
    {
        public static string ToRaw(this IrcMessageData target) =>
            $"(Raw: {string.Join(", ", target.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(target)))})";
    }
}
