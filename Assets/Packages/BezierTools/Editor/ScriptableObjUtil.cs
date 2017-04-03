using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

namespace BezierTools
{
	public static class ScriptableObjUtil {

		public static void CreateAsset<T>() where T : ScriptableObject {
			var path = AssetDatabase.GetAssetPath(Selection.activeObject);
			if (path == "")
				path = "Assets";
			else if (!Directory.Exists(path)) {
				var lastSlash = path.LastIndexOf("/");
				if (lastSlash >= 0)
					path = path.Substring(0, lastSlash);
			}
			path = AssetDatabase.GenerateUniqueAssetPath(path + "/" + typeof(T).Name + ".asset");

			var asset = ScriptableObject.CreateInstance<T>();
			AssetDatabase.CreateAsset(asset, path);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = asset;
		}

		public static void CreateAssetNonGeneric(System.Type T) {
			var ti = typeof(ScriptableObjUtil);
			var mi = ti.GetMethod("CreateAsset");
			mi = mi.MakeGenericMethod(T);
			mi.Invoke(null, null);
		}
				
		[MenuItem("Assets/Create/ScriptableObject")]
		static void CreateScriptableObject() {
			var selected = Selection.activeObject as MonoScript;
			if (selected == null || !AssetDatabase.Contains(selected)) {
				Debug.Log("Select ScriptableObject in Assets");
				return;
			}
			
			var type = selected.GetClass();
			if (!type.IsSubclassOf(typeof(ScriptableObject))) {
				Debug.Log("Not a Subclass of ScriptableObject");
				return;
			}
			
			ScriptableObjUtil.CreateAssetNonGeneric(type);
		}
	}
}