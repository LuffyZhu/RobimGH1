namespace Robim
{
    partial class CollisionComputeForm
    {
        /// <summary>
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置受控資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 元件設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.Stopbtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(28, 29);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(346, 25);
            this.progressBar1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label1.Location = new System.Drawing.Point(28, 57);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(346, 23);
            this.label1.TabIndex = 1;
            this.label1.Text = "label1";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Stopbtn
            // 
            this.Stopbtn.BackColor = System.Drawing.SystemColors.Control;
            this.Stopbtn.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.Stopbtn.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.Stopbtn.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.Stopbtn.Font = new System.Drawing.Font("微軟正黑體 Light", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.Stopbtn.Location = new System.Drawing.Point(304, 83);
            this.Stopbtn.Margin = new System.Windows.Forms.Padding(0);
            this.Stopbtn.Name = "Stopbtn";
            this.Stopbtn.Size = new System.Drawing.Size(70, 27);
            this.Stopbtn.TabIndex = 2;
            this.Stopbtn.Text = "Cancel";
            this.Stopbtn.UseVisualStyleBackColor = false;
            this.Stopbtn.Click += new System.EventHandler(this.Stopbtn_Click);
            // 
            // CollisionComputeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(404, 121);
            this.ControlBox = false;
            this.Controls.Add(this.Stopbtn);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.progressBar1);
            this.Icon = global::Robim.Properties.Resources.RobimFormlogo;
            this.Name = "CollisionComputeForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Collision Computing";
            this.Load += new System.EventHandler(this.CollisionComputeForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button Stopbtn;
    }
}
