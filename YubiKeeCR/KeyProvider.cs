using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using KeePass.Plugins;
using KeePass.UI;

using KeePassLib.Cryptography;
using KeePassLib.Keys;
using KeePassLib.Serialization;
using KeePassLib.Utility;

namespace YubiKeeCR
{
	/// <summary>
	/// The key provider must not be used as it creates and auxiliary challenge file.
	/// However you can compile it with USEKEYPROVIDER flag to enable the key provider method
	/// </summary>
	[Obsolete("This class is obsolete, you may not use it")]
	public sealed class YubiKeeCRProvider : KeyProvider
	{
		public override bool SecureDesktopCompatible => true;
		public override bool GetKeyMightShowGui => false;
		public override string Name => "YubiKee Challenge Response";

		YubiWrapper yubi = new YubiWrapper();

		public uint ChallengeKeyLength = 64;

		public override byte[] GetKey(KeyProviderQueryContext ctx)
		{
			if (ctx == null) return null;

			VistaTaskDialog dlg = new VistaTaskDialog()
			{
				CommandLinks = true,
				DefaultButtonID = (int)DialogResult.OK,
				Content = "content",
				MainInstruction = "instruction",
				WindowTitle = "Challenge backup"
			};


			while (!yubi.Init())
			{
				bool retry = MessageService.AskYesNo(
					"Check if your Yubikey is properly inserted or if your plugin installation is corrupted.\n\nTry again?", 
					"Failed to initialize Yubikey Wrapper.",
					true, 
					System.Windows.Forms.MessageBoxIcon.Warning
				);

				if (!retry) return null;
			}

			string databaseFileName = ctx.DatabasePath;
			Regex rgx = new Regex(@"\.kdbx$");
			string databaseChallenge = rgx.Replace(databaseFileName, ".chl");


			byte[] challenge = new byte[] { 1, 2, 3, 4, 5, 6 };

			if (ctx.CreatingNewKey)
			{
				challenge = CryptoRandom.Instance.GetRandomBytes(ChallengeKeyLength);

				if (File.Exists(databaseChallenge))
				{
					bool replace = MessageService.AskYesNo(
						"There is already a database challenge file located in this directory with the same database file name. Proceeding with this action will replace the found challenge file and may result in unrecoverable data loss of one of your databases.\n\nShould we proceeding to create a new challenge file?",
						"WARNING!",
						false,
						MessageBoxIcon.Warning
						);

					if (!replace) return null;
				}

				WriteChallengeFile(databaseChallenge, challenge);

				int byteGroups = challenge.Length / 8 + 1;
				string[] stringGroups = new string[byteGroups];

				for (int i = 0; i < byteGroups; i++)
				{
					stringGroups[i] = BitConverter.ToString(challenge.Skip(i * 8).Take(8).ToArray());
				}

				MessageService.ShowInfo(
					"Before proceding, make sure you write down or print your challenge key so you can access your database. If your challenge file is lost and you have no backup you may be locked out of your database forever. Also, be sure that the challenge file is stored among the database file itself",
					$"The database file is located at: {databaseFileName}",
					$"The challenge file is located at: {databaseChallenge}",
					$"Your challenge key is a {ChallengeKeyLength} byte array",
					string.Join("\n", stringGroups)
				);
			}
			else if (!File.Exists(databaseChallenge))
			{
				bool trymanual = MessageService.AskYesNo(
					"Cannot find the database challenge file. Please check if the file is really missing.\n\nYou will NOT be able to open the database without the database challenge.\n\nDo you want to try to enter a backup challenge byte array?",
					"Failed to read the challenge key file",
					true,
					MessageBoxIcon.Warning
				);
			}

			challenge = ReadChallengeFile(databaseChallenge);

			yubi.ChallengeResponse(YubiSlot.SLOT2, challenge, out byte[] response);





			return response;
		}

		private byte[] ReadChallengeFile(string databaseChallenge)
		{
			var fs = File.OpenRead(databaseChallenge);
			var br = new BinaryReader(fs);
			byte[] challenge = br.ReadBytes((int)ChallengeKeyLength);
			br.Close();

			return challenge;
		}

		private static void WriteChallengeFile(string databaseChallenge, byte[] challenge)
		{
			var fs = File.Create(databaseChallenge);
			var bw = new BinaryWriter(fs);
			bw.Write(challenge);
			bw.Flush();
			bw.Close();
		}
	}
}