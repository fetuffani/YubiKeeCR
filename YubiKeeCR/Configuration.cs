using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeePassLib;

namespace YubiKeeCR
{
	internal class Configuration
	{
		public static YubiSlot YubiSlot { get; set; } = YubiSlot.SLOT2;
		public static int Iterations { get; set; } = 10000;

		public static PwDatabase Database { get; set; } = null;
	}
}
