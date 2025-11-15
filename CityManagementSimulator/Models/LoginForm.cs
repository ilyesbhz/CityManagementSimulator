using System;
using System.Drawing;
using System.Windows.Forms;
using Topshelf.Runtime.Windows;

namespace CityManager
{
    public partial class LoginForm : Form
    {
        public string LoggedUser { get; private set; }

        public LoginForm()
        {
            // InitializeComponent(); // ❌ Supprimé
            BuildUI();               // ✔ On construit l'UI ici
        }

        private void BuildUI()
        {
            // --- Form ---
            this.Text = "Connexion";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;
            this.Size = new Size(1000, 600);

            // --- Left Image ---
            PictureBox pic = new PictureBox();
            pic.Image = CityManagementSimulator.Properties.Resources.login_city;
            pic.SizeMode = PictureBoxSizeMode.Zoom;
            pic.Location = new Point(30, 50);
            pic.Size = new Size(450, 500);
            this.Controls.Add(pic);

            // --- SHADOW ---
            Panel shadow = new Panel();
            shadow.BackColor = Color.FromArgb(230, 230, 230);
            shadow.Size = new Size(420, 470);
            shadow.Location = new Point(515, 65);
            shadow.Region = Region.FromHrgn(
                NativeMethods.CreateRoundRectRgn(0, 0, shadow.Width, shadow.Height, 20, 20)
            );
            this.Controls.Add(shadow);
            shadow.SendToBack();

            // --- WHITE CARD ---
            Panel card = new Panel();
            card.BackColor = Color.White;
            card.Size = new Size(400, 450);
            card.Location = new Point(525, 75);
            card.Padding = new Padding(20);
            card.Region = Region.FromHrgn(
                NativeMethods.CreateRoundRectRgn(0, 0, card.Width, card.Height, 20, 20)
            );
            this.Controls.Add(card);
            card.BringToFront();

            // TITLE
            Label lblTitle = new Label();
            lblTitle.Text = "Connectez-vous";
            lblTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(0, 140, 60);
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Height = 60;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(lblTitle);

            // ============= USERNAME FIELD WITH ICON =============
            PictureBox userIcon = new PictureBox();
            userIcon.Image = CityManagementSimulator.Properties.Resources.profile;
            userIcon.Size = new Size(28, 28);
            userIcon.SizeMode = PictureBoxSizeMode.Zoom;
            userIcon.Location = new Point(20, 90);
            card.Controls.Add(userIcon);

            TextBox txtEmail = new TextBox();
            txtEmail.Name = "txtUsername";
            txtEmail.Location = new Point(60, 90);
            txtEmail.Size = new Size(290, 30);
            txtEmail.Font = new Font("Segoe UI", 11);
            card.Controls.Add(txtEmail);

            // ============= PASSWORD FIELD WITH ICON =============
            PictureBox passIcon = new PictureBox();
            passIcon.Image = CityManagementSimulator.Properties.Resources.pass;
            passIcon.Size = new Size(28, 28);
            passIcon.SizeMode = PictureBoxSizeMode.Zoom;
            passIcon.Location = new Point(20, 150);
            card.Controls.Add(passIcon);

            TextBox txtPassword = new TextBox();
            txtPassword.Name = "txtPassword";
            txtPassword.Location = new Point(60, 150);
            txtPassword.Size = new Size(290, 30);
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.Font = new Font("Segoe UI", 11);
            card.Controls.Add(txtPassword);

            // LOGIN BUTTON
            Button btn = new Button();
            btn.Text = "Connexion";
            btn.Size = new Size(200, 45);
            btn.Location = new Point(100, 220);
            btn.BackColor = Color.FromArgb(0, 150, 60);
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            card.Controls.Add(btn);
            this.AcceptButton = btn;


            // Button action
            btn.Click += (s, e) =>
            {
                if (txtEmail.Text.Trim() == "" || txtPassword.Text.Trim() == "")
                {
                    MessageBox.Show("Veuillez entrer email et mot de passe.");
                    return;
                }

                if (txtEmail.Text == "admin" && txtPassword.Text == "admin")
                {
                    LoggedUser = txtEmail.Text;
                    this.DialogResult = DialogResult.OK;
                }
                else
                {
                    MessageBox.Show("Identifiants incorrects !");
                }
            };
        }

    }
}
