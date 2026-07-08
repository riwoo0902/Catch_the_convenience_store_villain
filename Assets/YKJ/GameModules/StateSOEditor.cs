using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Reflection;
using System.Linq;
using Agents.FSM;

[CustomEditor(typeof(StateSO))]
public class StateSOEditor : UnityEditor.Editor
{
    [SerializeField] private VisualTreeAsset editorView = default;

    private StateSO _targetData;

    public override VisualElement CreateInspectorGUI()
    {
        _targetData = target as StateSO;
        VisualElement root = new VisualElement();
        editorView.CloneTree(root);

        FillDropdownField(root);
        return root;
    }

    private void FillDropdownField(VisualElement root)
    {
        DropdownField field = root.Q<DropdownField>("ClassNameDropdown");
        Assembly fsmAssembly = Assembly.GetAssembly(typeof(StateSO));

        var choices = fsmAssembly.GetTypes()
              .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(AgentState)))
              .Select(type => type.FullName);

        field.choices.AddRange(choices);
        if (_targetData != null && !string.IsNullOrEmpty(_targetData.className) && field.choices.Contains(_targetData.className))
        {
            field.value = _targetData.className;
        }
        else if (_targetData != null && field.choices.Count > 0)
        {
            _targetData.className = field.choices.First();
            EditorUtility.SetDirty(_targetData);
        }
        AssetDatabase.SaveAssetIfDirty(_targetData);


    }
}


