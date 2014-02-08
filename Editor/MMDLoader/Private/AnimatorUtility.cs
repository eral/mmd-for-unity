using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// アバターの設定を行うスクリプト
public class AnimatorUtility
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
	/// コンストラクタ
	/// </summary>
	/// <param name="animator">アニメーター</param>
	public AnimatorUtility(Animator animator) {
		animator_ = animator;
		skeleton_index_to_hash_ = CreateSkeletonIndexToHash(animator.avatar);
		human_index_to_hash_ = CreateHumanIndexToHash(animator.avatar);
		bone_index_to_human_index_ = CreateBoneIndexToHumanIndex(animator.avatar);
		hash_to_path_ = CreateHashToPath(animator.avatar);
	}
	
	/// <summary>
	/// スタティックコンストラクタ
	/// </summary>
	static AnimatorUtility() {
		AvatarUtility_SetHumanPose_ = Types.GetType("UnityEditor.AvatarUtility", "UnityEditor.dll")
											.GetMethod("SetHumanPose", BindingFlags.Public | BindingFlags.Static);
	}
	
	/// <summary>
	/// Muscle値の設定
	/// </summary>
	/// <param name="muscles_value_">Muscle値配列</param>
	public void SetMuscleValue(float[] muscles_value_) {
		if (null != animator_.avatar) {
			//ボーン操作
			for (HumanBodyFullBones bone_index = (HumanBodyFullBones)0, bone_index_max = (HumanBodyFullBones)System.Enum.GetValues(typeof(HumanBodyFullBones)).Length; bone_index < bone_index_max; ++bone_index) {
				Transform bone_transform = GetTransformFromBoneIndex(bone_index);
				if (null != bone_transform) {
					//ボーンが有るなら
					//Muscle値算出
					Vector3 muscle = Vector3.zero;
					for (int axis_index = 0, axis_index_max = 3; axis_index < axis_index_max; ++axis_index) {
						HumanBodyMuscles muscle_index = (HumanBodyMuscles)HumanTrait.MuscleFromBone((int)bone_index, axis_index);
						if ((uint)muscle_index < (uint)System.Enum.GetValues(typeof(HumanBodyMuscles)).Length) {
							muscle[axis_index] = muscles_value_[(int)muscle_index];
						}
					}
					Quaternion rotation = GetRotationFromMuscleValue(bone_index, muscle);
					bone_transform.localRotation = rotation;
				}
			}
		}
	}
	
	/// <summary>
	/// UnityEditor.dllを用いたMuscle値の設定
	/// </summary>
	/// <param name="muscles_value_">Muscle値配列</param>
	public void SetMuscleValueWithUnityEditorDll(float[] muscles_value_) {
		AvatarUtility_SetHumanPose_.Invoke(null, new object[]{animator_, muscles_value_});
	}
	
	/// <summary>
	/// Muscle値の取得
	/// </summary>
	/// <returns>(現在の姿勢の)Muscle値配列</returns>
	public float[] GetMuscleValue() {
		float[] result = null;
		if (null != animator_.avatar) {
			//先に戻り値用の配列を確保(後でボーン・軸順に格納していく)
			result = Enumerable.Repeat(0.0f, System.Enum.GetValues(typeof(HumanBodyMuscles)).Length)
								.ToArray();
			
			//ボーン操作
			for (HumanBodyFullBones bone_index = (HumanBodyFullBones)0, bone_index_max = (HumanBodyFullBones)System.Enum.GetValues(typeof(HumanBodyFullBones)).Length; bone_index < bone_index_max; ++bone_index) {
				Transform bone_transform = GetTransformFromBoneIndex(bone_index);
				if (null != bone_transform) {
					//ボーンが有るなら
					//Muscle値算出
					var muscle_values = GetMuscleValueFromRotation(bone_index, bone_transform.localRotation);
					for (int axis_index = 0, axis_index_max = 3; axis_index < axis_index_max; ++axis_index) {
						HumanBodyMuscles muscle_index = (HumanBodyMuscles)HumanTrait.MuscleFromBone((int)bone_index, axis_index);
						if ((uint)muscle_index < (uint)System.Enum.GetValues(typeof(HumanBodyMuscles)).Length) {
							result[(int)muscle_index] = muscle_values[axis_index];
						}
					}
				}
			}
		}
		return result;
	}
	
	/// <summary>
	/// Muscle値の取得
	/// </summary>
	/// <returns>Muscle値</returns>
	/// <param name="index">ボーンインデックス</param>
	/// <param name="rotation">回転値</param>
	private Vector3 GetMuscleValueFromRotation(HumanBodyFullBones index, Quaternion rotation) {
		Vector3 result = Vector3.zero;
		//Muscle値算出
		AxesInformation axes_information = GetAxesInformation(index);
		Quaternion quaternion_avatar = Quaternion.Inverse(axes_information.pre_quaternion) * rotation * axes_information.post_quaternion;
		Vector3 euler_avatar = quaternion_avatar.eulerAngles;
		//軸操作
		for (int axis_index = 0, axis_index_max = 3; axis_index < axis_index_max; ++axis_index) {
			float value = euler_avatar[axis_index];
			value = ((value < -180.0f)? value + 360.0f: ((180.0f < value)? value - 360.0f: value)); //範囲を-180.0f～180.0fに収める
			value *= axes_information.sign[axis_index];
			if ((value < 0) && (0 != axes_information.limit.min[axis_index])) {
				//標準ポーズより小さいなら
				value = value / (axes_information.limit.min[axis_index] * -Mathf.Rad2Deg); 
			} else if ((0 < value) && (0.0f != axes_information.limit.max[axis_index])) {
				//標準ポーズより大きいなら
				value = value / (axes_information.limit.max[axis_index] * Mathf.Rad2Deg);
			} else {
				//標準ポーズと同じ もしくは 未対応軸なら
				value = 0.0f;
			}
			value = Mathf.Clamp(value, -1.0f, 1.0f);
			result[axis_index] = value;
		}
		return result;
	}
	
	/// <summary>
	/// Muscle値から回転値の取得
	/// </summary>
	/// <returns>回転値</returns>
	/// <param name="index">ボーンインデックス</param>
	/// <param name="rotation">Muscle値</param>
	private Quaternion GetRotationFromMuscleValue(HumanBodyFullBones index, Vector3 muscle) {
		//Muscle値算出
		AxesInformation axes_information = GetAxesInformation(index);
		Vector3 euler = new Vector3(muscle.x * axes_information.sign.x
									, muscle.y * axes_information.sign.y
									, muscle.z * axes_information.sign.z
									);
		for (int i = 0, i_max = 3; i < i_max; ++i) {
			if (euler[i] < 0) {
				//負数なら
				euler[i] *= axes_information.limit.min[i] * -Mathf.Rad2Deg;
			} else {
				//正数なら
				euler[i] *= axes_information.limit.max[i] * Mathf.Rad2Deg;
			}
		}
		Quaternion rotation = Quaternion.Euler(euler);
		Quaternion result = axes_information.pre_quaternion * rotation * Quaternion.Inverse(axes_information.post_quaternion);
		return result;
	}
	
	/// <summary>
	/// Point値の取得
	/// </summary>
	/// <returns>(現在の姿勢の)Point値配列</returns>
	private float[] GetPointValue() {
		float[] result = null;
		if (null != animator_.avatar) {
			result = new float[System.Enum.GetValues(typeof(HumanBodyPoints)).Length];

			//登録用関数の作成
			System.Action<HumanBodyPoints, Vector3, Quaternion> SetResult = (index, position, rotation)=>{
				for (int i = 0, i_max = 3; i < i_max; ++i) {
					result[(int)index + i] = position[i];
				}
				for (int i = 0, i_max = 4; i < i_max; ++i) {
					result[(int)index + i + 3] = rotation[i];
				}
			};

			{ //Root
				var transform = animator_.GetBoneTransform(HumanBodyBones.Hips);
				SetResult(HumanBodyPoints.RootPositionX, transform.position, transform.rotation);
			}
			{ //LeftFoot
				var transform = animator_.GetBoneTransform(HumanBodyBones.LeftFoot);
				SetResult(HumanBodyPoints.LeftFootPositionX, transform.position, transform.rotation);
			}
			{ //RightFoot
				var transform = animator_.GetBoneTransform(HumanBodyBones.RightFoot);
				SetResult(HumanBodyPoints.RightFootPositionX, transform.position, transform.rotation);
			}
			{ //LeftHand
				var transform = animator_.GetBoneTransform(HumanBodyBones.LeftHand);
				SetResult(HumanBodyPoints.LeftHandPositionX, transform.position, transform.rotation);
			}
			{ //RightHand
				var transform = animator_.GetBoneTransform(HumanBodyBones.RightHand);
				SetResult(HumanBodyPoints.RightHandPositionX, transform.position, transform.rotation);
			}
		}
		return result;
	}
	
	/// <summary>
	/// Point値の取得
	/// </summary>
	/// <returns>Pointsインデックスと値の辞書</returns>
	/// <param name="index">ボーンインデックス</param>
	/// <param name="position">位置</param>
	/// <param name="rotation">回転値</param>
	private Dictionary<HumanBodyPoints, float> GetPointValue(HumanBodyFullBones index, Vector3 position, Quaternion rotation) {
		Dictionary<HumanBodyPoints, float> result = new Dictionary<HumanBodyPoints, float>();
		
		//登録用関数の作成
		System.Action<HumanBodyPoints, Vector3, Quaternion> SetResult = (start_index, bone_position, bone_rotation)=>{
			for (int i = 0, i_max = 3; i < i_max; ++i) {
				result.Add((HumanBodyPoints)(start_index + i), bone_position[i]);
			}
			for (int i = 0, i_max = 4; i < i_max; ++i) {
				result.Add((HumanBodyPoints)(start_index + i + 3), bone_rotation[i]);
			}
		};
		
		switch (index) {
		case HumanBodyFullBones.Hips:
			SetResult(HumanBodyPoints.RootPositionX, position, rotation);
			break;
		case HumanBodyFullBones.LeftFoot:
			SetResult(HumanBodyPoints.LeftFootPositionX, position, rotation);
			break;
		case HumanBodyFullBones.RightFoot:
			SetResult(HumanBodyPoints.RightFootPositionX, position, rotation);
			break;
		case HumanBodyFullBones.LeftHand:
			SetResult(HumanBodyPoints.LeftHandPositionX, position, rotation);
			break;
		case HumanBodyFullBones.RightHand:
			SetResult(HumanBodyPoints.RightHandPositionX, position, rotation);
			break;
		default:
			//empty.
			break;
		}
		return result;
	}
	
	/// <summary>
	/// 基本ポーズのボーン回転値の取得
	/// </summary>
	/// <returns>Tポーズのボーン回転値</returns>
	/// <param name="index">ボーンインデックス</param>
	public Quaternion GetRotationDefaultPose(HumanBodyFullBones index) {
		Quaternion result = Quaternion.identity;
		if (HumanBodyFullBones.Hips == index) {
			//腰ボーンなら
			if (null == human_transform_for_t_pose_) {
				human_transform_for_t_pose_ = CreateHumanTransformForTstylePose();
			}
			int human_index = GetHumanIndexFromBoneIndex(index);
			if ((uint)human_index < (uint)human_transform_for_t_pose_.Length) {
				result = human_transform_for_t_pose_[human_index].rotation;
			}
		} else {
			//腰ボーン以外なら
			AxesInformation axes_information = GetAxesInformation(index);
			result = axes_information.pre_quaternion * result * Quaternion.Inverse(axes_information.post_quaternion);
		}
		return result;
	}
	
	/// <summary>
	/// Tポーズのボーン回転値の取得
	/// </summary>
	/// <returns>Tポーズのボーン回転値</returns>
	/// <param name="index">ボーンインデックス</param>
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
	/// <param name="index">ボーンインデックス</param>
	public Vector3[] GetRotationLimit(HumanBodyFullBones index) {
		AxesInformation.Limit axes_information_limit = GetAxesInformation(index).limit;
		Vector3[] result = new[]{new Vector3(axes_information_limit.min.x, axes_information_limit.min.y, axes_information_limit.min.z)
								, new Vector3(axes_information_limit.max.x, axes_information_limit.max.y, axes_information_limit.max.z)
								};
		result = result.Select(x=>x / Mathf.PI).ToArray(); //-1.0f～1.0fに正規化
		return result;
	}
	
	/// <summary>
	/// Muscle範囲値の取得
	/// </summary>
	/// <returns>Muscle範囲値</returns>
	public Vector2 GetMuscleLimit(HumanBodyMuscles index) {
		Vector2 result = new Vector2();
		HumanBodyFullBones bone_index = (HumanBodyFullBones)HumanTrait.BoneFromMuscle((int)index);
		for (int axis_index = 0, axis_index_max = 3; axis_index < axis_index_max; ++axis_index) {
			HumanBodyMuscles muscle_index = (HumanBodyMuscles)HumanTrait.MuscleFromBone((int)bone_index, axis_index);
			if (index == muscle_index) {
				AxesInformation.Limit axes_information_limit = GetAxesInformation(bone_index).limit;
				result = new Vector2(axes_information_limit.min[axis_index]
									, axes_information_limit.max[axis_index]
									);
				result = result / Mathf.PI; //-1.0f～1.0fに正規化
				break;
			}
		}
		return result;
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
		result = GetTransformFromPath(path);
		return result;
	}
	
	/// <summary>
	/// ルートボーンのノードトランスフォームの取得
	/// </summary>
	/// <returns>ノードトランスフォーム</returns>
	public Transform GetTransformFromRootBone() {
		Transform result = null;
		var path = GetPathFromHumanIndex(0);
		result = GetTransformFromPath(path);
		return result;
	}
	
	/// <summary>
	/// ボーンインデックスから軸情報を取得
	/// </summary>
	/// <returns>ボーン軸情報</returns>
	/// <param name="index">ボーンインデックス</param>
	private AxesInformation GetAxesInformation(HumanBodyFullBones index) {
		AxesInformation result = new AxesInformation();
		if (null == bone_axes_information_) {
			bone_axes_information_ = CreateBoneAxesInformation();
		}
		if ((uint)index < (uint)bone_axes_information_.Length) {
			result = bone_axes_information_[(int)index];
		} else {
			throw new System.ArgumentOutOfRangeException();
		}
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
		result = GetTransformFromPath(path);
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
	/// アニメーションクリップをアバターに対応させる
	/// </summary>
	/// <returns>アバター対応アニメーションクリップ</returns>
	/// <param name="clip">アバター未対応アニメーションクリップ</param>
	/// <param name="start">人型アバターサンプリング時の初回更新関数</param>
	/// <param name="update">人型アバターサンプリング時の更新関数(内部でAnimation.Sample()を呼んで下さい)</param>
	public AnimationClip AdaptAnimationClip(AnimationClip clip, System.Action<Animation> start = null, System.Func<Animation, IEnumerable<string>, HumanBodyFullBones[]> update = null) {
		AnimationClip result = null;
		if (animator_.avatar.isHuman && !clip.isHumanMotion) {
			//アバターが人型 かつ アニメーションクリップが人型未対応なら
			//初回更新関数と更新関数の生成
			if (null == start) {
				start = x=>{};
			}
			if (null == update) {
				update = (anim,path)=>{anim.Sample();return new HumanBodyFullBones[0];};
			}
			//サンプリング時にトランスフォームが破壊されるのでダミーを作成してそちらで行う
			GameObject dummy_game_object = CreateAnimationClipGameObject(animator_, clip, "clip");
			//フレームレートに準じてサンプリング
			Animation dummy_animation = dummy_game_object.GetComponent<Animation>();
			var muscle_value_animations = CreateMuscleValueAnimation(dummy_animation, "clip", start, update);
			//ダミー破棄
			GameObject.DestroyImmediate(dummy_game_object);
			//人型アバター対応アニメーションクリップの作成
			result = CreateHumanMecanimAnimationClip(muscle_value_animations);
			result.name = clip.name;
		} else if (animator_.avatar.isValid && !clip.isAnimatorMotion) {
			//アバターが有り かつ アニメーションクリップがアバター未対応なら
			result = clip;
			//AnimationTypeをアバター対応アニメーションクリップに設定
			AnimationUtility.SetAnimationType(result, ModelImporterAnimationType.Generic);
		}
		return result;
	}
	
	/// <summary>
	/// アニメーションクリップを持ったゲームオブジェクトを複製する
	/// </summary>
	/// <returns>ゲームオブジェクト</returns>
	/// <param name="animator">複製元のゲームオブジェクトに付加されているアニメーター</param>
	/// <param name="clip">アニメーションクリップ</param>
	/// <param name="clip">アニメーション名</param>
	private static GameObject CreateAnimationClipGameObject(Animator animator, AnimationClip clip, string clip_name) {
		GameObject result = (GameObject)GameObject.Instantiate(animator.gameObject);
		Animation animation = result.GetComponent<Animation>();
		if (null != animation) {
			//既にAnimationコンポーネントを持っているなら
			//不要なアニメーションクリップを削除
			foreach (AnimationState state in animation) {
				animation.RemoveClip(state.name);
			}
		} else {
			//Animationコンポーネントを持っていないなら
			//付与
			animation = result.AddComponent<Animation>();
		}
		//アニメーションクリップの付与
		animation.AddClip(clip, clip_name);
		return result;
	}

	/// <summary>
	/// アニメーションクリップからMuscle値アニメーションを作成する
	/// </summary>
	/// <returns>Muscle値アニメーションの配列</returns>
	/// <param name="animation">サンプリングするアニメーション(トランスフォームを破壊します)</param>
	/// <param name="clip_name">サンプリングするクリップ名</param>
	/// <param name="delta_time">サンプリング周期</param>
	/// <param name="start_cb">初回サンプリング前コールバック</param>
	/// <param name="update_cb">サンプリングコールバック</param>
	private static Dictionary<float, float>[] CreateMuscleValueAnimation(Animation animation, string clip_name, System.Action<Animation> start_cb, System.Func<Animation, IEnumerable<string>, HumanBodyFullBones[]> update_cb) {
		AnimatorUtility animator_utility = new AnimatorUtility(animation.gameObject.GetComponent<Animator>());
		//戻り値バッファ作成
		int muscles_length = System.Enum.GetValues(typeof(HumanBodyMuscles)).Length;
		int points_length = System.Enum.GetValues(typeof(HumanBodyPoints)).Length;
		Dictionary<float, float>[] result = new Dictionary<float, float>[muscles_length + points_length];
		for (int i = 0, i_max = muscles_length; i < i_max; ++i) {
			if (animator_utility.HasBoneIndex((HumanBodyMuscles)i)) {
				result[i] = new Dictionary<float, float>();
			}
		}
		for (int i = muscles_length, i_max = result.Length; i < i_max; ++i) {
			result[i] = new Dictionary<float, float>();
		}
		//アニメーションクリップの有効化
		animation[clip_name].weight = 1.0f;
		animation[clip_name].enabled = true;
		//初回サンプリング前
		start_cb(animation);
		//サンプリング
		var curves = AnimationUtility.GetAllCurves(animation[clip_name].clip, true);
		var keyframes_transforms = PigeonholeAnimationClipCurveData(curves);
		//時刻走査
		float frame_rate = animation[clip_name].clip.frameRate;
		foreach (var keyframe_transforms in keyframes_transforms) {
			float time = (keyframe_transforms.Key * frame_rate + 0.5f) / frame_rate; //フレーム単位に整形
			//プログレスバー表示
			var is_cancel = EditorUtility.DisplayCancelableProgressBar("Convert From LegacyAnimation To MusclesAnimation"
																	, string.Format("{0:#00.00%}  {1:##0.00}／{2:##0.00}  ({3:#0})"
																					, time / animation[clip_name].length
																					, time
																					, animation[clip_name].length
																					, keyframe_transforms.Value.Count()
																					)
																	, time / animation[clip_name].length
																	);
			if (is_cancel) {
				break;
			}
			//ポーズ適応
			animation[clip_name].time = time;
			var target_paths = keyframe_transforms.Value.Select(x=>x.Key);
			var add_bones = update_cb(animation, target_paths);
			//ボーン走査
			var bone_indexes = keyframe_transforms.Value.Select(x=>x.Key) //パスの取り出し
														.Select(x=>animator_utility.GetBoneIndexFromPath(x)) //ボーンインデックス変換
														.Where(x=>x.HasValue) //ボーンインデックス変換に失敗した対象の除去
														.Select(x=>x.Value) //Nullableオブジェクトからボーンインデックス取り出し
														.Concat(add_bones) //追加要望ボーンインデックスの連結
														.Distinct(); //重複削除
			foreach (var bone_index in bone_indexes) {
				Transform transform = animator_utility.GetTransformFromBoneIndex(bone_index);
				if (null != transform) {
					//トランスフォームが取得出来るなら
					//Muscle値作成
					Quaternion rotation = transform.rotation;
					var parent_bone_index_fuzzy = GetParentBoneIndex(bone_index);
					if (parent_bone_index_fuzzy.HasValue) {
						//親が居るなら
						//相対値の算出
						HumanBodyFullBones parent_bone_index = parent_bone_index_fuzzy.Value;
						Transform parent_transform = animator_utility.GetTransformFromBoneIndex(parent_bone_index);
						if (null != parent_transform) {
							//親のMuscle値に依る回転値を求め、それからの相対値を求める
							Quaternion parent_rotation = parent_transform.localRotation;
							parent_rotation = parent_transform.parent.rotation * parent_rotation;
							rotation = Quaternion.Inverse(parent_rotation) * rotation;
						}
					}
					//Muscle値変換
					var muscle_values = animator_utility.GetMuscleValueFromRotation(bone_index, rotation);
					for (int axis_index = 0, axis_index_max = 3; axis_index < axis_index_max; ++axis_index) {
						HumanBodyMuscles muscle_index = (HumanBodyMuscles)HumanTrait.MuscleFromBone((int)bone_index, axis_index);
						if ((uint)muscle_index < (uint)System.Enum.GetValues(typeof(HumanBodyMuscles)).Length) {
							result[(int)muscle_index].Add(time, muscle_values[axis_index]);
						}
					}
				}
			}
			{ //Points値作成
				var values = animator_utility.GetPointValue();
				for (int i = 0, i_max = values.Length; i < i_max; ++i) {
					int key_index = i + System.Enum.GetValues(typeof(HumanBodyMuscles)).Length;
					result[key_index].Add(time, values[i]);
				}
			}
		}
		//プログレスバー削除
		EditorUtility.ClearProgressBar();
		return result;
	}

	/// <summary>
	/// 複数のアニメーションカーブデータをトランスフォーム単位で整理する
	/// </summary>
	/// <returns>トランスフォーム単位のアニメーションカーブデータ</returns>
	/// <param name="curves">複数のアニメーションカーブデータ</param>
	private static Dictionary<float, Dictionary<string, PortableTransform>> PigeonholeAnimationClipCurveData(AnimationClipCurveData[] curves) {
		var position = new Dictionary<string, Dictionary<float, Vector3>>();
		var rotation = new Dictionary<string, Dictionary<float, Vector4>>(); //Quaternionを1要素ずつ設定すると正規化が行われてしまうので一旦Vector4に格納
		var scale = new Dictionary<string, Dictionary<float, Vector3>>();
		var time = new Dictionary<float, object>();
		foreach (var curve in curves) {
			string path = curve.path;
			if (!position.ContainsKey(path)) {
				position[path] = new Dictionary<float, Vector3>();
				rotation[path] = new Dictionary<float, Vector4>();
				scale[path] = new Dictionary<float, Vector3>();
			}
			foreach (var key in curve.curve.keys) {
				//指定時刻マーク
				time[key.time] = null;
				//指定時刻がまだ無ければ作成
				switch (curve.propertyName) {
				//位置
				case "m_LocalPosition.x": goto case "m_LocalPosition.z";
				case "m_LocalPosition.y": goto case "m_LocalPosition.z";
				case "m_LocalPosition.z":
					if (!position[path].ContainsKey(key.time)) {
						position[path][key.time] = Vector3.zero;
					}
					switch (curve.propertyName.Substring(16)) {
					case "x":
						position[path][key.time] = new Vector3(key.value                 , position[path][key.time].y, position[path][key.time].z);
						break;
					case "y":
						position[path][key.time] = new Vector3(position[path][key.time].x, key.value                 , position[path][key.time].z);
						break;
					case "z":
						position[path][key.time] = new Vector3(position[path][key.time].x, position[path][key.time].y, key.value                 );
						break;
					}
					break;
				//回転
				case "m_LocalRotation.x": goto case "m_LocalRotation.w";
				case "m_LocalRotation.y": goto case "m_LocalRotation.w";
				case "m_LocalRotation.z": goto case "m_LocalRotation.w";
				case "m_LocalRotation.w":
					if (!rotation[path].ContainsKey(key.time)) {
						var q = Quaternion.identity;
						rotation[path][key.time] = new Vector4(q.x, q.y, q.z, q.w);
					}
					switch (curve.propertyName.Substring(16)) {
					case "x":
						rotation[path][key.time] = new Vector4(key.value                 , rotation[path][key.time].y, rotation[path][key.time].z, rotation[path][key.time].w);
						break;
					case "y":
						rotation[path][key.time] = new Vector4(rotation[path][key.time].x, key.value                 , rotation[path][key.time].z, rotation[path][key.time].w);
						break;
					case "z":
						rotation[path][key.time] = new Vector4(rotation[path][key.time].x, rotation[path][key.time].y, key.value                 , rotation[path][key.time].w);
						break;
					case "w":
						rotation[path][key.time] = new Vector4(rotation[path][key.time].x, rotation[path][key.time].y, rotation[path][key.time].z, key.value                 );
						break;
					}
					break;
				//拡縮
				case "m_LocalScale.x": goto case "m_LocalScale.z";
				case "m_LocalScale.y": goto case "m_LocalScale.z";
				case "m_LocalScale.z":
					if (!scale[path].ContainsKey(key.time)) {
						scale[path][key.time] = Vector3.one;
					}
					switch (curve.propertyName.Substring(13)) {
					case "x":
						scale[path][key.time] = new Vector3(key.value              , scale[path][key.time].y, scale[path][key.time].z);
						break;
					case "y":
						scale[path][key.time] = new Vector3(scale[path][key.time].x, key.value              , scale[path][key.time].z);
						break;
					case "z":
						scale[path][key.time] = new Vector3(scale[path][key.time].x, scale[path][key.time].y, key.value              );
						break;
					}
					break;
				default:
					throw new System.ArgumentException();
				}
			}
		}
		//PortableTransform化
		var result = new Dictionary<float, Dictionary<string, PortableTransform>>();
		var times = time.Select(x=>x.Key).ToArray();
		System.Array.Sort(times);
		foreach (var t in times) {
			result.Add(t, new Dictionary<string, PortableTransform>());
			foreach (var n in position.Select(x=>x.Key)) {
				var has_position = position[n].ContainsKey(t);
				var has_rotation = rotation[n].ContainsKey(t);
				var has_scale = scale[n].ContainsKey(t);
				if (has_position || has_rotation || has_scale) {
					//指定時刻にキーフレームを持つパスが有るなら
					var transform = PortableTransform.identity;
					if (has_position) {
						transform.position = position[n][t];
					}
					if (has_rotation) {
						transform.rotation = new Quaternion(rotation[n][t].x, rotation[n][t].y, rotation[n][t].z, rotation[n][t].w);
					}
					if (has_scale) {
						transform.scale = scale[n][t];
					}
					result[t].Add(n, transform);
				}
			}
		}
		return result;
	}
	
	/// <summary>
	/// 人型アバター対応アニメーションクリップを作成する
	/// </summary>
	/// <returns>人型アバター対応アニメーションクリップ</returns>
	/// <param name="muscle_value_animation">Muscle値アニメーションの配列</param>
	private static AnimationClip CreateHumanMecanimAnimationClip(Dictionary<float, float>[] value_animations) {
		int muscles_length = System.Enum.GetValues(typeof(HumanBodyMuscles)).Length;
		AnimationClip result = new AnimationClip();
		for (int i = 0, i_max = value_animations.Length; i < i_max; ++i) {
			if (null != value_animations[i]) {
				var key_frames = value_animations[i].Select(x=>new Keyframe(x.Key, x.Value))
																		.ToArray();
				AnimationCurve curve = new AnimationCurve(key_frames);
				string attribute;
				if (i < muscles_length) {
					attribute = c_muscles_anim_attribute[i];
				} else {
					attribute = c_points_anim_attribute[i - muscles_length];
				}
#if !UNITY_4_2 //4.3以降
				AnimationUtility.SetEditorCurve(result
												, EditorCurveBinding.FloatCurve(""
																				, typeof(Animator)
																				, attribute)
												, curve
												);
#else
				AnimationUtility.SetEditorCurve(result
												, ""
												, typeof(Animator)
												, attribute
												, curve
												);
#endif
			}
		}
		//AnimationType人型アバター対応アニメーションクリップに設定
		AnimationUtility.SetAnimationType(result, ModelImporterAnimationType.Human);
		return result;
	}

	/// <summary>
	/// Muscleインデックス所持確認
	/// </summary>
	/// <returns>true:所持, false:未所持</returns>
	/// <param name="index">ボーンインデックス</param>
	private bool HasBoneIndex(HumanBodyMuscles index) {
		HumanBodyFullBones bone_index = (HumanBodyFullBones)HumanTrait.BoneFromMuscle((int)index);
		return HasBoneIndex(bone_index);
	}
	
	/// <summary>
	/// スケルトンインデックスからノードトランスフォームの取得
	/// </summary>
	/// <returns>ノードトランスフォーム</returns>
	/// <param name="index">スケルトンインデックス</param>
	private Transform GetTransformFromSkeletonIndex(int index) {
		Transform result = null;
		var path = GetPathFromSkeletonIndex(index);
		result = GetTransformFromPath(path);
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
	/// <param name="path">ノードパス</param>
	private Transform GetTransformFromPath(string path) {
		Transform result = null;
		if (!string.IsNullOrEmpty(path)) {
			result = animator_.transform.FindChild(path);
		}
		return result;
	}
	
	/// <summary>
	/// ノードパスからボーンインデックスの取得
	/// </summary>
	/// <returns>ボーンインデックス</returns>
	/// <param name="path">ノードパス</param>
	private HumanBodyFullBones? GetBoneIndexFromPath(string path) {
		HumanBodyFullBones? result = null;
		var bone_human = Enumerable.Range(0, bone_index_to_human_index_.Length)
									.Select(x=>new {bone_index = x, human_index = bone_index_to_human_index_[x]});
		var human_hash = Enumerable.Range(0, human_index_to_hash_.Length)
									.Select(x=>new {human_index = x, hash = human_index_to_hash_[x]});
		var bone_index_linq = hash_to_path_.Where(x=>x.Value == path)
											.Join(human_hash, x=>x.Key, y=>y.hash, (x,y)=>y.human_index)
											.Join(bone_human, x=>x, y=>y.human_index, (x,y)=>y.bone_index);
		if (0 < bone_index_linq.Count()) {
			result = (HumanBodyFullBones)bone_index_linq.First();
		}
		return result;
	}
	
	/// <summary>
	/// 親ボーンインデックスの取得
	/// </summary>
	/// <returns>親ボーンインデックス</returns>
	/// <param name="inex">ボーンインデックス</param>
	private static HumanBodyFullBones? GetParentBoneIndex(HumanBodyFullBones index) {
		HumanBodyFullBones? result = null;
		if ((uint)index < (uint)c_parent_bone_index.Length) {
			var parent_index = c_parent_bone_index[(int)index];
			if ((uint)parent_index < (uint)System.Enum.GetValues(typeof(HumanBodyFullBones)).Length) {
				result = parent_index;
			}
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
		
		//AxesArrayはヒューマンインデックスから未割当ボーン分を詰めた独自のインデックス体系を持つ
		//それをボーンインデックスへと変換する為の配列を作る
		var axes_array_index_to_bone_index_ = Enumerable.Range(0, bone_index_to_human_index_.Length) //ボーンインデックス-ヒューマンインデックス変換デーブル分の配列を用意する
														.Select(x=>new {bone_index = x, human_index = bone_index_to_human_index_[x]}) //ボーンインデックス-ヒューマンインデックス変換辞書らしき物を作る
														.Where(x=>-1 != x.human_index) //未割当ボーン分を除外
														.OrderBy(x=>x.human_index) //ヒューマンインデックスで並び替え
														.Select(x=>x.bone_index) //ボーンインデックス取り出し
														.ToArray(); //配列化
		
		AxesInformation[] result = Enumerable.Repeat(new AxesInformation(), System.Enum.GetValues(typeof(HumanBodyFullBones)).Length)
											.ToArray();
		for (int i = 0, i_max = sp_axes_array.arraySize; i < i_max; ++i) {
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
			
			int bone_index = axes_array_index_to_bone_index_[i];
			result[bone_index] = axes_information;
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
		
		public static PortableTransform identity {get{
			PortableTransform result = new PortableTransform();
			result.position = Vector3.zero;
			result.rotation = Quaternion.identity;
			result.scale = Vector3.one;
			return result;
		}}
	}
	PortableTransform[] human_transform_for_t_pose_ = null;
	
	private static	MethodInfo	AvatarUtility_SetHumanPose_;	//UnityEditor.dll/UnityEditor.AvatarUtility/SetHumanPoseのリフレクションキャッシュ
	
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
	
	/// <summary>
	/// Pointsインデックス
	/// </summary>
	public enum HumanBodyPoints {
		RootPositionX,
		RootPositionY,
		RootPositionZ,
		RootRotationX,
		RootRotationY,
		RootRotationZ,
		RootRotationW,
		LeftFootPositionX,
		LeftFootPositionY,
		LeftFootPositionZ,
		LeftFootRotationX,
		LeftFootRotationY,
		LeftFootRotationZ,
		LeftFootRotationW,
		RightFootPositionX,
		RightFootPositionY,
		RightFootPositionZ,
		RightFootRotationX,
		RightFootRotationY,
		RightFootRotationZ,
		RightFootRotationW,
		LeftHandPositionX,
		LeftHandPositionY,
		LeftHandPositionZ,
		LeftHandRotationX,
		LeftHandRotationY,
		LeftHandRotationZ,
		LeftHandRotationW,
		RightHandPositionX,
		RightHandPositionY,
		RightHandPositionZ,
		RightHandRotationX,
		RightHandRotationY,
		RightHandRotationZ,
		RightHandRotationW,
	}
	
	private static readonly string[] c_points_anim_attribute = new [] {
		"RootT.x",
		"RootT.y",
		"RootT.z",
		"RootQ.x",
		"RootQ.y",
		"RootQ.z",
		"RootQ.w",
		"LeftFootT.x",
		"LeftFootT.y",
		"LeftFootT.z",
		"LeftFootQ.x",
		"LeftFootQ.y",
		"LeftFootQ.z",
		"LeftFootQ.w",
		"RightFootT.x",
		"RightFootT.y",
		"RightFootT.z",
		"RightFootQ.x",
		"RightFootQ.y",
		"RightFootQ.z",
		"RightFootQ.w",
		"LeftHandT.x",
		"LeftHandT.y",
		"LeftHandT.z",
		"LeftHandQ.x",
		"LeftHandQ.y",
		"LeftHandQ.z",
		"LeftHandQ.w",
		"RightHandT.x",
		"RightHandT.y",
		"RightHandT.z",
		"RightHandQ.x",
		"RightHandQ.y",
		"RightHandQ.z",
		"RightHandQ.w",
	};

	private static readonly HumanBodyFullBones[] c_parent_bone_index = new [] {
		(HumanBodyFullBones)(-1),			//Hips,
		HumanBodyFullBones.Hips,			//LeftUpperLeg,
		HumanBodyFullBones.Hips,			//RightUpperLeg,
		HumanBodyFullBones.LeftUpperLeg,	//LeftLowerLeg,
		HumanBodyFullBones.RightUpperLeg,	//RightLowerLeg,
		HumanBodyFullBones.LeftLowerLeg,	//LeftFoot,
		HumanBodyFullBones.RightLowerLeg,	//RightFoot,
		HumanBodyFullBones.Hips,			//Spine,
		HumanBodyFullBones.Spine,			//Chest,
		HumanBodyFullBones.Chest,			//Neck,
		HumanBodyFullBones.Neck,			//Head,
		HumanBodyFullBones.Chest,			//LeftShoulder,
		HumanBodyFullBones.Chest,			//RightShoulder,
		HumanBodyFullBones.LeftShoulder,	//LeftUpperArm,
		HumanBodyFullBones.RightShoulder,	//RightUpperArm,
		HumanBodyFullBones.LeftUpperArm,	//LeftLowerArm,
		HumanBodyFullBones.RightUpperArm,	//RightLowerArm,
		HumanBodyFullBones.LeftLowerArm,	//LeftHand,
		HumanBodyFullBones.RightLowerArm,	//RightHand,
		HumanBodyFullBones.LeftFoot,		//LeftToes,
		HumanBodyFullBones.RightFoot,		//RightToes,
		HumanBodyFullBones.Head,			//LeftEye,
		HumanBodyFullBones.Head,			//RightEye,
		HumanBodyFullBones.Head,			//Jaw,
		
		HumanBodyFullBones.LeftHand,				//LeftThumbProximal,
		HumanBodyFullBones.LeftThumbProximal,		//LeftThumbIntermediate,
		HumanBodyFullBones.LeftThumbIntermediate,	//LeftThumbDistal,
		HumanBodyFullBones.LeftHand,				//LeftIndexProximal,
		HumanBodyFullBones.LeftIndexProximal,		//LeftIndexIntermediate,
		HumanBodyFullBones.LeftIndexIntermediate,	//LeftIndexDistal,
		HumanBodyFullBones.LeftHand,				//LeftMiddleProximal,
		HumanBodyFullBones.LeftMiddleProximal,		//LeftMiddleIntermediate,
		HumanBodyFullBones.LeftMiddleIntermediate,	//LeftMiddleDistal,
		HumanBodyFullBones.LeftHand,				//LeftRingProximal,
		HumanBodyFullBones.LeftRingProximal,		//LeftRingIntermediate,
		HumanBodyFullBones.LeftRingIntermediate,	//LeftRingDistal,
		HumanBodyFullBones.LeftHand,				//LeftLittleProximal,
		HumanBodyFullBones.LeftLittleProximal,		//LeftLittleIntermediate,
		HumanBodyFullBones.LeftLittleIntermediate,	//LeftLittleDistal,
		HumanBodyFullBones.RightHand,				//RightThumbProximal,
		HumanBodyFullBones.RightThumbProximal,		//RightThumbIntermediate,
		HumanBodyFullBones.RightThumbIntermediate,	//RightThumbDistal,
		HumanBodyFullBones.RightHand,				//RightIndexProximal,
		HumanBodyFullBones.RightIndexProximal,		//RightIndexIntermediate,
		HumanBodyFullBones.RightIndexIntermediate,	//RightIndexDistal,
		HumanBodyFullBones.RightHand,				//RightMiddleProximal,
		HumanBodyFullBones.RightMiddleProximal,		//RightMiddleIntermediate,
		HumanBodyFullBones.RightMiddleIntermediate,	//RightMiddleDistal,
		HumanBodyFullBones.RightHand,				//RightRingProximal,
		HumanBodyFullBones.RightRingProximal,		//RightRingIntermediate,
		HumanBodyFullBones.RightRingIntermediate,	//RightRingDistal,
		HumanBodyFullBones.RightHand,				//RightLittleProximal,
		HumanBodyFullBones.RightLittleProximal,		//RightLittleIntermediate,
		HumanBodyFullBones.RightLittleIntermediate,	//RightLittleDistal,
	};
}
