using Atlassian.Jira;
using Community.VisualStudio.Toolkit;
using JiraWorklogs2019.Options;
using Microsoft.VisualStudio;
using System;
using System.ComponentModel;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace JiraWorklogs2019.ToolWindows
{
    /// <summary>
    /// Interaction logic for JiraEntryControl.xaml
    /// </summary>
    public partial class JiraEntryControl : UserControl
    {
        public Issue Issue
        {
            get => InternalIssue;

            set
            {
                JiraKeyLabel.Content = $"{value.Key}";
                JiraKeyLabel.ToolTip = $"{value.Summary}";
                JiraStatusLabel.Content = $"{value.Status.Name}";
                JiraStatusLabel.ToolTip = $"{value.Summary}";
                OverallLabel.Content = value.TimeTrackingData.OriginalEstimateInSeconds ?? 0;
                RemainingLabel.Content = value.TimeTrackingData.RemainingEstimateInSeconds ?? 0;
                ElapsedLabel.Content = value.TimeTrackingData.TimeSpentInSeconds ?? 0;
                InternalIssue = value;
            }
        }

        //public string SummaryText => JiraSummary.Text;
        public event EventHandler Expiring;

        [Browsable(false)]
        public long Original
        {
            get => OriginalInternal;

            set
            {
                OriginalInternal = value;
                ElementSet(nameof(Original));
            }
        }

        [Browsable(false)]
        public long Remaining
        {
            private get => RemainingInternal;

            set
            {
                RemainingInternal = value;
                ElementSet(nameof(Remaining));
            }
        }

        [Browsable(false)]
        public long Logged
        {
            private get => LoggedInternal;

            set
            {
                LoggedInternal = value;
                ElementSet(nameof(Logged));
            }
        }

        [Browsable(false)]
        public long Spent
        {
            get => SpentInternal;

            set
            {
                SpentInternal = value;
                ElementSet(nameof(Spent));
            }
        }

        [Browsable(false)]
        private string ElapsedString => SecondsToTime(Spent);

        [Browsable(false)]
        public DateTime? WhenStarted
        {
            get => WhenStartedInternal;
            set
            {
                if (value is null)
                {
                    Seconds.Stop();
                    CancelToken.Cancel();
                    WhenStartedInternal = null;
                }
                else
                {
                    TimeSpan timeLost = (TimeSpan)(DateTime.Now - value);
                    Remaining -= (long)timeLost.TotalSeconds;
                    Spent += (long)timeLost.TotalSeconds;
                    Seconds.Start();
                    StartButton.Content = "Stop";
                    WhenStartedInternal = value;
                }
            }
        }

        private readonly System.Timers.Timer Seconds = new(1000);
        private readonly System.Timers.Timer FlashTimer = new(500);
        private int FlashCount;
        private long OriginalInternal;
        private long SpentInternal;
        private long RemainingInternal;
        private long LoggedInternal;
        private DateTime? WhenStartedInternal;
        private bool RemainingSet;
        private bool OriginalSet;
        private bool LoggedSet;
        private float BarScale;
        private readonly CancellationTokenSource CancelToken = new();

        private Issue InternalIssue;
        private readonly Brush ControlBackground;

        public JiraEntryControl()
        {
            InitializeComponent();
            Seconds.Elapsed += Tick;
            FlashTimer.Elapsed += FlashTimer_Elapsed;
            FlashTimer.AutoReset = true;
            Seconds.AutoReset = true;
            BorderBrush = new SolidColorBrush(Colors.Red);
            ControlBackground = Background;
        }

        public void FlashBorder()
        {
            BorderThickness = new Thickness(2);
            FlashCount = 0;
            FlashTimer.Start();
        }

        private void FlashTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            FlashCount++;
            Dispatcher.Invoke(new Action(() =>
            {
                BorderThickness = new Thickness((FlashCount % 2 == 0) ? 2 : 0);
            }));
            if (FlashCount == 3)
            {
                FlashTimer.Stop();
            }
        }

        private void Tick(object sender, ElapsedEventArgs e)
        {
            Remaining--;
            Spent++;
            if (Remaining == 300) Expiring?.Invoke(this, EventArgs.Empty);
            Dispatcher.Invoke(new Action(() =>
            {
                if (CancelToken.Token.IsCancellationRequested) return;
                RemainingLabel.Content = SecondsToTime(Remaining);
                ElapsedLabel.Content = SecondsToTime(Logged + Spent);
                RemainingBar.Value = (int)(Remaining / BarScale);
                ElapsedBar.Value = (int)((Logged + Spent) / BarScale);
                Background = new SolidColorBrush(CalcBackground());
            }),
            DispatcherPriority.Normal,
            CancelToken.Token);
        }

        private Color CalcBackground()
        {
            float intensity = (float)(Original - Remaining) / Original;
            if (intensity < 0) intensity = 0;
            if (intensity > 1) intensity = 1;
            return Color.FromArgb((byte)(intensity * 255), Colors.Red.R, Colors.Red.G, Colors.Red.B);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (StartButton.Content.ToString() == "Start")
            {
                StartButton.Content = "Stop";
                WhenStarted = DateTime.Now;
            }
            else
            {
                Background = ControlBackground;
                StartButton.Content = "Start";
                WhenStarted = null;
                PromptForLog();
            }
        }

        private async void PromptForLog()
        {
            if (JiraWorklogs2019Options.Instance.ConfirmLog)
            {
                VSConstants.MessageBoxResult result =
                    VS.MessageBox.Show($"Do you wish to log {ElapsedString} to {Issue.Key}?",
                        "",
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_QUERY,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_YESNO);
                if (result == VSConstants.MessageBoxResult.IDNO) return;
            }
            try
            {
                Worklog wl = new((Spent > 60) ? ElapsedString : "1m", DateTime.Now)
                {
                    Author = Issue.AssigneeUser.DisplayName
                };
                await Issue.AddWorklogAsync(wl);
                Issue.SaveChanges();
            }
            catch (Exception ex)
            {
                VS.MessageBox.Show(ex.Message);
            }
        }

        private void ElementSet(string name)
        {
            switch (name)
            {
                case nameof(Original):
                    OriginalSet = true;
                    BarScale = Original / 100f;
                    break;

                case nameof(Logged):
                    LoggedSet = true;
                    break;

                case nameof(Remaining):
                    RemainingSet = true;
                    break;

                default:
                    break;
            }

            if (!OriginalSet || !LoggedSet || !RemainingSet) return;

            Dispatcher.Invoke(new Action(() =>
            {
                if (CancelToken.Token.IsCancellationRequested) return;
                OverallLabel.Content = SecondsToTime(Original);
                RemainingLabel.Content = SecondsToTime(Remaining);
                ElapsedLabel.Content = SecondsToTime(Logged);
                RemainingBar.Value = (int)(Remaining / BarScale);
                ElapsedBar.Value = (int)(Logged / BarScale);
                OverallBar.Value = 100;
            }), DispatcherPriority.Normal,
            CancelToken.Token);
        }

        public void Closing()
        {
            Seconds.Stop();
            CancelToken.Cancel();
        }

        private static string SecondsToTime(long secs)
        {
            int weeks = (int)(secs / 144000);
            secs -= (weeks * 144000);
            int days = (int)(secs / 28800);
            secs -= (days * 28800);
            int hours = (int)(secs / 3600);
            secs -= (hours * 3600);
            int minutes = (int)(secs / 60);
            secs -= (minutes * 60);
            if (secs > 0) minutes++;
            if (minutes > 59)
            {
                hours++;
                minutes -= 60;
            }

            if (hours > 7)
            {
                days++;
                hours -= 8;
            }

            if (days > 4)
            {
                weeks++;
                days -= 5;
            }
            string retVal = string.Empty;
            if (weeks > 0) retVal = $"{weeks}w";
            if (days > 0) retVal = $"{retVal}{(retVal.Length > 0 ? " " : string.Empty)}{days}d";
            if (hours > 0) retVal = $"{retVal}{(retVal.Length > 0 ? " " : string.Empty)}{hours}h";
            if (minutes > 0) retVal = $"{retVal}{(retVal.Length > 0 ? " " : string.Empty)}{minutes}m";
            return retVal;
        }
    }
}