using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.UIObjects
{
    /// <summary>
    /// Interaction logic for DialogBox.xaml
    /// </summary>
    public partial class DialogBox : FluentWindow
    {
        public delegate void LeftClickDelegate();

        private string CopyText = "";

        public DialogBox(string Message, string LeftButtonContent, string RightButtonContent, bool ShowLeftButton, LeftClickDelegate LeftClick)
        {
            InitializeComponent();
            MessageBox.Text = Message;
            LeftButton.Content = LeftButtonContent;
            RightButton.Content = RightButtonContent;
            if (ShowLeftButton )
            {
                LeftButton.Visibility = Visibility.Visible;
            }
            else
            {
                LeftButton.Visibility = Visibility.Hidden;
            }
            LeftButton.Click += (sender, e) =>
            {
                Close();
                LeftClick();
            };
            RightButton.Click += (sender, e) =>
            {
                Close();
            };
            this.MinWidth = 400.0;
            this.Width = 400.0;
            this.MinHeight = 200.0;
            this.Height = 200.0;
            this.Topmost = true;
        }

        public static void Show(string Message)
        {
            DialogBox output = new DialogBox(
                Message,
                "",
                "OK",
                false,
                () =>{ }
                );
            output.ShowDialog();
        }

        public static void Show(string Message, string LeftButtonContent, string RightButtonContent, LeftClickDelegate LeftClick)
        {
            DialogBox output = new DialogBox(
                Message,
                LeftButtonContent,
                RightButtonContent,
                true,
                LeftClick
                );
            output.ShowDialog();
        }

        public static void Show(string Message, string CopyText)
        {
            DialogBox output = new DialogBox(
                Message,
                "",
                "OK",
                false,
                () => { }
                );
            output.CopyText = CopyText;
            output.CopyBox.Text = CopyText;
            output.CopyBox.Visibility = Visibility.Visible;
            output.Width = 500.0;
            output.ShowDialog();
        }

        private void CopyBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CopyBox.Text = CopyText;
        }
    }
}
