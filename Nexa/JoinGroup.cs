using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexa
{
    public partial class JoinGroup : Form
    {
        private string inviteCode;
        private int currentUserId;

        private string connectionString =
            @"Server=.;Database=Nexa;Trusted_Connection=True;TrustServerCertificate=True;";
        public JoinGroup(string inviteCode, int currentUserId)
        {
            InitializeComponent();

            this.inviteCode = inviteCode;
            this.currentUserId = currentUserId;

            LoadGroupByInviteCode();
        }
        private void LoadGroupByInviteCode()
        {
            try
            {
                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                SELECT
                    G.Id,
                    G.GroupName,
                    G.GroupBio,
                    G.GroupPhoto,
                    COUNT(GM.UserId) AS MemberCount
                FROM Groups G
                LEFT JOIN GroupMembers GM
                    ON G.Id = GM.GroupId
                WHERE G.InviteCode = @InviteCode
                GROUP BY
                    G.Id,
                    G.GroupName,
                    G.GroupBio,
                    G.GroupPhoto;";

                    using (SqlCommand command =
                           new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@InviteCode",
                            SqlDbType.NVarChar,
                            100).Value =
                            inviteCode;

                        connection.Open();

                        using (SqlDataReader reader =
                               command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show(
                                    "لینک دعوت نامعتبر یا منقضی شده است.",
                                    "Nexa",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);

                                btnJoin.Enabled = false;
                                return;
                            }

                            lblGroupName.Text =
                                reader["GroupName"] == DBNull.Value
                                ? "بدون نام"
                                : reader["GroupName"].ToString();

                            lblGroupBio.Text =
                                reader["GroupBio"] == DBNull.Value
                                ? "توضیحی ثبت نشده است."
                                : reader["GroupBio"].ToString();

                            lblMemberCount.Text =
                                "Members: " +
                                Convert.ToInt32(
                                    reader["MemberCount"]);

                            if (reader["GroupPhoto"] != DBNull.Value)
                            {
                                byte[] imageData =
                                    (byte[])reader["GroupPhoto"];

                                using (MemoryStream ms =
                                       new MemoryStream(imageData))
                                {
                                    picGroupPhoto.Image =
                                        Image.FromStream(ms);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در دریافت اطلاعات گروه:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                btnJoin.Enabled = false;
            }
        }
        private void JoinGroup_Load(object sender, EventArgs e)
        {

        }

        private void btnJoin_Click(object sender, EventArgs e)
        {
            if (currentUserId <= 0)
            {
                MessageBox.Show(
                    "کاربر فعلی شناسایی نشد.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (string.IsNullOrWhiteSpace(inviteCode))
            {
                MessageBox.Show(
                    "لینک دعوت نامعتبر است.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            try
            {
                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    connection.Open();

                    // =========================================
                    // پیدا کردن گروه با InviteCode
                    // =========================================

                    string groupQuery = @"
                SELECT Id, GroupName
                FROM Groups
                WHERE InviteCode = @InviteCode;";

                    int groupId = 0;
                    string groupName = "";

                    using (SqlCommand groupCommand =
                           new SqlCommand(
                               groupQuery,
                               connection))
                    {
                        groupCommand.Parameters.Add(
                            "@InviteCode",
                            SqlDbType.NVarChar,
                            100).Value =
                            inviteCode;

                        using (SqlDataReader reader =
                               groupCommand.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show(
                                    "گروه پیدا نشد یا لینک دعوت معتبر نیست.",
                                    "Nexa",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);

                                return;
                            }

                            groupId =
                                Convert.ToInt32(
                                    reader["Id"]);

                            groupName =
                                reader["GroupName"] == DBNull.Value
                                ? "گروه"
                                : reader["GroupName"].ToString();
                        }
                    }

                    // =========================================
                    // بررسی عضویت قبلی
                    // =========================================

                    string checkQuery = @"
                SELECT COUNT(*)
                FROM GroupMembers
                WHERE GroupId = @GroupId
                  AND UserId = @UserId;";

                    using (SqlCommand checkCommand =
                           new SqlCommand(
                               checkQuery,
                               connection))
                    {
                        checkCommand.Parameters.Add(
                            "@GroupId",
                            SqlDbType.Int).Value =
                            groupId;

                        checkCommand.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        int count =
                            Convert.ToInt32(
                                checkCommand.ExecuteScalar());

                        if (count > 0)
                        {
                            MessageBox.Show(
                                "شما قبلاً عضو این گروه هستید.",
                                "Nexa",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                            return;
                        }
                    }

                    // =========================================
                    // عضویت در گروه
                    // =========================================

                    string insertQuery = @"
                INSERT INTO GroupMembers
                (
                    GroupId,
                    UserId
                )
                VALUES
                (
                    @GroupId,
                    @UserId
                );";

                    using (SqlCommand insertCommand =
                           new SqlCommand(
                               insertQuery,
                               connection))
                    {
                        insertCommand.Parameters.Add(
                            "@GroupId",
                            SqlDbType.Int).Value =
                            groupId;

                        insertCommand.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        int result =
                            insertCommand.ExecuteNonQuery();

                        if (result > 0)
                        {
                            MessageBox.Show(
                                "با موفقیت عضو گروه \"" +
                                groupName +
                                "\" شدید.",
                                "Nexa",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                            this.DialogResult =
                                DialogResult.OK;

                            this.Close();
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "خطای SQL هنگام عضویت در گروه:\n\n" +
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
                    "خطا هنگام عضویت در گروه:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
