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

namespace Wpf_pr2_kiri
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private void ComboLang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboLang.SelectedItem is ComboBoxItem selectedItem)
            {
                string lang = selectedItem.Tag.ToString();
                ChangeLanguage(lang);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void ChangeLanguage(string lang)
        {
            ResourceDictionary dict = new ResourceDictionary();

            // Шлях до файлів. Переконайся, що папка Resources називається саме так!
            dict.Source = new Uri($"/Resources/Lang.{lang}.xaml", UriKind.Relative);

            // Видаляємо стару мову і додаємо нову
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }   
    }
}
