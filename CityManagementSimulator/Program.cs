using System;
using System.Windows.Forms;
using CityManager;

namespace CityManagementSimulator
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1. Show login window
            LoginForm login = new LoginForm();
            DialogResult result = login.ShowDialog();

            // 2. If login is OK → open MainForm
            if (result == DialogResult.OK)
            {
                Application.Run(new MainForm());
            }
            else
            {
                // 3. Exit the app if login cancelled or failed
                Application.Exit();
            }
        }
    }
}
