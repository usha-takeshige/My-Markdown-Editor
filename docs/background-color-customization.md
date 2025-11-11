# 背景色カスタマイズ機能の実装

## 概要

issue #7として、ウィンドウの背景色を任意の色（16進数で表現）に変更できる機能を実装しました。
シンタックスハイライト設定と同様に、JSON設定ファイルを直接編集することで背景色を変更できます。

## 実装内容

### 1. WindowSettings.csの拡張

`src/Settings/WindowSettings.cs` に背景色を保存するプロパティを追加しました。

**追加したプロパティ:**
- `BackgroundColor` (string型): 16進数カラーコード（例: #FFFFFF）
- デフォルト値: `#FFFFFF` (白)

設定はJSON形式で `%AppData%/MyMarkdownEditor/window-settings.json` に保存されます。

### 2. MainWindowの更新

`src/MainWindow.xaml.cs` に背景色適用機能を追加しました:

**追加したメソッド:**
- `ApplyBackgroundColor(string hexColor)`: 背景色を適用

**初期化処理:**
- アプリケーション起動時にJSON設定ファイルから背景色を読み込み、自動的に適用

## 使い方

### 背景色の変更手順

1. アプリケーションを一度起動して終了（設定ファイルが自動生成されます）
2. 設定ファイルを開く: `%AppData%/MyMarkdownEditor/window-settings.json`
   - Windowsの場合: `C:\Users\<ユーザー名>\AppData\Roaming\MyMarkdownEditor\window-settings.json`
3. `BackgroundColor` の値を変更（例: `"#F5F5F5"`）
4. ファイルを保存
5. アプリケーションを再起動

### 設定ファイルの例

```json
{
  "Width": 900,
  "Height": 600,
  "Left": 100,
  "Top": 100,
  "WindowState": 0,
  "BackgroundColor": "#F5F5F5"
}
```

### 設定の永続化

- 設定ファイルはアプリケーション起動時に自動的に読み込まれます
- ウィンドウサイズや位置は自動的に保存されますが、背景色は手動でJSON編集が必要です
- 設定ファイルの場所: `%AppData%/MyMarkdownEditor/window-settings.json`

## カラーコード例

以下は使用できるカラーコードの例です:

| 色 | カラーコード | 説明 |
|---|---|---|
| 白 | #FFFFFF | デフォルト |
| ライトグレー | #F5F5F5 | 柔らかい白 |
| グレー | #E0E0E0 | 薄いグレー |
| ベージュ | #F5F5DC | 暖かみのある白 |
| ライトブルー | #E6F2FF | 目に優しい青 |
| ライトグリーン | #E8F5E9 | 目に優しい緑 |
| ライトイエロー | #FFFDE7 | 暖かみのある黄色 |
| ダークグレー | #2D2D2D | ダークモード風 |
| ブラック | #000000 | 完全な黒 |

## 技術的な詳細

### カラーコード形式

以下の形式をサポートしています:
- `#RGB` (例: #FFF)
- `#RRGGBB` (例: #FFFFFF)
- `#AARRGGBB` (例: #80FFFFFF - 透明度付き)

### エラーハンドリング

- 無効なカラーコードの場合、デフォルト値（白）が使用されます
- 設定ファイルの読み込みに失敗した場合、デフォルト値（白）が使用されます
- 設定の保存に失敗してもアプリケーションは正常に動作します

## ファイル構成

```
src/
├── MainWindow.xaml.cs           # 背景色適用ロジックを追加
└── Settings/
    └── WindowSettings.cs        # BackgroundColorプロパティを追加
```

## シンタックスハイライト設定との類似点

この機能は、シンタックスハイライト設定（`highlight-settings.json`）と同様に、JSON設定ファイルを直接編集する方式を採用しています。これにより：

- UIを介さずに設定を変更できる
- 設定ファイルをバックアップ・共有しやすい
- テキストエディタで簡単に編集できる

## 関連issue

- issue #7: ウィンドウ背景色のカスタマイズ機能

---

**実装日**: 2025-11-11
**担当**: Claude
**コミットプレフィックス**: feat #7:
