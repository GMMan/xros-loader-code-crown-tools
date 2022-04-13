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

namespace CodeCrownTool.Windows
{
    /// <summary>
    /// Provides Windows-specific platform operations.
    /// </summary>
    public class WindowsPlatformOperations : IPlatformOperations
    {
        /// <inheritdoc/>
        public IDisk GetDisk(string name)
        {
            return new WindowsDisk(name);
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<string> GetVolumes()
        {
            List<string> drives = new List<string>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Removable)
                    drives.Add(drive.Name);
            }
            return drives;
        }
    }
}
