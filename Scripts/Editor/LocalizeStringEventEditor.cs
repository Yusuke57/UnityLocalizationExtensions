using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UIElements;

namespace YujiAp.UnityLocalizationExtensions.Editor
{
    [CustomEditor(typeof(LocalizeStringEvent))]
    public class LocalizeStringEventEditor : UnityEditor.Editor
    {
        private PopupField<string> _tableNamePopup;
        private PopupField<string> _localeCodePopup;

        private MethodInfo _raiseTableEntryAddedMethodInfo;
        private MethodInfo RaiseTableEntryModifiedMethodInfo => _raiseTableEntryAddedMethodInfo
            ??= LocalizationEditorSettings.EditorEvents.GetType().GetMethod("RaiseTableEntryAdded", BindingFlags.NonPublic | BindingFlags.Instance);

        private static string TargetTableIndexPrefsKey => $"{Application.dataPath}.LocalizeStringEvent.TargetTableIndex";
        private static string TargetLocaleIndexPrefsKey => $"{Application.dataPath}.LocalizeStringEvent.TargetLocaleIndex";

        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();

            var horizontalLayout = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 10,
                }
            };
            container.Add(horizontalLayout);

            // テーブル選択ドロップダウン
            var tableNames = LocalizationEditorSettings.GetStringTableCollections().Select(v => v.name).ToList();
            _tableNamePopup = new PopupField<string>("", tableNames, EditorPrefs.GetInt(TargetTableIndexPrefsKey, 0));
            _tableNamePopup.RegisterValueChangedCallback(ev =>
            {
                var newIndex = Mathf.Max(tableNames.IndexOf(ev.newValue), 0);
                EditorPrefs.SetInt(TargetTableIndexPrefsKey, newIndex);
            });
            horizontalLayout.Add(_tableNamePopup);

            // 初期値指定言語選択ドロップダウン
            var localeCodes = LocalizationEditorSettings.GetLocales().Select(v => v.Identifier.Code).ToList();
            _localeCodePopup = new PopupField<string>("", localeCodes, EditorPrefs.GetInt(TargetLocaleIndexPrefsKey, 0));
            _localeCodePopup.RegisterValueChangedCallback(ev =>
            {
                var newIndex = Mathf.Max(localeCodes.IndexOf(ev.newValue), 0);
                EditorPrefs.SetInt(TargetLocaleIndexPrefsKey, newIndex);
            });
            horizontalLayout.Add(_localeCodePopup);

            // 新規Entry作成 & デフォルト値指定ボタン
            var createNewEntryButton = new Button(CreateNewEntryAndSetDefault)
            {
                text = "Create Entry (If Needed) and Set Default",
                style =
                {
                    flexGrow = 1
                }
            };
            horizontalLayout.Add(createNewEntryButton);

            // デフォルトのInspector表示
            InspectorElement.FillDefaultInspector(container, serializedObject, this);

            return container;
        }

        private void CreateNewEntryAndSetDefault()
        {
            serializedObject.Update();

            var tableCollection = LocalizationEditorSettings.GetStringTableCollection(_tableNamePopup.value);
            if (tableCollection == null)
            {
                return;
            }

            var sharedData = tableCollection.SharedData;
            Undo.RecordObject(sharedData, "Create Entry (If Needed) and Set Default");

            // テーブルを指定
            var localizeStringEvent = (LocalizeStringEvent) target;
            localizeStringEvent.SetTable(_tableNamePopup.value);

            // 既にEntryの指定があればそれを、なければ新規Entryを作成して取得
            var entryKey = localizeStringEvent.StringReference.TableEntryReference.Key;
            var hasEntry = !string.IsNullOrEmpty(entryKey);
            var entry = hasEntry
                ? sharedData.GetEntry(entryKey)
                : sharedData.AddKey();

            if (!hasEntry)
            {
                // 作成したものを更新通知してからTableEntryReferenceに指定
                EditorUtility.SetDirty(sharedData);
                RaiseTableEntryModifiedMethodInfo?.Invoke(LocalizationEditorSettings.EditorEvents, new object[] { tableCollection, entry });
                localizeStringEvent.StringReference.TableEntryReference = entry.Id;
            }

            // 紐づくTextMeshProUGUI.textの文字列を取得
            var defaultText = string.Empty;
            var persistentEventCount = localizeStringEvent.OnUpdateString.GetPersistentEventCount();
            for (var i = 0; i < persistentEventCount; i++)
            {
                var persistentTarget = localizeStringEvent.OnUpdateString.GetPersistentTarget(i);
                if (persistentTarget is TextMeshProUGUI textComponent)
                {
                    defaultText = textComponent.text;
                    break;
                }
            }

            // Entryに初期値を指定して登録
            var targetLanguageStringTable = tableCollection.StringTables
                .FirstOrDefault(v => v.LocaleIdentifier.Code == _localeCodePopup.value);
            if (targetLanguageStringTable != null)
            {
                targetLanguageStringTable.AddEntry(entry.Id, defaultText);
                LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, tableCollection);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(localizeStringEvent);
        }
    }
}