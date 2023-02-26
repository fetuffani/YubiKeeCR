// #define USEKEYPROVIDER

using System;
using KeePass.Plugins;

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
			if (host == null) return false;
			m_host = host;
#if USEKEYPROVIDER
			m_host.KeyProviderPool.Add(m_prov);
#endif
			m_host.CipherPool.AddCipher(m_CRCipherEngine);

			return true;
		}
	}
}