using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Nexa
{
    public partial class MyInformation : Form
    {
        string connectionString =
            @"Server=.;Database=Nexa;Trusted_Connection=True;TrustServerCertificate=True;";

        public MyInformation()
        {
            InitializeComponent();
        }

        private void MyInformation_Load(object sender, EventArgs e)
        {
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

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
                        Bio,
                        ProfilePhoto
                    FROM Users
                    WHERE Id = @Id";

                using (SqlCommand cmd =
                       new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@Id", SqlDbType.Int)
                        .Value = CurrentUser.Id;

                    try
                    {
                        con.Open();

                        using (SqlDataReader reader =
                               cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                listBox1.Items.Clear();

                                listBox1.Items.Add(
                                    "Name: " +
                                    reader["FirstAndLastName"].ToString());

                                listBox1.Items.Add(
                                    "ID: " +
                                    reader["YourID"].ToString());

                                listBox1.Items.Add(
                                    "Phone: " +
                                    reader["PhoneNumber"].ToString());

                                string bio =
                                    reader["Bio"] == DBNull.Value
                                    ? "Bio: هنوز بیویی ثبت نشده است."
                                    : "Bio: " +
                                      reader["Bio"].ToString();

                                listBox1.Items.Add(bio);

                                if (pictureBox1.Image != null)
                                {
                                    pictureBox1.Image.Dispose();
                                    pictureBox1.Image = null;
                                }

                                if (reader["ProfilePhoto"] != DBNull.Value)
                                {
                                    string photoPath =
                                        reader["ProfilePhoto"].ToString();

                                    if (!string.IsNullOrWhiteSpace(photoPath))
                                    {
                                        if (File.Exists(photoPath))
                                        {
                                            using (Image tempImage =
                                                   Image.FromFile(photoPath))
                                            {
                                                pictureBox1.Image =
                                                    new Bitmap(tempImage);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show(
                                    "اطلاعات کاربر یافت نشد.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "خطا:\n\n" +
                            ex.Message);
                    }
                }
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            Hide();

            EditInformation edit =
                new EditInformation();

            edit.ShowDialog();

            Show();

            LoadUserInformation();
        }

        protected override void OnFormClosed(
            FormClosedEventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
                pictureBox1.Image = null;
            }

            base.OnFormClosed(e);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            Hide();

            Nexa nexa =
                new Nexa();

            nexa.ShowDialog();
        }
    }
}