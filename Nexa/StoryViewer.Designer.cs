namespace Nexa
{
    partial class StoryViewer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StoryViewer));
            this.storyTimer = new System.Windows.Forms.Timer(this.components);
            this.pictureStory = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureStory)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureStory
            // 
            this.pictureStory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureStory.Location = new System.Drawing.Point(0, 0);
            this.pictureStory.Name = "pictureStory";
            this.pictureStory.Size = new System.Drawing.Size(437, 601);
            this.pictureStory.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureStory.TabIndex = 0;
            this.pictureStory.TabStop = false;
            // 
            // StoryViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(437, 601);
            this.Controls.Add(this.pictureStory);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "StoryViewer";
            this.Text = "StoryViewer";
            this.Load += new System.EventHandler(this.StoryViewer_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureStory)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureStory;
        private System.Windows.Forms.Timer storyTimer;
    }
}