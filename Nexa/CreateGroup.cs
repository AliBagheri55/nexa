using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Nexa
{
    public partial class CreateGroup : Form
    {
        string connectionString =
    @"Server=.;Database=Nexa;Trusted_Connection=True;TrustServerCertificate=True;";

        byte[] groupPhoto = null;
        public CreateGroup()
        {
            InitializeComponent();
        }

        private void CreateGroup_Load(object sender, EventArgs e)
        {

        }

        private void btnSelectPhoto_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter =
                    "Image Files|*.jpg;*.jpeg;*.png;*.bmp";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    groupPhoto = File.ReadAllBytes(dialog.FileName);

                    using (MemoryStream ms = new MemoryStream(groupPhoto))
                    {
                        picGroupPhoto.Image = Image.FromStream(ms);
                    }
                }
            }
        }
        private void AddCreatorToGroup(int groupId)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
            INSERT INTO GroupMembers
            (
                GroupId,
                UserId,
                Role
            )
            VALUES
            (
                @GroupId,
                @UserId,
                'Admin'
            )";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@GroupId", SqlDbType.Int)
                        .Value = groupId;

                    cmd.Parameters.Add("@UserId", SqlDbType.Int)
                        .Value = CurrentUser.Id;

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private void btnCreateGroup_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtGroupName.Text))
            {
                MessageBox.Show(
                    "نام گروه را وارد کنید.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (CurrentUser.Id <= 0)
            {
                MessageBox.Show(
                    "کاربر فعلی شناسایی نشد.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
                INSERT INTO Groups
                (
                  GroupName,
                  GroupBio,
                  GroupPhoto,
                  CreatedBy
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                   @GroupName,
                   @GroupBio,
                   @GroupPhoto,
                   @CreatedBy
                )";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@GroupName", SqlDbType.NVarChar, 100)
                        .Value = txtGroupName.Text.Trim();

                    cmd.Parameters.Add("@GroupBio", SqlDbType.NVarChar, 500)
                        .Value = string.IsNullOrWhiteSpace(txtGroupBio.Text)
                            ? (object)DBNull.Value
                            : txtGroupBio.Text.Trim();

                    cmd.Parameters.Add("@GroupPhoto", SqlDbType.VarBinary, -1)
                        .Value = groupPhoto == null
                            ? (object)DBNull.Value
                            : groupPhoto;

                    cmd.Parameters.Add("@CreatedBy", SqlDbType.Int)
                        .Value = CurrentUser.Id;

                    con.Open();

                    int groupId = Convert.ToInt32(cmd.ExecuteScalar());

                    // اضافه کردن سازنده به عنوان Admin
                    AddCreatorToGroup(groupId);

                    // باز کردن فرم افزودن اعضا
                    this.Hide();

                    AddGroupMembers membersForm = new AddGroupMembers(groupId);

                    membersForm.ShowDialog();

                    this.Close();
                }
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Hide();
            Nexa nexa= new Nexa();
            nexa.ShowDialog();
        }
    }
}
