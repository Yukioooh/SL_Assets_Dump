using System.Windows;
using System.Windows.Input;

namespace SL_Asset_Extractor.UI.Views
{
    public partial class AddCharacterDialog : Window
    {
        public string CharacterName { get; private set; } = "";

        public AddCharacterDialog()
        {
            InitializeComponent();
            Loaded += (s, e) => CharacterNameBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var name = CharacterNameBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Veuillez entrer un nom.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            CharacterName = name;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void CharacterNameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                OkButton_Click(sender, e);
            else if (e.Key == Key.Escape)
                CancelButton_Click(sender, e);
        }
    }
}