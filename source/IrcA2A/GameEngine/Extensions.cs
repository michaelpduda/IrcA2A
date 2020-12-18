/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace IrcA2A.GameEngine
{
    internal static class Extensions
    {
        internal static string AsName(this string name) =>
            $"10{name}99,99";

        internal static string AsAdjective(this string adjective) =>
            $"9,99▒1,9{adjective}9,99▒99,99";

        internal static string AsNoun(this string noun) =>
            $"5,99▒0,5{noun}5,99▒99,99";

        internal static void ShuffeFrom<T>(this Queue<T> destination, IEnumerable<T> source)
        {
            foreach (var item in source.ToShuffled())
                destination.Enqueue(item);
        }

        internal static string AsJoinedNouns(this IEnumerable<string> source) =>
            String.Join(", ", source.Select((s, i) => $"2[{i + 1}]99,99 {s.AsNoun()}"));

        internal static IEnumerable<T> ToShuffled<T>(this IEnumerable<T> source)
        {
            var all = source.ToList();
            var random = new Random(DateTime.UtcNow.Millisecond);
            while (all.Count > 0)
            {
                var index = random.Next(0, all.Count);
                yield return all[index];
                all.RemoveAt(index);
            }
        }
    }
}
