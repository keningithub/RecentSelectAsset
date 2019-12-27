
//////////////////////////////////////////////////////////////////////////
/// 列出最近选中的资源文件, 便于来回跳转
//////////////////////////////////////////////////////////////////////////



using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class RecentSelectAssetWindow: EditorWindow {
	
	private static bool m_isHooked = false;
	private static List<string> m_recentGuidList = new List<string>();
	private static Stack<string> m_backStack = new Stack<string>();
	private static Stack<string> m_forwardStack = new Stack<string>();
	private static bool m_isIgnore = false;
	private static string m_lastGuid = null;

	private static string m_savePath = "Assets/RecentSelectData.asset";

	private static int m_changedCount = 0;
	private static int m_maxRecordCount = 30;

	//
	[InitializeOnLoadMethod]
	static void Hook() {
		if (m_isHooked) return;
		m_isHooked = true;

		//EditorApplication.searchChanged += OnSearchChanged;
		Selection.selectionChanged += OnSelectionChanged;

		//
		LoadRecentData();
	}

	public static void OnSelectionChanged() {

		/*
		Debug.LogFormat("OnSelectionChanged: {0}", ++m_changedCount);

		if (null != Selection.activeObject) {
			Debug.LogFormat("OnSelectionChanged0: {0}", Selection.activeObject.ToString());
			Debug.LogFormat("OnSelectionChanged1: {0}", AssetDatabase.GetAssetPath(Selection.activeObject));
		}
		if (null != Selection.activeContext) {
			Debug.LogFormat("OnSelectionChanged2: {0}", Selection.activeContext.ToString());
		}
		if (null != Selection.assetGUIDs && Selection.assetGUIDs.Length > 0) {
			Debug.LogFormat("OnSelectionChanged3: {0}", Selection.assetGUIDs[0]);
		}
		*/

		do {
			var guidArr = Selection.assetGUIDs;
			if (0 == guidArr.Length) break;
			var guid = guidArr[0];

			// ignore folder
			var path = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(path) || path.IndexOf('.') <= 0) {
				break;
			}

			// ignore same last
			if (guid == m_lastGuid) break;
			m_lastGuid = guid;

			// ignore naviaget operation
			if (m_isIgnore) {
				m_isIgnore = false;
			}
			else {
				if (0 == m_backStack.Count || guid != m_backStack.Peek()) {
					m_backStack.Push(guid);
					m_forwardStack.Clear();
				}
			}

			// recent list
			for (int i = m_recentGuidList.Count - 1; i >= 0; --i) {
				if (guid == m_recentGuidList[i]) {
					m_recentGuidList.RemoveAt(i);
					break;
				}
			}
			m_recentGuidList.Add(guid);
			SaveRecentData();

		} while (false);


	}

	//
	private void OnEnable() {

	}

	//
	[MenuItem("Tools/RecentSelectAsset/ShowWindow %`")]
	private static void ShowWindow() {
		RemoveInvalidGuid();
		EditorWindow.GetWindow<RecentSelectAssetWindow>(true, "最近选中的资源文件", true);
	}

	//
	[MenuItem("Tools/RecentSelectAsset/Back %,")]
	private static void Back() {
		do {
			if (m_backStack.Count <= 1) break;

			string guid = m_backStack.Pop();
			m_forwardStack.Push(guid);

			guid = m_backStack.Peek();
			m_isIgnore = true;
			SelectAssetByGuid(guid);

		} while (false);
	}

	//
	[MenuItem("Tools/RecentSelectAsset/Forward %.")]
	private static void Forward() {
		do {
			if (m_forwardStack.Count == 0) break;

			string guid = m_forwardStack.Pop();
			m_backStack.Push(guid);

			m_isIgnore = true;
			SelectAssetByGuid(guid);

		} while (false);
	}

	//
	[MenuItem("Tools/RecentSelectAsset/Clear")]
	private static void Clear() {
		do {
			m_recentGuidList.Clear();
			m_backStack.Clear();
			m_forwardStack.Clear();
			m_lastGuid = null;

			SaveRecentData();

		} while (false);
	}

	//
	private static void SelectAssetByGuid(string guid) {
		do {
			if (string.IsNullOrEmpty(guid)) break;
			var path = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(path)) break;

			SelectAssetByPath(path);
		} while (false);
	}

	//
	private static void SelectAssetByPath(string path) {
		do {
			var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
			if (null == obj) break;

			//
			SetSearchString("");

			//
			Selection.SetActiveObjectWithContext(obj, null);			
		} while (false);
	}

	//
	void OnGUI() {
		for (int i = m_recentGuidList.Count - 1; i >= 0; --i) {
			var guid = m_recentGuidList[i];
			var path = AssetDatabase.GUIDToAssetPath(guid);

			//
			if (GUILayout.Button(path)) {
				SelectAssetByPath(path);
			}
		}
	}

	//
	private static void RemoveInvalidGuid() {
		// recent list
		for (int i = m_recentGuidList.Count - 1; i >= 0; --i) {
			var guid = m_recentGuidList[i];
			var path = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(path)) {
				m_recentGuidList.RemoveAt(i);
			}
		}
		// back
		{
			Stack<string> buff = new Stack<string>();
			while (m_backStack.Count > 0) {
				var guid = m_backStack.Pop();
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (!string.IsNullOrEmpty(path)) {
					buff.Push(guid);
				}
			}
			while (buff.Count > 0) m_backStack.Push(buff.Pop());
		}

		// forward
		{
			Stack<string> buff = new Stack<string>();
			while (m_forwardStack.Count > 0) {
				var guid = m_forwardStack.Pop();
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (!string.IsNullOrEmpty(path)) {
					buff.Push(guid);
				}
			}
			while (buff.Count > 0) m_forwardStack.Push(buff.Pop());
		}

	}

	//
	[Serializable]
	class RecentSelectData: ScriptableObject {		
		public string[] m_arr = null;
	}

	//
	private static void SaveRecentData() {
		do {
			/*
			var data = ScriptableObject.CreateInstance<RecentSelectAssetWindow.RecentSelectData>();
			data.m_arr = m_recentGuidList.ToArray();			
			AssetDatabase.CreateAsset(data, m_savePath);			
			*/

			string[] arr = null;

			if (m_recentGuidList.Count > m_maxRecordCount) {
				arr = new string[m_maxRecordCount];
				int start = m_recentGuidList.Count - m_maxRecordCount;
				for (int i = 0; i < m_maxRecordCount; ++i) {
					arr[i] = m_recentGuidList[i + start];
				}
			}
			else {
				arr = m_recentGuidList.ToArray();
			}

			var value = string.Join(",", arr);
			PlayerPrefs.SetString(m_savePath, value);


		} while (false);
	}

	//
	private static void LoadRecentData() {   
		do {
			string[] arr = null;

			/*
			var data = AssetDatabase.LoadAssetAtPath<RecentSelectAssetWindow.RecentSelectData>(m_savePath);
			if (null == data || null == data.m_arr) break;
			arr = data.m_arr;
			*/
			var value = PlayerPrefs.GetString(m_savePath);
			if (string.IsNullOrEmpty(value)) break;
			arr = value.Split(',');
			if (null == arr) break;

			foreach (var guid in arr) {
				m_recentGuidList.Add(guid);
			}

			RemoveInvalidGuid();			

			foreach (var guid in m_recentGuidList) {
				m_backStack.Push(guid);
			}


		} while (false);
	}

	//
	private static void SetSearchString(string searchString) {
		Type projectBrowserType = Type.GetType("UnityEditor.ProjectBrowser, UnityEditor");
		EditorWindow window = GetWindow(projectBrowserType);

		// UnityEditor.ProjectBrowser.SetSearch(string searchString)
		MethodInfo setSearchMethodInfo = projectBrowserType.GetMethod("SetSearch", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
			null, new Type[] { typeof(string) }, null);

		setSearchMethodInfo.Invoke(window, new object[] { searchString });
	}


}