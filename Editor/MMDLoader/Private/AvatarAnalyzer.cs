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
	/// Muscle値の取得
	/// </summary>
	/// <returns>(現在の姿勢の)Muscle値</returns>
	public float[] GetMuscleValue() {
		float[] result = null;
		if (null != animator_.avatar) {
		
		}
		return result;
	}
	
	/// <summary>
	/// 基本ポーズのボーン回転値の取得
	/// </summary>
	/// <returns>Tポーズのボーン回転値</returns>
	public Quaternion GetRotationDefaultPose(HumanBodyFullBones index) {
		if (null == human_transform_for_t_pose_) {
			human_transform_for_t_pose_ = CreateHumanTransformForTstylePose();
		}
		if (null == bone_axes_information_) {
			bone_axes_information_ = CreateBoneAxesInformation();
		}
		Quaternion result = Quaternion.identity;
		int human_index = GetHumanIndexFromBoneIndex(index);
		if (((uint)index < (uint)bone_axes_information_.Length) && ((uint)human_index < (uint)human_transform_for_t_pose_.Length)) {
//			result = bone_axes_information_[(int)index].pre_quaternion * human_transform_for_t_pose_[human_index].rotation;
			result = bone_axes_information_[(int)index].pre_quaternion * human_transform_for_t_pose_[human_index].rotation * bone_axes_information_[(int)index].post_quaternion;
		} else {
			throw new System.ArgumentOutOfRangeException();
		}
		return result;
	}
	
	/// <summary>
	/// Tポーズのボーン回転値の取得
	/// </summary>
	/// <returns>Tポーズのボーン回転値</returns>
	public Quaternion GetRotationTstylePose(HumanBodyFullBones index) {
		if (null == human_transform_for_t_pose_) {
			human_transform_for_t_pose_ = CreateHumanTransformForTstylePose();
		}
		Quaternion result = Quaternion.identity;
		int human_index = GetHumanIndexFromBoneIndex(index);
		if ((uint)human_index < (uint)human_transform_for_t_pose_.Length) {
			result = human_transform_for_t_pose_[human_index].rotation;
		} else {
			throw new System.ArgumentOutOfRangeException();
		}
		return result;
	}
	
	/// <summary>
	/// ボーン回転制限値の取得
	/// </summary>
	/// <returns>ボーン回転制限値</returns>
	public Vector3[] GetRotationLimit(HumanBodyFullBones index) {
		if (null == bone_axes_information_) {
			bone_axes_information_ = CreateBoneAxesInformation();
		}
		Vector3[] result = null;
		if ((uint)index < (uint)bone_axes_information_.Length) {
			AxesInformation.Limit limit = bone_axes_information_[(int)index].limit;
			result = new[]{new Vector3(limit.min.x, limit.min.y, limit.min.z), new Vector3(limit.max.x, limit.max.y, limit.max.z)};
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
		if (null == bone_axes_information_) {
			bone_axes_information_ = CreateBoneAxesInformation();
		}
		Vector2? result = null;
		HumanBodyFullBones bone_index = (HumanBodyFullBones)HumanTrait.BoneFromMuscle((int)index);
		if ((uint)bone_index < (uint)bone_axes_information_.Length) {
			for (int axis_index = 0, axis_index_max = 3; axis_index < axis_index_max; ++axis_index) {
				HumanBodyMuscles muscle_index = (HumanBodyMuscles)HumanTrait.MuscleFromBone((int)bone_index, axis_index);
				if (index == muscle_index) {
					result = new Vector2(bone_axes_information_[(int)bone_index].limit.min[axis_index]
										, bone_axes_information_[(int)bone_index].limit.min[axis_index]
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
	/// ボーンインデックス所持確認
	/// </summary>
	/// <returns>true:所持, false:未所持</returns>
	/// <param name="index">ボーンインデックス</param>
	public bool HasBoneIndex(HumanBodyFullBones index) {
		bool result = false;
		if ((uint)index < (uint)bone_index_to_human_index_.Length) {
			var human_index = bone_index_to_human_index_[(int)index];
			result = HasHumanIndex(human_index);
		}
		return result;
	}
	
	/// <summary>
	/// ボーンインデックスからノードトランスフォームの取得
	/// </summary>
	/// <returns>ノードトランスフォーム</returns>
	/// <param name="index">ボーンインデックス</param>
	public Transform GetTransformFromBoneIndex(HumanBodyFullBones index) {
		Transform result = null;
		int human_index = GetHumanIndexFromBoneIndex(index);
		var path = GetPathFromHumanIndex(human_index);
		result = GetTransformFromPath(animator_, path);
		return result;
	}
	
	/// <summary>
	/// ボーンインデックス所持確認
	/// </summary>
	/// <returns>ヒューマンインデックス</returns>
	/// <param name="index">ボーンインデックス</param>
	private int GetHumanIndexFromBoneIndex(HumanBodyFullBones index) {
		int result = -1;
		if ((uint)index < (uint)bone_index_to_human_index_.Length) {
			result = bone_index_to_human_index_[(int)index];
		} else {
			throw new System.ArgumentOutOfRangeException();
		}
		return result;
	}
	
	/// <summary>
	/// ヒューマンインデックス所持確認
	/// </summary>
	/// <returns>true:所持, false:未所持</returns>
	/// <param name="index">ヒューマンインデックス</param>
	private bool HasHumanIndex(int index) {
		bool result = false;
		if ((uint)index < (uint)human_index_to_hash_.Length) {
			var hash = human_index_to_hash_[index];
			if (hash_to_path_.ContainsKey(hash)) {
				result = true;
			}
		}
		return result;
	}
	
	/// <summary>
	/// ヒューマンインデックスからノードトランスフォームの取得
	/// </summary>
	/// <returns>ノードトランスフォーム</returns>
	/// <param name="index">ヒューマンインデックス</param>
	private Transform GetTransformFromHumanIndex(int index) {
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
	private string GetPathFromHumanIndex(int index) {
		string result = null;
		if ((uint)index < (uint)human_index_to_hash_.Length) {
			var hash = human_index_to_hash_[index];
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
	/// ボーンインデックス-ヒューマンインデックス変換デーブルの作成
	/// </summary>
	/// <param name="avatar">アバター</param>
	/// <returns>ハッシュ変換デーブル</returns>
	private static int[] CreateBoneIndexToHumanIndex(Avatar avatar) {
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
			SerializedProperty sp_left_hand_array = so_avatar.FindProperty("m_Avatar.m_Human.data.m_LeftHand.data.m_HandBoneIndex");
			for (int i = 0, i_max = sp_left_hand_array.arraySize; i < i_max; ++i) {
				var hash = sp_left_hand_array.GetArrayElementAtIndex(i).intValue;
				result[i + (int)HumanBodyFullBones.LeftThumbProximal] = hash;
			}
		}
		{ //右手
			SerializedProperty sp_right_hand_array = so_avatar.FindProperty("m_Avatar.m_Human.data.m_RightHand.data.m_HandBoneIndex");
			for (int i = 0, i_max = sp_right_hand_array.arraySize; i < i_max; ++i) {
				var hash = sp_right_hand_array.GetArrayElementAtIndex(i).intValue;
				result[i + (int)HumanBodyFullBones.RightThumbProximal] = hash;
			}
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
	/// ボーンの軸情報を取得する
	/// </summary>
	/// <returns>ボーンの軸情報配列(AxesInformation[HumanBodyFullBones])</returns>
	private AxesInformation[] CreateBoneAxesInformation() {
		SerializedObject so_avatar = new SerializedObject(animator_.avatar);
		SerializedProperty sp_axes_array = so_avatar.FindProperty("m_Avatar.m_Human.data.m_Skeleton.data.m_AxesArray");
		
		AxesInformation[] result = Enumerable.Repeat(new AxesInformation(), System.Enum.GetValues(typeof(HumanBodyFullBones)).Length)
											.ToArray();
		HumanBodyFullBones bone_index = (HumanBodyFullBones)(-1);
		for (int i = 0, i_max = sp_axes_array.arraySize; i < i_max; ++i) {
			while (!HasBoneIndex(++bone_index)) {};
			var axes_information = new AxesInformation();

			var sp_axes = sp_axes_array.GetArrayElementAtIndex(i);
			{ //PreQ
				var sp_pre_q = sp_axes.FindPropertyRelative("m_PreQ");
				sp_pre_q.Next(true);
				float[] float4 = new float[4];
				for (int k = 0, k_max = float4.Length; k < k_max; ++k) {
					float4[k] = sp_pre_q.floatValue;
					sp_pre_q.Next(false);
				}
				axes_information.pre_quaternion = new Quaternion(float4[0], float4[1], float4[2], float4[3]);
			}
			{ //PostQ
				var sp_post_q = sp_axes.FindPropertyRelative("m_PostQ");
				sp_post_q.Next(true);
				float[] float4 = new float[4];
				for (int k = 0, k_max = float4.Length; k < k_max; ++k) {
					float4[k] = sp_post_q.floatValue;
					sp_post_q.Next(false);
				}
				axes_information.post_quaternion = new Quaternion(float4[0], float4[1], float4[2], float4[3]);
			}
			{ //Sign
				var sp_sign = sp_axes.FindPropertyRelative("m_Sgn");
				sp_sign.Next(true);
				for (int k = 0, k_max = 4; k < k_max; ++k) {
					axes_information.sign[k] = sp_sign.floatValue;
					sp_sign.Next(false);
				}
			}
			{ //リミット
				var sp_limit = sp_axes.FindPropertyRelative("m_Limit");
				{ //最小値
					var sp_unit = sp_limit.FindPropertyRelative("m_Min");
					sp_unit.Next(true);
					for (int k = 0, k_max = 4; k < k_max; ++k) {
						axes_information.limit.min[k] = sp_unit.floatValue;
						sp_unit.Next(false);
					}
				}
				{ //最大値
					var sp_unit = sp_limit.FindPropertyRelative("m_Max");
					sp_unit.Next(true);
					for (int k = 0, k_max = 4; k < k_max; ++k) {
						axes_information.limit.max[k] = sp_unit.floatValue;
						sp_unit.Next(false);
					}
				}
			}

			result[(int)bone_index] = axes_information;
		}
		return result;
	}
	
	/// <summary>
	/// Tポーズのトランスフォームを取得する
	/// </summary>
	/// <returns>Tポーズのボーンのトランスフォーム配列(Quaternion[HumanBodyFullBones])</returns>
	private PortableTransform[] CreateHumanTransformForTstylePose() {
		SerializedObject so_avatar = new SerializedObject(animator_.avatar);
		SerializedProperty sp_transform_array = so_avatar.FindProperty("m_Avatar.m_Human.data.m_SkeletonPose.data.m_X");
		
		PortableTransform[] result = new PortableTransform[sp_transform_array.arraySize];
		for (int i = 0, i_max = result.Length; i < i_max; ++i) {
			var sp_transform = sp_transform_array.GetArrayElementAtIndex(i);
			
			PortableTransform transform = new PortableTransform();
			{ //位置
				var sp_position = sp_transform.FindPropertyRelative("t");
				sp_position.Next(true);
				float[] float4 = new float[4];
				for (int k = 0, k_max = float4.Length; k < k_max; ++k) {
					float4[k] = sp_position.floatValue;
					sp_position.Next(false);
				}
				transform.position = new Vector3(float4[0], float4[1], float4[2]);
			}
			{ //回転
				var sp_rotation = sp_transform.FindPropertyRelative("q");
				sp_rotation.Next(true);
				float[] float4 = new float[4];
				for (int k = 0, k_max = float4.Length; k < k_max; ++k) {
					float4[k] = sp_rotation.floatValue;
					sp_rotation.Next(false);
				}
				transform.rotation = new Quaternion(float4[0], float4[1], float4[2], float4[3]);
			}
			{ //拡縮率
				var sp_scale = sp_transform.FindPropertyRelative("s");
				sp_scale.Next(true);
				float[] float4 = new float[4];
				for (int k = 0, k_max = float4.Length; k < k_max; ++k) {
					float4[k] = sp_scale.floatValue;
					sp_scale.Next(false);
				}
				transform.scale = new Vector3(float4[0], float4[1], float4[2]);
			}
			result[i] = transform;
		}
		return result;
	}
	
	private Animator animator_;
	int[] skeleton_index_to_hash_;
	int[] human_index_to_hash_;
	int[] bone_index_to_human_index_;
	Dictionary<int, string> hash_to_path_;
	struct AxesInformation {
		public struct Limit {
			public Vector4	min;
			public Vector4	max;
		}
		public Quaternion	pre_quaternion;
		public Quaternion	post_quaternion;
		public Vector4		sign;
		public Limit		limit;
	}
	AxesInformation[] bone_axes_information_ = null;
	struct PortableTransform {
		public Vector3		position;
		public Quaternion	rotation;
		public Vector3		scale;
	}
	PortableTransform[] human_transform_for_t_pose_ = null;
}
