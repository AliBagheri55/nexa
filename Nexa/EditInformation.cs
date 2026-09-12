using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Nexa
{
    public partial class EditInformation : Form
    {
        private string selectedPhotoPath = "";

        string connectionString =
            @"Server=.;Database=Nexa;Trusted_Connection=True;TrustServerCertificate=True;";

        [DllImport(
            @"C:\Users\Pixel\Desktop\NexaCpp\x64\Debug\NexaCpp.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            ExactSpelling = true)]
        private static extern int UpdateUser(
            int userId,
            string firstAndLastName,
            string yourId,
            string phoneNumber);

        public EditInformation()
        {
            InitializeComponent();
        }

        private void EditInformation_Load(object sender, EventArgs e)
        {
            pickphoto.SizeMode = PictureBoxSizeMode.Zoom;
            txtPassword.UseSystemPasswordChar = true;
            txtPhone.Enabled = false;

            LoadUserInformation();
        }

        private void LoadUserInformation()
        {
            if (CurrentUser.Id <= 0)
            {
                MessageBox.Show("کاربر وارد نشده است.");
                return;
            }

            using (SqlConnection con =
                   new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT
                        FirstAndLastName,
                        YourID,
                        PhoneNumber,
                        Password,
                        Bio,
                        ProfilePhoto
                    FROM Users
                    WHERE Id = @Id";

                using (SqlCommand cmd =
                       new SqlCommand(query, con))
                {
                    cmd.Parameters.Add(
                        "@Id",
                        SqlDbType.Int).Value =
                        CurrentUser.Id;

                    try
                    {
                        con.Open();

                        using (SqlDataReader reader =
                               cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtUsername.Text =
                                    reader["FirstAndLastName"] == DBNull.Value
                                    ? ""
                                    : reader["FirstAndLastName"].ToString();

                                txtId.Text =
                                    reader["YourID"] == DBNull.Value
                                    ? ""
                                    : reader["YourID"].ToString();

                                txtPhone.Text =
                                    reader["PhoneNumber"] == DBNull.Value
                                    ? ""
                                    : reader["PhoneNumber"].ToString();

                                txtPassword.Text =
                                    reader["Password"] == DBNull.Value
                                    ? ""
                                    : reader["Password"].ToString();

                                txtBio.Text =
                                    reader["Bio"] == DBNull.Value
                                    ? ""
                                    : reader["Bio"].ToString();

                                if (reader["ProfilePhoto"] != DBNull.Value)
                                {
                                    string photoPath =
                                        reader["ProfilePhoto"].ToString();

                                    if (!string.IsNullOrWhiteSpace(photoPath) &&
                                        File.Exists(photoPath))
                                    {
                                        if (pickphoto.Image != null)
                                        {
                                            pickphoto.Image.Dispose();
                                            pickphoto.Image = null;
                                        }

                                        using (Image tempImage =
                                               Image.FromFile(photoPath))
                                        {
                                            pickphoto.Image =
                                                new Bitmap(tempImage);
                                        }

                                        selectedPhotoPath = photoPath;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "خطا در دریافت اطلاعات:\n\n" +
                            ex.Message);
                    }
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
        }

        private void btnEditProfile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog =
                   new OpenFileDialog())
            {
                openFileDialog.Title = "انتخاب عکس جدید";

                openFileDialog.Filter =
                    "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

                openFileDialog.Multiselect = false;

                if (openFileDialog.ShowDialog() ==
                    DialogResult.OK)
                {
                    try
                    {
                        string newPhotoPath =
                            openFileDialog.FileName;

                        using (Image tempImage =
                               Image.FromFile(newPhotoPath))
                        {
                            Bitmap newBitmap =
                                new Bitmap(tempImage);

                            if (pickphoto.Image != null)
                            {
                                pickphoto.Image.Dispose();
                                pickphoto.Image = null;
                            }

                            pickphoto.Image = newBitmap;
                        }

                        selectedPhotoPath = newPhotoPath;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "خطا در انتخاب عکس:\n\n" +
                            ex.Message);
                    }
                }
            }
        }

        private void btnOkEdit_Click(object sender, EventArgs e)
        {
            try
            {
                int result = UpdateUser(
                    CurrentUser.Id,
                    txtUsername.Text.Trim(),
                    txtId.Text.Trim(),
                    txtPhone.Text.Trim());

                if (result != 1)
                {
                    MessageBox.Show(
                        "بروزرسانی اطلاعات انجام نشد.\nکد خطا: " +
                        result,
                        "Nexa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                        UPDATE Users
                        SET
                            Bio = @Bio,
                            ProfilePhoto = @ProfilePhoto
                        WHERE Id = @Id";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@Bio",
                            SqlDbType.NVarChar,
                            500).Value =
                            string.IsNullOrWhiteSpace(txtBio.Text)
                            ? (object)DBNull.Value
                            : txtBio.Text.Trim();

                        cmd.Parameters.Add(
                            "@ProfilePhoto",
                            SqlDbType.NVarChar,
                            -1).Value =
                            string.IsNullOrWhiteSpace(selectedPhotoPath)
                            ? (object)DBNull.Value
                            : selectedPhotoPath;

                        cmd.Parameters.Add(
                            "@Id",
                            SqlDbType.Int).Value =
                            CurrentUser.Id;

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                CurrentUser.Username =
                    txtUsername.Text.Trim();

                CurrentUser.PhoneNumber =
                    txtPhone.Text.Trim();

                MessageBox.Show(
                    "اطلاعات با موفقیت بروزرسانی شد.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                Close();
            }
            catch (DllNotFoundException ex)
            {
                MessageBox.Show(ex.ToString());
            }
            catch (BadImageFormatException ex)
            {
                MessageBox.Show(ex.ToString());
            }
            catch (EntryPointNotFoundException ex)
            {
                MessageBox.Show(ex.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا:\n\n" +
                    ex.Message);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (pickphoto.Image != null)
            {
                pickphoto.Image.Dispose();
                pickphoto.Image = null;
            }

            base.OnFormClosed(e);
        }

        private void EditInformation_Load_1(
            object sender,
            EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = true;
            txtPhone.Enabled = false;

            LoadUserInformation();
        }

        private void btnPassShow_Click(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar =
                !txtPassword.UseSystemPasswordChar;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Hide();

            MyInformation information =
                new MyInformation();

            information.ShowDialog();

            this.Show();
        }
    }
}