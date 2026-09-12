using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net;
using System.Windows.Forms;

namespace Nexa
{
    public partial class frmLogin : Form
    {
        string connectionString =
            @"Server=.;Database=Nexa;Trusted_Connection=True;TrustServerCertificate=True;";

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

        private void lbllCreateanaccount_LinkClicked(
            object sender,
            LinkLabelLinkClickedEventArgs e)
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

            path.AddArc(
                0,
                0,
                radius,
                radius,
                180,
                90);

            path.AddArc(
                btnLogin.Width - radius,
                0,
                radius,
                radius,
                270,
                90);

            path.AddArc(
                btnLogin.Width - radius,
                btnLogin.Height - radius,
                radius,
                radius,
                0,
                90);

            path.AddArc(
                0,
                btnLogin.Height - radius,
                radius,
                radius,
                90,
                90);

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
                    MessageBoxIcon.Warning);

                return;
            }

            int userId = 0;
            string username = "";
            string phoneNumber = "";
            bool loginSuccess = false;

            using (SqlConnection con =
                   new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT
                        Id,
                        FirstAndLastName,
                        PhoneNumber
                    FROM Users
                    WHERE FirstAndLastName = @UserName
                    AND Password = @Password";

                using (SqlCommand cmd =
                       new SqlCommand(query, con))
                {
                    cmd.Parameters.Add(
                        "@UserName",
                        SqlDbType.NVarChar,
                        100).Value =
                        txtUserName.Text.Trim();

                    cmd.Parameters.Add(
                        "@Password",
                        SqlDbType.NVarChar,
                        255).Value =
                        txtPassword.Text;

                    con.Open();

                    using (SqlDataReader reader =
                           cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            userId =
                                Convert.ToInt32(
                                    reader["Id"]);

                            username =
                                reader["FirstAndLastName"]
                                .ToString();

                            phoneNumber =
                                reader["PhoneNumber"]
                                .ToString();

                            loginSuccess = true;
                        }
                    }
                }

                // اگر ورود ناموفق بود،
                // بررسی می‌کنیم آیا خود کاربر وجود دارد یا نه.
                if (!loginSuccess)
                {
                    string findUserQuery = @"
                        SELECT Id
                        FROM Users
                        WHERE FirstAndLastName = @UserName";

                    using (SqlCommand findCmd =
                           new SqlCommand(
                               findUserQuery,
                               con))
                    {
                        findCmd.Parameters.Add(
                            "@UserName",
                            SqlDbType.NVarChar,
                            100).Value =
                            txtUserName.Text.Trim();

                        object result =
                            findCmd.ExecuteScalar();

                        if (result != null &&
                            result != DBNull.Value)
                        {
                            userId =
                                Convert.ToInt32(result);
                        }
                    }
                }
            }

            if (loginSuccess)
            {
                CurrentUser.Id = userId;
                CurrentUser.Username = username;
                CurrentUser.PhoneNumber = phoneNumber;

                // ثبت ورود موفق
                LogLogin(
                    userId,
                    "Success",
                    "Password");

                this.Hide();

                Nexa nexa = new Nexa();
                nexa.Show();
            }
            else
            {
                // ثبت ورود ناموفق
                LogLogin(
                    userId,
                    "Failed",
                    "Password");

                MessageBox.Show(
                    "نام کاربری یا رمز عبور اشتباه است!",
                    "خطا",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LogLogin(
            int userId,
            string loginStatus,
            string loginType)
        {
            try
            {
                string deviceName =
                    Environment.MachineName;

                string operatingSystem =
                    Environment.OSVersion
                    .VersionString;

                string ipAddress =
                    GetLocalIPAddress();

                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                        INSERT INTO LoginHistory
                        (
                            UserId,
                            LoginDate,
                            LoginStatus,
                            DeviceName,
                            OperatingSystem,
                            IPAddress,
                            LoginType
                        )
                        VALUES
                        (
                            @UserId,
                            GETDATE(),
                            @LoginStatus,
                            @DeviceName,
                            @OperatingSystem,
                            @IPAddress,
                            @LoginType
                        )";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        if (userId > 0)
                        {
                            cmd.Parameters.Add(
                                "@UserId",
                                SqlDbType.Int).Value =
                                userId;
                        }
                        else
                        {
                            cmd.Parameters.Add(
                                "@UserId",
                                SqlDbType.Int).Value =
                                DBNull.Value;
                        }

                        cmd.Parameters.Add(
                            "@LoginStatus",
                            SqlDbType.NVarChar,
                            20).Value =
                            loginStatus;

                        cmd.Parameters.Add(
                            "@DeviceName",
                            SqlDbType.NVarChar,
                            255).Value =
                            deviceName;

                        cmd.Parameters.Add(
                            "@OperatingSystem",
                            SqlDbType.NVarChar,
                            255).Value =
                            operatingSystem;

                        cmd.Parameters.Add(
                            "@IPAddress",
                            SqlDbType.NVarChar,
                            50).Value =
                            ipAddress;

                        cmd.Parameters.Add(
                            "@LoginType",
                            SqlDbType.NVarChar,
                            50).Value =
                            loginType;

                        con.Open();

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // خطای ثبت LoginHistory نباید
                // مانع ورود کاربر به Nexa شود.
            }
        }

        private string GetLocalIPAddress()
        {
            try
            {
                string hostName =
                    Dns.GetHostName();

                IPAddress[] addresses =
                    Dns.GetHostAddresses(hostName);

                foreach (IPAddress address in addresses)
                {
                    if (address.AddressFamily ==
                        System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return address.ToString();
                    }
                }
            }
            catch
            {
            }

            return "Unknown";
        }

        private void lbllForgotPassword_LinkClicked(
            object sender,
            LinkLabelLinkClickedEventArgs e)
        {
            this.Hide();

            ForgotPassword password =
                new ForgotPassword();

            password.ShowDialog();

            this.Show();
        }

        private void btnSeed_Click(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar =
                !txtPassword.UseSystemPasswordChar;
        }

        private void txtUserName_MouseEnter(
            object sender,
            EventArgs e)
        {
        }

        private void txtUserName_MouseLeave(
            object sender,
            EventArgs e)
        {
        }
    }
}