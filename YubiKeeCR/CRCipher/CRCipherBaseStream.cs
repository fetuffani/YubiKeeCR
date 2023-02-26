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
using KeePassLib.Cryptography.Cipher;

namespace YubiKeeCR
{
	internal class CRCipherBaseStream : Stream
	{
		protected Stream m_UpStream;
		protected readonly bool m_bWriting;

		protected CRCipherBaseStream(Stream upStream, bool bWriting)
		{
			if (upStream == null)
				throw new ArgumentNullException("upStream");

			m_UpStream = upStream;
			m_bWriting = bWriting;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && m_UpStream != null)
			{
				m_UpStream.Close();
				m_UpStream = null;
			}
			base.Dispose(disposing);
		}

		public override bool CanRead { get { return !m_bWriting; } }
		public override bool CanSeek { get { return false; } }
		public override bool CanWrite { get { return m_bWriting; } }

		public override void Flush()
		{
			Debug.Assert(m_UpStream != null);

			if (m_bWriting && (m_UpStream != null)) m_UpStream.Flush();

		}

		public override long Length
		{
			get { return m_UpStream.Length; }
		}

		public override long Position
		{
			get
			{
				return m_UpStream.Position;
			}
			set
			{
				throw new NotSupportedException();
			}
		}


		public override long Seek(long offset, SeekOrigin origin)
		{
			Debug.Assert(false);
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			Debug.Assert(false);
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}
	}
}