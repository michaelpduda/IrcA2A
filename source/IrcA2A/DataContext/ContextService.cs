/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using IrcA2A.DataModel;
using Microsoft.EntityFrameworkCore;

namespace IrcA2A.DataContext
{
    public class ContextService
    {
        public event EventHandler<SavedChangesEventArgs> Updated;

        public A2AContext Open() =>
            new A2AContext(OnSavedChanges);

        protected void OnSavedChanges(object sender, SavedChangesEventArgs e) =>
            Updated?.Invoke(sender, e);
    }
}
