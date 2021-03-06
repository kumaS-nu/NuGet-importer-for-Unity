# 使い方

## インストール方法

 パッケージをインストールするには、UPM (Unity Package Manager) を利用する、または、.unitypackage をインポートするの二通りがあります。

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

## Unityでの使い方

### メニューアイテム

![メニューアイテム](../images/MenuItem.png)

- Manage packages ・・・ パッケージを管理するメインウィンドウを表示する。
- Repair packages ・・・ インストールされているパッケージの依存関係を最適化し、パッケージを修復する。
- Delete cache ・・・ キャッシュを削除する。（ただし、アセンブリがロードされるたびにキャッシュは消えている。）
- Cache settings ・・・ キャッシュに関する設定をするウィンドウを表示する。
- Check update ・・・ 更新があるか確認する。
- Go to project page ・・・ NuGet importer for Unity のページを開く。

### メインウィンドウ

![メインウィンドウ](../images/MainWindow.png)

1. NuGet から検索するときのモード。
1. インストールされているものから検索するときのモード。
1. フレームワークの設定。
1. 非安定版も含めるかどうか。
1. 依存関係のパッケージのバージョンの選択方法。
1. 検索語句の入力場所。（インクリメンタルサーチされる。）
1. 検索結果。
1. パッケージの詳細情報。
1. バージョン選択。
1. パッケージに対する操作。


### キャッシュの設定

![キャッシュの設定](../images/CacheSettings.png)

1. 検索結果のキャッシュする最大数。（0以下はキャッシュしない。）
1. カタログのキャッシュする最大数。（0以下はキャッシュしない。）
1. アイコンのキャッシュする最大数。（0以下はキャッシュしない。）