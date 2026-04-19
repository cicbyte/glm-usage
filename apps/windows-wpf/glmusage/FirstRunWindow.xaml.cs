using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace glmusage
{
    public partial class FirstRunWindow : Window
    {
        public string ApiKey { get; private set; } = string.Empty;

        public FirstRunWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApiKeyBox.Focus();
            ApiKeyBox.SelectAll();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is TextBox or Button) return;
            DragMove();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            var key = ApiKeyBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                MessageBox.Show("请输入 API Key", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                ApiKeyBox.Focus();
                return;
            }

            ApiKey = key;
            DialogResult = true;
        }
    }
}
