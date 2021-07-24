using Atlassian.Jira;
using Community.VisualStudio.Toolkit;
using DevExpress.Xpf.Editors;
using JiraWorklogs2019.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Windows;
using System.Windows.Controls;

namespace JiraWorklogs2019.ToolWindows
{
    public partial class JiraWorklogControl : UserControl
    {
        public ObservableCollection<string> ViewList { get; set; } = new();

        public ObservableCollection<Viewbox> CurrentView { get; set; } = new();

        public bool EnableRetrieve
        {
            get => !string.IsNullOrEmpty(JiraWorklogs2019Options.Instance.ServerUri)
                && !string.IsNullOrEmpty(JiraWorklogs2019Options.Instance.UserName)
                && !string.IsNullOrEmpty(PasswordBox.Password);
        }

        private readonly List<ActiveIssue> ActiveIssues = new();
        private readonly List<Viewbox> FoundIssues = new();

        private JiraAccess Access;
        private int LastSearchIndex;
        private string CurrentSearchText = string.Empty;
        private readonly string LocalAppdataFolder;
        private readonly string JiraWorklogsFolder;
        private readonly string ActiveIssuesFile;

        public JiraWorklogControl()
        {
            InitializeComponent();
            LocalAppdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            JiraWorklogsFolder = Path.Combine(LocalAppdataFolder, "JiraWorklogs");
            ActiveIssuesFile = Path.Combine(JiraWorklogsFolder, "ActiveIssues.json");
            if (!Directory.Exists(JiraWorklogsFolder))
            {
                _ = Directory.CreateDirectory(JiraWorklogsFolder);
            }
            else if (File.Exists(ActiveIssuesFile))
            {
                ActiveIssues = ReadActiveIssues();
                File.Delete(ActiveIssuesFile);
            }
            DataContext = this;
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
            JiraWorklogs2019Options.Instance.PropertyChanged += OptionsPropertyChanged;
            RetrieveButton.IsEnabled = EnableRetrieve;
        }

        private void OptionsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RetrieveButton.IsEnabled = EnableRetrieve;
        }

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            List<ActiveIssue> issues = new();
            (StackPanel2.Children[0] as ItemsControl)?.Items.OfType<Viewbox>().ToList().ForEach(viewbox =>
            {
                if (((JiraEntryControl)viewbox.Child).WhenStarted is null) return;
                JiraEntryControl entry = (JiraEntryControl)viewbox.Child;
                ActiveIssue issue = new()
                {
                    IssueKey = entry.Issue.Key.Value,
                    WhenStarted = (DateTime)entry.WhenStarted
                };
                entry.Closing();
                issues.Add(issue);
            });
            if (issues.Count == 0) return;
            WriteActiveIssues(issues);
        }

        private void WriteActiveIssues(IReadOnlyCollection<ActiveIssue> issues)
        {
            if (File.Exists(ActiveIssuesFile)) File.Delete(ActiveIssuesFile);
            File.WriteAllText(ActiveIssuesFile, JsonConvert.SerializeObject(issues));
        }

        private List<ActiveIssue> ReadActiveIssues()
        {
            return File.Exists(ActiveIssuesFile)
                ? JsonConvert.DeserializeObject<List<ActiveIssue>>(File.ReadAllText(ActiveIssuesFile))
                : (new());
        }

        private async void RetrieveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EnableRetrieve)
            {
                bool needUri = string.IsNullOrEmpty(JiraWorklogs2019Options.Instance.ServerUri);
                bool needName = string.IsNullOrEmpty(JiraWorklogs2019Options.Instance.UserName);
                return;
            }
            RetrieveButton.Content = "Retrieving . . .";
            RetrieveButton.IsEnabled = false;

            try
            {
                Access = new JiraAccess(
                    JiraWorklogs2019Options.Instance.ServerUri,
                    JiraWorklogs2019Options.Instance.UserName,
                    PasswordBox.Password);
                await Access.GetDataAsync();
                ViewList.Clear();
                List<string> statuses = Access.GetStatuses().ToList();
                statuses.ForEach(s =>
                {
                    ViewList.Add(s);
                });
                ViewSelect.SelectAllItems();
                ConstructView(statuses);
            }
            catch (AuthenticationException)
            {
                _ = VS.MessageBox.Show("A response was received that indicated a failure to authenticate.", "");
            }
            catch (InvalidOperationException)
            {
                _ = VS.MessageBox.Show("We received an invalid operation exception.", "Confirm that the Uri is correct.");
            }

            RetrieveButton.Content = "Retrieve";
            RetrieveButton.IsEnabled = true;
        }

        private void ConstructView(List<string> visibles)
        {
            CurrentView.Clear();
            visibles.ForEach(v =>
            {
                List<Issue> results = Access.GetIssuesForStatus(v).ToList();
                results.ForEach(r =>
                {
                    Viewbox vb = new()
                    {
                        StretchDirection = StretchDirection.Both,
                        Stretch = System.Windows.Media.Stretch.Uniform
                    };
                    JiraEntryControl je = new()
                    {
                        Width = Width,
                        Issue = r,
                        Original = r.TimeTrackingData.OriginalEstimateInSeconds ?? 0,
                        Remaining = r.TimeTrackingData.RemainingEstimateInSeconds ?? 0,
                        Logged = r.TimeTrackingData.TimeSpentInSeconds ?? 0
                    };
                    if (ActiveIssues.Select(ai => ai.IssueKey).Contains(r.Key.Value))
                    {
                        je.WhenStarted = ActiveIssues.FirstOrDefault(ai => ai.IssueKey == r.Key.Value)?.WhenStarted;
                    }
                    vb.Child = je;
                    CurrentView.Add(vb);
                });
            });
        }

        private void ViewSelect_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            List<object> selected = e.NewValue as List<object>;
            List<string> items = new();
            selected?.ForEach(i => items.Add(i.ToString()));
            ConstructView(items);
            LastSearchIndex = 0;
            CurrentSearchText = string.Empty;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            RetrieveButton.IsEnabled = EnableRetrieve;
        }

        private Point? GetItemPositionInScroller(Viewbox vb)
        {
            if (vb is not FrameworkElement element) return null;
            var transform = element.TransformToVisual(IssuesListView);
            var position = transform.Transform(new Point(0, 0));
            return position;
        }

        private void SearchControl_SearchClicked(object sender, SearchEventArgs e)
        {
            if (CurrentSearchText.ToLower() != e.SearchKey.ToLower())
            {
                string searchTerm = e.SearchKey.ToLower();
                FoundIssues.Clear();
                FoundIssues.AddRange(CurrentView.Where(v =>
                {
                    Issue issue = ((JiraEntryControl)v.Child).Issue;
                    return issue.Key.Value.ToLower().Contains(searchTerm)
                    || issue.Status.Name.ToLower().Contains(searchTerm)
                    || issue.Summary.ToLower().Contains(searchTerm);
                }));
                if (FoundIssues.Count == 0)
                {
                    CurrentSearchText = string.Empty;
                    return;
                }
                FocusItem(FoundIssues[0]);
                LastSearchIndex = 0;
                CurrentSearchText = e.SearchKey;
            }
            else
            {
                LastSearchIndex++;
                if (LastSearchIndex < FoundIssues.Count) FocusItem(FoundIssues[LastSearchIndex]);
                else if (FoundIssues.Count > 0)
                {
                    LastSearchIndex = 0;
                    FocusItem(FoundIssues[LastSearchIndex]);
                }
            }
        }

        private void FocusItem(Viewbox vb)
        {
            Point? p = GetItemPositionInScroller(vb);
            if (p is null)
            {
                CurrentSearchText = string.Empty;
                return;
            }
            IssuesListView.ScrollToVerticalOffset(IssuesListView.VerticalOffset + p.Value.Y);
            (vb.Child as JiraEntryControl).FlashBorder();
        }
    }
}