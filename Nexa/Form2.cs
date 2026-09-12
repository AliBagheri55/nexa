using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Nexa
{
    public partial class CreateAnAccount : Form
    {
        private string selectedPhotoPath = "";
        private string captchaCode;

        string connectionString =
            @"Server=.;Database=Nexa;Trusted_Connection=True;TrustServerCertificate=True;";

        public CreateAnAccount()
        {
            InitializeComponent();
        }

        private void CreateAnAccount_Load(object sender, EventArgs e)
        {
            GenerateCaptcha();
        }

        private void GenerateCaptcha()
        {
            const string chars =
                "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

            Random random = new Random();

            captchaCode = "";

            for (int i = 0; i < 5; i++)
            {
                captchaCode +=
                    chars[random.Next(chars.Length)];
            }

            Bitmap bitmap = new Bitmap(
                picSecurityCode.Width,
                picSecurityCode.Height
            );

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);

                using (Font font =
                    new Font("Arial", 24, FontStyle.Bold))
                {
                    g.DrawString(
                        captchaCode,
                        font,
                        Brushes.Black,
                        20,
                        10
                    );
                }
            }

            if (picSecurityCode.Image != null)
            {
                picSecurityCode.Image.Dispose();
            }

            picSecurityCode.Image = bitmap;
        }

        private void btnProfilePhoto_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog =
                new OpenFileDialog())
            {
                openFileDialog.Title = "انتخاب عکس";

                openFileDialog.Filter =
                    "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

                openFileDialog.Multiselect = false;

                if (openFileDialog.ShowDialog() ==
                    DialogResult.OK)
                {
                    selectedPhotoPath =
                        openFileDialog.FileName;

                    if (picPhoto.Image != null)
                    {
                        picPhoto.Image.Dispose();
                        picPhoto.Image = null;
                    }

                    using (Image tempImage =
                        Image.FromFile(selectedPhotoPath))
                    {
                        picPhoto.Image =
                            new Bitmap(tempImage);
                    }

                    picPhoto.SizeMode =
                        PictureBoxSizeMode.Zoom;
                }
            }
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            if (txtSecurityCode.Text.Trim() != captchaCode)
            {
                MessageBox.Show(
                    "کد کپچا اشتباه است!");

                txtSecurityCode.Clear();

                GenerateCaptcha();

                return;
            }

            if (txtPassword.Text !=
                txtRepeatPassword.Text)
            {
                MessageBox.Show(
                    "مقادیر وارد شده یکسان نیستند!");

                return;
            }

            using (SqlConnection con =
                new SqlConnection(connectionString))
            {
                try
                {
                    con.Open();

                    string checkQuery = @"
                        SELECT COUNT(*)
                        FROM Users
                        WHERE YourID = @YourID";

                    using (SqlCommand checkCmd =
                        new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.Add(
                            "@YourID",
                            SqlDbType.NVarChar,
                            50
                        ).Value =
                            txtId.Text.Trim();

                        int count =
                            Convert.ToInt32(
                                checkCmd.ExecuteScalar());

                        if (count > 0)
                        {
                            MessageBox.Show(
                                "Id تکراری است.");

                            return;
                        }
                    }

                    string insertQuery = @"
                        INSERT INTO Users
                        (
                            FirstAndLastName,
                            YourID,
                            PhoneNumber,
                            Password,
                            Bio,
                            ProfilePhoto
                        )
                        VALUES
                        (
                            @FirstAndLastName,
                            @YourID,
                            @PhoneNumber,
                            @Password,
                            @Bio,
                            @ProfilePhoto
                        )";

                    using (SqlCommand insertCmd =
                        new SqlCommand(insertQuery, con))
                    {
                        insertCmd.Parameters.Add(
                            "@FirstAndLastName",
                            SqlDbType.NVarChar,
                            100
                        ).Value =
                            txtFirstAndLastName.Text.Trim();

                        insertCmd.Parameters.Add(
                            "@YourID",
                            SqlDbType.NVarChar,
                            50
                        ).Value =
                            txtId.Text.Trim();

                        insertCmd.Parameters.Add(
                            "@PhoneNumber",
                            SqlDbType.NVarChar,
                            50
                        ).Value =
                            txtPhoneNumber.Text.Trim();

                        insertCmd.Parameters.Add(
                            "@Password",
                            SqlDbType.NVarChar,
                            255
                        ).Value =
                            txtPassword.Text;

                        insertCmd.Parameters.Add(
                            "@Bio",
                            SqlDbType.NVarChar,
                            500
                        ).Value =
                            string.IsNullOrWhiteSpace(
                                txtBio.Text)
                            ? (object)DBNull.Value
                            : txtBio.Text.Trim();

                        if (!string.IsNullOrWhiteSpace(
                            selectedPhotoPath))
                        {
                            insertCmd.Parameters.Add(
                                "@ProfilePhoto",
                                SqlDbType.NVarChar,
                                -1
                            ).Value =
                                selectedPhotoPath;
                        }
                        else
                        {
                            insertCmd.Parameters.Add(
                                "@ProfilePhoto",
                                SqlDbType.NVarChar,
                                -1
                            ).Value =
                                DBNull.Value;
                        }

                        insertCmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "خطا در ساخت حساب:\n\n" +
                        ex.Message);

                    return;
                }
            }

            Random random = new Random();

            string code;

            using (SqlConnection con =
                new SqlConnection(connectionString))
            {
                try
                {
                    con.Open();

                    while (true)
                    {
                        code =
                            random.Next(
                                100000,
                                1000000
                            ).ToString();

                        string checkQuery = @"
                            SELECT COUNT(*)
                            FROM Users
                            WHERE Code = @Code";

                        using (SqlCommand checkCmd =
                            new SqlCommand(
                                checkQuery,
                                con))
                        {
                            checkCmd.Parameters.Add(
                                "@Code",
                                SqlDbType.NVarChar,
                                50
                            ).Value = code;

                            int count =
                                Convert.ToInt32(
                                    checkCmd.ExecuteScalar());

                            if (count == 0)
                            {
                                break;
                            }
                        }
                    }

                    string updateQuery = @"
                        UPDATE Users
                        SET Code = @Code
                        WHERE YourID = @YourID";

                    using (SqlCommand cmd =
                        new SqlCommand(
                            updateQuery,
                            con))
                    {
                        cmd.Parameters.Add(
                            "@Code",
                            SqlDbType.NVarChar,
                            50
                        ).Value = code;

                        cmd.Parameters.Add(
                            "@YourID",
                            SqlDbType.NVarChar,
                            50
                        ).Value =
                            txtId.Text.Trim();

                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show(
                        "کد بازیابی حساب شما: " +
                        code +
                        "\n\nاین کد را در جای امن نگه دارید.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "خطا در ساخت کد بازیابی:\n\n" +
                        ex.Message);

                    return;
                }
            }

            Hide();

            frmLogin login = new frmLogin();

            login.ShowDialog();
        }

        private void lbllBack_LinkClicked(
            object sender,
            LinkLabelLinkClickedEventArgs e)
        {
            Hide();

            frmLogin login = new frmLogin();

            login.ShowDialog();
        }

        private void txtFirstAndLastName_KeyPress(
            object sender,
            KeyPressEventArgs e)
        {
            if (char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void txtPhoneNumber_KeyPress(
            object sender,
            KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) &&
                !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}