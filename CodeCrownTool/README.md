Digimon Xros Loader Code Crown Tool
===================================

This tool allows you to create and verify Code Crowns, and extract and load
missions.

Usage
-----

The program can automatically read out the required information from the SD
card if you are on Linux and are using a supported SD card reader (one that
shows up as a `mmcblk` device under `/dev`). If you are on Windows or not
using a supported reader, you can manually specify the SD card's CID, CSD,
and SSR. Use the `--cid`, `--csd`, and `--ssr` options.

Dumping and loading Code Crowns do not require the registers, but creating
and verifying Code Crowns do.

### `list`

```
CodeCrownTool list
```

This command lists the disks that can be operated on. You need to enter the
disk name exactly in other commands to be able to operate on this disk.
The list will only include SD cards and SD card readers (it may also include
USB drives, but operating on one is meaningless).

### `create`

```
CodeCrownTool create <devicePath>
```

This command writes the security sector on to the specified device to enable
the card to be used as a Code Crown. Note that the disk should contain one
partition, with a bit over 1MB space remaining after the partition. Multiple
partitions are allowed only if there is enough unallocated space following the
first partition.

### `verify`

```
CodeCrownTool verify <devicePath>
```

This command verifies that the security sector on a Code Crown is valid.

### `dump`

```
CodeCrownTool dump <devicePath> [dataPath]
```

This command dumps the current quest from the Code Crown. By default it will
be written to `quest.bin`; if you want to write it elsewhere, specify
`dataPath`.

### `load`

```
CodeCrownTool load <devicePath> <dataPath>
```

This command writes the quest file at path `dataPath` to the Code Crown. The
quest file must be valid.

### `extract`

```
CodeCrownTool extract <exePath> [destPath]
```

This command extracts the quest file from a Code Crown downloader EXE. By default
the quest file will be written to `quest.bin`; if you want to write it elsewhere,
specify `destPath`.
