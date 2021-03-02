# Usage

## Getting Start

 There are two ways to install the package: using the UPM (Unity Package Manager) or importing the .unitypackage.

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

## Usage in Unity

### Menu item

![Menu item](../images/MenuItem.png)

- Manage packages ・・・ Open the main window for managing packages.
- Repair packages ・・・ Optimize the dependencies of installed packages and repair them.
- Delete cache ・・・ Delete the cache. (However, the cache is deleted every time the assembly is loaded.)
- Cache settings ・・・ Open the window for configuring settings with cache.
- Check update ・・・ Check for updates.
- Go to project page ・・・ Open the "NuGet importer for Unity" page.

### Main window

![Main window](../images/MainWindow.png)

1. Mode to search from NuGet.
1. Mode to search from the installed packages.
1. Framework settings.
1. Whether include development version.
1. Method of selecting the version of the dependency package.
1. Input area for search words. (Incremental search.)
1. Search results.
1. Package details.
1. Version selection.
1. Operations on the package.

### Cache settings

![Cache settings](../images/CacheSettings.png)

1. The maximum number of search results to cache. (0 or less is not cached.)
1. The maximum number of catalog to cache. (0 or less is not cached.)
1. The maximum number of icon to cache. (0 or less is not cached.)