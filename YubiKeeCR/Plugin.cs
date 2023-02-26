// #define USEKEYPROVIDER

using System;
using System.Windows.Forms;

using KeePass.Plugins;
using KeePass.UI;

using KeePassLib;
using KeePassLib.Utility;

namespace YubiKeeCR
{
	public sealed class YubiKeeCRExt : Plugin
	{
		private IPluginHost m_host = null;

#if USEKEYPROVIDER
		private YubiKeeCRProvider m_prov = new YubiKeeCRProvider();
#endif
		private CRCipherEngine m_CRCipherEngine = new CRCipherEngine();

		public override String UpdateUrl
		{
			get { return "https://raw.githubusercontent.com/fetuffani/YubiKeeCR/master/VERSION"; }
		}

		public override bool Initialize(IPluginHost host)
		{
#if DEBUG
			MessageService.ShowWarning($"DATABASES SAVED WITH THE YUBIKEYCR IN DEBUG MODE WILL NOT BE COMPATIBLE WITH NON DEBUG!!!\n\n" +
				$"BE WARNED THAT YOU MAY ONLY USE THE DEBUG MODE WITH DUMMY DATABASES OR YOU MAY CORRUPT YOUR DATABASE!!!\n\n" +
				$"YOU HAVE BEEN WARNED");
#endif
			if (host == null) return false;
			m_host = host;
#if USEKEYPROVIDER
			m_host.KeyProviderPool.Add(m_prov);
#endif
			m_host.CipherPool.AddCipher(m_CRCipherEngine);

			return true;
		}

		public override ToolStripMenuItem GetMenuItem(PluginMenuType t)
		{
			if (t == PluginMenuType.Main)
			{
				ToolStripMenuItem tsmi = new ToolStripMenuItem();
				tsmi.Text = "YubiKeeCR options";
				tsmi.Click += Tsmi_Click;

				return tsmi;
			}

			return null;
		}

		private void Tsmi_Click(object sender, EventArgs e)
		{
			var form = new Settings(m_host);

			UIUtil.ShowDialogAndDestroy(form);
		}
	}
}