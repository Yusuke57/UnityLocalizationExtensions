# Unity Localization Extensions
UnityのLocalizationパッケージをより便利にするエディタ拡張  
Editor extensions to make Unity's Localization package more convenient

## 詳細
1. GoogleSpreadsheet連携をした際に、Unityエディタ上部に追加したボタンで **テーブルの一括Push/Pull** ができる  
**対象のGoogleSpreadsheetを開く** ボタンも一緒に追加
2. LocalizeStringEventとLocalizeSpriteEventのコンポーネントに対して、1ボタンで以下を行える
   - Entryが指定されていなければ、**指定テーブルにEntryを新規作成**
   - TextMeshProUGUIやImageの値を、**LocalizeStringEventやLocalizeSpriteEventの指定言語に自動入力**

https://github.com/user-attachments/assets/2582107a-78ee-44a0-bc1a-de8bf43833e4

1. When linking with Google Spreadsheet, additional buttons are added to the top of the Unity Editor, allowing you to Push/Pull all tables at once.  
A button to open the target Google Spreadsheet is also added.
2. For LocalizeStringEvent and LocalizeSpriteEvent components, a single button can perform the following:
   - If no entry is specified, automatically create a new entry in the specified table.
   - Automatically input the value of TextMeshProUGUI or Image into the corresponding language field of LocalizeStringEvent or LocalizeSpriteEvent.


## 導入方法
PackageManagerの+ボタンから「Install package from git URL...」を選択し、
`https://github.com/Yusuke57/UnityLocalizationExtensions.git`
を入力して「Install」

一部の機能だけ必要な場合は、ダウンロードしてファイルをUnityプロジェクトにインポートしてもOKです

## 注意
Localizationパッケージを使い始めたばかりなので、機能不足や問題点が見つかり次第アップデートするかもしれません  
Unity6000.0で動作確認済み
