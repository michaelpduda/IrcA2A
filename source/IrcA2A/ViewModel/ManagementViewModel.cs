/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using IrcA2A.DataContext;
using UpbeatUI.ViewModel;

namespace IrcA2A.ViewModel
{
    public class ManagementViewModel : IrcA2AViewModel, IDisposable
    {
        private readonly ContextService _contextService;

        public ManagementViewModel(IUpbeatService upbeatService, ContextService contextService)
            : base(upbeatService)
        {
            _contextService = contextService ?? throw new ArgumentNullException(nameof(contextService));

            HistoryViewModel = new HistoryViewModel(_upbeatService, _contextService);
        }

        public HistoryViewModel HistoryViewModel { get; }

        public void Dispose()
        {
            HistoryViewModel.Dispose();
        }

        public class Parameters
        {
            public string[] Args { get; init; }
        }
    }
}
