using System.Windows;

namespace JiraWorklogs2019.ToolWindows
{
    public class SearchEventArgs : RoutedEventArgs
    {
        private readonly string SearchKeyInternal;

        public string SearchKey
        {
            get => SearchKeyInternal;
        }

        public SearchEventArgs(RoutedEvent routedEvent, string searchKey) : base(routedEvent)
        {
            SearchKeyInternal = searchKey;
        }
    }
}