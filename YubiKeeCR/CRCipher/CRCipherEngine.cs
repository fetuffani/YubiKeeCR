/*

    MultiCipher Plugin for Keepass Password Safe
    Copyright (C) 2019 Titas Raha <support@titasraha.com>

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.

*/
using System;
using System.IO;
using System.Diagnostics;
using KeePassLib;
using KeePassLib.Cryptography.Cipher;
using KeePassLib.Keys;
using KeePassLib.Utility;
using System.Security.Cryptography;
using KeePassLib.Cryptography;

namespace YubiKeeCR
{
	public sealed class CRCipherEngine : ICipherEngine2
	{
		private PwUuid m_uuidCipher;

		private static readonly byte[] Level2CipherUuidBytes = new byte[]{
			0x9B, 0x7E, 0x32, 0x50, 0x96, 0xB3, 0x9F, 0x47,
			0xAC, 0x35, 0xBD, 0x80, 0xAB, 0x4F, 0x11, 0xED
		};

		public static readonly PwUuid CipherUuid2 = new PwUuid(Level2CipherUuidBytes);

		public CRCipherEngine()
		{
			m_uuidCipher = new PwUuid(Level2CipherUuidBytes);
		}


		public PwUuid CipherUuid
		{
			get
			{
				Debug.Assert(m_uuidCipher != null);
				return m_uuidCipher;
			}
		}

		public string DisplayName
		{
			get { return "YubiKeeCR (AES256 + CR64/256)"; }
		}

		public int KeyLength { get { return 32; } }  // Formalize the use of 32 byte Key

		public int IVLength { get { return 16; } }   // Formalize the use of 16 byte IV


		public Stream EncryptStream(Stream sPlainText, byte[] pbKey32, byte[] pbIV16)
		{

			//return new StandardAesEngine().EncryptStream(new CRCipherChallenge(sPlainText, true), pbKey32, pbIV16); 


			//this way the Challenge key is encrypted inside the AES layer so it is not exposed unless you know the database master key
			return new CRCipherChallenge(new StandardAesEngine().EncryptStream(sPlainText, pbKey32, pbIV16), true);
		}

		public Stream DecryptStream(Stream sEncrypted, byte[] pbKey32, byte[] pbIV16)
		{
			//return new StandardAesEngine().DecryptStream(new CRCipherChallenge(sEncrypted, false), pbKey32, pbIV16) ;


			return new CRCipherChallenge(new StandardAesEngine().DecryptStream(sEncrypted, pbKey32, pbIV16), false);
		}
	}
}