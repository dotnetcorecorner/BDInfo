# BDInfo

It scans bluray disc (full hd, ultra hd and 3D) on various operating systems. The binaries provided are portable so no need to install any framework.

## Command arguments

| Short argument | Long argument | Meaning | Required | Default |
| --- | --- | --- | --- | --- |
| _`-p`_ | _`--path`_ | The path to iso or bluray folder | x |  |
| _`-g`_ | _`--generatestreamdiagnostics`_ | Generate the stream diagnostics |  | False |
| _`-e`_ | _`--extendedstreamdiagnostics`_ | Generete the extended stream diagnostics |  | False |
| _`-b`_ | _`--enablessif`_ | Enable SSIF support |  | False |
| _`-c`_ | _`--displaychaptercount`_ | Enable chapter count |  | False |
| _`-a`_ | _`--autosavereport`_ | Auto save report |  | False |
| _`-f`_ | _`--generateframedatafile`_ | Generate frame data file |  | False |
| _`-l`_ | _`--filterloopingplaylists`_ | Filter loopig playlist |  | False |
| _`-y`_ | _`--filtershortplaylist`_ | Filter short playlist |  | False |
| _`-v`_ | _`--filtershortplaylistvalue`_ | Filter number of short playlist |  | 20 |
| _`-i`_ | _`--useimageprefix`_ | Use image prefix |  | False |
| _`-x`_ | _`--useimageprefixvalue`_ | Image prefix |  | video- |
| _`-k`_ | _`--keepstreamorder`_ | Keep stream order |  | False |
| _`-m`_ | _`--generatetextsummary`_ | Generate summary |  | False |
| _`-o`_ | _`--reportfilename`_ | The report filename with extension. If no extension provided then will append .txt at end of filename |  |  |
| _`-q`_ | _`--includeversionandnotes`_ | Include version and notes inside report |  | False |
| _`-z`_ | _`--printonlybigplaylist`_ | Print report with only biggest playlist |  | False |
| _`-w`_ | _`--printtoconsole`_ | Print report to console |  | False |
| _`-j`_ | _`--groupbytime`_ | Group by time |  | False |
| _`-d`_ | _`--isexecutedasscript`_ | Check if is executed as script |  | False |


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
| _`-o`_ | _`--output`_ | The output folder | x |

For linux, make `BDExtractor` as executable (yes, that one without extension) using `chmod +x BDExtractor`

## How to use

### Windows
`BDExtractor.exe -p PATH_TO_ISO_FILE -o FOLDER_OUTPUT`  

### Linux:  
`./BDExtractor -p PATH_TO_ISO_FILE -o FOLDER_OUTPUT`

# Known issue

https://github.com/DiscUtils/DiscUtils/issues/199

[![Build x64](https://github.com/dotnetcorecorner/BDInfo/actions/workflows/dotnet.yml/badge.svg)](https://github.com/dotnetcorecorner/BDInfo/actions/workflows/dotnet.yml)

# Statistics

![GitHub release (by tag)](https://img.shields.io/github/downloads/dotnetcorecorner/bdinfo/win-1.0.0/total?style=flat-square)  ![GitHub release (by tag)](https://img.shields.io/github/downloads/dotnetcorecorner/bdinfo/linux-1.0.0/total?style=flat-square)
