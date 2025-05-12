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
        private MethodInfo _raiseTableEntryAddedMethodInfo;
        private MethodInfo _raiseTableEntryModifiedMethodInfo;

        private const string TargetTableName = "UI";

        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();
            InspectorElement.FillDefaultInspector(container, serializedObject, this);
            
            var initializeButton = new Button(() =>
            {
                serializedObject.Update();
                var localizeStringEvent = (LocalizeStringEvent) target;
                
                // UIテーブルを指定
                localizeStringEvent.SetTable(TargetTableName);
                
                // 新規Entryを作成して指定
                var tableCollection = LocalizationEditorSettings.GetStringTableCollection(TargetTableName);
                var sharedData = tableCollection.SharedData;
                Undo.RecordObject(sharedData, "Add entry.");

                var entry = sharedData.AddKey();
                EditorUtility.SetDirty(sharedData);

                _raiseTableEntryAddedMethodInfo ??= LocalizationEditorSettings.EditorEvents.GetType()
                    .GetMethod("RaiseTableEntryAdded", BindingFlags.NonPublic | BindingFlags.Instance);
                _raiseTableEntryAddedMethodInfo?.Invoke(LocalizationEditorSettings.EditorEvents, new object[] { tableCollection, entry });
                localizeStringEvent.SetEntry(entry.Key);
                
                // 紐づくTextMeshProUGUIの値を取得
                var initialText = string.Empty;
                var persistentEventCount = localizeStringEvent.OnUpdateString.GetPersistentEventCount();
                for (var i = 0; i < persistentEventCount; i++)
                {
                    var persistentTarget = localizeStringEvent.OnUpdateString.GetPersistentTarget(i);
                    if (persistentTarget is TextMeshProUGUI textComponent)
                    {
                        initialText = textComponent.text;
                        break;
                    }
                }
                
                // EntryのJapaneseに初期値を指定して登録
                var japaneseStringTable = tableCollection.StringTables.FirstOrDefault(v => v.LocaleIdentifier.Code == "ja");
                if (japaneseStringTable != null)
                {
                    japaneseStringTable.AddEntry(entry.Id, initialText);
                    _raiseTableEntryModifiedMethodInfo ??= LocalizationEditorSettings.EditorEvents.GetType()
                        .GetMethod("RaiseTableEntryModified", BindingFlags.NonPublic | BindingFlags.Instance);
                    _raiseTableEntryModifiedMethodInfo?.Invoke(LocalizationEditorSettings.EditorEvents, new object[] { entry });
                }
                
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(localizeStringEvent);
            })
            {
                text = $"Create New {TargetTableName} Entry",
            };
            
            container.Add(initializeButton);

            return container;
        }
    }
}