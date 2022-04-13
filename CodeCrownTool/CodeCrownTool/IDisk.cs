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

namespace CodeCrownTool
{
    /// <summary>
    /// Represents a disk device.
    /// </summary>
    public interface IDisk : IDisposable
    {
        /// <summary>
        /// Gets the name of the disk.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Gains exclusive lock on the disk.
        /// </summary>
        /// <returns><c>true</c> if lock obtained, otherwise <c>false</c>.</returns>
        bool Lock();
        /// <summary>
        /// Releases exclusive lock on the disk.
        /// </summary>
        /// <returns><c>true</c> if lock was released, otherwise <c>false</c>.</returns>
        bool Unlock();
        /// <summary>
        /// Seek to a sector.
        /// </summary>
        /// <param name="sector">The sector number to seek to.</param>
        void SeekSector(int sector);
        /// <summary>
        /// Gets the total number of sectors on the disk.
        /// </summary>
        /// <returns>The number of sectors on the disk.</returns>
        long GetTotalSectors();
        /// <summary>
        /// Reads sectors.
        /// </summary>
        /// <param name="numSectors">The number of sectors to read.</param>
        /// <returns>The data that was read. It may be less than <paramref name="numSectors"/>.</returns>
        byte[] ReadSectors(int numSectors);
        /// <summary>
        /// Writes sectors.
        /// </summary>
        /// <param name="numSectors">The number of sectors to write.</param>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="bufOffset">The offset in the buffer to start writing from.</param>
        /// <returns></returns>
        int WriteSectors(int numSectors, byte[] buffer, int bufOffset);
        /// <summary>
        /// Checks whether the disk state supports authentication.
        /// </summary>
        /// <returns><c>true</c> if the disk's state supports authentication, <c>false</c> otherwise.</returns>
        bool IsOkForCheck();
        /// <summary>
        /// Gets the SD card's CID, CSD, and SSR
        /// </summary>
        /// <param name="cid">The obtained CID.</param>
        /// <param name="csd">The obtained CSD.</param>
        /// <param name="ssr">The obtained SSR.</param>
        void GetCidCsdSsr(out string cid, out string csd, out string ssr);
    }
}
