using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using LocalizationTablesWindow = UnityEditor.Localization.UI.LocalizationTablesWindow;

namespace YujiAp.UnityLocalizationExtensions.Editor
{
    public static class LocalizationTablesWindowToolbarButtonInjector
    {
        private static readonly HashSet<EditorWindow> _processedWindows = new();
        private static EditorWindow _lastFocusedWindow;
        private static FieldInfo _selectionChangedFieldInfo;

        private static FieldInfo SelectionChangedField => _selectionChangedFieldInfo
            ??= typeof(LocalizationTablesWindow).GetField("m_SelectionChanged", BindingFlags.Instance | BindingFlags.NonPublic);

        private const string ReloadButtonName = "ReloadButton";

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            var currentWindow = EditorWindow.focusedWindow;
            if (currentWindow == null || currentWindow == _lastFocusedWindow)
            {
                return;
            }

            _lastFocusedWindow = currentWindow;

            if (currentWindow.GetType() == typeof(LocalizationTablesWindow) && !_processedWindows.Contains(currentWindow))
            {
                InjectToolbarButton(currentWindow);
            }
        }

        private static void InjectToolbarButton(EditorWindow window)
        {
            // Toolbar を探す
            var toolbar = window.rootVisualElement?.Query<Toolbar>().First();
            if (toolbar == null)
            {
                return;
            }

            // 既に追加されていないか確認
            if (toolbar.Q<ToolbarButton>(ReloadButtonName) != null)
            {
                return;
            }

            // ボタンの作成
            var button = new ToolbarButton(RefreshWindow)
            {
                name = ReloadButtonName,
                style =
                {
                    width = 24,
                    paddingTop = 0,
                    paddingBottom = 0,
                    paddingLeft = 0,
                    paddingRight = 0
                }
            };

            var searchFieldIndex = toolbar.IndexOf(toolbar.Q<ToolbarSearchField>());
            toolbar.Insert(searchFieldIndex + 1, button);

            // アイコンの作成
            var iconImage = new Image
            {
                image = EditorGUIUtility.IconContent("d_Refresh").image as Texture2D,
                scaleMode = ScaleMode.ScaleToFit,
                style =
                {
                    width = 16,
                    height = 20,
                    alignSelf = Align.Center,
                    justifyContent = Justify.Center
                }
            };
            button.Add(iconImage);

            _processedWindows.Add(window);
        }

        private static void RefreshWindow()
        {
            var windowType = typeof(LocalizationTablesWindow);
            var window = EditorWindow.GetWindow(windowType);
            if (window == null)
            {
                return;
            }

            // 選択テーブルの変更フラグを立てて更新させる
            SelectionChangedField?.SetValue(window, true);

            // 一応ウィンドウの再描画も呼ぶ
            window.Repaint();
        }
    }
}