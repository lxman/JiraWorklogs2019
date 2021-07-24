using Community.VisualStudio.Toolkit;
using JiraWorklogs2019.ToolWindows;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace JiraWorklogs2019.Commands
{
    [Command(PackageIds.JWCommand)]
    internal sealed class JWToolWindowCommand : BaseCommand<JWToolWindowCommand>
    {
        protected override Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            return JWToolWindow.ShowAsync();
        }
    }
}
