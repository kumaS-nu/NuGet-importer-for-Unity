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
- Roslyn Analyzer も対応
- CI/CD に対応
- わかりやすいUI
- UPM対応・unitypackageあり
- [GlitchEnzo/NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)との互換性あり

## 使い方

### メニューアイテム

![メニューアイテム](images/MenuItem.png)

- Manage packages ・・・ パッケージを管理するメインウィンドウを表示する。
- Repair packages ・・・ インストールされているパッケージの依存関係を最適化し、パッケージを修復する。
- Delete cache ・・・ キャッシュを削除する。（ただし、アセンブリがロードされるたびにキャッシュは消えている。）
- Clean up this plugin ・・・ パッケージを全削除し、このプラグインを初期化する。
- NuGet importer settings ・・・ NuGet importer に関する設定をするウィンドウを表示する。
- Check update ・・・ 更新があるか確認する。
- Go to project page ・・・ NuGet importer for Unity のページを開く。

### メインウィンドウ

![メインウィンドウ](images/MainWindow.png)

1. NuGet から検索するときのモード。
1. インストールされているものから検索するときのモード。
1. 非安定版も含めるかどうか。
1. 検索語句の入力場所。（インクリメンタルサーチされる。）
1. 検索結果。
1. パッケージの詳細情報。
1. バージョン選択。
1. パッケージに対する操作。
1. 無視するパッケージか。

### NuGet importer の設定

![NuGet importer の設定](images/Settings.png)

1. インストール先を指定する。（UPMを推奨。）
1. 依存関係のバージョン決定方法を指定する。（Suitを推奨。）
1. 起動時にパッケージがインストールされているか確認するか。パッケージのディレクトリが存在すればインストール済みと判断。インストールされていないパッケージを見つけた場合、自動的に修復する。（オンを推奨。）
1. 検索結果のキャッシュする最大数。（0以下はキャッシュしない。）
1. カタログのキャッシュする最大数。（0以下はキャッシュしない。）
1. アイコンのキャッシュする最大数。（0以下はキャッシュしない。）
1. 通信のデータ量を少なくするモードか。オンの場合、インストールされていないパッケージの画像が取得されなくなり、入力が落ち着くまでパッケージ検索が行われなくなります。
1. 通信失敗時に再度通信を試みる最大数。
1. 通信のタイムアウトとする秒数。
1. 無視するパッケージ一覧。Addでパッケージ追加。Removeで最後尾のパッケージ削除。

## .gitignoreの設定

インストールしたパッケージは Git の監理外にしたいと思います。その場合は、`.gitignore`に以下を追加してください。インストールしたパッケージ一覧は`Asset/package.config`で管理され、このファイルを共有すればパッケージの復元ができます。
```bash
# NuGet importer
/[Aa]ssets/[Pp]ackages.meta
/[Aa]ssets/[Pp]ackages/

/[Nn]u[Gg]et/

/[Pp]ackages/*/
!/[Pp]ackages/your embedded package to share with git/
```

ただし、パッケージを Git の監理外にするとそのままではコンパイルエラーが発生し CI/CD に使用できません。
そのため、パッケージを Git の監理外にし CI/CD をする際は以下の3つの対応策のうちどれかを行ってください。

- バッチモードの起動オプションに `-ignoreCompilerErrors` を追加。
- 導入したパッケージに依存する .asmdef の `Define Constraints` に `NUGET_PACKAGE_READY` を追加。
- 導入したパッケージに依存するコードを以下のようにプリプロセッサディレクディブで囲う。
    ```csharp
    #if NUGET_PACKAGE_READY

    // your code

    #endif
    ```

## 注意点

実行時に必要なファイル以外（例：アナライザーやドキュメントなど）は `(your project)/NuGet` 以下に配置されます。参照する際は手動で追加してください。

このパッケージを導入する際、以下の変更を加えます。
- `PlayerSettings -> assemblyVersionValidation` をオフに。（NuGet と同様にアセンブリ参照のバージョンの同一性をチェックしなくさせるため。）
- `System.IO.Compression.FileSystem.dll` を参照に追加。（NuGet importer for Unity が Zip ファイルを扱うため。）

## ライセンス

これについては、[Apache License 2.0](../LICENSE.md) です。  
NuGet のパッケージについてはそれぞれのライセンスに従います。詳細は [NuGet の F&Q](https://docs.microsoft.com/ja-jp/nuget/nuget-org/nuget-org-faq#license-terms) をご覧ください。