using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using KeePass.Plugins;

using KeePassLib;

namespace YubiKeeCR
{
	public partial class Settings : Form
	{
		private bool validConfig = true;
		private IPluginHost m_host;
		public Settings(IPluginHost host)
		{
			m_host = host;
			InitializeComponent();

#if DEBUG
			Text = $"{Text} - (DEBUG BUILD)";
#endif

			iterationsTextBox.Text = Configuration.Iterations.ToString();

			radioButton2.Checked = !(radioButton1.Checked = Configuration.YubiSlot == YubiSlot.SLOT1);

			PwDatabase pd = host.Database;

			bool isUsingYKCR = (pd != null) && pd.IsOpen && CRCipherEngine.CipherUuid2.Equals(pd.DataCipherUuid);
			label5.Visible = !isUsingYKCR;
			radioButton1.Enabled = radioButton2.Enabled = iterationsTextBox.Enabled = isUsingYKCR;
		}

		private void iterationsTextBox_TextChanged(object sender, EventArgs e)
		{
			if (int.TryParse(iterationsTextBox.Text, out int value) && value > 0 && value <= int.MaxValue)
			{
				Configuration.Iterations = value;
				iterationsTextBox.BackColor = SystemColors.Window;
				validConfig = true;
			}
			else
			{
				iterationsTextBox.BackColor = Color.IndianRed;
				validConfig = false;
			}
		}

		private void Settings_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (!validConfig)
			{
				MessageBox.Show("Invalid settings!");
				e.Cancel = true;
			}

			m_host.MainWindow.SetStatusEx($"Saved YubiKeeCR settings. ({Configuration.Iterations} interations on {Configuration.YubiSlot})");
		}

		private void radioButton_CheckedChanged(object sender, EventArgs e)
		{
			//Explicity checks to avoid insanity
			if ((radioButton1.Checked && radioButton2.Checked) || (!radioButton1.Checked && !radioButton2.Checked))
			{
				panel1.BackColor = radioButton1.BackColor = radioButton2.BackColor = Color.IndianRed;
				validConfig = false;
			}
			else if (radioButton1.Checked && !radioButton2.Checked) 
			{
				panel1.BackColor = radioButton1.BackColor = radioButton2.BackColor = SystemColors.Control;
				Configuration.YubiSlot = YubiSlot.SLOT1;
			}
			else if (!radioButton1.Checked && radioButton2.Checked)
			{
				panel1.BackColor = radioButton1.BackColor = radioButton2.BackColor = SystemColors.Control;
				Configuration.YubiSlot = YubiSlot.SLOT2;
			}
			else
			{
				panel1.BackColor = radioButton1.BackColor = radioButton2.BackColor = Color.IndianRed;
			}
		}

		private void saveButton_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
