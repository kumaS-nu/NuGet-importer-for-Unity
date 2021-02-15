# NuGet importer for Unity
[![GitHub](https://img.shields.io/github/license/kumaS-nu/NuGet-importer-for-Unity)](https://github.com/kumaS-nu/NuGet-importer-for-Unity/blob/master/NuGetImporterForUnity/Packages/NuGet%20Importer/LICENSE.md)
[![Test](https://github.com/kumaS-nu/NuGet-importer-for-Unity/workflows/Test/badge.svg?branch=main&event=push)](https://github.com/kumaS-nu/NuGet-importer-for-Unity/actions)
[![GitHub all releases](https://img.shields.io/github/downloads/kumaS-nu/NuGet-importer-for-Unity/total)](https://github.com/kumaS-nu/NuGet-importer-for-Unity/releases)
[![GitHub release (latest by SemVer including pre-releases)](https://img.shields.io/github/downloads-pre/kumaS-nu/NuGet-importer-for-Unity/lastest/total?sort=semver)](https://github.com/kumaS-nu/NuGet-importer-for-Unity/releases)
[![CodeFactor Grade](https://img.shields.io/codefactor/grade/github/kumaS-nu/NuGet-importer-for-Unity)](https://www.codefactor.io/repository/github/kumaS-nu/NuGet-importer-for-Unity)

 "NuGet importer for Unity" is a fast, easy to use, and very powerful editor extension that provides you to import NuGet packages into Unity.
This is also fully support for native plugins.
(This was inspired by [GlitchEnzo/NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity), but made from scratch.)  
[日本語はこちら。](README_jp.md) 

![demo](NuGetImporterForUnity/Packages/NuGet%20Importer/Documentation~/images/Demo.gif)

## Features

- High performance using asynchronous
- Powerful dependency solving
- Full support for native plugins
- Useful UI
- UPM and unitypackage support
- Compatible with [GlitchEnzo/NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)

## Getting Start

### via UPM (Unity Package Manager)

 There are two ways to install using UPM: using this Git URL, or using OpenUPM.

#### Use This Git URL

1. Open Package Manager window.
1. Click the **add** (+) button in the status bar.
1. Select **Add package from git URL**.
1. Enter "`https://github.com/kumaS-nu/NuGet-importer-for-Unity.git?path=NuGetImporterForUnity/Packages/NuGet Importer`" or "`git@github.com:kumaS-nu/NuGet-importer-for-Unity.git?path=NuGetImporterForUnity/Packages/NuGet Importer`".
1. Click **Add**.

 For more information, see the [Official page (Installing from a Git URL - Unity)](https://docs.unity3d.com/Manual/upm-ui-giturl.html).

#### Use OpenUPM

1. If you have not installed OpenUPM-CLI, run the following command to install OpenUPM-CLI. (Node.js 12 is required.)
    ```bash
    npm install -g openupm-cli
    ```
1. Go to the unity project folder
1. Run the following command to install "NuGet importer for Unity" in your project.
    ```bash
    openupm add org.kumas.nuget-importer
    ```

 For more information, see the [Official page (Getting Started with OpenUPM-CLI - OpenUPM)](https://openupm.com/docs/getting-started.html)

### via .unitypackage

1. Go to the [release page](https://github.com/kumaS-nu/NuGet-importer-for-Unity/releases) and download the zip file of the version you need.
1. Extract the zip file and import the .unitypackage into your project.

## More Infomation

 For more information or contributions, please see https://kumaS-nu.github.io/NuGet-importer-for-Unity .

## License

This is [Apache License 2.0](NuGetImporterForUnity/Packages/NuGet%20Importer/LICENSE.md).  
Each package in NuGet is governed by the terms that are included with the package. For more details, see [NuGet F&Q](https://docs.microsoft.com/en-us/nuget/nuget-org/nuget-org-faq#license-terms).