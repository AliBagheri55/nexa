using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexa
{
    public partial class AddGroupMember : Form
    {
        private int groupId;
        private string connectionString =
    @"Server=.;Database=Nexa;Trusted_Connection=True;TrustServerCertificate=True;";
        public AddGroupMember(int groupId)
        {
            InitializeComponent();

            this.groupId = groupId;
        }

        private void txtYourID_TextChanged(object sender, EventArgs e)
        {
            string search = txtYourID.Text.Trim();

            lstUsers.Items.Clear();

            if (string.IsNullOrWhiteSpace(search))
                return;

            try
            {
                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                SELECT
                    Id,
                    FirstAndLastName,
                    YourID
                FROM Users
                WHERE FirstAndLastName LIKE @Search
                   OR YourID LIKE @Search
                ORDER BY FirstAndLastName ASC";

                    using (SqlCommand command =
                           new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@Search",
                            SqlDbType.NVarChar,
                            100).Value = "%" + search + "%";

                        connection.Open();

                        using (SqlDataReader reader =
                               command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string name =
                                    reader["FirstAndLastName"] == DBNull.Value
                                    ? "بدون نام"
                                    : reader["FirstAndLastName"].ToString();

                                string yourId =
                                    reader["YourID"] == DBNull.Value
                                    ? ""
                                    : reader["YourID"].ToString();

                                string item =
                                    name +
                                    "  (@" +
                                    yourId +
                                    ")";

                                lstUsers.Items.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در جستجوی کاربران:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (groupId <= 0)
            {
                MessageBox.Show(
                    "گروه نامعتبر است.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (lstUsers.SelectedItem == null)
            {
                MessageBox.Show(
                    "ابتدا یک کاربر را انتخاب کنید.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            string selectedText =
                lstUsers.SelectedItem.ToString();

            int start =
                selectedText.LastIndexOf("(@");

            if (start < 0 || !selectedText.EndsWith(")"))
            {
                MessageBox.Show(
                    "اطلاعات کاربر نامعتبر است.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            string yourId =
                selectedText.Substring(
                    start + 2,
                    selectedText.Length - start - 3);

            if (string.IsNullOrWhiteSpace(yourId))
            {
                MessageBox.Show(
                    "YourID کاربر نامعتبر است.",
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
                    // پیدا کردن کاربر
                    // =========================================

                    string userQuery = @"
                SELECT Id
                FROM Users
                WHERE YourID = @YourID;";

                    int userId = 0;

                    using (SqlCommand userCommand =
                           new SqlCommand(
                               userQuery,
                               connection))
                    {
                        userCommand.Parameters.Add(
                            "@YourID",
                            SqlDbType.NVarChar,
                            50).Value =
                            yourId;

                        object userResult =
                            userCommand.ExecuteScalar();

                        if (userResult == null ||
                            userResult == DBNull.Value)
                        {
                            MessageBox.Show(
                                "کاربر پیدا نشد.",
                                "Nexa",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                            return;
                        }

                        userId =
                            Convert.ToInt32(userResult);
                    }

                    // =========================================
                    // بررسی اینکه کاربر عضو گروه نباشد
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
                            userId;

                        int count =
                            Convert.ToInt32(
                                checkCommand.ExecuteScalar());

                        if (count > 0)
                        {
                            MessageBox.Show(
                                "این کاربر قبلاً عضو گروه است.",
                                "Nexa",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                            return;
                        }
                    }

                    // =========================================
                    // افزودن کاربر به گروه
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
                            userId;

                        int inserted =
                            insertCommand.ExecuteNonQuery();

                        if (inserted > 0)
                        {
                            MessageBox.Show(
                                "کاربر با موفقیت به گروه اضافه شد.",
                                "Nexa",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                            txtYourID.Clear();
                            lstUsers.Items.Clear();

                            this.DialogResult =
                                DialogResult.OK;

                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show(
                                "کاربر به گروه اضافه نشد.",
                                "Nexa",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "خطای SQL هنگام افزودن کاربر:\n\n" +
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
                    "خطا در افزودن کاربر:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
