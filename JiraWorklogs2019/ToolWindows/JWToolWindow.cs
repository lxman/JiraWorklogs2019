using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace JiraWorklogs2019.ToolWindows
{
    public class JWToolWindow : BaseToolWindow<JWToolWindow>
    {
        public override string GetTitle(int toolWindowId) => "Jira Worklogs";
        public override Type PaneType => typeof(Pane);

        public override async Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            return new JiraWorklogControl();
        }

        [Guid("4a0363d0-029a-4578-a75d-aad0e05593e1")]
        internal class Pane : ToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}