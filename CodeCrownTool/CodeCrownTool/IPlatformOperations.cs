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
    /// Provides per-OS operation implementations.
    /// </summary>
    public interface IPlatformOperations
    {
        /// <summary>
        /// Gets the volumes that can be authenticated.
        /// </summary>
        /// <returns>The names of the volumes that can be authenticated.</returns>
        IReadOnlyCollection<string> GetVolumes();
        /// <summary>
        /// Gets a platform-specific disk instance for the given disk.
        /// </summary>
        /// <param name="name">The path of the disk to get.</param>
        /// <returns>The platform-specific disk.</returns>
        IDisk GetDisk(string name);
    }
}
