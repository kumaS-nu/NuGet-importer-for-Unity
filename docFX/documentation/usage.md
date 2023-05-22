# Usage

## Getting Start

 There are two ways to install the package: using the UPM (Unity Package Manager) or importing the .unitypackage.

### via UPM (Unity Package Manager)

 There are two ways to install using UPM: this Git URL or OpenUPM.

#### Use This Git URL

1. Open the Package Manager window.
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

## Configure of .gitignore

You may want to keep installed packages out of git's tracking. In that case, you can add the following to  `.gitignore`. NuGet-importer-for-Unity manages the list of installed packages in `Asset/package.config`, and you can restore packages by sharing this file.
```bash
# NuGet importer
/[Aa]ssets/[Pp]ackages.meta
/[Aa]ssets/[Pp]ackages/

/[Nn]u[Gg]et/

/[Pp]ackages/*/
!/[Pp]ackages/your embedded package to share with git/
```

If you set these packages out of git's tracking, you will get a compile error when you run CI/CD.
Therefore, if you want to use CI/CD with the packages out of git's tracking, you should take one of the following three ways.

- Add `-ignoreCompilerErrors` command line options when launching Unity with batch mode.
- Add `NUGET_PACKAGE_READY` to `Define Constraints` in .asmdef that depends on the installed package.
- Enclose your code that depends on the imported packages in the preprocessor directives below.
    ```csharp
    #if NUGET_PACKAGE_READY

    // your code

    #endif
    ```

## Usage in Unity

### Menu item

![Menu item](../images/MenuItem.png)

- Manage packages ・・・ Open the main window for managing packages.
- Repair packages ・・・ Optimize the dependencies of installed packages and repair them.
- Delete cache ・・・ Delete the cache. (However, the cache is deleted every time the assembly is loaded.)
- Clean up this plugin ・・・ Delete all packages and initialize this plugin.
- NuGet importer settings ・・・ Open the window for configuring settings with NuGet importer.
- Check update ・・・ Check for updates.
- Go to the project page ・・・ Open the "NuGet importer for Unity" web page.

### Main window

![Main window](../images/MainWindow.png)

1. Mode to search from NuGet.
1. Mode to search from the installed packages.
1. Include the development version or not.
1. Input area for search words. (Incremental search.)
1. Search results.
1. Package details.
1. Version selection.
1. Operations on the package.
1. Ignored package or not.

### NuGet importer settings

![NuGet importer settings](../images/Settings.png)

1. Specify the installation location. （We recommend using UPM.)
1. Specify the method for determining the package version when solving dependency. (We recommend setting Suit.)
1. Specifies whether check the package is installed at startup. If the package directory exists, NuGet-importer-for-Unity determines as the package is already installed in the project. If NuGet-importer-for-Unity finds missing packages, automatically install them. ( We recommend turning it on.)
1. The maximum number of search results to cache. (0 or less is not cached.)
1. The maximum number of catalogs to cache. (0 or less is not cached.)
1. The maximum number of icons to cache. (0 or less is not cached.)
1. Generate an Assembly Definition file for Roslyn Analyzer, allowing the scope of analysis to be specified by the Assembly Definition file.
1. Reduce the amount of data in the communication or not. If on, images of packages not installed in the project will not fetch, and NuGet-importer-for-Unity will not perform package searches until the input has settled.
1. The maximum number of retry attempts to get data when data fetch fails.
1. Time out seconds of communication.
1. List of packages to ignore. The Add button works to append a blank package. The remove button works to remove the last package.

## Note

NuGet-importer-for-Unity install files not required at runtime (e.g., rulesets, documentation, etc.) are installed to `(your project)/NuGet`. If you want to reference them, add them reference manually.

When importing this package into your project, this package makes the following changes.
- Turn off `PlayerSettings -> assemblyVersionValidation`. (To make Unity not check the assembly version as NuGet does.)
- Add reference `System.IO.Compression.FileSystem.dll`. (NuGet importer for Unity handles Zip files.)