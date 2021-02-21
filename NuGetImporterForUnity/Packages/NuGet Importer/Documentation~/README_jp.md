# NuGet importer for Unity
[![GitHub](https://img.shields.io/github/license/kumaS-nu/NuGet-importer-for-Unity)](https://github.com/kumaS-nu/NuGet-importer-for-Unity/blob/master/NuGetImporterForUnity/Packages/NuGet%20Importer/LICENSE.md)
[![Test](https://github.com/kumaS-nu/NuGet-importer-for-Unity/workflows/Test/badge.svg?branch=main&event=push)](https://github.com/kumaS-nu/NuGet-importer-for-Unity/actions)
[![GitHub all releases](https://img.shields.io/github/downloads/kumaS-nu/NuGet-importer-for-Unity/total)](https://github.com/kumaS-nu/NuGet-importer-for-Unity/releases)
[![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/kumaS-nu/NuGet-importer-for-Unity/latest/total)](https://github.com/kumaS-nu/NuGet-importer-for-Unity/releases)
[![CodeFactor Grade](https://img.shields.io/codefactor/grade/github/kumaS-nu/NuGet-importer-for-Unity)](https://www.codefactor.io/repository/github/kumaS-nu/NuGet-importer-for-Unity)
[![openupm](https://img.shields.io/npm/v/org.kumas.nuget-importer?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/org.kumas.nuget-importer/)

 NuGet importer for Unity は高速で使いやすく、非常に強力に NuGet のパッケージを Unity へ導入できるようにするエディタ拡張です。
また、ネイティブプラグインに対しても完全に対応しております。
（[GlitchEnzo/NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) に影響を受けましたが一から作ってます。）

![デモ](images/Demo.gif)

## 特徴

- 非同期を活用した高速な動作
- 強力な依存関係解決
- ネイティブプラグインに対する完全な対応
- わかりやすいUI
- UPM対応・unitypackageあり
- [GlitchEnzo/NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)との互換性あり

## 使い方

### メニューアイテム

![メニューアイテム](images/MenuItem.png)

- Manage packages ・・・ パッケージを管理するメインウィンドウを表示する。
- Repair packages ・・・ インストールされているパッケージの依存関係を最適化し、パッケージを修復する。
- Delete cache ・・・ キャッシュを削除する。（ただし、アセンブリがロードされるたびにキャッシュは消えている。）
- Cache settings ・・・ キャッシュに関する設定をするウィンドウを表示する。
- Check update ・・・ 更新があるか確認する。
- Go to project page ・・・ NuGet importer for Unity のページを開く。

### メインウィンドウ

![メインウィンドウ](images/MainWindow.png)

1. NuGet から検索するときのモード。
1. インストールされているものから検索するときのモード。
1. フレームワークの設定。
1. 非安定版も含めるかどうか。
1. 検索語句の入力場所。（インクリメンタルサーチされる。）
1. 検索結果。
1. パッケージの詳細情報。
1. バージョン選択。
1. パッケージに対する操作。


### キャッシュの設定

![キャッシュの設定](images/CacheSettings.png)

1. 検索結果のキャッシュする最大数。（0以下はキャッシュしない。）
1. カタログのキャッシュする最大数。（0以下はキャッシュしない。）
1. アイコンのキャッシュする最大数。（0以下はキャッシュしない。）

## ライセンス

これについては、[Apache License 2.0](../LICENSE.md) です。  
NuGet のパッケージについてはそれぞれのライセンスに従います。詳細は [NuGet の F&Q](https://docs.microsoft.com/ja-jp/nuget/nuget-org/nuget-org-faq#license-terms) をご覧ください。