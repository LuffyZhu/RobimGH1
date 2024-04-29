namespace RobimUpdate
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.Install = new System.Windows.Forms.Button();
            this.Done = new System.Windows.Forms.Button();
            this.Output = new System.Windows.Forms.Label();
            this.version = new System.Windows.Forms.Label();
            this.UpdateContext = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.Bar = new System.Windows.Forms.Panel();
            this.slider = new System.Windows.Forms.Panel();
            this.downloadcount = new System.Windows.Forms.Label();
            this.panel4 = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.Bar.SuspendLayout();
            this.panel4.SuspendLayout();
            this.SuspendLayout();
            // 
            // Install
            // 
            this.Install.BackColor = System.Drawing.Color.Transparent;
            this.Install.Enabled = false;
            this.Install.FlatAppearance.BorderSize = 0;
            this.Install.Font = new System.Drawing.Font("微軟正黑體 Light", 10F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Install.Location = new System.Drawing.Point(366, 230);
            this.Install.Margin = new System.Windows.Forms.Padding(1);
            this.Install.Name = "Install";
            this.Install.Size = new System.Drawing.Size(128, 37);
            this.Install.TabIndex = 0;
            this.Install.Text = "Install";
            this.Install.UseVisualStyleBackColor = false;
            this.Install.Click += new System.EventHandler(this.Install_Click);
            // 
            // Done
            // 
            this.Done.BackColor = System.Drawing.Color.Transparent;
            this.Done.Enabled = false;
            this.Done.FlatAppearance.BorderSize = 0;
            this.Done.Font = new System.Drawing.Font("微軟正黑體 Light", 10F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Done.Location = new System.Drawing.Point(366, 286);
            this.Done.Margin = new System.Windows.Forms.Padding(1);
            this.Done.Name = "Done";
            this.Done.Size = new System.Drawing.Size(128, 37);
            this.Done.TabIndex = 0;
            this.Done.Text = "Done";
            this.Done.UseVisualStyleBackColor = false;
            this.Done.Click += new System.EventHandler(this.Done_Click);
            // 
            // Output
            // 
            this.Output.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(251)))), ((int)(((byte)(170)))), ((int)(((byte)(38)))));
            this.Output.Font = new System.Drawing.Font("微軟正黑體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.Output.Location = new System.Drawing.Point(13, 19);
            this.Output.Margin = new System.Windows.Forms.Padding(0);
            this.Output.Name = "Output";
            this.Output.Size = new System.Drawing.Size(484, 27);
            this.Output.TabIndex = 1;
            this.Output.Text = "output";
            this.Output.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // version
            // 
            this.version.BackColor = System.Drawing.Color.Transparent;
            this.version.Font = new System.Drawing.Font("微軟正黑體", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.version.Location = new System.Drawing.Point(219, 431);
            this.version.Margin = new System.Windows.Forms.Padding(0);
            this.version.Name = "version";
            this.version.Size = new System.Drawing.Size(278, 32);
            this.version.TabIndex = 2;
            this.version.Text = "version";
            this.version.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            // 
            // UpdateContext
            // 
            this.UpdateContext.AutoEllipsis = true;
            this.UpdateContext.AutoSize = true;
            this.UpdateContext.BackColor = System.Drawing.Color.Transparent;
            this.UpdateContext.Font = new System.Drawing.Font("微軟正黑體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.UpdateContext.ForeColor = System.Drawing.Color.DimGray;
            this.UpdateContext.Location = new System.Drawing.Point(0, 0);
            this.UpdateContext.Margin = new System.Windows.Forms.Padding(0);
            this.UpdateContext.Name = "UpdateContext";
            this.UpdateContext.Size = new System.Drawing.Size(218, 20);
            this.UpdateContext.TabIndex = 7;
            this.UpdateContext.Text = "New change in this version :";
            this.UpdateContext.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(251)))), ((int)(((byte)(170)))), ((int)(((byte)(38)))));
            this.panel1.BackgroundImage = global::RobimUpdate.Properties.Resources.LOGO_4;
            this.panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel1.Location = new System.Drawing.Point(366, 92);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(128, 112);
            this.panel1.TabIndex = 8;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox1.Image = global::RobimUpdate.Properties.Resources.LOGO_2;
            this.pictureBox1.Location = new System.Drawing.Point(9, 366);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(210, 97);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 9;
            this.pictureBox1.TabStop = false;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.panel2.Location = new System.Drawing.Point(12, 365);
            this.panel2.Margin = new System.Windows.Forms.Padding(0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(485, 1);
            this.panel2.TabIndex = 10;
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.panel3.Location = new System.Drawing.Point(12, 91);
            this.panel3.Margin = new System.Windows.Forms.Padding(0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(485, 1);
            this.panel3.TabIndex = 11;
            // 
            // Bar
            // 
            this.Bar.BackColor = System.Drawing.Color.White;
            this.Bar.Controls.Add(this.slider);
            this.Bar.Location = new System.Drawing.Point(12, 56);
            this.Bar.Margin = new System.Windows.Forms.Padding(0);
            this.Bar.Name = "Bar";
            this.Bar.Size = new System.Drawing.Size(485, 10);
            this.Bar.TabIndex = 12;
            this.Bar.Visible = false;
            // 
            // slider
            // 
            this.slider.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.slider.Location = new System.Drawing.Point(0, 0);
            this.slider.Margin = new System.Windows.Forms.Padding(0);
            this.slider.Name = "slider";
            this.slider.Size = new System.Drawing.Size(1, 10);
            this.slider.TabIndex = 13;
            // 
            // downloadcount
            // 
            this.downloadcount.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(251)))), ((int)(((byte)(170)))), ((int)(((byte)(38)))));
            this.downloadcount.Font = new System.Drawing.Font("微軟正黑體", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.downloadcount.Location = new System.Drawing.Point(12, 66);
            this.downloadcount.Margin = new System.Windows.Forms.Padding(0);
            this.downloadcount.Name = "downloadcount";
            this.downloadcount.Size = new System.Drawing.Size(485, 25);
            this.downloadcount.TabIndex = 13;
            this.downloadcount.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel4
            // 
            this.panel4.AutoScroll = true;
            this.panel4.Controls.Add(this.UpdateContext);
            this.panel4.Location = new System.Drawing.Point(9, 95);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(354, 267);
            this.panel4.TabIndex = 15;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(251)))), ((int)(((byte)(170)))), ((int)(((byte)(38)))));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(504, 464);
            this.Controls.Add(this.downloadcount);
            this.Controls.Add(this.Bar);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.Install);
            this.Controls.Add(this.Done);
            this.Controls.Add(this.version);
            this.Controls.Add(this.Output);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panel4);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(1);
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Robim Update";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Shown += new System.EventHandler(this.GetUpdate);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.Bar.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button Install;
        private System.Windows.Forms.Button Done;
        private System.Windows.Forms.Label Output;
        private System.Windows.Forms.Label version;
        private System.Windows.Forms.Label UpdateContext;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel Bar;
        private System.Windows.Forms.Panel slider;
        private System.Windows.Forms.Label downloadcount;
        private System.Windows.Forms.Panel panel4;
    }
}

