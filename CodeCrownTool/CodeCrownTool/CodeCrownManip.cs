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
using System.Text;
using System.IO;

namespace CodeCrownTool
{
    /// <summary>
    /// Provides functions for manipulating a Code Crown
    /// </summary>
    public class CodeCrownManip : IDisposable
    {
        IDisk disk;
        byte[] cid;
        byte[] csd;
        byte[] ssr;
        bool hasCardRegs;
        private bool disposedValue;

        public CodeCrownManip(IDisk disk, bool hasCardRegs)
        {
            this.disk = disk ?? throw new ArgumentNullException(nameof(disk));
            if (hasCardRegs)
            {
                disk.GetCidCsdSsr(out string sCid, out string sCsd, out string sSsr);
                cid = SecuritySector.HexStringToBytes(sCid);
                csd = SecuritySector.HexStringToBytes(sCsd);
                ssr = SecuritySector.HexStringToBytes(sSsr);
            }
            this.hasCardRegs = hasCardRegs;
        }

        public CodeCrownManip(IDisk disk, string cid, string csd, string ssr)
        {
            this.disk = disk ?? throw new ArgumentNullException(nameof(disk));

            if (string.IsNullOrEmpty(cid)) throw new ArgumentNullException(nameof(cid));
            if (string.IsNullOrEmpty(csd)) throw new ArgumentNullException(nameof(csd));
            if (string.IsNullOrEmpty(ssr)) throw new ArgumentNullException(nameof(ssr));

            try
            {
                this.cid = SecuritySector.HexStringToBytes(cid);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("CID is not a valid hex string.", nameof(cid), ex);
            }
            try
            {
                this.csd = SecuritySector.HexStringToBytes(csd);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("CSD is not a valid hex string.", nameof(csd), ex);
            }
            try
            {
                this.ssr = SecuritySector.HexStringToBytes(ssr);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("SSR is not a valid hex string.", nameof(ssr), ex);
            }
            hasCardRegs = true;
        }

        public void CreateCodeCrown()
        {
            CheckDisposed();
            CheckHasCardRegs();
            var crownLba = GetCrownLba();
            byte[] sec = SecuritySector.GenerateSecuritySector(cid, csd, ssr);
            disk.SeekSector((int)crownLba);
            if (disk.WriteSectors(1, sec, 0) != sec.Length)
                throw new IOException("Failed to write data.");
        }

        public bool VerifyCodeCrown()
        {
            CheckDisposed();
            CheckHasCardRegs();
            var crownLba = GetCrownLba();
            disk.SeekSector((int)crownLba);
            byte[] sec = disk.ReadSectors(1);
            return SecuritySector.ValidateSecuritySector(sec, cid, csd, ssr);
        }

        public void UploadData(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length != 0x100000) throw new ArgumentException("Data must be 1MB in length.", nameof(data));
            CheckDisposed();

            var crownLba = GetCrownLba();
            disk.SeekSector((int)(crownLba + 1));
            if (disk.WriteSectors(data.Length / 0x200, data, 0) != data.Length)
                throw new IOException("Failed to write data.");
        }

        public byte[] DumpData()
        {
            CheckDisposed();
            var crownLba = GetCrownLba();
            disk.SeekSector((int)(crownLba + 1));
            return disk.ReadSectors(0x100000 / 0x200);
        }

        uint GetCrownLba()
        {
            // 1. Check that the disk is not GPT
            disk.SeekSector(1);
            byte[] gpt = disk.ReadSectors(1);
            if (Encoding.ASCII.GetString(gpt, 0, 8) == "EFI PART")
                throw new ArgumentException("Cannot process GPT disks.", nameof(disk));
            // We should also check whether this is MBR, but strictly speaking the 55aa
            // marker is not required for non-bootable disks

            // 2. Find offset of end of first partition. We'll allow multiple partitions
            // only as long as there's enough of a gap to fit Code Crown data. If the
            // first partition stretches all the way to the end of disk, also fail.
            disk.SeekSector(0);
            byte[] mbr = disk.ReadSectors(1);
            if (mbr[0x1be + (0 * 16) + 4] == 0) throw new Exception("First partition cannot be free.");
            List<Tuple<uint, uint>> partitions = new List<Tuple<uint, uint>>();
            for (int i = 0; i < 4; ++i)
            {
                // Skip free partition
                if (mbr[0x1be + (i * 16) + 4] == 0) continue;
                uint startLba = BitConverter.ToUInt32(mbr, 0x1be + (i * 16) + 8);
                uint sectorLength = BitConverter.ToUInt32(mbr, 0x1be + (0 * 16) + 12);
                partitions.Add(new Tuple<uint, uint>(startLba, startLba + sectorLength));
            }

            // 3. Check for free space. Requires 1MB + 512 byte = 0x801 sectors of space.
            // Strictly speaking only 0xe0200 bytes are needed, but downloader will use
            // full 1MB
            if (partitions.Count == 1)
            {
                var endLba = disk.GetTotalSectors();
                if (endLba - partitions[0].Item2 < 0x801)
                    throw new Exception("Not enough space after first partition for Code Crown data.");
                return partitions[0].Item2;
            }
            else
            {
                if (partitions[1].Item1 - partitions[0].Item2 < 0x801)
                    throw new Exception("Not enough space after first partition and before next partition for Code Crown data.");
                return partitions[0].Item2;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    disk.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        void CheckDisposed()
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);
        }

        void CheckHasCardRegs()
        {
            if (!hasCardRegs) throw new InvalidOperationException("This operation requires card registers to be available.");
        }
    }
}
