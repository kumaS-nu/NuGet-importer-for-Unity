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
- Cache settings ・・・ Open the window for configuring settings with cache.
- Check update ・・・ Check for updates.
- Go to project page ・・・ Open the "NuGet importer for Unity" page.

### Main window

![Main window](images/MainWindow.png)

1. Mode to search from NuGet.
1. Mode to search from the installed packages.
1. Framework settings.
1. Whether include development version
1. Input area for search words. (Incremental search.)
1. Search results.
1. Package details.
1. Version selection.
1. Operations on the package.

### Cache settings

![Cache settings](images/CacheSettings.png)

1. The maximum number of search results to cache. (0 or less is not cached.)
1. The maximum number of catalog to cache. (0 or less is not cached.)
1. The maximum number of icon to cache. (0 or less is not cached.)

## License

This is [Apache License 2.0](../LICENSE.md).  
Each package in NuGet is governed by the terms that are included with the package. For more details, see [NuGet F&Q](https://docs.microsoft.com/en-us/nuget/nuget-org/nuget-org-faq#license-terms).