using System.Windows;
using System.Windows.Controls;

namespace JiraWorklogs2019.ToolWindows
{
    /// <summary>
    /// Interaction logic for SearchControl.xaml
    /// </summary>
    public partial class SearchControl : UserControl
    {
        public delegate void SearchEventHandler(object sender, SearchEventArgs e);

        public static readonly RoutedEvent SearchClickedEvent =
            EventManager.RegisterRoutedEvent(
                "SearchClickedEvent",
                RoutingStrategy.Bubble,
                typeof(SearchEventHandler),
                typeof(SearchControl));

        public event SearchEventHandler SearchClicked
        {
            add { AddHandler(SearchClickedEvent, value); }
            remove { RemoveHandler(SearchClickedEvent, value); }
        }

        public SearchControl()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new SearchEventArgs(SearchClickedEvent, SearchKey.Text));
        }
    }
}