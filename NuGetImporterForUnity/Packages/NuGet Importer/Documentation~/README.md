# NuGet importer for Unity
[![GitHub](https://img.shields.io/github/license/kumaS-nu/NuGet-importer-for-Unity)](https://github.com/kumaS-nu/NuGet-importer-for-Unity/blob/master/NuGetImporterForUnity/Packages/NuGet%20Importer/LICENSE.md)
[![Test](https://github.com/kumaS-nu/NuGet-importer-for-Unity/workflows/Test/badge.svg?branch=main&event=push)](https://github.com/kumaS-nu/NuGet-importer-for-Unity/actions)
[![GitHub all releases](https://img.shields.io/github/downloads/kumaS-nu/NuGet-importer-for-Unity/total)](https://github.com/kumaS-nu/NuGet-importer-for-Unity/releases)
[![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/kumaS-nu/NuGet-importer-for-Unity/latest/total)](https://github.com/kumaS-nu/NuGet-importer-for-Unity/releases)
[![CodeFactor Grade](https://img.shields.io/codefactor/grade/github/kumaS-nu/NuGet-importer-for-Unity)](https://www.codefactor.io/repository/github/kumaS-nu/NuGet-importer-for-Unity)
[![openupm](https://img.shields.io/npm/v/org.kumas.nuget-importer?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/org.kumas.nuget-importer/)
 
 "NuGet importer for Unity" is a fast, easy to use, and very powerful editor extension that provides you to import NuGet packages into Unity.
This is also fully support for native plugins.
(This was inspired by [GlitchEnzo/NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity), but made from scratch.)  
[日本語はこちら。](README_jp.md) 

![demo](images/Demo.gif)

## Features

- High performance using asynchronous
- Powerful dependency solving
- Full support for native plugins
- Useful UI
- UPM and unitypackage support
- Compatible with [GlitchEnzo/NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)

## Usage

### Menu item

![Menu item](images/MenuItem.png)

- Manage packages ・・・ Open the main window for managing packages.
- Repair packages ・・・ Optimize the dependencies of installed packages and repair them.
- Delete cache ・・・ Delete the cache. (However, the cache is deleted every time the assembly is loaded.)
- NuGet importer settings ・・・ Open the window for configuring settings with NuGet importer.
- Check update ・・・ Check for updates.
- Go to project page ・・・ Open the "NuGet importer for Unity" page.

### Main window

![Main window](images/MainWindow.png)

1. Mode to search from NuGet.
1. Mode to search from the installed packages.
1. Whether include development version.
1. Input area for search words. (Incremental search.)
1. Search results.
1. Package details.
1. Version selection.
1. Operations on the package.


### NuGet importer settings

![NuGet importer settings](images/Settings.png)

1. Specify the installation location. （It is recommended to use UPM.)
1. Specify the method for determining the package version when solve dependency. (Suit is recommended.)
1. Specifies whether the package is installed at startup. If the package directory exists, it is determined that the package is already installed. If missing packages are found, they are automatically installed. ( It is recommended to turn it on.)
1. The maximum number of search results to cache. (0 or less is not cached.)
1. The maximum number of catalog to cache. (0 or less is not cached.)
1. The maximum number of icon to cache. (0 or less is not cached.)

## Configure of .gitignore

You may want to keep installed packages to out of git's tracking. In that case, you can add the following to `.gitignore`. The list of installed packages is managed in `Asset/package.config`, and you can restore packages by sharing this file.
```bash
# NuGet importer
/[Aa]ssets/[Pp]ackages.meta
/[Aa]ssets/[Pp]ackages/

/[Nn]u[Gg]et/

/[Pp]ackages/*/
!/[Pp]ackages/your embedded package to share with git/
```

## Note

Files that are not required at runtime (e.g. analyzers, documentationn, etc.) are installed to `(your project)/NuGet`. If you want to reference them, add them reference manually.

When importing this package into your project, make the following changes
- Turn off `PlayerSettings -> assemblyVersionValidation`. (To make Unity not check version of assembly as NuGet does.)
- Add reference `System.IO.Compression.FileSystem.dll`. (NuGet importer for Unity handles Zip files.)

## License

This is [Apache License 2.0](../LICENSE.md).  
Each package in NuGet is governed by the terms that are included with the package. For more details, see [NuGet F&Q](https://docs.microsoft.com/en-us/nuget/nuget-org/nuget-org-faq#license-terms).