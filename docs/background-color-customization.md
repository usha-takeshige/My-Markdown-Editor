# 背景色カスタマイズ機能の実装

## 概要

issue #7として、ウィンドウの背景色を任意の色（16進数で表現）に変更できる機能を実装しました。

## 実装内容

### 1. WindowSettings.csの拡張

`src/Settings/WindowSettings.cs` に背景色を保存するプロパティを追加しました。

**追加したプロパティ:**
- `BackgroundColor` (string型): 16進数カラーコード（例: #FFFFFF）
- デフォルト値: `#FFFFFF` (白)

設定はJSON形式で `%AppData%/MyMarkdownEditor/window-settings.json` に保存されます。

### 2. 背景色設定ダイアログの作成

新しいダイアログウィンドウを作成しました:

**ファイル:**
- `src/Settings/BackgroundColorDialog.xaml`
- `src/Settings/BackgroundColorDialog.xaml.cs`

**機能:**
- 16進数カラーコード入力（例: #FFFFFF, #F0F0F0, #E0E0E0）
- リアルタイムプレビュー表示
- 入力値の妥当性検証
- 無効な色コードの場合はエラーメッセージを表示

### 3. MainWindowの更新

`src/MainWindow.xaml.cs` に以下の機能を追加しました:

**追加したメソッド:**
- `ApplyBackgroundColor(string hexColor)`: 背景色を適用
- `ChangeBackgroundColor()`: 背景色設定ダイアログを表示

**追加したコマンド:**
- `ChangeBackgroundColorCommand`: 背景色変更コマンド

**初期化処理:**
- アプリケーション起動時に保存された背景色を自動的に適用

### 4. キーボードショートカットの追加

`src/MainWindow.xaml` にキーバインディングを追加しました:

**ショートカット:**
- `Ctrl + K`: 背景色設定ダイアログを開く

## 使い方

### 背景色の変更手順

1. アプリケーションを起動
2. `Ctrl + K` を押す
3. 背景色設定ダイアログが表示される
4. 16進数カラーコードを入力（例: `#F5F5F5`）
5. プレビューで色を確認
6. 「OK」ボタンをクリック

### 設定の永続化

- 変更した背景色は自動的に保存されます
- 次回起動時に設定が復元されます
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

- 無効なカラーコードが入力された場合、エラーメッセージが表示されます
- 設定ファイルの読み込みに失敗した場合、デフォルト値（白）が使用されます
- 設定の保存に失敗してもアプリケーションは正常に動作します

## ファイル構成

```
src/
├── MainWindow.xaml              # キーバインディングを追加
├── MainWindow.xaml.cs           # 背景色適用ロジックを追加
└── Settings/
    ├── WindowSettings.cs        # BackgroundColorプロパティを追加
    ├── BackgroundColorDialog.xaml     # 新規作成
    └── BackgroundColorDialog.xaml.cs  # 新規作成
```

## 今後の拡張案

- プリセットカラーパレットの追加
- カラーピッカーUIの実装
- 最近使用した色の履歴機能
- テーマプリセット（ライト、ダーク、セピアなど）

## 関連issue

- issue #7: ウィンドウ背景色のカスタマイズ機能

---

**実装日**: 2025-11-10
**担当**: Claude
**コミットプレフィックス**: feat #7:
