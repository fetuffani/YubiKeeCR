using KeePassLib.Cryptography;
using KeePassLib.Utility;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace YubiKeeCR
{
	internal class CRCipherChallenge : CRCipherBaseStream
	{
		private readonly uint ChallengeKeyLength = 64;

		YubiWrapper m_Yubi = new YubiWrapper();

		protected bool m_Writing;

		protected bool m_IsDisposed = false;

		protected ICryptoTransform m_AesTransform;
		protected CryptoStream m_AesStream;
		private byte[] pbKey32;
		private byte[] pbIV16;

		private readonly byte[] DEADBEEF = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

		public CRCipherChallenge(Stream sbaseStream, bool bWriting) : base(sbaseStream, bWriting)
		{
			m_Writing = bWriting;

			Aes aes = Aes.Create();
			aes.Padding = PaddingMode.PKCS7;
			aes.Mode = CipherMode.CBC;

#if DEBUG
#else
			InitYubikeyConnection();
#endif
			if (bWriting)
				InitWrite();
			else
				InitRead();

#if DEBUG
#else
			m_Yubi.Close();
#endif

			if (bWriting)
				m_AesTransform = aes.CreateEncryptor(pbKey32, pbIV16);
			else
				m_AesTransform = aes.CreateDecryptor(pbKey32, pbIV16);

			m_AesStream = new CryptoStream(sbaseStream, m_AesTransform, bWriting ? CryptoStreamMode.Write : CryptoStreamMode.Read);
		}



		private void InitYubikeyConnection()
		{
			while (!m_Yubi.Init())
			{
				bool retry = MessageService.AskYesNo(
					"Check if your Yubikey is properly inserted or if your plugin installation is corrupted.\n\nTry again?",
					"Failed to initialize Yubikey Wrapper.",
					true,
					System.Windows.Forms.MessageBoxIcon.Warning
				);

				if (!retry) throw new Exception("Failed Initializing Yubikey Wrapper");
			}
		}

		private void InitRead()
		{
			//Must read/write to the upstream otherwise we will encrypt the challenge with it's response making it unrecoverable
			BinaryReader br = new BinaryReader(m_UpStream); 

			byte[] deadbeef = br.ReadBytes(4); // 0xDEADBEEF - sanity check and also make it easier to find this section when reading the database with an hex editor

			bool validDeadbeef = deadbeef.SequenceEqual(DEADBEEF);
			Debug.Assert(validDeadbeef);

			if (!validDeadbeef) //Should display this when in Release?
			{
				MessageService.ShowInfo($"DEADBEEF FAILED:\n" +
					$"GOT\t\t{BitConverter.ToString(deadbeef)}\n" +
					$"EXPECTED\t\t{BitConverter.ToString(DEADBEEF)}\n" +
					$"BOOL\t\t{validDeadbeef}");
			}

			ushort version = (ushort)br.ReadUInt16();
			Configuration.YubiSlot = (YubiSlot)br.ReadByte();
			int challengeLength = (int)br.ReadInt32();
			byte[] challenge = br.ReadBytes(challengeLength); 

			int saltLength = (int)br.ReadInt32(); 
			byte[] salt = br.ReadBytes(saltLength);

			Configuration.Iterations = (int)br.ReadInt32();

#if DEBUG
			MessageService.ShowInfo($"READ DATABASE CONFIG:\n" +
				$"Configuration.YubiSlot: {Configuration.YubiSlot}\n" +
				$"Configuration.Iterations: {Configuration.Iterations}"
				);
#endif

#if DEBUG
			byte[] response = challenge;
#else
			m_Yubi.ChallengeResponse(Configuration.YubiSlot, challenge, out byte[] response);
#endif
			Rfc2898DeriveBytes rfc = new Rfc2898DeriveBytes(response, salt, Configuration.Iterations);

			pbKey32 = rfc.GetBytes(32);
			pbIV16 = rfc.GetBytes(16);

			rfc.Dispose();
		}

		private void InitWrite()
		{
			BinaryWriter bw = new BinaryWriter(m_UpStream);

			bw.Write(DEADBEEF); //placeholder to easily find this section in the hex file editor

			bw.Write((ushort)0); //version

			bw.Write((byte)Configuration.YubiSlot); //Yubikey Slot

#if DEBUG
			byte[] challenge = Enumerable.Repeat((byte)0xFF, (int)ChallengeKeyLength).ToArray(); //same as deadbeed
#else
			byte[] challenge = CryptoRandom.Instance.GetRandomBytes(ChallengeKeyLength);
#endif
			bw.Write((int)challenge.Length); //challenge length
			bw.Write(challenge); //challenge

#if DEBUG
			byte[] salt = Enumerable.Repeat((byte)0xEE, (int)ChallengeKeyLength).ToArray(); //same as deadbeed
#else
			byte[] salt = CryptoRandom.Instance.GetRandomBytes(ChallengeKeyLength);
#endif
			bw.Write((int)salt.Length); //salt length
			bw.Write(salt); //salt

			bw.Write((int)Configuration.Iterations); // rfc iteraction number

#if DEBUG
			byte[] response = challenge;
#else
			m_Yubi.ChallengeResponse(Configuration.YubiSlot, challenge, out byte[] response);
#endif
			Rfc2898DeriveBytes rfc = new Rfc2898DeriveBytes(response, salt, Configuration.Iterations);

			pbKey32 = rfc.GetBytes(32);
			pbIV16 = rfc.GetBytes(16);

			rfc.Dispose();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			Debug.Assert(!m_bWriting);
			if (m_bWriting) throw new InvalidOperationException();

			if (m_IsDisposed) throw new ObjectDisposedException("CRCipherChallenge already disposed");

			return m_AesStream.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			Debug.Assert(m_bWriting);
			if (!m_bWriting) throw new InvalidOperationException();

			if (m_IsDisposed) throw new ObjectDisposedException("CRCipherChallenge already disposed");

			m_AesStream.Write(buffer, offset, count);

		}

		protected override void Dispose(bool disposing)
		{
			m_AesStream.Dispose();
			m_AesTransform.Dispose();
			MemUtil.ZeroByteArray(pbIV16);
			MemUtil.ZeroByteArray(pbKey32);

			m_IsDisposed = true;
			base.Dispose(disposing);
		}
	}
}