using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace AIDungeon_Extension
{
    /// <summary>
    /// SaveAccountWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SaveAccountWindow : Window
    {
        public SaveAccountWindow()
        {
            InitializeComponent();

            UpdateValidateSaveButton();
            this.idBox.Focus();
        }

        public static void RemoveSavedAccount()
        {
            var filePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "account");
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }
        public static bool GetSavedAccount_ID(out string id)
        {
            id = string.Empty;
            var filePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "account");
            if (System.IO.File.Exists(filePath))
            {
                var lines = System.IO.File.ReadAllLines(filePath);
                if (lines.Length != 2) return false;

                id = MachineKeyProtect.Unprotect(lines[0], "id");
                if (!string.IsNullOrEmpty(id))
                    return true;
            }
            return false;
        }
        public static bool GetSavedAccount_Password(out string password)
        {
            password = string.Empty;
            var filePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "account");
            if (System.IO.File.Exists(filePath))
            {
                var lines = System.IO.File.ReadAllLines(filePath);
                if (lines.Length != 2) return false;

                password = MachineKeyProtect.Unprotect(lines[1], "pw");
                if (!string.IsNullOrEmpty(password))
                    return true;
            }
            return false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var filePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "account");
            var id_protected = MachineKeyProtect.Protect(idBox.Text, "id");
            var pw_protected = MachineKeyProtect.Protect(passwordBox.Password, "pw");

            System.IO.File.WriteAllText(filePath, id_protected + System.Environment.NewLine + pw_protected);

            this.Close();
        }

        private void UpdateValidateSaveButton()
        {
            this.saveButton.IsEnabled = !string.IsNullOrEmpty(this.idBox.Text) && !string.IsNullOrEmpty(this.passwordBox.Password);
        }

        private void idBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateValidateSaveButton();
        }
        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdateValidateSaveButton();
        }

        private void box_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (this.saveButton.IsEnabled)
                    SaveButton_Click(sender, null);
            }
        }
    }
}
