# Unity Localization Extensions
UnityのLocalizationパッケージをより便利にするエディタ拡張  
Editor extensions to make Unity's Localization package more convenient

## 詳細
1. GoogleSpreadsheet連携をした際に、Unityエディタ上部に追加したボタンで **テーブル一括Push/Pull** ができる  
**対象のGoogleSpreadsheetを開く** ボタンも一緒に追加
2. LocalizeStringEventとLocalizeSpriteEventのコンポーネントに対して、1ボタンで以下を行える
   - Entryが指定されていなければ、**指定テーブルにEntryを新規作成**
   - TextMeshProUGUIやImageの値を、**LocalizeStringEventやLocalizeSpriteEventの指定言語に自動入力**
3. LocalizationTablesWindowに追加した表示更新ボタンで **テーブルの強制表示更新** ができる

https://github.com/user-attachments/assets/2582107a-78ee-44a0-bc1a-de8bf43833e4

<img width="320" alt="スクリーンショット 2025-05-14 22 08 12" src="https://github.com/user-attachments/assets/ec79ee81-e5de-4a01-94d6-b834f50cd6f7" />

1. When linking with Google Spreadsheet, additional buttons are added to the top of the Unity Editor, allowing you to Push/Pull all tables at once.  
A button to open the target Google Spreadsheet is also added.
2. For LocalizeStringEvent and LocalizeSpriteEvent components, a single button can perform the following:
   - If no entry is specified, automatically create a new entry in the specified table.
   - Automatically input the value of TextMeshProUGUI or Image into the corresponding language field of LocalizeStringEvent or LocalizeSpriteEvent.
3. The refresh button added to the LocalizationTablesWindow can force the table to visually update.

## 導入方法
PackageManagerの+ボタンから「Install package from git URL...」を選択し、
`https://github.com/Yusuke57/UnityLocalizationExtensions.git`
を入力して「Install」

一部の機能だけ必要な場合は、ダウンロードしてファイルをUnityプロジェクトにインポートしてもOKです

## その他
Localizationパッケージを使い始めたばかりなので、機能不足や問題点が見つかり次第アップデートするかもしれません  
Unity6000.0で動作確認済み

LocalizationとSpreadsheetの連携方法はこちらにまとめました  
https://unity-yuji.xyz/localization-spreadsheet/
