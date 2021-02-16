# NuGet importer for Unity
[![GitHub](https://img.shields.io/github/license/kumaS-nu/NuGet-importer-for-Unity)](https://github.com/kumaS-nu/NuGet-importer-for-Unity/blob/master/NuGetImporterForUnity/Packages/NuGet%20Importer/LICENSE.md)
[![Test](https://github.com/kumaS-nu/NuGet-importer-for-Unity/workflows/Test/badge.svg?branch=main&event=push)](https://github.com/kumaS-nu/NuGet-importer-for-Unity/actions)
[![GitHub all releases](https://img.shields.io/github/downloads/kumaS-nu/NuGet-importer-for-Unity/total)](https://github.com/kumaS-nu/NuGet-importer-for-Unity/releases)
[![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/kumaS-nu/NuGet-importer-for-Unity/latest/total)](https://github.com/kumaS-nu/NuGet-importer-for-Unity/releases)
[![CodeFactor Grade](https://img.shields.io/codefactor/grade/github/kumaS-nu/NuGet-importer-for-Unity)](https://www.codefactor.io/repository/github/kumaS-nu/NuGet-importer-for-Unity)

 NuGet importer for Unity は高速で使いやすく、非常に強力に NuGet のパッケージを Unity へ導入できるようにするエディタ拡張です。
また、ネイティブプラグインに対しても完全に対応しております。
（[GlitchEnzo/NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) に影響を受けましたが一から作ってます。）

![デモ](NuGetImporterForUnity/Packages/NuGet%20Importer/Documentation~/images/Demo.gif)

## 特徴

- 非同期を活用した高速な動作
- 強力な依存関係解決
- ネイティブプラグインに対する完全な対応
- わかりやすいUI
- UPM対応・unitypackageあり
- [GlitchEnzo/NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)との互換性あり

## 導入方法

### UPM (Unity Package Manager) を使う

 UPMを使って導入するには、この Git URL を指定する。または、OpenUPM を使って導入するという二通りあります。

#### Git URL を指定する

1. Package Manager ウィンドウを開く
1. ステータスバーの **Add** (+) ボタンをクリック
1. **Add package from git URL** を選択
1. 「`https://github.com/kumaS-nu/NuGet-importer-for-Unity.git?path=NuGetImporterForUnity/Packages/NuGet Importer`」または「`git@github.com:kumaS-nu/NuGet-importer-for-Unity.git?path=NuGetImporterForUnity/Packages/NuGet Importer`」を入力
1. **Add** をクリック

詳細は[公式ページ(Git URL からのインストール - Unity)](https://docs.unity3d.com/ja/2019.4/Manual/upm-ui-giturl.html)をご覧ください。

#### OpenUPM を利用する

1. OpenUPM-CLI をインストールしていない場合は、以下のコマンドで OpenUPM-CLI をインストールする。(Node.js 12が必要です。)
    ``` bash
    npm install -g openupm-cli
    ```
1. インストールする予定のプロジェクトのフォルダへ移動する。
1. 以下のコマンドで、プロジェクトにインストールする。
    ``` bash
    openupm add org.kumas.nuget-importer
    ```

詳細は[公式ページ(Getting Started with OpenUPM-CLI - OpenUPM)](https://openupm.com/docs/getting-started.html)をご覧ください。

### .unitypackage で導入する

1. [リリースページ](https://github.com/kumaS-nu/NuGet-importer-for-Unity/releases)へ行き、必要なバージョンのzipファイルをダウンロードする。
1. zipファイルを解凍し、中の .unitypackage をプロジェクトにインポートする。

## 詳細

 より詳しい情報や、コントリビューションについては https://kumaS-nu.github.io/NuGet-importer-for-Unity をご覧ください。

## ライセンス

これについては、[Apache License 2.0](NuGetImporterForUnity/Packages/NuGet%20Importer/LICENSE.md) です。  
NuGet のパッケージについてはそれぞれのライセンスに従います。詳細は [NuGet の F&Q](https://docs.microsoft.com/ja-jp/nuget/nuget-org/nuget-org-faq#license-terms) をご覧ください。