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
					var muscle_values = GetMuscleValue(bone_index, bone_transform.localRotation);
					foreach (var muscle_value in muscle_values) {
						result[(int)muscle_value.Key] = muscle_value.Value;
					}
				}
			}
		}
		return result;
	}
	
	/// <summary>
	/// Muscle値の取得
	/// </summary>
	/// <returns>Muscleインデックスと値の辞書</returns>
	/// <param name="index">ボーンインデックス</param>
	/// <param name="rotation">回転値</param>
	private Dictionary<HumanBodyMuscles, float> GetMuscleValue(HumanBodyFullBones index, Quaternion rotation) {
		Dictionary<HumanBodyMuscles, float> result = new Dictionary<HumanBodyMuscles, float>();
		//Muscle値算出
		AxesInformation axes_information = GetAxesInformation(index);
		Vector3 rotation_avatar = (Quaternion.Inverse(axes_information.pre_quaternion) * rotation * axes_information.post_quaternion).eulerAngles;
		//軸操作
		for (int axis_index = 0, axis_index_max = 3; axis_index < axis_index_max; ++axis_index) {
			HumanBodyMuscles muscle_index = (HumanBodyMuscles)HumanTrait.MuscleFromBone((int)index, axis_index);
			if ((uint)muscle_index < (uint)System.Enum.GetValues(typeof(HumanBodyMuscles)).Length) {
				float value = rotation_avatar[axis_index];
				value = ((value < -180.0f)? value + 360.0f: ((180.0f < value)? value - 360.0f: value)); //範囲を-180.0f～180.0fに収める
				value *= axes_information.sign[axis_index];
				if (value < 0) {
					//標準ポーズより小さいなら
					value = value / (axes_information.limit.min[axis_index] * -Mathf.Rad2Deg); 
				} else {
					//標準ポーズより大きいなら
					value = value / (axes_information.limit.max[axis_index] * Mathf.Rad2Deg);
				}
				value = Mathf.Clamp(value, -1.0f, 1.0f);
				result.Add (muscle_index, value);
			}
		}
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
		result = GetTransformFromPath(animator_, path);
		return result;
	}
	
	/// <summary>
	/// ルートボーンのノードトランスフォームの取得
	/// </summary>
	/// <returns>ノードトランスフォーム</returns>
	public Transform GetTransformFromRootBone() {
		Transform result = null;
		var path = GetPathFromHumanIndex(0);
		result = GetTransformFromPath(animator_, path);
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
	/// アニメーションクリップをアバターに対応させる
	/// </summary>
	/// <returns>アバター対応アニメーションクリップ</returns>
	/// <param name="clip">アバター未対応アニメーションクリップ</param>
	/// <param name="start">人型アバターサンプリング時の初回更新関数</param>
	/// <param name="update">人型アバターサンプリング時の更新関数(内部でAnimation.Sample()を呼んで下さい)</param>
	public AnimationClip AdaptAnimationClip(AnimationClip clip, System.Action<Animation> start = null, System.Action<Animation> update = null) {
		AnimationClip result = null;
		if (animator_.avatar.isHuman && !clip.isHumanMotion) {
			//アバターが人型 かつ アニメーションクリップが人型未対応なら
			//サンプリング時にトランスフォームが破壊されるのでダミーを作成してそちらで行う
			GameObject dummy_game_object = CreateAnimationClipGameObject(animator_, clip, "clip");
			//初回更新関数と更新関数の生成
			if (null == start) {
				start = x=>{};
			}
			if (null == update) {
				update = x=>x.Sample();
			}
			//フレームレートに準じてサンプリング
			Animation dummy_animation = dummy_game_object.GetComponent<Animation>();
			float delta_time = 1.0f; //1.0f / clip.frameRate;
			start(dummy_animation);
			var muscle_value_animations = CreateMuscleValueAnimation(dummy_animation, "clip", delta_time, update);
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
	/// <param name="sample_cb">サンプリングコールバック</param>
	private static Dictionary<float, float>[] CreateMuscleValueAnimation(Animation animation, string clip_name, float delta_time, System.Action<Animation> sample_cb) {
		AnimatorUtility animator_utility = new AnimatorUtility(animation.gameObject.GetComponent<Animator>());
		//アニメーションクリップの有効化
		animation[clip_name].weight = 1.0f;
		animation[clip_name].enabled = true;
		//フレームレートに準じてサンプリング
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
		for (float t = 0.0f, t_max = animation[clip_name].length; t < t_max; t += delta_time) {
			animation[clip_name].time = t;
			sample_cb(animation);
			float[] muscle_value = animator_utility.GetMuscleValue();
			for (int i = 0, i_max = muscle_value.Length; i < i_max; ++i) {
				if (null != result[i]) {
					result[i].Add(t, muscle_value[i]);
				}
			}
			float[] point_value = animator_utility.GetPointValue();
			for (int i = 0, i_max = point_value.Length; i < i_max; ++i) {
				result[i + muscles_length].Add(t, point_value[i]);
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
}
