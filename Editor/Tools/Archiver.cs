using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class Archiver : EditorWindow {
	
	/// <summary>
	/// メニュー
	/// </summary>
	[MenuItem("MMD for Unity/Tools/Archiver")]
	static void OnMenuClick() {
		Archiver window = EditorWindow.GetWindow<Archiver>(true, "Archiver");
		window.Show();
	}
	
	/// <summary>
	/// コンストラクタ
	/// </summary>
	Archiver() {
		c_on_gui_func_table_ = new System.Action[]{OnGUIforExtract, OnGUIforInsert};
	}
	
	/// <summary>
	/// GUI描画
	/// </summary>
	void OnGUI() {
		mode_ = (Mode)GUILayout.Toolbar((int)mode_, System.Enum.GetNames(typeof(Mode)));
		c_on_gui_func_table_[(int)mode_]();
	}
	
	/// <summary>
	/// 抽出の為のGUI描画
	/// </summary>
	private void OnGUIforExtract() {
		archive_asset_ = EditorGUILayout.ObjectField("ArchiveAsset", archive_asset_, typeof(Object), false);
		extract_asset_name_ = EditorGUILayout.TextField("ExtractName", extract_asset_name_);
		
		GUI.enabled = (null != archive_asset_) && (null != extract_asset_name_);
		if (GUILayout.Button("Extract")) {
			ExtractAsset();
		}
	}

	/// <summary>
	/// 摘出
	/// </summary>
	private void ExtractAsset() {
		Object archive_instance = Instantiate(archive_asset_);
		AssetDatabase.CreateAsset(archive_instance, extract_asset_name_);
	}
	
	/// <summary>
	/// 挿入の為のGUI描画
	/// </summary>
	private void OnGUIforInsert() {
		archive_asset_ = EditorGUILayout.ObjectField("ArchiveAsset", archive_asset_, typeof(Object), false);
		insert_asset_ = EditorGUILayout.ObjectField("InsertAsset", insert_asset_, typeof(Object), true);
		
		GUI.enabled = (null != archive_asset_) && (null != insert_asset_);
		if (GUILayout.Button("Insert")) {
			InsertAsset();
		}
	}
	
	/// <summary>
	/// 挿入
	/// </summary>
	private void InsertAsset() {
		string archive_asset_path = AssetDatabase.GetAssetPath(archive_asset_);
		Object insert_instance = Instantiate(insert_asset_);
		AssetDatabase.AddObjectToAsset(insert_instance, archive_asset_path);
		AssetDatabase.ImportAsset(archive_asset_path);
	}
	
	/// <summary>
	/// アーカイバモード
	/// </summary>
	private enum Mode {
		Extract,	//抽出
		Insert,		//挿入
	}
	
	private readonly System.Action[] c_on_gui_func_table_;

	private Mode	mode_				= Mode.Extract;
	private Object	archive_asset_		= null;
	private string	extract_asset_name_	= "Assets/extract.asset";
	private Object	insert_asset_		= null;
}
