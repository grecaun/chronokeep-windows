using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Chronokeep.UI.UIObjects
{
    /// <summary>
    /// Interaction logic for DialogBox.xaml
    /// </summary>
    public partial class DialogBox : UiWindow
    {
        public delegate void LeftClickDelegate();

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
    }
}
