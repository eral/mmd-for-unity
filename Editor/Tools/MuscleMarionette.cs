using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class MuscleMarionette : EditorWindow {
	
	/// <summary>
	/// メニュー
	/// </summary>
	[MenuItem("MMD for Unity/Tools/MuscleMarionette")]
	static void OnMenuClick() {
		MuscleMarionette window = EditorWindow.GetWindow<MuscleMarionette>("MuscleMarionette");
		window.Show();
	}
	
	/// <summary>
	/// スタティックコンストラクタ
	/// </summary>
	static MuscleMarionette() {
		group_tree_displays_ = true;
		limb_tree_displays_ = Enumerable.Repeat(false, System.Enum.GetValues(typeof(Limb)).Length).ToArray();
	}
	
	/// <summary>
	/// コンストラクタ
	/// </summary>
	MuscleMarionette() {
		ResetValue();
	}
	
	/// <summary>
	/// 値リセット
	/// </summary>
	void ResetValue() {
		group_value_ = Enumerable.Repeat(0.0f, System.Enum.GetValues(typeof(Group)).Length).ToArray();
		muscles_value_ = Enumerable.Repeat(0.0f, HumanTrait.MuscleCount).ToArray();
	}
	
	/// <summary>
	/// GUI描画
	/// </summary>
	void OnGUI() {
		bool is_dirty = false;
		
		is_dirty = OnGUIforAnimator() || is_dirty;
		Avatar avatar = ((null != animator_)? animator_.avatar: null);
		if ((null != animator_) && (null == avatar)) {
			//Animatorは設定されたがAvatarが取得出来無いなら
			is_dirty = OnGUIforNoSetAvatarErrorMessage() || is_dirty;
		}
		GUI.enabled = (null != avatar);
		is_dirty = OnGUIforGroup() || is_dirty;
		is_dirty = OnGUIforMuscles() || is_dirty;

		if (is_dirty && (null != animator_)) {
			//更新が有ったなら
			MMDEngine mmd_engine = animator_.GetComponent<MMDEngine>();
			if (null != mmd_engine) {
				//ボーン外のTransformが初期化されるので保存
				MmdBackupTransform[] mmd_backup_transforms = BackupMmdTransform();

				//ポーズ更新
				ApplyMusclesValue();

				//ボーン外のTransformが復元
				RollbackMmdTransform(mmd_backup_transforms);

				//MMDEngine更新
				mmd_engine.LateUpdate();
			} else {
				//ポーズ更新
				ApplyMusclesValue();
			}
			
			//Window更新
			EditorUtility.SetDirty(this);
		}
	}
	
	/// <summary>
	/// Animatorの為のGUI描画
	/// </summary>
	/// <returns>更新が有ったか(true:更新有り, false:未更新)</returns>
	private bool OnGUIforAnimator() {
		bool is_update = false;
		Animator new_animator_ = (Animator)EditorGUILayout.ObjectField("Animator", animator_, typeof(Animator), true);
		if (animator_ != new_animator_) {
			//Animatorが更新されたなら
			animator_ = new_animator_;
			if (null != animator_) {
				//Animatorが有効なら
				//MMDEngineの有効化
				StartMmdEngine();
				//ポーズ取得
				//empty
			}
			//値リセット
			ResetValue();

			is_update = true;
		}
		return is_update;
	}
	
	/// <summary>
	/// グループの為のGUI描画
	/// </summary>
	/// <returns>更新が有ったか(true:更新有り, false:未更新)</returns>
	private bool OnGUIforGroup() {
		bool is_update = false;
		
		//ツリータイトル
		string name = "Group";
		group_tree_displays_ = EditorGUILayout.Foldout(group_tree_displays_, name);
		//ツリー内部
		if (group_tree_displays_) {
			//このツリーを表示するなら
			++EditorGUI.indentLevel;
			EditorGUILayout.BeginVertical();
			{
				for (int i = 0, i_max = System.Enum.GetValues(typeof(Group)).Length; i < i_max; ++i) {
					Group group_index = (Group)i;
					is_update = OnGUIforGroupInner(group_index.ToString()
										, group_index
										, c_muscles_list_in_group[(int)group_index]
										) || is_update;
				}
			}
			EditorGUILayout.EndVertical();
			--EditorGUI.indentLevel;
		}
		
		return is_update;
	}
	
	/// <summary>
	/// グループ内の為のGUI描画
	/// </summary>
	/// <returns>更新が有ったか(true:更新有り, false:未更新)</returns>
	private bool OnGUIforGroupInner(string name, Group group_index, HumanBodyMuscles[] muscles) {
		bool is_update = false;
		
		float value = group_value_[(int)group_index];
		value = EditorGUILayout.Slider(name, value, -1.0f, 1.0f);
		if (group_value_[(int)group_index] != value) {
			//変更が掛かったなら
			//Undo登録
#if !UNITY_4_2 //4.3以降
			Undo.RecordObject(this, "Change Group Value");
#else
			Undo.RegisterUndo(this, "Change Group Value");
#endif
			//更新
			switch (group_index) {
			case Group.All: //ALLなら全グループ更新
				group_value_ = Enumerable.Repeat(value, group_value_.Length).ToArray();
				break;
			default: //ALL以外なら個々の更新
				group_value_[(int)group_index] = value;
				break;
			}
			//Muscles走査
			foreach (var muscle in muscles) {
				muscles_value_[(int)muscle] = value;
			}
			
			is_update = true;
		}
		
		return is_update;
	}

	/// <summary>
	/// Muscleの為のGUI描画
	/// </summary>
	/// <returns>更新が有ったか(true:更新有り, false:未更新)</returns>
	private bool OnGUIforMuscles() {
		bool is_update = false;
		
		for (int i = 0, i_max = System.Enum.GetValues(typeof(Limb)).Length; i < i_max; ++i) {
			Limb limb_index = (Limb)i;
			is_update = OnGUIforMusclesLimb(limb_index.ToString()
								, limb_index
								, c_muscles_list_in_limb[(int)limb_index]
								) || is_update;
		}
		
		return is_update;
	}
	
	/// <summary>
	/// 四肢Musclesの為のGUI描画
	/// </summary>
	/// <returns>更新が有ったか(true:更新有り, false:未更新)</returns>
	private bool OnGUIforMusclesLimb(string name, Limb tree_displays_index, HumanBodyMuscles[] muscles) {
		bool is_update = false;
		
		//ツリータイトル
		limb_tree_displays_[(int)tree_displays_index] = EditorGUILayout.Foldout(limb_tree_displays_[(int)tree_displays_index], name);
		//ツリー内部
		if (limb_tree_displays_[(int)tree_displays_index]) {
			//このツリーを表示するなら
			++EditorGUI.indentLevel;
			EditorGUILayout.BeginVertical();
			{
				//Muscles走査
				foreach (var muscle in muscles) {
					string muscle_name = HumanTrait.MuscleName[(int)muscle];
					
					float value = muscles_value_[(int)muscle];
					value = EditorGUILayout.Slider(muscle_name, value, -1.0f, 1.0f);
					
					if (muscles_value_[(int)muscle] != value) {
						//変更が掛かったなら
						//Undo登録
#if !UNITY_4_2 //4.3以降
						Undo.RecordObject(this, "Change Muscle Value");
#else
						Undo.RegisterUndo(this, "Change Muscle Value");
#endif
						//更新
						muscles_value_[(int)muscle] = value;
						
						is_update = true;
					}
				}
			}
			EditorGUILayout.EndVertical();
			--EditorGUI.indentLevel;
		}
		
		return is_update;
	}
	

	/// <summary>
	/// Avatar未設定エラーの為のGUI描画
	/// </summary>
	/// <returns>更新が有ったか(true:更新有り, false:未更新)</returns>
	private bool OnGUIforNoSetAvatarErrorMessage() {
		bool is_update = false;
		
		EditorGUILayout.LabelField("no set avatar in animator.");
		
		return is_update;
	}
	

	/// <summary>
	/// Muscles値の反映
	/// </summary>
	private void ApplyMusclesValue() {
		if ((null != animator_) && (null != animator_.avatar)) {
			Types.GetType("UnityEditor.AvatarUtility", "UnityEditor.dll")
				.InvokeMember("SetHumanPose"
							, BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod
							, null
							, null
							, new object[]{animator_, muscles_value_}
							);
		}
	}
	
	/// <summary>
	/// MMDモデル保存用トランスフォーム
	/// </summary>
	private struct MmdBackupTransform {
		public Vector3		position;
		public Quaternion	rotation;
		public MmdBackupTransform(Vector3 p, Quaternion r) {
			position = p;
			rotation = r;
		}
	};

	/// <summary>
	/// MMDモデルのトランスフォームを保存
	/// </summary>
	/// <value>MMDモデルの保存用トランスフォーム</value>
	private MmdBackupTransform[] BackupMmdTransform() {
		MmdBackupTransform[] result = null;
		MMDEngine mmd_engine = animator_.GetComponent<MMDEngine>();
		if (null != mmd_engine) {
			//ボーン外のTransformが初期化されるので保存
			var human_bones_transform = Enumerable.Range(0, ((int)HumanBodyBones.LastBone) - 1)					//Mecanimボーン数カウント
													.Select(x=>animator_.GetBoneTransform((HumanBodyBones)x));	//MecanimボーンのTransformを列挙
			result = mmd_engine.bone_controllers.Select(x=>x.transform)													//モデルのTransformを列挙
												.Except(human_bones_transform)											//MecanimボーンのTransformを除外
												.Select(x=>new MmdBackupTransform(x.localPosition, x.localRotation))	//トランスフォームの保存
												.ToArray();
		}
		return result;
	}
	
	/// <summary>
	/// MMDモデルのトランスフォームを復元
	/// </summary>
	/// <param name='transforms'>MMDモデルの保存用トランスフォーム</param>
	private void RollbackMmdTransform(MmdBackupTransform[] transforms) {
		MMDEngine mmd_engine = animator_.GetComponent<MMDEngine>();
		if (null != mmd_engine) {
			//ボーン外のTransformが復元
			var human_bones_transform = Enumerable.Range(0, ((int)HumanBodyBones.LastBone) - 1)					//Mecanimボーン数カウント
													.Select(x=>animator_.GetBoneTransform((HumanBodyBones)x));	//MecanimボーンのTransformを列挙
			var new_gameobject = mmd_engine.bone_controllers.Select(x=>x.transform)													//モデルのTransformを列挙
															.Except(human_bones_transform)											//MecanimボーンのTransformを除外
															.Select(x=>x.gameObject)												//GameObjectの取得
															.ToArray();
			foreach(var zip in Enumerable.Range(0, new_gameobject.Length)
										.Select(x=>new {gameobject = new_gameobject[x], transform = transforms[x]})) {
				zip.gameobject.transform.localPosition = zip.transform.position;
				zip.gameobject.transform.localRotation = zip.transform.rotation;
			}
		}
	}
	
	/// <summary>
	/// MMDEngineの有効化
	/// </summary>
	private void StartMmdEngine() {
		MMDEngine mmd_engine = animator_.GetComponent<MMDEngine>();
		if (null != mmd_engine) {
			//全てのボーンコントローラーのStart()を呼ぶ
			foreach (var bone_controller in mmd_engine.bone_controllers) {
				bone_controller.Start();
			}
			//付与親等を更新
			mmd_engine.LateUpdate();
		}
	}

	/// <summary>
	/// グループ分け
	/// </summary>
	enum Group {
		All,
		OpenClose,
		LeftRight,
		RollLeftRigjt,
		InOut,
		RollInOut,
		FingerOpenClose,
		FingerInOut,
	}
	
	/// <summary>
	/// 四肢分け
	/// </summary>
	enum Limb {
		Body,
		Head,
		LeftArm,
		LeftFingers,
		RightArm,
		RightFingers,
		LeftLeg,
		RightLeg,
	}
	
	/// <summary>
	/// Musclesインデックス
	/// </summary>
	enum HumanBodyMuscles {
		//●Body
		SpineFrontBack,
		SpineLeftRight,
		SpineTwistLeftRight,
		ChestFrontBack,
		ChestLeftRight,
		ChestTwistLeftRight,
		
		//●Head
		NeckNodDownUp,
		NeckTiltLeftRight,
		NeckTurnLeftRight,
		HeadNodDownUp,
		HeadTiltLeftRight,
		HeadTurnLeftRight,
		LeftEyeDownUp,
		LeftEyeInOut,
		RightEyeDownUp,
		RightEyeInOut,
		JawClose,
		JawLeftRight,
		
		//●LeftLeg
		LeftUpperLegFrontBack,
		LeftUpperLegInOut,
		LeftUpperLegTwistInOut,
		LeftLowerLegStretch,
		LeftLowerLegTwistInOut,
		LeftFootUpDown,
		LeftFootTwistInOut,
		LeftToesUpDown,
		
		//●RightLeg
		RightUpperLegFrontBack,
		RightUpperLegInOut,
		RightUpperLegTwistInOut,
		RightLowerLegStretch,
		RightLowerLegTwistInOut,
		RightFootUpDown,
		RightFootTwistInOut,
		RightToesUpDown,
		
		//●LeftArm
		LeftShoulderDownUp,
		LeftShoulderFrontBack,
		LeftArmDownUp,
		LeftArmFrontBack,
		LeftArmTwistInOut,
		LeftForearmStretch,
		LeftForearmTwistInOut,
		LeftHandDownUp,
		LeftHandInOut,
		
		//●RightArm
		RightShoulderDownUp,
		RightShoulderFrontBack,
		RightArmDownUp,
		RightArmFrontBack,
		RightArmTwistInOut,
		RightForearmStretch,
		RightForearmTwistInOut,
		RightHandDownUp,
		RightHandInOut,
		
		//●LeftFingers
		LeftHandThumb1Stretched,
		LeftHandThumbSpread,
		LeftHandThumb2Stretched,
		LeftHandThumb3Stretched,
		LeftHandIndex1Stretched,
		LeftHandIndexSpread,
		LeftHandIndex2Stretched,
		LeftHandIndex3Stretched,
		LeftHandMiddle1Stretched,
		LeftHandMiddleSpread,
		LeftHandMiddle2Stretched,
		LeftHandMiddle3Stretched,
		LeftHandRing1Stretched,
		LeftHandRingSpread,
		LeftHandRing2Stretched,
		LeftHandRing3Stretched,
		LeftHandLittle1Stretched,
		LeftHandLittleSpread,
		LeftHandLittle2Stretched,
		LeftHandLittle3Stretched,
		
		//●RightFingers
		RightHandThumb1Stretched,
		RightHandThumbSpread,
		RightHandThumb2Stretched,
		RightHandThumb3Stretched,
		RightHandIndex1Stretched,
		RightHandIndexSpread,
		RightHandIndex2Stretched,
		RightHandIndex3Stretched,
		RightHandMiddle1Stretched,
		RightHandMiddleSpread,
		RightHandMiddle2Stretched,
		RightHandMiddle3Stretched,
		RightHandRing1Stretched,
		RightHandRingSpread,
		RightHandRing2Stretched,
		RightHandRing3Stretched,
		RightHandLittle1Stretched,
		RightHandLittleSpread,
		RightHandLittle2Stretched,
		RightHandLittle3Stretched,
	}

	private Animator animator_ = null;
	private float[] group_value_ = null;
	private float[] muscles_value_ = null;

	private static	bool	group_tree_displays_;	//グループツリー表示
	private static	bool[]	limb_tree_displays_;	//四肢ツリー表示

	private static readonly HumanBodyMuscles[][] c_muscles_list_in_group = new [] {
		new []{	//Group.All
			HumanBodyMuscles.SpineFrontBack,
			HumanBodyMuscles.SpineLeftRight,
			HumanBodyMuscles.SpineTwistLeftRight,
			HumanBodyMuscles.ChestFrontBack,
			HumanBodyMuscles.ChestLeftRight,
			HumanBodyMuscles.ChestTwistLeftRight,
			HumanBodyMuscles.NeckNodDownUp,
			HumanBodyMuscles.NeckTiltLeftRight,
			HumanBodyMuscles.NeckTurnLeftRight,
			HumanBodyMuscles.HeadNodDownUp,
			HumanBodyMuscles.HeadTiltLeftRight,
			HumanBodyMuscles.HeadTurnLeftRight,
			HumanBodyMuscles.LeftEyeDownUp,
			HumanBodyMuscles.LeftEyeInOut,
			HumanBodyMuscles.RightEyeDownUp,
			HumanBodyMuscles.RightEyeInOut,
			HumanBodyMuscles.JawClose,
			HumanBodyMuscles.JawLeftRight,
			HumanBodyMuscles.LeftShoulderDownUp,
			HumanBodyMuscles.LeftShoulderFrontBack,
			HumanBodyMuscles.LeftArmDownUp,
			HumanBodyMuscles.LeftArmFrontBack,
			HumanBodyMuscles.LeftArmTwistInOut,
			HumanBodyMuscles.LeftForearmStretch,
			HumanBodyMuscles.LeftForearmTwistInOut,
			HumanBodyMuscles.LeftHandDownUp,
			HumanBodyMuscles.LeftHandInOut,
			HumanBodyMuscles.LeftHandThumb1Stretched,
			HumanBodyMuscles.LeftHandThumbSpread,
			HumanBodyMuscles.LeftHandThumb2Stretched,
			HumanBodyMuscles.LeftHandThumb3Stretched,
			HumanBodyMuscles.LeftHandIndex1Stretched,
			HumanBodyMuscles.LeftHandIndexSpread,
			HumanBodyMuscles.LeftHandIndex2Stretched,
			HumanBodyMuscles.LeftHandIndex3Stretched,
			HumanBodyMuscles.LeftHandMiddle1Stretched,
			HumanBodyMuscles.LeftHandMiddleSpread,
			HumanBodyMuscles.LeftHandMiddle2Stretched,
			HumanBodyMuscles.LeftHandMiddle3Stretched,
			HumanBodyMuscles.LeftHandRing1Stretched,
			HumanBodyMuscles.LeftHandRingSpread,
			HumanBodyMuscles.LeftHandRing2Stretched,
			HumanBodyMuscles.LeftHandRing3Stretched,
			HumanBodyMuscles.LeftHandLittle1Stretched,
			HumanBodyMuscles.LeftHandLittleSpread,
			HumanBodyMuscles.LeftHandLittle2Stretched,
			HumanBodyMuscles.LeftHandLittle3Stretched,
			HumanBodyMuscles.RightShoulderDownUp,
			HumanBodyMuscles.RightShoulderFrontBack,
			HumanBodyMuscles.RightArmDownUp,
			HumanBodyMuscles.RightArmFrontBack,
			HumanBodyMuscles.RightArmTwistInOut,
			HumanBodyMuscles.RightForearmStretch,
			HumanBodyMuscles.RightForearmTwistInOut,
			HumanBodyMuscles.RightHandDownUp,
			HumanBodyMuscles.RightHandInOut,
			HumanBodyMuscles.RightHandThumb1Stretched,
			HumanBodyMuscles.RightHandThumbSpread,
			HumanBodyMuscles.RightHandThumb2Stretched,
			HumanBodyMuscles.RightHandThumb3Stretched,
			HumanBodyMuscles.RightHandIndex1Stretched,
			HumanBodyMuscles.RightHandIndexSpread,
			HumanBodyMuscles.RightHandIndex2Stretched,
			HumanBodyMuscles.RightHandIndex3Stretched,
			HumanBodyMuscles.RightHandMiddle1Stretched,
			HumanBodyMuscles.RightHandMiddleSpread,
			HumanBodyMuscles.RightHandMiddle2Stretched,
			HumanBodyMuscles.RightHandMiddle3Stretched,
			HumanBodyMuscles.RightHandRing1Stretched,
			HumanBodyMuscles.RightHandRingSpread,
			HumanBodyMuscles.RightHandRing2Stretched,
			HumanBodyMuscles.RightHandRing3Stretched,
			HumanBodyMuscles.RightHandLittle1Stretched,
			HumanBodyMuscles.RightHandLittleSpread,
			HumanBodyMuscles.RightHandLittle2Stretched,
			HumanBodyMuscles.RightHandLittle3Stretched,
			HumanBodyMuscles.LeftUpperLegFrontBack,
			HumanBodyMuscles.LeftUpperLegInOut,
			HumanBodyMuscles.LeftUpperLegTwistInOut,
			HumanBodyMuscles.LeftLowerLegStretch,
			HumanBodyMuscles.LeftLowerLegTwistInOut,
			HumanBodyMuscles.LeftFootUpDown,
			HumanBodyMuscles.LeftFootTwistInOut,
			HumanBodyMuscles.LeftToesUpDown,
			HumanBodyMuscles.RightUpperLegFrontBack,
			HumanBodyMuscles.RightUpperLegInOut,
			HumanBodyMuscles.RightUpperLegTwistInOut,
			HumanBodyMuscles.RightLowerLegStretch,
			HumanBodyMuscles.RightLowerLegTwistInOut,
			HumanBodyMuscles.RightFootUpDown,
			HumanBodyMuscles.RightFootTwistInOut,
			HumanBodyMuscles.RightToesUpDown,
		},	
		new []{	//Group.OpenClose
			HumanBodyMuscles.SpineFrontBack,
			HumanBodyMuscles.ChestFrontBack,
			HumanBodyMuscles.NeckNodDownUp,
			HumanBodyMuscles.HeadNodDownUp,
			HumanBodyMuscles.LeftShoulderDownUp,
			HumanBodyMuscles.LeftArmDownUp,
			HumanBodyMuscles.LeftForearmStretch,
			HumanBodyMuscles.LeftHandDownUp,
			HumanBodyMuscles.RightShoulderDownUp,
			HumanBodyMuscles.RightArmDownUp,
			HumanBodyMuscles.RightForearmStretch,
			HumanBodyMuscles.RightHandDownUp,
			HumanBodyMuscles.LeftUpperLegFrontBack,
			HumanBodyMuscles.LeftLowerLegStretch,
			HumanBodyMuscles.LeftFootUpDown,
			HumanBodyMuscles.RightUpperLegFrontBack,
			HumanBodyMuscles.RightLowerLegStretch,
			HumanBodyMuscles.RightFootUpDown,
		},	
		new []{	//Group.LeftRight
			HumanBodyMuscles.SpineLeftRight,
			HumanBodyMuscles.ChestLeftRight,
			HumanBodyMuscles.NeckTiltLeftRight,
			HumanBodyMuscles.HeadTiltLeftRight,
		},	
		new []{	//Group.RollLeftRigjt
			HumanBodyMuscles.SpineTwistLeftRight,
			HumanBodyMuscles.ChestTwistLeftRight,
			HumanBodyMuscles.NeckTurnLeftRight,
			HumanBodyMuscles.HeadTurnLeftRight,
		},	
		new []{	//Group.InOut
			HumanBodyMuscles.LeftShoulderFrontBack,
			HumanBodyMuscles.LeftArmFrontBack,
			HumanBodyMuscles.LeftHandInOut,
			HumanBodyMuscles.RightShoulderFrontBack,
			HumanBodyMuscles.RightArmFrontBack,
			HumanBodyMuscles.RightHandInOut,
			HumanBodyMuscles.LeftUpperLegInOut,
			HumanBodyMuscles.LeftFootTwistInOut,
			HumanBodyMuscles.RightUpperLegInOut,
			HumanBodyMuscles.RightFootTwistInOut,
		},	
		new []{	//Group.RollInOut
			HumanBodyMuscles.LeftArmTwistInOut,
			HumanBodyMuscles.LeftForearmTwistInOut,
			HumanBodyMuscles.RightArmTwistInOut,
			HumanBodyMuscles.RightForearmTwistInOut,
			HumanBodyMuscles.LeftUpperLegTwistInOut,
			HumanBodyMuscles.LeftLowerLegTwistInOut,
			HumanBodyMuscles.RightUpperLegTwistInOut,
			HumanBodyMuscles.RightLowerLegTwistInOut,
		},	
		new []{	//Group.FingerOpenClose
			HumanBodyMuscles.LeftHandThumb1Stretched,
			HumanBodyMuscles.LeftHandThumb2Stretched,
			HumanBodyMuscles.LeftHandThumb3Stretched,
			HumanBodyMuscles.LeftHandIndex1Stretched,
			HumanBodyMuscles.LeftHandIndex2Stretched,
			HumanBodyMuscles.LeftHandIndex3Stretched,
			HumanBodyMuscles.LeftHandMiddle1Stretched,
			HumanBodyMuscles.LeftHandMiddle2Stretched,
			HumanBodyMuscles.LeftHandMiddle3Stretched,
			HumanBodyMuscles.LeftHandRing1Stretched,
			HumanBodyMuscles.LeftHandRing2Stretched,
			HumanBodyMuscles.LeftHandRing3Stretched,
			HumanBodyMuscles.LeftHandLittle1Stretched,
			HumanBodyMuscles.LeftHandLittle2Stretched,
			HumanBodyMuscles.LeftHandLittle3Stretched,
			HumanBodyMuscles.RightHandThumb1Stretched,
			HumanBodyMuscles.RightHandThumb2Stretched,
			HumanBodyMuscles.RightHandThumb3Stretched,
			HumanBodyMuscles.RightHandIndex1Stretched,
			HumanBodyMuscles.RightHandIndex2Stretched,
			HumanBodyMuscles.RightHandIndex3Stretched,
			HumanBodyMuscles.RightHandMiddle1Stretched,
			HumanBodyMuscles.RightHandMiddle2Stretched,
			HumanBodyMuscles.RightHandMiddle3Stretched,
			HumanBodyMuscles.RightHandRing1Stretched,
			HumanBodyMuscles.RightHandRing2Stretched,
			HumanBodyMuscles.RightHandRing3Stretched,
			HumanBodyMuscles.RightHandLittle1Stretched,
			HumanBodyMuscles.RightHandLittle2Stretched,
			HumanBodyMuscles.RightHandLittle3Stretched,
		},	
		new []{	//Group.FingerInOut
			HumanBodyMuscles.LeftHandThumbSpread,
			HumanBodyMuscles.LeftHandIndexSpread,
			HumanBodyMuscles.LeftHandMiddleSpread,
			HumanBodyMuscles.LeftHandRingSpread,
			HumanBodyMuscles.LeftHandLittleSpread,
			HumanBodyMuscles.RightHandThumbSpread,
			HumanBodyMuscles.RightHandIndexSpread,
			HumanBodyMuscles.RightHandMiddleSpread,
			HumanBodyMuscles.RightHandRingSpread,
			HumanBodyMuscles.RightHandLittleSpread,
		},	
	};
	private static readonly HumanBodyMuscles[][] c_muscles_list_in_limb = new [] {
		new []{	//Limb.Body
			HumanBodyMuscles.SpineFrontBack,
			HumanBodyMuscles.SpineLeftRight,
			HumanBodyMuscles.SpineTwistLeftRight,
			HumanBodyMuscles.ChestFrontBack,
			HumanBodyMuscles.ChestLeftRight,
			HumanBodyMuscles.ChestTwistLeftRight,
		},	
		new []{	//Limb.Head
			HumanBodyMuscles.NeckNodDownUp,
			HumanBodyMuscles.NeckTiltLeftRight,
			HumanBodyMuscles.NeckTurnLeftRight,
			HumanBodyMuscles.HeadNodDownUp,
			HumanBodyMuscles.HeadTiltLeftRight,
			HumanBodyMuscles.HeadTurnLeftRight,
			HumanBodyMuscles.LeftEyeDownUp,
			HumanBodyMuscles.LeftEyeInOut,
			HumanBodyMuscles.RightEyeDownUp,
			HumanBodyMuscles.RightEyeInOut,
			HumanBodyMuscles.JawClose,
			HumanBodyMuscles.JawLeftRight,
		},
		new []{	//Limb.LeftArm
			HumanBodyMuscles.LeftShoulderDownUp,
			HumanBodyMuscles.LeftShoulderFrontBack,
			HumanBodyMuscles.LeftArmDownUp,
			HumanBodyMuscles.LeftArmFrontBack,
			HumanBodyMuscles.LeftArmTwistInOut,
			HumanBodyMuscles.LeftForearmStretch,
			HumanBodyMuscles.LeftForearmTwistInOut,
			HumanBodyMuscles.LeftHandDownUp,
			HumanBodyMuscles.LeftHandInOut,
		},
		new []{	//Limb.LeftFingers
			HumanBodyMuscles.LeftHandThumb1Stretched,
			HumanBodyMuscles.LeftHandThumbSpread,
			HumanBodyMuscles.LeftHandThumb2Stretched,
			HumanBodyMuscles.LeftHandThumb3Stretched,
			HumanBodyMuscles.LeftHandIndex1Stretched,
			HumanBodyMuscles.LeftHandIndexSpread,
			HumanBodyMuscles.LeftHandIndex2Stretched,
			HumanBodyMuscles.LeftHandIndex3Stretched,
			HumanBodyMuscles.LeftHandMiddle1Stretched,
			HumanBodyMuscles.LeftHandMiddleSpread,
			HumanBodyMuscles.LeftHandMiddle2Stretched,
			HumanBodyMuscles.LeftHandMiddle3Stretched,
			HumanBodyMuscles.LeftHandRing1Stretched,
			HumanBodyMuscles.LeftHandRingSpread,
			HumanBodyMuscles.LeftHandRing2Stretched,
			HumanBodyMuscles.LeftHandRing3Stretched,
			HumanBodyMuscles.LeftHandLittle1Stretched,
			HumanBodyMuscles.LeftHandLittleSpread,
			HumanBodyMuscles.LeftHandLittle2Stretched,
			HumanBodyMuscles.LeftHandLittle3Stretched,
		},
		new []{	//Limb.RightArm
			HumanBodyMuscles.RightShoulderDownUp,
			HumanBodyMuscles.RightShoulderFrontBack,
			HumanBodyMuscles.RightArmDownUp,
			HumanBodyMuscles.RightArmFrontBack,
			HumanBodyMuscles.RightArmTwistInOut,
			HumanBodyMuscles.RightForearmStretch,
			HumanBodyMuscles.RightForearmTwistInOut,
			HumanBodyMuscles.RightHandDownUp,
			HumanBodyMuscles.RightHandInOut,
		},
		new []{	//Limb.RightFingers
			HumanBodyMuscles.RightHandThumb1Stretched,
			HumanBodyMuscles.RightHandThumbSpread,
			HumanBodyMuscles.RightHandThumb2Stretched,
			HumanBodyMuscles.RightHandThumb3Stretched,
			HumanBodyMuscles.RightHandIndex1Stretched,
			HumanBodyMuscles.RightHandIndexSpread,
			HumanBodyMuscles.RightHandIndex2Stretched,
			HumanBodyMuscles.RightHandIndex3Stretched,
			HumanBodyMuscles.RightHandMiddle1Stretched,
			HumanBodyMuscles.RightHandMiddleSpread,
			HumanBodyMuscles.RightHandMiddle2Stretched,
			HumanBodyMuscles.RightHandMiddle3Stretched,
			HumanBodyMuscles.RightHandRing1Stretched,
			HumanBodyMuscles.RightHandRingSpread,
			HumanBodyMuscles.RightHandRing2Stretched,
			HumanBodyMuscles.RightHandRing3Stretched,
			HumanBodyMuscles.RightHandLittle1Stretched,
			HumanBodyMuscles.RightHandLittleSpread,
			HumanBodyMuscles.RightHandLittle2Stretched,
			HumanBodyMuscles.RightHandLittle3Stretched,
		},
		new []{	//Limb.LeftLeg
			HumanBodyMuscles.LeftUpperLegFrontBack,
			HumanBodyMuscles.LeftUpperLegInOut,
			HumanBodyMuscles.LeftUpperLegTwistInOut,
			HumanBodyMuscles.LeftLowerLegStretch,
			HumanBodyMuscles.LeftLowerLegTwistInOut,
			HumanBodyMuscles.LeftFootUpDown,
			HumanBodyMuscles.LeftFootTwistInOut,
			HumanBodyMuscles.LeftToesUpDown,
		},
		new []{	//Limb.RightLeg
			HumanBodyMuscles.RightUpperLegFrontBack,
			HumanBodyMuscles.RightUpperLegInOut,
			HumanBodyMuscles.RightUpperLegTwistInOut,
			HumanBodyMuscles.RightLowerLegStretch,
			HumanBodyMuscles.RightLowerLegTwistInOut,
			HumanBodyMuscles.RightFootUpDown,
			HumanBodyMuscles.RightFootTwistInOut,
			HumanBodyMuscles.RightToesUpDown,
		},
	};
}
