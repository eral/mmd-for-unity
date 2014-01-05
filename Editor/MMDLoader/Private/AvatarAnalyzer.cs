using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

// アバターの設定を行うスクリプト
public class AnimatorAnalyzer
{
	/// <summary>
	/// ボーンフルインデックス
	/// </summary>
	public enum HumanBodyFullBones {
		//●HumanBodyBones
		Hips,
		LeftUpperLeg,
		RightUpperLeg,
		LeftLowerLeg,
		RightLowerLeg,
		LeftFoot,
		RightFoot,
		Spine,
		Chest,
		Neck,
		Head,
		LeftShoulder,
		RightShoulder,
		LeftUpperArm,
		RightUpperArm,
		LeftLowerArm,
		RightLowerArm,
		LeftHand,
		RightHand,
		LeftToes,
		RightToes,
		LeftEye,
		RightEye,
		Jaw,

		//●HumanBodyBones-Expands
		LeftThumbProximal,
		LeftThumbIntermediate,
		LeftThumbDistal,
		LeftIndexProximal,
		LeftIndexIntermediate,
		LeftIndexDistal,
		LeftMiddleProximal,
		LeftMiddleIntermediate,
		LeftMiddleDistal,
		LeftRingProximal,
		LeftRingIntermediate,
		LeftRingDistal,
		LeftLittleProximal,
		LeftLittleIntermediate,
		LeftLittleDistal,
		RightThumbProximal,
		RightThumbIntermediate,
		RightThumbDistal,
		RightIndexProximal,
		RightIndexIntermediate,
		RightIndexDistal,
		RightMiddleProximal,
		RightMiddleIntermediate,
		RightMiddleDistal,
		RightRingProximal,
		RightRingIntermediate,
		RightRingDistal,
		RightLittleProximal,
		RightLittleIntermediate,
		RightLittleDistal,
	}

	/// <summary>
	/// Musclesインデックス
	/// </summary>
	public enum HumanBodyMuscles {
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

	/// <summary>
	/// デフォルトコンストラクタ
	/// </summary>
	/// <remarks>
	/// ユーザーに依るインスタンス作成を禁止する
	/// </remarks>
	public AnimatorAnalyzer(Animator animator) {
		animator_ = animator;
		skeleton_index_to_hash_ = CreateSkeletonIndexToHash(animator.avatar);
		human_index_to_hash_ = CreateHumanIndexToHash(animator.avatar);
		bone_index_to_human_index_ = CreateBoneIndexToHumanIndex(animator.avatar);
		hash_to_path_ = CreateHashToPath(animator.avatar);
	}
	
	/// <summary>
	/// ポーン回転制限値の取得
	/// </summary>
	/// <returns>ポーン回転制限値</returns>
	public Vector3[] GetRotationLimit(HumanBodyFullBones index) {
		if (null == rotation_limit_) {
			rotation_limit_ = CreateRotationLimitFromAvatar();
		}
		Vector3[] result = null;
		if ((uint)index < (uint)rotation_limit_.Length) {
			result = rotation_limit_[(int)index];
		} else {
			throw new System.ArgumentOutOfRangeException();
		}
		return result;
	}
	
	/// <summary>
	/// Muscle範囲値の取得
	/// </summary>
	/// <returns>Muscle範囲値</returns>
	public Vector2 GetMuscleLimit(HumanBodyMuscles index) {
		if (null == rotation_limit_) {
			rotation_limit_ = CreateRotationLimitFromAvatar();
		}
		Vector2? result = null;
		HumanBodyFullBones bone_index = (HumanBodyFullBones)HumanTrait.BoneFromMuscle((int)index);
		if ((uint)bone_index < (uint)rotation_limit_.Length) {
			for (int axis_index = 0, axis_index_max = 3; axis_index < axis_index_max; ++axis_index) {
				HumanBodyMuscles muscle_index = (HumanBodyMuscles)HumanTrait.MuscleFromBone((int)bone_index, axis_index);
				if (index == muscle_index) {
					result = new Vector2(rotation_limit_[(int)bone_index][0][axis_index]
										, rotation_limit_[(int)bone_index][1][axis_index]
										);
					break;
				}
			}
		}
		if (!result.HasValue) {
			throw new System.ArgumentOutOfRangeException();
		}
		return result.Value;
	}
	
	/// <summary>
	/// ヒューマンインデックス所持確認
	/// </summary>
	/// <returns>true:所持, false:未所持</returns>
	/// <param name="index">ヒューマンインデックス</param>
	public bool HasHumanIndex(HumanBodyFullBones index) {
		bool result = false;
		if ((uint)index < (uint)human_index_to_hash_.Length) {
			var hash = human_index_to_hash_[(int)index];
			if (hash_to_path_.ContainsKey(hash)) {
				result = true;
			}
		} else {
			throw new System.ArgumentOutOfRangeException();
		}
		return result;
	}
	
	/// <summary>
	/// ヒューマンインデックスからノードトランスフォームの取得
	/// </summary>
	/// <returns>ノードトランスフォーム</returns>
	/// <param name="index">ヒューマンインデックス</param>
	public Transform GetTransformFromHumanIndex(HumanBodyFullBones index) {
		Transform result = null;
		var path = GetPathFromHumanIndex(index);
		result = GetTransformFromPath(animator_, path);
		return result;
	}
	
	/// <summary>
	/// ヒューマンインデックスからノードパスの取得
	/// </summary>
	/// <returns>ノードパス</returns>
	/// <param name="index">ヒューマンインデックス</param>
	private string GetPathFromHumanIndex(HumanBodyFullBones index) {
		string result = null;
		if ((uint)index < (uint)human_index_to_hash_.Length) {
			var hash = human_index_to_hash_[(int)index];
			if (hash_to_path_.ContainsKey(hash)) {
				result = hash_to_path_[hash];
			}
		}
		return result;
	}
	
	/// <summary>
	/// スケルトンインデックスからノードトランスフォームの取得
	/// </summary>
	/// <returns>ノードトランスフォーム</returns>
	/// <param name="index">スケルトンインデックス</param>
	private Transform GetTransformFromSkeletonIndex(int index) {
		Transform result = null;
		var path = GetPathFromSkeletonIndex(index);
		result = GetTransformFromPath(animator_, path);
		return result;
	}
	
	/// <summary>
	/// スケルトンインデックスからノードパスの取得
	/// </summary>
	/// <returns>ノードパス</returns>
	/// <param name="index">スケルトンインデックス</param>
	private string GetPathFromSkeletonIndex(int index) {
		string result = null;
		if ((uint)index < (uint)skeleton_index_to_hash_.Length) {
			var hash = skeleton_index_to_hash_[index];
			if (hash_to_path_.ContainsKey(hash)) {
				result = hash_to_path_[hash];
			}
		}
		return result;
	}

	/// <summary>
	/// ノードパスからノードトランスフォームの取得
	/// </summary>
	/// <returns>ノードトランスフォーム</returns>
	/// <param name="animator">アニメーター</param>
	/// <param name="path">ノードパス</param>
	private static Transform GetTransformFromPath(Animator animator, string path) {
		Transform result = null;
		if ((null != animator) && !string.IsNullOrEmpty(path)) {
			result = animator.transform.FindChild(path);
		}
		return result;
	}
	
	/// <summary>
	/// スケルトンインデックス-ハッシュ変換デーブルの作成
	/// </summary>
	/// <param name="avatar">アバター</param>
	/// <returns>ハッシュ変換デーブル</returns>
	private static int[] CreateSkeletonIndexToHash(Avatar avatar) {
		SerializedObject so_avatar = new SerializedObject(avatar);
		SerializedProperty sp_id_array = so_avatar.FindProperty("m_Avatar.m_AvatarSkeleton.data.m_ID");
		
		int[] result = new int[sp_id_array.arraySize];
		for (int i = 0, i_max = sp_id_array.arraySize; i < i_max; ++i) {
			var hash = sp_id_array.GetArrayElementAtIndex(i).intValue;
			result[i] = hash;
		}
		return result;
	}

	/// <summary>
	/// ヒューマンインデックス-ハッシュ変換デーブルの作成
	/// </summary>
	/// <param name="avatar">アバター</param>
	/// <returns>ハッシュ変換デーブル</returns>
	private static int[] CreateHumanIndexToHash(Avatar avatar) {
		int[] result = Enumerable.Repeat(-1, System.Enum.GetValues(typeof(HumanBodyFullBones)).Length)
								.ToArray();

		SerializedObject so_avatar = new SerializedObject(avatar);

		{ //主ボーン
			SerializedProperty sp_human_bone_index_array = so_avatar.FindProperty("m_Avatar.m_Human.data.m_HumanBoneIndex");
			for (int i = 0, i_max = sp_human_bone_index_array.arraySize; i < i_max; ++i) {
				var hash = sp_human_bone_index_array.GetArrayElementAtIndex(i).intValue;
				result[i] = hash;
			}
		}
		
		{ //左手
			SerializedProperty sp_left_hand_array = so_avatar.FindProperty("m_Avatar.m_Human.data.m_LeftHand.data");
			for (int i = 0, i_max = sp_left_hand_array.arraySize; i < i_max; ++i) {
				var hash = sp_left_hand_array.GetArrayElementAtIndex(i).intValue;
				result[i + (int)HumanBodyFullBones.LeftThumbProximal] = hash;
			}
		}
		
		{ //右手
			SerializedProperty sp_right_hand_array = so_avatar.FindProperty("m_Avatar.m_Human.data.m_RightHand.data");
			for (int i = 0, i_max = sp_right_hand_array.arraySize; i < i_max; ++i) {
				var hash = sp_right_hand_array.GetArrayElementAtIndex(i).intValue;
				result[i + (int)HumanBodyFullBones.RightThumbProximal] = hash;
			}
		}

		return result;
	}
	
	/// <summary>
	/// ボーンインデックス-ヒューマンインデックス変換デーブルの作成
	/// </summary>
	/// <param name="avatar">アバター</param>
	/// <returns>ハッシュ変換デーブル</returns>
	private static int[] CreateBoneIndexToHumanIndex(Avatar avatar) {
		SerializedObject so_avatar = new SerializedObject(avatar);
		SerializedProperty sp_id_array = so_avatar.FindProperty("m_Avatar.m_Human.data.m_Skeleton.data.m_ID");
		
		int[] result = new int[sp_id_array.arraySize];
		for (int i = 0, i_max = sp_id_array.arraySize; i < i_max; ++i) {
			var hash = sp_id_array.GetArrayElementAtIndex(i).intValue;
			result[i] = hash;
		}
		return result;
	}
	
	/// <summary>
	/// ハッシュ-ノードパス変換辞書の作成
	/// </summary>
	/// <param name="avatar">アバター</param>
	/// <returns>ハッシュ-ノードパス変換辞書</returns>
	private static Dictionary<int, string> CreateHashToPath(Avatar avatar) {
		SerializedObject so_avatar = new SerializedObject(avatar);
		SerializedProperty sp_tos_array = so_avatar.FindProperty("m_TOS");
		
		Dictionary<int, string> result = new Dictionary<int, string>();
		for (int i = 0, i_max = sp_tos_array.arraySize; i < i_max; ++i) {
			var sp_tos = sp_tos_array.GetArrayElementAtIndex(i);
			var hash = sp_tos.FindPropertyRelative("first").intValue;
			var path = sp_tos.FindPropertyRelative("second").stringValue;
			result.Add(hash, path);
		}
		return result;
	}
	
	/// <summary>
	/// アバターからポーンの回転制限値を取得する
	/// </summary>
	/// <returns>回転制限値のベクター配列(Vector3[HumanBodyFullBones][min=0,max=1])</returns>
	private Vector3[][] CreateRotationLimitFromAvatar() {
		SerializedObject so_avatar = new SerializedObject(animator_.avatar);
		SerializedProperty sp_axes_array = so_avatar.FindProperty("m_Avatar.m_Human.data.m_Skeleton.data.m_AxesArray");
		
		Vector3[][] result = Enumerable.Repeat(new[]{Vector3.zero, Vector3.zero}, System.Enum.GetValues(typeof(HumanBodyFullBones)).Length)
									.ToArray();
		HumanBodyFullBones bone_index = (HumanBodyFullBones)(-1);
		for (int i = 0, i_max = sp_axes_array.arraySize; i < i_max; ++i) {
			while (!HasHumanIndex(++bone_index)) {};

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
			}
			{
				var sp_unit = sp_limit.FindPropertyRelative("m_Max");
				sp_unit.Next(true);
				float_limit[1] = new float[4];
				for (int k = 0, k_max = float_limit[1].Length; k < k_max; ++k) {
					float_limit[1][k] = sp_unit.floatValue;
					sp_unit.Next(false);
				}
			}
			result[(int)bone_index] = float_limit.Select(x=>new Vector3(x[0], x[1], x[2])).ToArray();
		}
		return result;
	}
	
	private Animator animator_;
	int[] skeleton_index_to_hash_;
	int[] human_index_to_hash_;
	int[] bone_index_to_human_index_;
	Dictionary<int, string> hash_to_path_;
	Vector3[][] rotation_limit_ = null;
}
