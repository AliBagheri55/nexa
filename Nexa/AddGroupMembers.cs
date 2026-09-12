using System;
using System.Data;
using System.Data.SqlClient;
using System.Net.ServerSentEvents;
using System.Windows.Forms;

namespace Nexa
{
    public partial class AddGroupMembers : Form
    {
        string connectionString =
    @"Server=.;Database=Nexa;Trusted_Connection=True;TrustServerCertificate=True;";

        int groupId;
        public AddGroupMembers(int groupId)
        {
            InitializeComponent();
            this.groupId = groupId;
        }

        private void AddGroupMembers_Load(object sender, EventArgs e)
        {

        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            lstUsers.Items.Clear();

            string search = txtSearch.Text.Trim();

            if (string.IsNullOrWhiteSpace(search))
                return;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
            SELECT Id, FirstAndLastName, YourID
            FROM Users
            WHERE Id <> @CurrentUserId
            AND
            (
                FirstAndLastName LIKE @Search
                OR YourID LIKE @Search
            )
            ORDER BY FirstAndLastName";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@CurrentUserId", SqlDbType.Int)
                        .Value = CurrentUser.Id;

                    cmd.Parameters.Add("@Search", SqlDbType.NVarChar, 100)
                        .Value = "%" + search + "%";

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            UserItem user = new UserItem();

                            user.Id = Convert.ToInt32(reader["Id"]);
                            user.Name = reader["FirstAndLastName"].ToString();
                            user.YourID = reader["YourID"].ToString();

                            lstUsers.Items.Add(user);
                        }
                    }
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (lstUsers.SelectedItem == null)
            {
                MessageBox.Show(
                    "ابتدا یک کاربر را انتخاب کنید.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            UserItem user = lstUsers.SelectedItem as UserItem;

            if (user == null)
                return;

            bool alreadySelected = false;

            foreach (UserItem selectedUser in lstSelectedUsers.Items)
            {
                if (selectedUser.Id == user.Id)
                {
                    alreadySelected = true;
                    break;
                }
            }

            if (alreadySelected)
            {
                MessageBox.Show(
                    "این کاربر قبلاً انتخاب شده است.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            lstSelectedUsers.Items.Add(user);
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (lstSelectedUsers.SelectedItem == null)
            {
                MessageBox.Show(
                    "ابتدا یک کاربر را انتخاب کنید.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            lstSelectedUsers.Items.Remove(
                lstSelectedUsers.SelectedItem);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (lstSelectedUsers.Items.Count == 0)
            {
                MessageBox.Show(
                    "حداقل یک عضو انتخاب کنید.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                foreach (UserItem user in lstSelectedUsers.Items)
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
                    'Member'
                )";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@GroupId", SqlDbType.Int)
                            .Value = groupId;

                        cmd.Parameters.Add("@UserId", SqlDbType.Int)
                            .Value = user.Id;

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            AddGroupMembers membersForm = new AddGroupMembers(groupId);
            MessageBox.Show(
       "گروه با موفقیت ایجاد شد.",
       "Nexa",
       MessageBoxButtons.OK,
       MessageBoxIcon.Information);
            this.Hide();
            Nexa nexa = new Nexa();
            nexa.ShowDialog();

        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Hide();
            CreateGroup group= new CreateGroup();
            group.ShowDialog();
        }
    }
}
