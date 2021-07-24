using Community.VisualStudio.Toolkit;
using JiraWorklogs2019.Options;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using JiraWorklogs2019.Commands;
using JiraWorklogs2019.ToolWindows;
using Task = System.Threading.Tasks.Task;

namespace JiraWorklogs2019
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideToolWindow(typeof(JWToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.SolutionExplorer)]
    [ProvideOptionPage(typeof(OptionsProvider.JiraWorklogs2019OptionsOptions), "Jira Worklogs", "General", 0, 0, true)]
    [ProvideProfile(typeof(OptionsProvider.JiraWorklogs2019OptionsOptions), "Jira Worklogs", "General", 0, 0, true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.JiraWorklogs2019String)]
    public sealed class JiraWorklogs2019Package : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            JWToolWindow.Initialize(this);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await JWToolWindowCommand.InitializeAsync(this);
        }
    }
}