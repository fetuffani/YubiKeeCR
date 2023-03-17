using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using KeePassLib.Security;

namespace YubiKeeCR
{
	public partial class RecoveryDialog : Form
	{
		internal byte[] Secret { get; set; }
		public RecoveryDialog()
		{
			InitializeComponent();
			DialogResult = DialogResult.Cancel;
		}

		private static byte[] StringToByteArray(string hex)
		{
			return Enumerable.Range(0, hex.Length)
							 .Where(x => x % 2 == 0)
							 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
							 .ToArray();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (textBox1.Text.Length == 20 * 2) // 20 bytes
			{
				Secret = StringToByteArray(textBox1.Text
					.Replace(" ", String.Empty)
					.Replace("-", String.Empty)
					.Replace("0x", String.Empty)
					.Replace("0X", String.Empty));

				DialogResult = DialogResult.OK;

				Close();
			}
			else
			{
				MessageBox.Show("Invalid secret length, be sure the enter the full 20 bytes (40 characters)");
			}
		}

		private void RecoveryDialog_FormClosing(object sender, FormClosingEventArgs e)
		{

		}
	}
}
