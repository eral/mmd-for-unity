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
		AvatarUtility_SetHumanPose_ = Types.GetType("UnityEditor.AvatarUtility", "UnityEditor.dll")
											.GetMethod("SetHumanPose", BindingFlags.Public | BindingFlags.Static);
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
		if (null == animator_) {
			//Animator未設定なら
			//empty.
		} else if (null == avatar) {
			//Animatorは設定されたがAvatarが取得出来無いなら
			is_dirty = OnGUIforNoSetAvatarErrorMessage() || is_dirty;
			muscles_minmax_ = null;
		} else if (null == animator_.runtimeAnimatorController) {
			//Animatorは設定されたがAnimatorControllerが取得出来無いなら
			is_dirty = OnGUIforNoSetControllerErrorMessage() || is_dirty;
			muscles_minmax_ = null;
		} else if (is_dirty) {
			//動作可能の最初のフレームなら
			muscles_minmax_ = GetLimitMinMaxFromAvatar(avatar);
			PoseToValue();
		}
		GUI.enabled = (null != avatar);
		is_dirty = OnGUIforGroup() || is_dirty;
		is_dirty = OnGUIforMuscles() || is_dirty;

		if (is_dirty && (null != animator_)) {
			//更新が有ったなら
			MMDEngine mmd_engine = animator_.GetComponent<MMDEngine>();
			if (null != mmd_engine) {
#if UNITY_4_2 //4.2以前
				//ボーン外のTransformが初期化されるので保存
				MmdBackupTransform[] mmd_backup_transforms = BackupMmdTransform();
#endif

				//ポーズ更新
				ApplyMusclesValue();

#if UNITY_4_2 //4.2以前
				//ボーン外のTransformが復元
				RollbackMmdTransform(mmd_backup_transforms);
#endif

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
	/// AnimatorController未設定エラーの為のGUI描画
	/// </summary>
	/// <returns>更新が有ったか(true:更新有り, false:未更新)</returns>
	private bool OnGUIforNoSetControllerErrorMessage() {
		bool is_update = false;
		
		EditorGUILayout.LabelField("no set animator controller in animator.");
		
		return is_update;
	}
	
	/// <summary>
	/// Muscles値の反映
	/// </summary>
	private void ApplyMusclesValue() {
		if ((null != animator_) && (null != animator_.avatar)) {
			AvatarUtility_SetHumanPose_.Invoke(null, new object[]{animator_, muscles_value_});
			SceneView.RepaintAll();
		}
	}
	
	/// <summary>
	/// ポーズから値を設定
	/// </summary>
	void PoseToValue() {
		ResetValue();
	}
	
#if UNITY_4_2 //4.2以前
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
#endif
	
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
	/// アバターからMuscleのMin,Max値を取得する
	/// </summary>
	/// <returns>Min,Max値のクォータニオン配列(Quaternion[Muscleインデックス][min=0,max=1])</returns>
	/// <param name="avatar">アバター</param>
	private static Quaternion[][] GetLimitMinMaxFromAvatar(Avatar avatar) {
		SerializedObject so_avatar = new SerializedObject(avatar);
		SerializedProperty sp_axes_array = so_avatar.FindProperty("m_Avatar.m_Human.data.m_Skeleton.data.m_AxesArray");

		Quaternion[][] result = new Quaternion[sp_axes_array.arraySize][];
		for (int i = 0, i_max = result.Length; i < i_max; ++i) {
			var float_limit = new float[2][];
			var sp_limit = sp_axes_array.GetArrayElementAtIndex(i).FindPropertyRelative("m_Limit");
			{
				var sp_unit = sp_limit.FindPropertyRelative("m_Min");
				sp_unit.Next(true);
				float_limit[0] = new float[4];
				for (int k = 0, k_max = float_limit[0].Length; k < k_max; ++k) {
					float_limit[0][k] = sp_unit.floatValue;
					sp_unit.Next(false);
				}
				//sp_unit.Dispose();
			}
			{
				var sp_unit = sp_limit.FindPropertyRelative("m_Max");
				sp_unit.Next(true);
				float_limit[1] = new float[4];
				for (int k = 0, k_max = float_limit[1].Length; k < k_max; ++k) {
					float_limit[1][k] = sp_unit.floatValue;
					sp_unit.Next(false);
				}
				//sp_unit.Dispose();
			}
			//sp_limit.Dispose();
			result[i] = float_limit.Select(x=>new Quaternion(x[0], x[1], x[2], x[3])).ToArray();
		}
		//sp_axes_array.Dispose();
		//so_avatar.Dispose();

		return result;
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
	private Quaternion[][] muscles_minmax_ = null;
	
	private static	bool		group_tree_displays_;	//グループツリー表示
	private static	bool[]		limb_tree_displays_;	//四肢ツリー表示
	private static	MethodInfo	AvatarUtility_SetHumanPose_;	//UnityEditor.dll/UnityEditor.AvatarUtility/SetHumanPoseのリフレクションキャッシュ

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

	private static readonly string[] c_muscles_anim_attribute = new [] {
		"Spine Front-Back",
		"Spine Left-Right",
		"Spine Twist Left-Right",
		"Chest Front-Back",
		"Chest Left-Right",
		"Chest Twist Left-Right",
		"Neck Nod Down-Up",
		"Neck Tilt Left-Right",
		"Neck Turn Left-Right",
		"Head Nod Down-Up",
		"Head Tilt Left-Right",
		"Head Turn Left-Right",
		"Left Eye Down-Up",
		"Left Eye In-Out",
		"Right Eye Down-Up",
		"Right Eye In-Out",
		"Jaw Close",
		"Jaw Left-Right",
		"Left Upper Leg Front-Back",
		"Left Upper Leg In-Out",
		"Left Upper Leg Twist In-Out",
		"Left Lower Leg Stretch",
		"Left Lower Leg Twist In-Out",
		"Left Foot Up-Down",
		"Left Foot Twist In-Out",
		"Left Toes Up-Down",
		"Right Upper Leg Front-Back",
		"Right Upper Leg In-Out",
		"Right Upper Leg Twist In-Out",
		"Right Lower Leg Stretch",
		"Right Lower Leg Twist In-Out",
		"Right Foot Up-Down",
		"Right Foot Twist In-Out",
		"Right Toes Up-Down",
		"Left Shoulder Down-Up",
		"Left Shoulder Front-Back",
		"Left Arm Down-Up",
		"Left Arm Front-Back",
		"Left Arm Twist In-Out",
		"Left Forearm Stretch",
		"Left Forearm Twist In-Out",
		"Left Hand Down-Up",
		"Left Hand In-Out",
		"Right Shoulder Down-Up",
		"Right Shoulder Front-Back",
		"Right Arm Down-Up",
		"Right Arm Front-Back",
		"Right Arm Twist In-Out",
		"Right Forearm Stretch",
		"Right Forearm Twist In-Out",
		"Right Hand Down-Up",
		"Right Hand In-Out",
		"LeftHand.Thumb.1 Stretched",
		"LeftHand.Thumb.Spread",
		"LeftHand.Thumb.2 Stretched",
		"LeftHand.Thumb.3 Stretched",
		"LeftHand.Index.1 Stretched",
		"LeftHand.Index.Spread",
		"LeftHand.Index.2 Stretched",
		"LeftHand.Index.3 Stretched",
		"LeftHand.Middle.1 Stretched",
		"LeftHand.Middle.Spread",
		"LeftHand.Middle.2 Stretched",
		"LeftHand.Middle.3 Stretched",
		"LeftHand.Ring.1 Stretched",
		"LeftHand.Ring.Spread",
		"LeftHand.Ring.2 Stretched",
		"LeftHand.Ring.3 Stretched",
		"LeftHand.Little.1 Stretched",
		"LeftHand.Little.Spread",
		"LeftHand.Little.2 Stretched",
		"LeftHand.Little.3 Stretched",
		"RightHand.Thumb.1 Stretched",
		"RightHand.Thumb.Spread",
		"RightHand.Thumb.2 Stretched",
		"RightHand.Thumb.3 Stretched",
		"RightHand.Index.1 Stretched",
		"RightHand.Index.Spread",
		"RightHand.Index.2 Stretched",
		"RightHand.Index.3 Stretched",
		"RightHand.Middle.1 Stretched",
		"RightHand.Middle.Spread",
		"RightHand.Middle.2 Stretched",
		"RightHand.Middle.3 Stretched",
		"RightHand.Ring.1 Stretched",
		"RightHand.Ring.Spread",
		"RightHand.Ring.2 Stretched",
		"RightHand.Ring.3 Stretched",
		"RightHand.Little.1 Stretched",
		"RightHand.Little.Spread",
		"RightHand.Little.2 Stretched",
		"RightHand.Little.3 Stretched",
	};

	private static readonly string[] c_muscles_anim_attribute_sub = new [] {
		"RootT.x",
		"RootT.y",
		"RootT.z",
		"RootQ.w",
		"RootQ.x",
		"RootQ.y",
		"RootQ.z",
		"LeftFootT.x",
		"LeftFootT.y",
		"LeftFootT.z",
		"LeftFootQ.w",
		"LeftFootQ.x",
		"LeftFootQ.y",
		"LeftFootQ.z",
		"RightFootT.x",
		"RightFootT.y",
		"RightFootT.z",
		"RightFootQ.w",
		"RightFootQ.x",
		"RightFootQ.y",
		"RightFootQ.z",
		"LeftHandT.x",
		"LeftHandT.y",
		"LeftHandT.z",
		"LeftHandQ.w",
		"LeftHandQ.x",
		"LeftHandQ.y",
		"LeftHandQ.z",
		"RightHandT.x",
		"RightHandT.y",
		"RightHandT.z",
		"RightHandQ.w",
		"RightHandQ.x",
		"RightHandQ.y",
		"RightHandQ.z",
	};
}
