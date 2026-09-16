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
                    groupPhoto =
                        File.ReadAllBytes(dialog.FileName);

                    using (MemoryStream ms =
                           new MemoryStream(groupPhoto))
                    {
                        picGroupPhoto.Image =
                            Image.FromStream(ms);
                    }
                }
            }
        }

        private string GenerateInviteCode()
        {
            return Guid.NewGuid()
                .ToString("N")
                .Substring(0, 12)
                .ToUpper();
        }

        private void AddCreatorToGroup(int groupId)
        {
            using (SqlConnection con =
                   new SqlConnection(connectionString))
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

                using (SqlCommand cmd =
                       new SqlCommand(query, con))
                {
                    cmd.Parameters.Add(
                        "@GroupId",
                        SqlDbType.Int).Value =
                        groupId;

                    cmd.Parameters.Add(
                        "@UserId",
                        SqlDbType.Int).Value =
                        CurrentUser.Id;

                    con.Open();

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void btnCreateGroup_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(
                txtGroupName.Text))
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

            try
            {
                string inviteCode =
                    GenerateInviteCode();

                using (SqlConnection con =
                       new SqlConnection(
                           connectionString))
                {
                    string query = @"
                        INSERT INTO Groups
                        (
                            GroupName,
                            GroupBio,
                            GroupPhoto,
                            CreatedBy,
                            InviteCode
                        )
                        OUTPUT INSERTED.Id
                        VALUES
                        (
                            @GroupName,
                            @GroupBio,
                            @GroupPhoto,
                            @CreatedBy,
                            @InviteCode
                        )";

                    using (SqlCommand cmd =
                           new SqlCommand(
                               query,
                               con))
                    {
                        cmd.Parameters.Add(
                            "@GroupName",
                            SqlDbType.NVarChar,
                            100).Value =
                            txtGroupName.Text.Trim();

                        cmd.Parameters.Add(
                            "@GroupBio",
                            SqlDbType.NVarChar,
                            500).Value =
                            string.IsNullOrWhiteSpace(
                                txtGroupBio.Text)
                            ? (object)DBNull.Value
                            : txtGroupBio.Text.Trim();

                        cmd.Parameters.Add(
                            "@GroupPhoto",
                            SqlDbType.VarBinary,
                            -1).Value =
                            groupPhoto == null
                            ? (object)DBNull.Value
                            : groupPhoto;

                        cmd.Parameters.Add(
                            "@CreatedBy",
                            SqlDbType.Int).Value =
                            CurrentUser.Id;

                        cmd.Parameters.Add(
                            "@InviteCode",
                            SqlDbType.NVarChar,
                            100).Value =
                            inviteCode;

                        con.Open();

                        int groupId =
                            Convert.ToInt32(
                                cmd.ExecuteScalar());

                        // اضافه کردن سازنده به گروه
                        AddCreatorToGroup(groupId);

                        // ساخت لینک دعوت
                        string inviteLink =
                            "https://nexa.app/group/" +
                            inviteCode;

                        // نمایش لینک دعوت
                        MessageBox.Show(
                            "گروه با موفقیت ساخته شد.\n\n" +
                            "لینک دعوت گروه:\n\n" +
                            inviteLink +
                            "\n\n" +
                            "لینک در کلیپ‌بورد کپی شد.",
                            "Nexa",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        Clipboard.SetText(inviteLink);

                        // باز کردن فرم افزودن اعضا
                        this.Hide();

                        AddGroupMembers membersForm =
                            new AddGroupMembers(groupId);

                        membersForm.ShowDialog();

                        this.Close();
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "خطای SQL هنگام ساخت گروه:\n\n" +
                    ex.Message +
                    "\n\nشماره خطا: " +
                    ex.Number,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا هنگام ساخت گروه:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Hide();

            Nexa nexa =
                new Nexa();

            nexa.ShowDialog();

            this.Close();
        }
    }
}