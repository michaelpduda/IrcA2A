/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using System.Windows.Input;
using UpbeatUI.ViewModel;

namespace IrcA2A.ViewModel
{
    public class InputViewModel : IrcA2AViewModel
    {
        private Parameters _parameters;

        public InputViewModel(IUpbeatService upbeatService, Parameters parameters)
            : base(upbeatService)
        {
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            CompleteCommand = new DelegateCommand<string>(
                s =>
                {
                    _parameters.ReturnedInput = s;
                    _upbeatService.Close();
                },
                s => !string.IsNullOrWhiteSpace(s),
                ShowError);
        }

        public string CommandName => _parameters.CommandName;
        public ICommand CompleteCommand { get; }
        public string EntryMessage => $"Enter {_parameters.TypeName} cards below, separated by new lines.";

        public class Parameters
        {
            public string CommandName { get; init; }
            public string ReturnedInput { get; internal set; }
            public string TypeName { get; init; }
        }
    }
}
