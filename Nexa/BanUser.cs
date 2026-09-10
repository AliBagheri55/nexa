using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Nexa
{
    public partial class BanUser : Form
    {
        private string connectionString =
            @"Server=.;Database=Nexa;Trusted_Connection=True;TrustServerCertificate=True;";

        private string userPhone;
        private int currentUserId = 0;

        public BanUser(string phoneNumber)
        {
            InitializeComponent();
            userPhone = phoneNumber;
        }

        private void BanUser_Load(object sender, EventArgs e)
        {
            GetCurrentUser();
            LoadBanList();
        }

        private void GetCurrentUser()
        {
            if (string.IsNullOrWhiteSpace(userPhone))
            {
                MessageBox.Show("شماره تلفن کاربر مشخص نیست!");
                return;
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT Id
                    FROM Users
                    WHERE PhoneNumber = @PhoneNumber";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar, 50)
                        .Value = userPhone;

                    con.Open();

                    object result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        currentUserId = Convert.ToInt32(result);
                    }
                    else
                    {
                        MessageBox.Show("کاربر فعلی پیدا نشد!");
                    }
                }
            }
        }

        private void txtSearch_TextChanged_1(object sender, EventArgs e)
        {
            SearchUsers(txtSearch.Text);
        }

        private void SearchUsers(string text)
        {
            lstResults.Items.Clear();

            if (string.IsNullOrWhiteSpace(text) || currentUserId == 0)
            {
                lstResults.Visible = false;
                return;
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT FirstAndLastName
                    FROM Users
                    WHERE FirstAndLastName LIKE '%' + @Search + '%'
                    AND Id <> @CurrentUserId
                    AND Id NOT IN
                    (
                        SELECT BannedUserId
                        FROM UserBanList
                        WHERE UserId = @CurrentUserId
                    )
                    ORDER BY FirstAndLastName";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@Search", SqlDbType.NVarChar, 100)
                        .Value = text;

                    cmd.Parameters.Add("@CurrentUserId", SqlDbType.Int)
                        .Value = currentUserId;

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lstResults.Items.Add(
                                reader["FirstAndLastName"].ToString());
                        }
                    }
                }
            }

            lstResults.Visible = lstResults.Items.Count > 0;
        }

        private void btnBan_Click(object sender, EventArgs e)
        {
            if (lstResults.SelectedItem == null)
                return;

            string selectedName = lstResults.SelectedItem.ToString();

            AddUserToBanList(selectedName);

            txtSearch.Clear();
            lstResults.Items.Clear();
            lstResults.Visible = false;
        }

        private void lstResults_DoubleClick(object sender, EventArgs e)
        {
            if (lstResults.SelectedItem == null)
                return;

            string selectedName = lstResults.SelectedItem.ToString();

            AddUserToBanList(selectedName);

            txtSearch.Clear();
            lstResults.Items.Clear();
            lstResults.Visible = false;
        }

        private void AddUserToBanList(string name)
        {
            if (currentUserId == 0)
                return;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                string getUserQuery = @"
                    SELECT TOP 1 Id
                    FROM Users
                    WHERE FirstAndLastName = @Name
                    AND Id <> @CurrentUserId";

                int bannedUserId;

                using (SqlCommand cmd = new SqlCommand(getUserQuery, con))
                {
                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100)
                        .Value = name;

                    cmd.Parameters.Add("@CurrentUserId", SqlDbType.Int)
                        .Value = currentUserId;

                    object result = cmd.ExecuteScalar();

                    if (result == null)
                        return;

                    bannedUserId = Convert.ToInt32(result);
                }

                string checkQuery = @"
                    SELECT COUNT(*)
                    FROM UserBanList
                    WHERE UserId = @UserId
                    AND BannedUserId = @BannedUserId";

                using (SqlCommand cmd = new SqlCommand(checkQuery, con))
                {
                    cmd.Parameters.Add("@UserId", SqlDbType.Int)
                        .Value = currentUserId;

                    cmd.Parameters.Add("@BannedUserId", SqlDbType.Int)
                        .Value = bannedUserId;

                    int count = Convert.ToInt32(cmd.ExecuteScalar());

                    if (count > 0)
                        return;
                }

                string insertQuery = @"
                    INSERT INTO UserBanList
                    (
                        UserId,
                        BannedUserId
                    )
                    VALUES
                    (
                        @UserId,
                        @BannedUserId
                    )";

                using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                {
                    cmd.Parameters.Add("@UserId", SqlDbType.Int)
                        .Value = currentUserId;

                    cmd.Parameters.Add("@BannedUserId", SqlDbType.Int)
                        .Value = bannedUserId;

                    cmd.ExecuteNonQuery();
                }
            }

            listBan.Items.Add(name);
        }

        private void LoadBanList()
        {
            listBan.Items.Clear();

            if (currentUserId == 0)
                return;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
            SELECT U.YourID
            FROM UserBanList B
            INNER JOIN Users U
                ON U.Id = B.BannedUserId
            WHERE B.UserId = @UserId
            ORDER BY U.YourID";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@UserId", SqlDbType.Int)
                        .Value = currentUserId;

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listBan.Items.Add(
                                reader["YourID"].ToString()
                            );
                        }
                    }
                }
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Hide();

            Nexa nexa = new Nexa();
            nexa.ShowDialog();

            this.Close();
        }

        private void btnDeleteBan_Click(object sender, EventArgs e)
        {
            if (listBan.SelectedItem == null)
                return;

            string yourID = listBan.SelectedItem.ToString();

            DialogResult result = MessageBox.Show(
                "آیا مطمئن هستید که می‌خواهید این کاربر را از لیست بلاک حذف کنید؟",
                "تأیید حذف",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            int bannedUserId = 0;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
            SELECT Id
            FROM Users
            WHERE YourID = @YourID";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@YourID", SqlDbType.NVarChar, 50)
                        .Value = yourID;

                    con.Open();

                    object resultId = cmd.ExecuteScalar();

                    if (resultId != null)
                        bannedUserId = Convert.ToInt32(resultId);
                }
            }

            if (bannedUserId == 0)
                return;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
            DELETE FROM UserBanList
            WHERE UserId = @UserId
            AND BannedUserId = @BannedUserId";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@UserId", SqlDbType.Int)
                        .Value = currentUserId;

                    cmd.Parameters.Add("@BannedUserId", SqlDbType.Int)
                        .Value = bannedUserId;

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            listBan.Items.Remove(listBan.SelectedItem);
        }
    }
}