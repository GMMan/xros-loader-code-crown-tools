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
using McMaster.Extensions.CommandLineUtils;
using CodeCrownTool.Linux;
using CodeCrownTool.Windows;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using System.ComponentModel.DataAnnotations;

namespace CodeCrownTool
{
    [Command("CodeCrownTool", FullName = "Digimon Xros Loader Code Crown Tool")]
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    [Subcommand(
        typeof(ListDevicesCommand),
        typeof(CreateCommand),
        typeof(VerifyCommand),
        typeof(DumpCommand),
        typeof(LoadCommand),
        typeof(ExtractCommand))]
    class Program : BaseCommand
    {
        [Option("-i|--cid", "Card identification register value", CommandOptionType.SingleValue)]
        public string Cid { get; set; }
        [Option("-s|--csd", "Card specific data register value", CommandOptionType.SingleValue)]
        public string Csd { get; set; }
        [Option("-r|--ssr", "Card SD status register value", CommandOptionType.SingleValue)]
        public string Ssr { get; set; }

        public IDisk GetDisk(string path)
        {
            var platform = GetPlatform();
            if (!platform.GetVolumes().Contains(path))
            {
                Console.Error.WriteLine("Could not find specified device in available disks.");
                Environment.Exit(-4);
            }
            var disk = platform.GetDisk(path);
            if (!disk.IsOkForCheck())
                throw new InvalidOperationException("Disk cannot be used as a Code Crown.");
            return disk;
        }

        public IPlatformOperations GetPlatform()
        {
            IPlatformOperations platform = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                platform = new WindowsPlatformOperations();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                platform = new LinuxPlatformOperations();
            }
            else
            {
                Console.Error.WriteLine("Current platform is not supported.");
                Environment.Exit(-3);
            }
            return platform;
        }

        protected override int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 1;
        }

        static int Main(string[] args)
        {
            try
            {
                return CommandLineApplication.Execute<Program>(args);
            }
            catch (CommandParsingException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Something went wrong: " + ex.Message);
                return -2;
            }
        }

        static string GetVersion()
            => typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        public static bool CheckValidCrownData(byte[] dump)
        {
            if (dump.Length != 0x100000) return false;
            return dump[0] == 'D' && dump[1] == 'X' && dump[2] == 'L' && dump[3] >= 1 && dump[3] <= 4;
        }
    }

    [Command("list", "List available disks")]
    class ListDevicesCommand : BaseCommand
    {
        Program Parent { get; set; }

        protected override int OnExecute(CommandLineApplication app)
        {
            foreach (var vol in Parent.GetPlatform().GetVolumes())
            {
                Console.WriteLine(vol);
            }
            return 0;
        }
    }

    // Things that require a device path
    abstract class DeviceOperationCommand : BaseCommand
    {
        [Argument(0, "devicePath", "Path to card device")]
        [LegalFilePath]
        [Required]
        public string DevicePath { get; set; }

        protected CodeCrownManip GetManip(Program parent, bool needRegs)
        {
            if (parent.Cid != null || parent.Csd != null || parent.Ssr != null)
            {
                if (parent.Cid == null) throw new ArgumentNullException("CID needs to be specified when any of CID, CSD, or SSR is specified.", "cid");
                if (parent.Csd == null) throw new ArgumentNullException("CSD needs to be specified when any of CID, CSD, or SSR is specified.", "csd");
                if (parent.Ssr == null) throw new ArgumentNullException("SSR needs to be specified when any of CID, CSD, or SSR is specified.", "ssr");

                return new CodeCrownManip(parent.GetDisk(DevicePath), parent.Cid, parent.Csd, parent.Ssr);
            }
            else
            {
                return new CodeCrownManip(parent.GetDisk(DevicePath), needRegs);
            }
        }
    }

    [Command("create", "Create Code Crown with valid security sector")]
    class CreateCommand : DeviceOperationCommand
    {
        Program Parent { get; set; }

        protected override int OnExecute(CommandLineApplication app)
        {
            using (var manip = GetManip(Parent, true))
            {
                manip.CreateCodeCrown();
            }
            return 0;
        }
    }

    [Command("verify", "Verify Code Crown security sector")]
    class VerifyCommand : DeviceOperationCommand
    {
        Program Parent { get; set; }

        protected override int OnExecute(CommandLineApplication app)
        {
            using (var manip = GetManip(Parent, true))
            {
                if (manip.VerifyCodeCrown())
                {
                    Console.WriteLine("Code Crown is valid.");
                }
                else
                {
                    Console.WriteLine("Code crown is not valid.");
                    return 2;
                }
            }
            return 0;
        }
    }

    [Command("dump", "Dump quest data from Code Crown to file")]
    class DumpCommand : DeviceOperationCommand
    {
        [Argument(1, "dataPath", "Path to file to dump to")]
        [LegalFilePath]
        public string DataPath { get; set; } = "quest.bin";

        Program Parent { get; set; }

        protected override int OnExecute(CommandLineApplication app)
        {
            using (var manip = GetManip(Parent, false))
            {
                byte[] dump = manip.DumpData();
                if (!Program.CheckValidCrownData(dump))
                {
                    Console.Error.WriteLine("Card does not contain valid quest data.");
                    return 2;
                }
                File.WriteAllBytes(DataPath, dump);
            }
            return 0;
        }
    }

    [Command("load", "Install quest file to Code Crown")]
    class LoadCommand : DeviceOperationCommand
    {
        [Argument(1, "dataPath", "Path to file to load from")]
        [FileExists]
        public string DataPath { get; set; }

        Program Parent { get; set; }

        protected override int OnExecute(CommandLineApplication app)
        {
            using (var manip = GetManip(Parent, false))
            {
                byte[] data = File.ReadAllBytes(DataPath);
                if (!Program.CheckValidCrownData(data))
                {
                    Console.Error.WriteLine("File does not contain valid quest data.");
                    return 2;
                }
                manip.UploadData(data);
            }
            return 0;
        }
    }

    [Command("extract", "Extract quest file from DCC Special Quest Downloader")]
    class ExtractCommand : BaseCommand
    {
        [Argument(0, "exePath", "Path to DCC Special Quest Downloader EXE")]
        [FileExists]
        [Required]
        public string ExePath { get; set; }
        [Argument(1, "destPath", "Path to extract quest file to")]
        [LegalFilePath]
        public string DestPath { get; set; } = "quest.bin";

        protected override int OnExecute(CommandLineApplication app)
        {
            using (FileStream fs = File.OpenRead(ExePath))
            {
                byte[] extracted = SpecialQuestExtractor.Extract(fs);
                if (extracted == null)
                {
                    Console.Error.WriteLine("DCC Special Quest Downloader not recognized.");
                    return 2;
                }
                else
                {
                    File.WriteAllBytes(DestPath, extracted);
                }
            }

            return 0;
        }
    }

    [HelpOption("--help")]
    abstract class BaseCommand
    {
        protected abstract int OnExecute(CommandLineApplication app);
    }
}
