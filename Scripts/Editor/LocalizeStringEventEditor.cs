using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.UIElements;
using UnityEngine.Localization.Components;
using UnityEngine.UIElements;

namespace Common.Localization.Editor
{
    [CustomEditor(typeof(LocalizeStringEvent))]
    public class LocalizeStringEventEditor : UnityEditor.Editor
    {
        private PopupField<string> _tableNamePopup;
        private PopupField<string> _localeCodePopup;

        private MethodInfo _raiseTableEntryAddedMethodInfo;
        private MethodInfo _raiseTableEntryModifiedMethodInfo;

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
            _tableNamePopup = new PopupField<string>("", tableNames, 0);
            horizontalLayout.Add(_tableNamePopup);

            // 初期値指定言語選択ドロップダウン
            var localeCodes = LocalizationEditorSettings.GetLocales().Select(v => v.Identifier.Code).ToList();
            _localeCodePopup = new PopupField<string>("", localeCodes, 0);
            horizontalLayout.Add(_localeCodePopup);

            // 新規Entry作成ボタン
            var createNewEntryButton = new Button(CreateNewEntryAndSetDefault)
            {
                text = "Create New Entry and Set Default",
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
            var sharedData = tableCollection.SharedData;
            Undo.RecordObject(sharedData, "Create new entry and set default");

            // テーブルを指定
            var localizeStringEvent = (LocalizeStringEvent) target;
            localizeStringEvent.SetTable(_tableNamePopup.value);

            // 新規Entryを作成して指定
            var entry = sharedData.AddKey();
            EditorUtility.SetDirty(sharedData);

            _raiseTableEntryAddedMethodInfo ??= LocalizationEditorSettings.EditorEvents.GetType()
                .GetMethod("RaiseTableEntryAdded", BindingFlags.NonPublic | BindingFlags.Instance);
            _raiseTableEntryAddedMethodInfo?.Invoke(LocalizationEditorSettings.EditorEvents, new object[] { tableCollection, entry });
            localizeStringEvent.SetEntry(entry.Key);

            // 紐づくTextMeshProUGUIの値を取得
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
                _raiseTableEntryModifiedMethodInfo ??= LocalizationEditorSettings.EditorEvents.GetType()
                    .GetMethod("RaiseTableEntryModified", BindingFlags.NonPublic | BindingFlags.Instance);
                _raiseTableEntryModifiedMethodInfo?.Invoke(LocalizationEditorSettings.EditorEvents,
                    new object[] { sharedData.GetEntry(entry.Id) });
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(localizeStringEvent);
        }
    }
}