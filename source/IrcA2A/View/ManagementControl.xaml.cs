/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Media;
using UpbeatUI.View;

namespace IrcA2A.View
{
    public partial class ManagementControl : UpbeatControl
    {
        public ManagementControl()
        {
            InitializeComponent();
            (_ircMessages.Items.SourceCollection as INotifyCollectionChanged).CollectionChanged += ScrollToBottom;
        }

        private void ScrollToBottom(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (VisualTreeHelper.GetChildrenCount(_ircMessages) > 0)
            {
                var border = (Border)VisualTreeHelper.GetChild(_ircMessages, 0);
                var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }
        }
    }
}
