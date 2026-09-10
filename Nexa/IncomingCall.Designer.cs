namespace Nexa
{
    partial class IncomingCall
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IncomingCall));
            this.lblCaller = new System.Windows.Forms.Label();
            this.btnAccept = new System.Windows.Forms.Button();
            this.btnReject = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblCaller
            // 
            this.lblCaller.AutoSize = true;
            this.lblCaller.Location = new System.Drawing.Point(9, 32);
            this.lblCaller.Name = "lblCaller";
            this.lblCaller.Size = new System.Drawing.Size(82, 16);
            this.lblCaller.TabIndex = 0;
            this.lblCaller.Text = "در حال تماس...";
            // 
            // btnAccept
            // 
            this.btnAccept.Location = new System.Drawing.Point(12, 118);
            this.btnAccept.Name = "btnAccept";
            this.btnAccept.Size = new System.Drawing.Size(105, 50);
            this.btnAccept.TabIndex = 1;
            this.btnAccept.Text = "قبول تماس";
            this.btnAccept.UseVisualStyleBackColor = true;
            this.btnAccept.Click += new System.EventHandler(this.btnAccept_Click_1);
            // 
            // btnReject
            // 
            this.btnReject.Location = new System.Drawing.Point(185, 118);
            this.btnReject.Name = "btnReject";
            this.btnReject.Size = new System.Drawing.Size(105, 50);
            this.btnReject.TabIndex = 2;
            this.btnReject.Text = " رد تماس";
            this.btnReject.UseVisualStyleBackColor = true;
            this.btnReject.Click += new System.EventHandler(this.btnReject_Click);
            // 
            // IncomingCall
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(302, 186);
            this.Controls.Add(this.btnReject);
            this.Controls.Add(this.btnAccept);
            this.Controls.Add(this.lblCaller);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "IncomingCall";
            this.Text = "IncomingCall";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblCaller;
        private System.Windows.Forms.Button btnAccept;
        private System.Windows.Forms.Button btnReject;
    }
}