using Community.VisualStudio.Toolkit;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JiraWorklogs2019.Options
{
    internal partial class OptionsProvider
    {
        // Register the options with these attributes on your package class:
        // [ProvideOptionPage(typeof(OptionsProvider.JiraWorklogs2019OptionsOptions), "JiraWorklogs2019.Options", "JiraWorklogs2019Options", 0, 0, true)]
        // [ProvideProfile(typeof(OptionsProvider.JiraWorklogs2019OptionsOptions), "JiraWorklogs2019.Options", "JiraWorklogs2019Options", 0, 0, true)]
        public class JiraWorklogs2019OptionsOptions : BaseOptionPage<JiraWorklogs2019Options> { }
    }

    public class JiraWorklogs2019Options : BaseOptionModel<JiraWorklogs2019Options>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string ServerUriInternal = string.Empty;
        private string UserNameInternal = string.Empty;
        private bool ConfirmLogInternal = true;

        [Category("Connectivity")]
        [DisplayName("Server URI")]
        [Description("Address of the Jira server")]
        public string ServerUri
        {
            get => ServerUriInternal;
            set
            {
                ServerUriInternal = value;
                OnPropertyChanged(nameof(ServerUri));
            }
        }

        [Category("Connectivity")]
        [DisplayName("UserName")]
        [Description("User name to log into the server")]
        public string UserName
        {
            get => UserNameInternal;
            set
            {
                UserNameInternal = value;
                OnPropertyChanged(nameof(UserName));
            }
        }

        [Category("Options")]
        [DisplayName("Prompt For Log")]
        [Description("Prompt before creating a log entry")]
        public bool ConfirmLog
        {
            get => ConfirmLogInternal;
            set
            {
                ConfirmLogInternal = value;
                OnPropertyChanged(nameof(ConfirmLog));
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
