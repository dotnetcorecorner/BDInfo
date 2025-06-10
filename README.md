# BDInfo

It scans bluray disc (full hd, ultra hd and 3D) on various operating systems. The binaries provided are portable so no need to install any framework.

## Command arguments

| Short argument | Long argument | Meaning | Required | Default |
| --- | --- | --- | --- | --- |
| _`-p`_ | _`--path`_ | The path to iso or bluray folder | x |  |
| _`-g`_ | _`--generatestreamdiagnostics`_ | Generate the stream diagnostics |  | False |
| _`-e`_ | _`--extendedstreamdiagnostics`_ | Generete the extended stream diagnostics |  | False |
| _`-b`_ | _`--enablessif`_ | Enable SSIF support |  | False |
| _`-l`_ | _`--filterloopingplaylists`_ | Filter loopig playlist |  | False |
| _`-y`_ | _`--filtershortplaylist`_ | Filter short playlist |  | True |
| _`-v`_ | _`--filtershortplaylistvalue`_ | Filter number of short playlist |  | 20 |
| _`-k`_ | _`--keepstreamorder`_ | Keep stream order |  | False |
| _`-m`_ | _`--generatetextsummary`_ | Generate summary |  | True |
| _`-o`_ | _`--reportfilename`_ | The report filename with extension. If no extension provided then will append .txt at end of filename |  |  |
| _`-q`_ | _`--includeversionandnotes`_ | Include version and notes inside report |  | False |
| _`-j`_ | _`--groupbytime`_ | Group by time |  | False |


For linux, make `BDInfo` as executable (yes, that one without extension) using `chmod +x BDInfo`

## How to use 

### Windows
`BDInfo.exe -p PATH_TO_DISC_FOLDER -o PATH_TO_FILE.EXTENSION`  
`BDInfo.exe -p PATH_TO_ISO_FILE -o PATH_TO_FILE.EXTENSION`  

### Linux  
`./BDInfo -p PATH_TO_DISC_FOLDER -o PATH_TO_FILE.EXTENSION`  
`./BDInfo -p PATH_TO_ISO_FILE -o PATH_TO_FILE.EXTENSION`

# BDExtractor

It extract bluray disc iso file on various operating systems without neeed to mount it (except non-EEF iso). The binaries provided are portable so no need to install any framework.

## Command arguments

| Short argument | Long argument | Meaning | Required |
| --- | --- | --- | --- |
| _`-p`_ | _`--path`_ | The path to iso file | x |
| _`-o`_ | _`--output`_ | The output folder (if not specified then will extract in same location with iso) |  |

For linux, make `BDExtractor` as executable (yes, that one without extension) using `chmod +x BDExtractor`

## How to use

### Windows
`BDExtractor.exe -p PATH_TO_ISO_FILE -o FOLDER_OUTPUT`  
`BDExtractor.exe -p PATH_TO_ISO_FILE`  

### Linux:  
`./BDExtractor -p PATH_TO_ISO_FILE -o FOLDER_OUTPUT`
`./BDExtractor -p PATH_TO_ISO_FILE`

# BDInfoDataSubstractor (beta)

It extracts main playlist from very long text based on many criteria

## How to use

### Windows
`BDInfoDataSubstractor.exe bdinfo.txt bdinfo2.txt`

### Linux
`./BDInfoDataSubstractor bdinfo.txt bdinfo2.txt`



# Known issue

https://github.com/DiscUtils/DiscUtils/issues/199

[![Build x64](https://github.com/dotnetcorecorner/BDInfo/actions/workflows/dotnet.yml/badge.svg)](https://github.com/dotnetcorecorner/BDInfo/actions/workflows/dotnet.yml)

# Statistics

![GitHub release (by tag)](https://img.shields.io/github/downloads/dotnetcorecorner/bdinfo/win-1.0.0/total?style=flat-square)  ![GitHub release (by tag)](https://img.shields.io/github/downloads/dotnetcorecorner/bdinfo/linux-1.0.0/total?style=flat-square)
