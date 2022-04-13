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
using System.IO;
using System.Runtime.InteropServices;
using Mono.Unix;
using Mono.Unix.Native;

namespace CodeCrownTool.Linux
{
    /// <summary>
    /// Provides Linux-specific disk implementation.
    /// </summary>
    class LinuxDisk : IDisk
    {
        const int BYTES_PER_SECTOR = 512;

        bool disposed;
        int fd = -1;

        /// <inheritdoc/>
        public string DisplayName { get; }

        /// <summary>
        /// Instantiates a new instance of <see cref="LinuxDisk"/>.
        /// </summary>
        /// <param name="path">The path to the disk device node.</param>
        public LinuxDisk(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            fd = Syscall.open(path, OpenFlags.O_RDWR);
            if (fd == -1) {
                var errno = Stdlib.GetLastError();
                throw new IOException(UnixMarshal.GetErrorDescription(errno));
            }
            DisplayName = path;
        }

        void CheckDisposed()
        {
            if (disposed) throw new ObjectDisposedException(GetType().FullName);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (fd != -1)
            {
                Syscall.close(fd);
                fd = -1;
            }
            disposed = true;
        }

        /// <inheritdoc/>
        public bool IsOkForCheck()
        {
            // No special conditions
            return !disposed;
        }

        /// <inheritdoc/>
        public bool Lock()
        {
            CheckDisposed();
            // Mono.Posix does not expose flock() syscall
            return true;
        }

        /// <inheritdoc/>
        public byte[] ReadSectors(int numSectors)
        {
            if (numSectors < 0) throw new ArgumentOutOfRangeException("Number of sectors cannot be negative.", nameof(numSectors));
            CheckDisposed();
            byte[] buffer = new byte[numSectors * BYTES_PER_SECTOR];
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            long read = Syscall.read(fd, handle.AddrOfPinnedObject(), (ulong)buffer.Length);
            handle.Free();
            Array.Resize(ref buffer, (int)read);
            return buffer;
        }

        /// <inheritdoc/>
        public void SeekSector(int sector)
        {
            if (sector < 0) throw new ArgumentOutOfRangeException("Sector cannot be negative.", nameof(sector));
            CheckDisposed();
            Syscall.lseek(fd, (long)sector * BYTES_PER_SECTOR, SeekFlags.SEEK_SET);
        }

        /// <inheritdoc/>
        public bool Unlock()
        {
            CheckDisposed();
            return true;
        }

        /// <inheritdoc/>
        public long GetTotalSectors()
        {
            return new DriveInfo(DisplayName).TotalSize / 512;
        }

        /// <inheritdoc/>
        public int WriteSectors(int numSectors, byte[] buffer, int bufOffset)
        {
            if (numSectors < 0) throw new ArgumentOutOfRangeException("Number of sectors cannot be negative.", nameof(numSectors));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (bufOffset < 0) throw new ArgumentOutOfRangeException("Offset cannot be negative.", nameof(bufOffset));
            if (bufOffset >= buffer.Length) throw new ArgumentOutOfRangeException("Offset cannot be past buffer length.", nameof(bufOffset));
            if (buffer.Length < bufOffset + numSectors * 512) throw new ArgumentException("Buffer is too small given number of sectors and offset.", nameof(buffer));

            CheckDisposed();

            byte[] sectors = new byte[numSectors * 512];
            Buffer.BlockCopy(buffer, bufOffset, sectors, 0, sectors.Length);
            GCHandle handle = GCHandle.Alloc(sectors, GCHandleType.Pinned);
            var written = Syscall.write(fd, handle.AddrOfPinnedObject(), (ulong)sectors.Length);
            handle.Free();
            return (int)written;
        }

        /// <inheritdoc/>
        public void GetCidCsdSsr(out string cid, out string csd, out string ssr)
        {
            string sysfsPath = Path.Combine("/sys/block", DisplayName.Substring("/dev/".Length), "device");
            if (!Directory.Exists(sysfsPath)) throw new DirectoryNotFoundException($"Cannot find sysfs path \"{sysfsPath}\".");

            cid = File.ReadAllText(Path.Combine(sysfsPath, "cid")).TrimEnd();
            csd = File.ReadAllText(Path.Combine(sysfsPath, "csd")).TrimEnd();
            ssr = File.ReadAllText(Path.Combine(sysfsPath, "ssr")).TrimEnd();
        }
    }
}
