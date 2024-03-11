using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace IsotopicSDK.Utils
{
	[System.Serializable]
	public class SceneField
	{
		[SerializeField]
		private Object m_SceneAsset;

		[SerializeField]
		private string m_SceneName = "";
		public string SceneName
		{
			get { return m_SceneName; }
		}

		public static implicit operator string(SceneField sceneField)
		{
			return sceneField.SceneName;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(SceneField))]
	public class SceneFieldPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
		{
			EditorGUI.BeginProperty(_position, GUIContent.none, _property);
			SerializedProperty sceneAsset = _property.FindPropertyRelative("m_SceneAsset");
			SerializedProperty sceneName = _property.FindPropertyRelative("m_SceneName");
			_position = EditorGUI.PrefixLabel(_position, GUIUtility.GetControlID(FocusType.Passive), _label);
			if (sceneAsset != null)
			{
				sceneAsset.objectReferenceValue = EditorGUI.ObjectField(_position, sceneAsset.objectReferenceValue, typeof(SceneAsset), false);

				if (sceneAsset.objectReferenceValue != null)
				{
					sceneName.stringValue = (sceneAsset.objectReferenceValue as SceneAsset).name;
				} else
                {
					sceneName.stringValue = null;
                }
			}
			EditorGUI.EndProperty();
		}
	}
#endif
}
