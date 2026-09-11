using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Nexa
{
    public partial class frmLogin : Form
    {
        string connectionString = @"Server=.;Database=Nexa;Trusted_Connection=True;TrustServerCertificate=True;";

        public frmLogin()
        {
            InitializeComponent();
        }

        private void txtUserName_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void lbllCreateanaccount_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Hide();

            CreateAnAccount account = new CreateAnAccount();
            account.ShowDialog();

            this.Show();
        }

        private void frmLogin_Load(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = true;
            int radius = 20;

            GraphicsPath path = new GraphicsPath();

            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(btnLogin.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(btnLogin.Width - radius, btnLogin.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, btnLogin.Height - radius, radius, radius, 90, 90);

            path.CloseFigure();

            btnLogin.Region = new Region(path);
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUserName.Text) ||
                string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show(
                    "نام کاربری و رمز عبور را وارد کنید.",
                    "خطا",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT Id, FirstAndLastName, PhoneNumber
                    FROM Users
                    WHERE FirstAndLastName = @UserName
                    AND Password = @Password";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@UserName", SqlDbType.NVarChar, 100)
                        .Value = txtUserName.Text.Trim();

                    cmd.Parameters.Add("@Password", SqlDbType.NVarChar, 255)
                        .Value = txtPassword.Text;

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            CurrentUser.Id = Convert.ToInt32(reader["Id"]);
                            CurrentUser.Username = reader["FirstAndLastName"].ToString();
                            CurrentUser.PhoneNumber = reader["PhoneNumber"].ToString();

                            this.Hide();

                            Nexa nexa = new Nexa();
                            nexa.Show();
                        }
                        else
                        {
                            MessageBox.Show(
                                "نام کاربری یا رمز عبور اشتباه است!",
                                "خطا",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                        }
                    }
                }
            }
        }

        private void lbllForgotPassword_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Hide();

            ForgotPassword password = new ForgotPassword();
            password.ShowDialog();

            this.Show();
        }

        private void btnSeed_Click(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar =
                !txtPassword.UseSystemPasswordChar;
        }

        private void txtUserName_MouseEnter(object sender, EventArgs e)
        {
        }

        private void txtUserName_MouseLeave(object sender, EventArgs e)
        {
        }
    }
}