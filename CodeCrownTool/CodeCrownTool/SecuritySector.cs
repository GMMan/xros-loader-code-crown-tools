// SPDX-License-Identifier: GPL-3.0-or-later
/*
 * CodeCrownTool: Digimon Xros Loader Code Crown Tool
 * Copyright (C) 2022  cyanic
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CodeCrownTool
{
    /// <summary>
    /// Code Crown security sector management
    /// </summary>
    public static class SecuritySector
    {
        static readonly string MAGIC = "BGSASTNHOD01A02I";

        /// <summary>
        /// Generates a security sector.
        /// </summary>
        /// <param name="cid">The SD card's CID as a hex string.</param>
        /// <param name="csd">The SD card's CSD as a hex string.</param>
        /// <param name="ssr">The SD card's SSR as a hex string.</param>
        /// <returns>The generated security sector.</returns>
        public static byte[] GenerateSecuritySector(string cid, string csd, string ssr)
        {
            if (string.IsNullOrEmpty(cid)) throw new ArgumentNullException(nameof(cid));
            if (string.IsNullOrEmpty(csd)) throw new ArgumentNullException(nameof(csd));
            if (string.IsNullOrEmpty(ssr)) throw new ArgumentNullException(nameof(ssr));

            byte[] cidBuf = HexStringToBytes(cid);
            byte[] csdBuf = HexStringToBytes(csd);
            byte[] ssrBuf = HexStringToBytes(ssr);
            return GenerateSecuritySector(cidBuf, csdBuf, ssrBuf);
        }

        /// <summary>
        /// Generates a security sector.
        /// </summary>
        /// <param name="cid">The SD card's CID in most significant byte order.</param>
        /// <param name="csd">The SD card's CDD in most significant byte order.</param>
        /// <param name="ssr">The SD card's SSR in most significant byte order.</param>
        /// <returns>The generated security sector.</returns>
        public static byte[] GenerateSecuritySector(byte[] cid, byte[] csd, byte[] ssr)
        {
            if (cid == null) throw new ArgumentNullException(nameof(cid));
            if (cid.Length != 16) throw new ArgumentException("CID is not 16 bytes in length.", nameof(cid));
            if (csd == null) throw new ArgumentNullException(nameof(csd));
            if (csd.Length != 16) throw new ArgumentException("CSD is not 16 bytes in length.", nameof(csd));
            if (ssr == null) throw new ArgumentNullException(nameof(ssr));
            if (ssr.Length != 64) throw new ArgumentException("SD status register is not 64 bytes in length.", nameof(ssr));

            byte[] blob = new byte[512];
            new Random().NextBytes(blob);

            cid = ConvertR2(cid);
            csd = ConvertR2(csd);

            int startOffset = cid[0];

            for (int i = 0; i < MAGIC.Length; ++i)
            {
                blob[startOffset + i] = (byte)((byte)MAGIC[i] ^ cid[i]);
            }

            ushort ssrSum = 0;
            for (int i = 2; i < 14; ++i)
            {
                ssrSum += ssr[i];
            }

            blob[startOffset + 0x10] = (byte)((byte)ssrSum ^ cid[0]);
            blob[startOffset + 0x11] = (byte)((byte)(ssrSum >> 8) ^ csd[0]);
            blob[startOffset + 0x12] = (byte)(cid[0] ^ csd[0]);

            return blob;
        }

        /// <summary>
        /// Verifies that a security sector is valid.
        /// </summary>
        /// <param name="blob">The security sector.</param>
        /// <param name="cid">The SD card's CID as a hex string.</param>
        /// <param name="csd">The SD card's CSD as a hex string.</param>
        /// <param name="ssr">The SD card's SSR as a hex string.</param>
        /// <returns>Whether the security sector is valid given the CID, CSD, and SSR.</returns>
        public static bool ValidateSecuritySector(byte[] blob, string cid, string csd, string ssr)
        {
            if (string.IsNullOrEmpty(cid)) throw new ArgumentNullException(nameof(cid));
            if (string.IsNullOrEmpty(csd)) throw new ArgumentNullException(nameof(csd));
            if (string.IsNullOrEmpty(ssr)) throw new ArgumentNullException(nameof(ssr));

            byte[] cidBuf = HexStringToBytes(cid);
            byte[] csdBuf = HexStringToBytes(csd);
            byte[] ssrBuf = HexStringToBytes(ssr);
            return ValidateSecuritySector(blob, cidBuf, csdBuf, ssrBuf);
        }

        /// <summary>
        /// Verifies that a security sector is valid.
        /// </summary>
        /// <param name="blob">The security sector.</param>
        /// <param name="cid">The SD card's CID in most significant byte order.</param>
        /// <param name="csd">The SD card's CDD in most significant byte order.</param>
        /// <param name="ssr">The SD card's SSR in most significant byte order.</param>
        /// <returns>Whether the security sector is valid given the CID, CSD, and SSR.</returns>
        public static bool ValidateSecuritySector(byte[] blob, byte[] cid, byte[] csd, byte[] ssr)
        {
            if (blob == null) throw new ArgumentNullException(nameof(blob));
            if (blob.Length != 512) throw new ArgumentException("Blob is not 512 bytes in length.", nameof(blob));
            if (cid == null) throw new ArgumentNullException(nameof(cid));
            if (cid.Length != 16) throw new ArgumentException("CID is not 16 bytes in length.", nameof(cid));
            if (csd == null) throw new ArgumentNullException(nameof(csd));
            if (csd.Length != 16) throw new ArgumentException("CSD is not 16 bytes in length.", nameof(csd));
            if (ssr == null) throw new ArgumentNullException(nameof(ssr));
            if (ssr.Length != 64) throw new ArgumentException("SD status register is not 64 bytes in length.", nameof(ssr));

            cid = ConvertR2(cid);
            csd = ConvertR2(csd);

            int startOffset = cid[0];

            for (int i = 0; i < MAGIC.Length; ++i)
            {
                if (blob[startOffset + i] != ((byte)MAGIC[i] ^ cid[i])) return false;
            }

            if (blob[startOffset + 0x12] != (cid[0] ^ csd[0])) return false;

            ushort ssrSum = 0;
            for (int i = 2; i < 14; ++i)
            {
                ssrSum += ssr[i];
            }

            return blob[startOffset + 0x10] == ((byte)ssrSum ^ cid[0]) && blob[startOffset + 0x11] == ((byte)(ssrSum >> 8) ^ csd[0]);
        }

        // Function for converting CID/CSD to order used in algorithm
        static byte[] ConvertR2(byte[] buf)
        {
            // 1. Recalculate CRC7 because some readers don't return the real value
            byte crc = (byte)(Crc7.Calculate(0, buf, buf.Length - 1) | 1);
            byte[] newBuf = new byte[16];
            newBuf[0] = crc;
            // 2. Reverse all the bytes
            for (int i = 1; i < newBuf.Length; ++i)
            {
                newBuf[i] = buf[buf.Length - 1 - i];
            }
            // 3. Add starting byte 0x3f
            // But we don't, because there's no more space

            return newBuf;
        }

        public static byte[] HexStringToBytes(string s)
        {
            if (s.Length % 2 != 0) throw new ArgumentException("String length is not even.", nameof(s));
            byte[] buf = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
            {
                buf[i / 2] = Convert.ToByte(s.Substring(i, 2), 16);
            }
            return buf;
        }
    }
}
