/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using System.Windows;
using UpbeatUI.ViewModel;

namespace IrcA2A.ViewModel
{
    public class IrcA2AViewModel : BaseViewModel
    {
        protected readonly IUpbeatService _upbeatService;

        public IrcA2AViewModel(IUpbeatService upbeatService) =>
            _upbeatService = upbeatService ?? throw new ArgumentNullException(nameof(upbeatService));

        protected void ShowError(Exception e) =>
            Application.Current?.Dispatcher?.Invoke(
                () => _upbeatService.OpenViewModel(
                    new MessageViewModel.Parameters { Message = $"Exception: {e.GetType().Name}\n\n{e.Message}\n\n{e.StackTrace.Replace("   ", "")}" }));
    }
}
