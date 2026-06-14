using System.Windows;
using Engeradios.Desktop.Helpers; // Certifique-se de que tem o ficheiro ConfigSecurity neste namespace

namespace Engeradios.Desktop
{
    public partial class SetupWindow : Window
    {
        public SetupWindow()
        {
            InitializeComponent();
        }

        private void BtnAtivar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtApiKey.Text))
            {
                MessageBox.Show("Por favor, introduza uma chave válida.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Guarda a chave encriptada de forma segura
            ConfigSecurity.SalvarApiKey(TxtApiKey.Text.Trim());

            MessageBox.Show("Configuração efetuada com sucesso! O sistema reiniciará os serviços de nuvem.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

            this.DialogResult = true;
            this.Close();
        }
    }
}