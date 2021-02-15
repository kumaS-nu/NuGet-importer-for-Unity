# コーディング規則

 基本的に Microsoft の[フレームワークデザインのガイドライン](https://docs.microsoft.com/ja-jp/dotnet/standard/design-guidelines/)または [C# のコーディング規則](https://docs.microsoft.com/ja-jp/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)に則っています。また、`NuGetImporterForUnity/.editorconfig` にてこのプロジェクトで使用するコードスタイルを定義しており、この設定でコードのクリーンアップをすれば基本的には問題ありません。（守れてないところがあったら突っ込んでください）

## 命名

 パラメーターは Camel 記法（先頭小文字、それ以外の単語の先頭が大文字）とし、それ以外は Pascal 記法（全ての単語の先頭を大文字）としてください。命名は、読みやすい英語で短縮形を使用しないでください。
  詳細は、[名前付けのガイドライン - Microsoft](https://docs.microsoft.com/ja-jp/dotnet/standard/design-guidelines/naming-guidelines) を参照してください。（といっても~~日本語の翻訳がガバガバなので~~[英語版](https://docs.microsoft.com/en-US/dotnet/standard/design-guidelines/naming-guidelines)の方が正確）

## レイアウト

* コード エディターの既定の設定（スマートインデント、4 文字インデント、タブを空白として保存）を使用します。
* 1つの行には1つのステートメントのみを記述します。
* 1つの行には1つの宣言のみを記述します。
* 継続行にインデントが自動的に設定されない場合は1タブストップ（4つの空白）分のインデントを設定します。
* メソッド定義とプロパティ定義の間に少なくとも1行の空白行を追加します。

## コメント規則

* コメントは、コード行の末尾ではなく別の行に記述します。
* コメントのテキストは大文字で開始します。
* コメントのテキストは（名詞一個の場合を除き）ピリオドで終了します。

## 変数宣言

* 変数の型が割り当ての右側から明らかである場合、または厳密な型が重要でない場合は、ローカル変数の暗黙の型指定を使用します。
* 割り当ての右側から型が明らかではない場合、var を使用しません。
* for ループでループ変数の型を決定するときは、暗黙の型指定を使用します。
* foreach ループでループ変数の型を決定するときは、暗黙の型指定を使用しません。

## usingディレクティブ

 usingディレクティブは namespace の外側（基本的にはファイルの先頭）に書きます。また、usingディレクティブの順番は以下の通りとし、各グループは1行づつ開けます。

1. System
1. その他


 例えば、以下のようになります。

``` csharp
using System;
using System.Collections.Generic;
using System.Linq;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor;

using UnityEngine;
```