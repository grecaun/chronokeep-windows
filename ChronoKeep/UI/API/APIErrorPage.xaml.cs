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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChronoKeep.UI.API
{
    /// <summary>
    /// Interaction logic for APIErrorPage.xaml
    /// </summary>
    public partial class APIErrorPage : Page
    {
        APIWindow window;
        public APIErrorPage(APIWindow window, bool noAPI)
        {
            InitializeComponent();
            this.window = window;
            if (noAPI)
            {
                errorLabel.Text = "An API must be set up before you can use this tool.";
            }
        }
    }
}
