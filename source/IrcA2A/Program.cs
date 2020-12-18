/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System.Windows;
using IrcA2A.DataContext;
using IrcA2A.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UpbeatUI.Extensions.Hosting;
using UpbeatUI.View;

namespace IrcA2A
{
    public class Program
    {
        private static void Main(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, serviceCollection) => serviceCollection
                    .AddSingleton<ContextService>())
                .ConfigureUpbeatHost(() => new ManagementViewModel.Parameters { Args = args }, hostedUpbeatBuilder => hostedUpbeatBuilder
                    .ConfigureWindow(() => new UpbeatMainWindow
                    {
                        Title = "IrcA2A Manager",
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    }))
                .Build()
                .Run();
    }
}
