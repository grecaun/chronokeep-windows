using Chronokeep.Objects;
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

namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for APIPage1.xaml
    /// </summary>
    public partial class APIPage1 : Page
    {
        APIWindow window;
        IDBInterface database;
        Dictionary<string, ResultsAPI> apiDict;

        public APIPage1(APIWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;

            AppSetting last_api = database.GetAppSetting(Constants.Settings.LAST_USED_API_ID);
            List<ResultsAPI> apis = database.GetAllResultsAPI();
            apiDict = new Dictionary<string, ResultsAPI>();
            int api_id = -1;
            if (last_api != null)
            {
                try
                {
                    api_id = Convert.ToInt32(last_api.value);
                }
                catch
                {
                    api_id = -1;
                }
            }
            int ix = 0;
            int count = 0;
            foreach (ResultsAPI api in apis)
            {
                apiDict[api.Identifier.ToString()] = api;
                APIBox.Items.Add(new ComboBoxItem
                {
                    Content = api.Nickname,
                    Uid = api.Identifier.ToString()
                });
                if (api_id > 0 && api_id == api.Identifier)
                {
                    ix = count;
                }
                count++;
            }
            APIBox.SelectedIndex = ix;
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            window.GotoPage2(apiDict[((ComboBoxItem)APIBox.SelectedItem).Uid]);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            window.Close();
        }
    }
}
