using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Nexa
{
    public partial class StoryViewer : Form
    {
        private string connectionString =
            @"Server=.;Database=Nexa;Trusted_Connection=True;TrustServerCertificate=True;";

        private int storyId;

        public StoryViewer(int storyId)
        {
            InitializeComponent();
            this.storyId = storyId;

            storyTimer = new Timer();
            storyTimer.Interval = 10000;
            storyTimer.Tick += StoryTimer_Tick;
            storyTimer.Start();
        }
        private void StoryTimer_Tick(object sender, EventArgs e)
        {
            storyTimer.Stop();
            storyTimer.Dispose();
            Close();
        }

        private void StoryViewer_Load(object sender, EventArgs e)
        {
            LoadStory();
        }

        private void LoadStory()
        {
            using (SqlConnection con =
                   new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT StoryData
                    FROM Stories
                    WHERE Id = @StoryId
                    AND ExpiresAt > GETDATE()";

                using (SqlCommand cmd =
                       new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@StoryId", SqlDbType.Int)
                        .Value = storyId;

                    con.Open();

                    object result = cmd.ExecuteScalar();

                    if (result != null &&
                        result != DBNull.Value)
                    {
                        byte[] imageData = (byte[])result;

                        using (MemoryStream ms =
                               new MemoryStream(imageData))
                        {
                            using (Image image =
                                   Image.FromStream(ms))
                            {
                                pictureStory.Image =
                                    new Bitmap(image);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            "این استوری منقضی شده است.");

                        Close();
                    }
                }
            }
        }
    }
}