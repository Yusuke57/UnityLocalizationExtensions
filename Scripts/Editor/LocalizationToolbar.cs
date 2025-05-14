using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace YujiAp.UnityLocalizationExtensions.Editor
{
    public static class LocalizationToolbar
    {
        private static EditorToolbarDropdown _targetTableDropdown;
        private static readonly HashSet<string> _targetTables = new();
        private static readonly List<EditorToolbarButton> _buttons = new();

        private const string TargetTableDropdownName = "TargetTableDropdown";

        private static string TargetTablesPrefsKey => $"{Application.dataPath}.LocalizationToolbar.TargetTables";

        private static List<StringTableCollection> GoogleSheetsTableCollections =>
            LocalizationEditorSettings.GetStringTableCollections()
                .Where(v => v.Extensions.OfType<GoogleSheetsExtension>().Any())
                .ToList();

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            var localizationParent = GetToolbar()?.Q("ToolbarZoneRightAlign");
            if (localizationParent == null)
            {
                return;
            }

            // ドロップダウンの有効/無効を更新
            _targetTableDropdown?.SetEnabled(GoogleSheetsTableCollections.Any());

            // ボタンの有効/無効を更新
            foreach (var button in _buttons)
            {
                button.SetEnabled(_targetTables.Count > 0);
            }

            // 対象Spreadsheet選択ドロップダウンが描画済みであれば何もしない
            // MEMO: Unityエディタをディスプレイ移動した時など、描画が消える場合があるため毎フレーム確認しておく
            if (localizationParent.Q(TargetTableDropdownName) != null)
            {
                return;
            }

            var group = CreateGroup();
            localizationParent.Add(group);

            // ラベル
            var label = new Label("LocalizeSheets")
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleLeft,
                    paddingLeft = 6,
                    paddingRight = 6,
                    backgroundColor = new Color(0, 0, 0, 0.3f),
                    color = new Color(0.5f, 0.5f, 0.5f),
                    borderBottomLeftRadius = 2,
                    borderTopLeftRadius = 2,
                }
            };
            group.Add(label);

            // 対象のTableを選択するドロップダウン
            group.Add(CreateTargetTableDropdown());

            // Open
            var openButton = new EditorToolbarButton(OpenSpreadsheet)
            {
                name = "OpenSpreadsheet",
                icon = (Texture2D) EditorGUIUtility.IconContent("d_Linked").image,
                tooltip = "Open spreadsheet",
            };
            group.Add(openButton);
            _buttons.Add(openButton);

            // Pull
            var pullButton = new EditorToolbarButton(PullSpreadsheetAll)
            {
                name = "PullSpreadsheetAll",
                icon = (Texture2D) EditorGUIUtility.IconContent("CollabPull").image,
                tooltip = "Pull from spreadsheets all",
            };
            group.Add(pullButton);
            _buttons.Add(pullButton);

            // Push
            var pushButton = new EditorToolbarButton(PushSpreadsheetAll)
            {
                name = "PushSpreadsheetAll",
                icon = (Texture2D) EditorGUIUtility.IconContent("CollabPush").image,
                tooltip = "Push to spreadsheets all",
            };
            group.Add(pushButton);
            _buttons.Add(pushButton);
            
            OnTargetTableChanged();
        }

        private static VisualElement GetToolbar()
        {
            var toolbarType = Type.GetType("UnityEditor.Toolbar,UnityEditor")!;

            var getField = toolbarType.GetField("get", BindingFlags.Static | BindingFlags.Public);
            var getValue = getField?.GetValue(null);

            var windowBackendProperty = toolbarType.GetProperty("windowBackend", BindingFlags.Instance | BindingFlags.NonPublic);
            var windowBackendValue = windowBackendProperty?.GetValue(getValue);

            var iWindowBackendType = Type.GetType("UnityEditor.IWindowBackend,UnityEditor")!;

            var visualTreeProperty = iWindowBackendType.GetProperty("visualTree", BindingFlags.Instance | BindingFlags.Public);
            var visualTreeValue = visualTreeProperty?.GetValue(windowBackendValue);

            return visualTreeValue as VisualElement;
        }

        private static VisualElement CreateGroup()
        {
            var group = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    paddingRight = 4,
                    marginRight = 4,
                    marginLeft = 4,
                    backgroundColor = new Color(0.4f, 0.4f, 0.4f, 0.3f),
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2,
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                }
            };

            return group;
        }

        private static async Task ProcessSpreadsheetAllAsync(
            Func<GoogleSheets, GoogleSheetsExtension, StringTableCollection, Task> processFunc, string title)
        {
            var targetCollections = GoogleSheetsTableCollections
                .Where(v => _targetTables.Contains(v.name))
                .ToList();
            var tasks = new List<Task>();

            try
            {
                var total = targetCollections.Count;
                var current = 0;

                var progress = (float) current / total;
                foreach (var stringTableCollection in targetCollections)
                {
                    var googleSheetsExtension = stringTableCollection.Extensions
                        .OfType<GoogleSheetsExtension>()
                        .First();

                    var googleSheets = new GoogleSheets(googleSheetsExtension.SheetsServiceProvider)
                    {
                        SpreadSheetId = googleSheetsExtension.SpreadsheetId
                    };

                    // プログレスバー表示
                    EditorUtility.DisplayProgressBar(title, $"Processing {stringTableCollection.name} ({current}/{total})", progress);

                    var task = processFunc(googleSheets, googleSheetsExtension, stringTableCollection);
                    tasks.Add(task);

                    current++;
                }

                await Task.WhenAll(tasks);

                Debug.Log("All spreadsheets processed successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to process spreadsheets: {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void PushSpreadsheetAll()
        {
            _ = ProcessSpreadsheetAllAsync((googleSheets, extension, collection)
                    => googleSheets.PushStringTableCollectionAsync(extension.SheetId, collection, extension.Columns),
                "Pushing to spreadsheets");
        }

        private static void PullSpreadsheetAll()
        {
            _ = ProcessSpreadsheetAllAsync((googleSheets, extension, collection) =>
            {
                googleSheets.PullIntoStringTableCollection(extension.SheetId, collection, extension.Columns,
                    extension.RemoveMissingPulledKeys, null, true);
                return Task.CompletedTask;
            }, "Pulling from spreadsheets");
        }

        private static void OpenSpreadsheet()
        {
            if (_targetTables.Count == 0)
            {
                return;
            }

            var targetTable = _targetTables.First();
            var targetCollection = LocalizationEditorSettings.GetStringTableCollection(targetTable);
            var googleSheetsExtension = targetCollection.Extensions
                .OfType<GoogleSheetsExtension>()
                .FirstOrDefault();
            if (googleSheetsExtension != null)
            {
                GoogleSheets.OpenSheetInBrowser(googleSheetsExtension.SpreadsheetId, googleSheetsExtension.SheetId);
            }
        }

        private static VisualElement CreateTargetTableDropdown()
        {
            _targetTableDropdown = new EditorToolbarDropdown
            {
                name = TargetTableDropdownName,
                tooltip = "Target spreadsheets",
            };

            _targetTableDropdown.clicked += () =>
            {
                var menu = new GenericMenu();
                var stringTableCollections = LocalizationEditorSettings.GetStringTableCollections();
                foreach (var stringTableCollection in stringTableCollections)
                {
                    AddTargetTableDropdownOption(menu, stringTableCollection.name);
                }

                var rect = new Rect(Event.current.mousePosition, Vector2.zero);
                menu.DropDown(rect);
            };

            // EditorPrefsから保存されたTableの情報を取得
            var targetTablesString = EditorPrefs.GetString(TargetTablesPrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(targetTablesString))
            {
                _targetTables.Clear();
                var targetTables = targetTablesString.Split(',');
                foreach (var targetTable in targetTables)
                {
                    _targetTables.Add(targetTable);
                }
            }

            return _targetTableDropdown;
        }

        private static void AddTargetTableDropdownOption(GenericMenu menu, string optionName)
        {
            var isSelected = _targetTables.Contains(optionName);
            menu.AddItem(new GUIContent(optionName), isSelected, () =>
            {
                if (isSelected)
                {
                    _targetTables.Remove(optionName);
                }
                else
                {
                    _targetTables.Add(optionName);
                }

                // 選択したTableの情報をEditorPrefsに保存
                var targetTablesString = string.Join(",", _targetTables);
                EditorPrefs.SetString(TargetTablesPrefsKey, targetTablesString);

                OnTargetTableChanged();
            });
        }

        private static void OnTargetTableChanged()
        {
            // ドロップダウンのテキスト更新
            string text;
            if (_targetTables.Count == 0)
            {
                text = "Target...";
            }
            else if (_targetTables.Count == LocalizationEditorSettings.GetStringTableCollections().Count)
            {
                text = "All";
            }
            else if (_targetTables.Count == 1)
            {
                text = _targetTables.First();
            }
            else
            {
                text = $"{_targetTables.Count} items";
            }

            _targetTableDropdown.text = text;
        }
    }
}