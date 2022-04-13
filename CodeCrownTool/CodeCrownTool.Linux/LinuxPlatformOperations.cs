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
using Mono.Unix;

namespace CodeCrownTool.Linux
{
    /// <summary>
    /// Provides Linux-specific platform operations.
    /// </summary>
    public class LinuxPlatformOperations : IPlatformOperations
    {
        /// <inheritdoc/>
        public IDisk GetDisk(string name)
        {
            return new LinuxDisk(name);
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<string> GetVolumes()
        {
            if (!Directory.Exists("/sys")) throw new NotSupportedException("sysfs is not available.");
            List<string> drives = new List<string>();
            foreach (var dev in Directory.GetDirectories("/sys/block"))
            {
                string ueventPath = Path.Combine(dev, "device/uevent");
                if (!File.Exists(ueventPath)) continue;
                string[] uevent = File.ReadAllLines(ueventPath);
                foreach (var line in uevent)
                {
                    bool doAdd = false;
                    // Detect USB SCSI device (e.g. card reader)
                    if (line == "DRIVER=sd")
                    {
                        string realPath = UnixPath.GetRealPath(dev);
                        if (realPath.Contains("/usb"))
                            doAdd = true;
                    }
                    // Detect MMC device (e.g. laptop SD card reader)
                    else if (line == "DRIVER=mmcblk")
                    {
                        doAdd = true;
                    }

                    if (doAdd)
                    {
                        drives.Add(Path.Combine("/dev", Path.GetFileName(dev)));
                        break;
                    }
                }
            }
            return drives;
        }
    }
}
