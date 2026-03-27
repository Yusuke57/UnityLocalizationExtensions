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
            var localizationParent = GetToolbarRightZone();
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
            var dropdown = CreateTargetTableDropdown();
            ApplyToolbarOverlayStyle(dropdown);
            group.Add(dropdown);

            // Open
            var openButton = new EditorToolbarButton(OpenSpreadsheet)
            {
                name = "OpenSpreadsheet",
                icon = (Texture2D) EditorGUIUtility.IconContent("d_Linked").image,
                tooltip = "Open spreadsheet",
            };
            ApplyToolbarOverlayStyle(openButton);
            group.Add(openButton);
            _buttons.Add(openButton);

            // Pull
            var pullButton = new EditorToolbarButton(PullSpreadsheetAll)
            {
                name = "PullSpreadsheetAll",
                icon = (Texture2D) EditorGUIUtility.IconContent("CollabPull").image,
                tooltip = "Pull from spreadsheets all",
            };
            ApplyToolbarOverlayStyle(pullButton);
            group.Add(pullButton);
            _buttons.Add(pullButton);

            // Push
            var pushButton = new EditorToolbarButton(PushSpreadsheetAll)
            {
                name = "PushSpreadsheetAll",
                icon = (Texture2D) EditorGUIUtility.IconContent("CollabPush").image,
                tooltip = "Push to spreadsheets all",
            };
            ApplyToolbarOverlayStyle(pushButton);
            group.Add(pushButton);
            _buttons.Add(pushButton);
            
            OnTargetTableChanged();
        }

        /// <summary>
        /// ツールバーの VisualElement を取得する。
        /// Unity 6000.3+: MainToolbarWindow の rootVisualElement を使用。
        /// Unity 6000.1: Toolbar (HostView) の m_Root を使用。
        /// </summary>
        private static VisualElement GetToolbar()
        {
            var editorAssembly = typeof(UnityEditor.Editor).Assembly;

#if UNITY_6000_3_OR_NEWER
            var mainToolbarWindowType = editorAssembly.GetType("UnityEditor.MainToolbarWindow");
            if (mainToolbarWindowType != null)
            {
                var instances = Resources.FindObjectsOfTypeAll(mainToolbarWindowType);
                if (instances.Length > 0 && instances[0] is EditorWindow toolbarWindow)
                {
                    return toolbarWindow.rootVisualElement;
                }
            }
#else
            var toolbarType = editorAssembly.GetType("UnityEditor.Toolbar");
            if (toolbarType != null)
            {
                var toolbarInstances = Resources.FindObjectsOfTypeAll(toolbarType);
                if (toolbarInstances.Length > 0)
                {
                    var rootField = toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                    return rootField?.GetValue(toolbarInstances[0]) as VisualElement;
                }
            }
#endif
            return null;
        }

        /// <summary>
        /// ツールバーの右側ゾーンを取得する。
        /// </summary>
        private static VisualElement GetToolbarRightZone()
        {
            var toolbar = GetToolbar();
            if (toolbar == null) return null;

#if UNITY_6000_3_OR_NEWER
            // overlay-toolbar__top 内の ContainerSection: [0]=左, [1]=中央, [2]=右
            var overlayContainer = toolbar.Q("overlay-toolbar__top");
            if (overlayContainer != null)
            {
                var sections = overlayContainer.Children().ToList();
                if (sections.Count >= 3) return sections[2];
            }
            return null;
#else
            return toolbar.Q("ToolbarZoneRightAlign");
#endif
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
#if UNITY_6000_3_OR_NEWER
                    alignSelf = Align.Center,
                    alignItems = Align.Center,
#endif
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

#if UNITY_6000_3_OR_NEWER
        /// <summary>
        /// ツールバー要素にOverlayToolbar相当のスタイルを適用する
        /// </summary>
        private static void ApplyToolbarOverlayStyle(VisualElement element)
        {
            if (element is EditorToolbarButton or EditorToolbarDropdown)
            {
                element.style.flexDirection = FlexDirection.Row;
                element.style.alignItems = Align.Center;
            }

            element.Query(className: "unity-editor-toolbar-element__icon").ForEach(icon =>
            {
                icon.style.width = 16;
                icon.style.height = 16;
            });
        }
#else
        private static void ApplyToolbarOverlayStyle(VisualElement element) { }
#endif
    }
}