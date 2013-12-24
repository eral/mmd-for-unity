using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MMD.VMD;

namespace MMD
{
	public class VMDConverter
	{
		/// <summary>
		/// AnimationClipを作成する
		/// </summary>
		/// <param name='name'>内部形式データ</param>
		/// <param name='assign_pmd'>使用するPMDのGameObject</param>
		public static AnimationClip CreateAnimationClip(VMDFormat format, GameObject assign_pmd) {
			VMDConverter converter = new VMDConverter();
			return converter.CreateAnimationClip_(format, assign_pmd);
		}

		/// <summary>
		/// デフォルトコンストラクタ
		/// </summary>
		/// <remarks>
		/// ユーザーに依るインスタンス作成を禁止する
		/// </remarks>
		private VMDConverter() {}

		// クリップをアニメーションに登録する
		private AnimationClip CreateAnimationClip_(MMD.VMD.VMDFormat format, GameObject assign_pmd)
		{
			//スケール設定
			scale_ = 1.0f;
			if (assign_pmd) {
				MMDEngine engine = assign_pmd.GetComponent<MMDEngine>();
				if (engine) {
					scale_ = engine.scale;
				}
			}

			//Animation anim = assign_pmd.GetComponent<Animation>();
			
			// クリップの作成
			AnimationClip clip = new AnimationClip();
			clip.name = assign_pmd.name + "_" + format.name;
			
			Dictionary<string, string> bone_path = new Dictionary<string, string>();
			Dictionary<string, GameObject> gameobj = new Dictionary<string, GameObject>();
			GetGameObjects(gameobj, assign_pmd);		// 親ボーン下のGameObjectを取得
			FullSearchBonePath(assign_pmd.transform, bone_path);
			FullEntryBoneAnimation(format, clip, bone_path, gameobj);

			CreateKeysForSkin(format, clip);	// 表情の追加
			
			SetAnimationType(clip, assign_pmd); //アニメーションタイプの設定
			
			return clip;
		}

		// ベジェハンドルを取得する
		// 0～127の値を 0f～1fとして返す
		static Vector2 GetBezierHandle(byte[] interpolation, int type, int ab)
		{
			// 0=X, 1=Y, 2=Z, 3=R
			// abはa?かb?のどちらを使いたいか
			Vector2 bezierHandle = new Vector2((float)interpolation[ab*8+type], (float)interpolation[ab*8+4+type]);
			return bezierHandle/127f;
		}
		// p0:(0f,0f),p3:(1f,1f)のベジェ曲線上の点を取得する
		// tは0～1の範囲
		static Vector2 SampleBezier(Vector2 bezierHandleA, Vector2 bezierHandleB, float t)
		{
			Vector2 p0 = Vector2.zero;
			Vector2 p1 = bezierHandleA;
			Vector2 p2 = bezierHandleB;
			Vector2 p3 = new Vector2(1f,1f);
			
			Vector2 q0 = Vector2.Lerp(p0, p1, t);
			Vector2 q1 = Vector2.Lerp(p1, p2, t);
			Vector2 q2 = Vector2.Lerp(p2, p3, t);
			
			Vector2 r0 = Vector2.Lerp(q0, q1, t);
			Vector2 r1 = Vector2.Lerp(q1, q2, t);
			
			Vector2 s0 = Vector2.Lerp(r0, r1, t);
			return s0;
		}
		// 補間曲線が線形補間と等価か
		static bool IsLinear(byte[] interpolation, int type)
		{
			byte ax=interpolation[0*8+type];
			byte ay=interpolation[0*8+4+type];
			byte bx=interpolation[1*8+type];
			byte by=interpolation[1*8+4+type];
			return (ax == ay) && (bx == by);
		}
		// 補間曲線の近似のために追加するキーフレームを含めたキーフレーム数を取得する
		int GetKeyframeCount(List<MMD.VMD.VMDFormat.Motion> mlist, int type)
		{
			return mlist.Count;
		}
		//キーフレームが1つの時、ダミーキーフレームを追加する
		void AddDummyKeyframe(ref Keyframe[] keyframes)
		{
			if(keyframes.Length==1)
			{
				Keyframe[] newKeyframes=new Keyframe[2];
				newKeyframes[0]=keyframes[0];
				newKeyframes[1]=keyframes[0];
				newKeyframes[1].time+=0.001f/60f;//1[ms]
				newKeyframes[0].outTangent=0f;
				newKeyframes[1].inTangent=0f;
				keyframes=newKeyframes;
			}
		}
		// 任意の型のvalueを持つキーフレーム
		abstract class CustomKeyframe<Type>
		{
			public CustomKeyframe(float time,Type value, Vector2 in_vector, Vector2 out_vector)
			{
				this.time=time;
				this.value=value;
				this.in_vector=in_vector;
				this.out_vector=out_vector;
			}
			public float time{ get; set; }
			public Type value{ get; set; }
			public Vector2 in_vector{ get; set; }
			public Vector2 out_vector{ get; set; }
		}
		// float型のvalueを持つキーフレーム
		class FloatKeyframe:CustomKeyframe<float>
		{
			public FloatKeyframe(float time,float value, Vector2 in_vector, Vector2 out_vector):base(time,value,in_vector,out_vector)
			{
			}
			// 線形補間
			public static FloatKeyframe Lerp(FloatKeyframe from, FloatKeyframe to,Vector2 t)
			{
				return new FloatKeyframe(
					Mathf.Lerp(from.time,to.time,t.x),
					Mathf.Lerp(from.value,to.value,t.y),
					Vector2.zero,
					Vector2.zero
				);
			}
			// ベジェを線形補間で近似したキーフレームを追加する
			public static void AddBezierKeyframes(byte[] interpolation, int type,
				FloatKeyframe prev_keyframe,FloatKeyframe cur_keyframe,
				ref FloatKeyframe[] keyframes,ref int index)
			{
				keyframes[index++]=cur_keyframe;
			}
		}
		// Quaternion型のvalueを持つキーフレーム
		class QuaternionKeyframe:CustomKeyframe<Quaternion>
		{
			public QuaternionKeyframe(float time,Quaternion value, Vector2 in_vector, Vector2 out_vector):base(time,value,in_vector,out_vector)
			{
			}
			// 線形補間
			public static QuaternionKeyframe Lerp(QuaternionKeyframe from, QuaternionKeyframe to,Vector2 t)
			{
				return new QuaternionKeyframe(
					Mathf.Lerp(from.time,to.time,t.x),
					Quaternion.Slerp(from.value,to.value,t.y),
					Vector2.zero,
					Vector2.zero
				);
			}
			// ベジェを線形補間で近似したキーフレームを追加する
			public static void AddBezierKeyframes(byte[] interpolation, int type,
				QuaternionKeyframe prev_keyframe,QuaternionKeyframe cur_keyframe,
				ref QuaternionKeyframe[] keyframes,ref int index)
			{
				keyframes[index++]=cur_keyframe;
			}
			
		}
		
		//移動の線形補間用tangentを求める 
		float GetLinearTangentForPosition(Keyframe from_keyframe,Keyframe to_keyframe)
		{
			return (to_keyframe.value-from_keyframe.value)/(to_keyframe.time-from_keyframe.time);
		}
		//-359～+359度の範囲を等価な0～359度へ変換する。
		float Mod360(float angle)
		{
			//剰余演算の代わりに加算にする
			return (angle<0)?(angle+360f):(angle);
		}
		//回転の線形補間用tangentを求める
		float GetLinearTangentForRotation(Keyframe from_keyframe,Keyframe to_keyframe)
		{
			float tv=Mod360(to_keyframe.value);
			float fv=Mod360(from_keyframe.value);
			float delta_value=Mod360(tv-fv);
			//180度を越える場合は逆回転
			if(delta_value<180f)
			{ 
				return delta_value/(to_keyframe.time-from_keyframe.time);
			}
			else
			{
				return (delta_value-360f)/(to_keyframe.time-from_keyframe.time);
			}
		}
		//アニメーションエディタでBothLinearを選択したときの値
		private const int TangentModeBothLinear=21;
		
		//UnityのKeyframeに変換する（回転用）
		void ToKeyframesForRotation(QuaternionKeyframe[] custom_keys,ref Keyframe[] rx_keys,ref Keyframe[] ry_keys,ref Keyframe[] rz_keys)
		{
			var axis_keys = new Keyframe[3][];
			for (int axis = 0, axis_max = axis_keys.Length; axis < axis_max; ++axis) {
				axis_keys[axis] = custom_keys.Select(x=>new Keyframe(x.time, x.value.eulerAngles[axis])).ToArray();
				var keys = axis_keys[axis];
				
				for (int i = 0, i_max = keys.Length - 1; i < i_max; i++) {
					float base_tangent = GetLinearTangentForRotation(keys[i], keys[i+1]);
					
					CurveUtility_SetKeyBroken(ref keys[i], true);
					CurveUtility_SetKeyTangentMode(ref keys[i], true, CurveUtility_TangentMode.Editable);
					keys[i].outTangent = custom_keys[i].in_vector.y / custom_keys[i].in_vector.x * base_tangent;
					
					CurveUtility_SetKeyBroken(ref keys[i+1], false);
					CurveUtility_SetKeyTangentMode(ref keys[i+1], false, CurveUtility_TangentMode.Editable);
					keys[i+1].inTangent = (1.0f - custom_keys[i].out_vector.y) / (1.0f - custom_keys[i].out_vector.x) * base_tangent;
				}
				
				AddDummyKeyframe(ref keys);
			}
			rx_keys = axis_keys[0];
			ry_keys = axis_keys[1];
			rz_keys = axis_keys[2];
		}
		
		
		// あるボーンに含まれるキーフレを抽出
		// これは回転のみ
		void CreateKeysForRotation(MMD.VMD.VMDFormat format, AnimationClip clip, string current_bone, string bone_path)
		{
			try 
			{
				List<MMD.VMD.VMDFormat.Motion> mlist = format.motion_list.motion[current_bone];
				int keyframeCount = GetKeyframeCount(mlist, 3);
				
				QuaternionKeyframe[] r_keys = new QuaternionKeyframe[keyframeCount];
				QuaternionKeyframe r_prev_key=null;
				int ir=0;
				for (int i = 0; i < mlist.Count; i++)
				{
					float tick = mlist[i].flame_no * c_frame_to_time;
					
					Quaternion rotation=mlist[i].rotation;
					QuaternionKeyframe r_cur_key=new QuaternionKeyframe(tick, rotation, GetBezierHandle(mlist[i].interpolation,3,0), GetBezierHandle(mlist[i].interpolation,3,1));
					QuaternionKeyframe.AddBezierKeyframes(mlist[i].interpolation,3,r_prev_key,r_cur_key,ref r_keys,ref ir);
					r_prev_key=r_cur_key;
				}
				
				Keyframe[] rx_keys=null;
				Keyframe[] ry_keys=null;
				Keyframe[] rz_keys=null;
				ToKeyframesForRotation(r_keys, ref rx_keys, ref ry_keys, ref rz_keys);
				AnimationCurve curve_x = new AnimationCurve(rx_keys);
				AnimationCurve curve_y = new AnimationCurve(ry_keys);
				AnimationCurve curve_z = new AnimationCurve(rz_keys);
				// ここで回転オイラー角をセット（補間はクォータニオン）
#if !UNITY_4_2 //4.3以降
				AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(bone_path,typeof(Transform),"localEulerAngles.x"),curve_x);
				AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(bone_path,typeof(Transform),"localEulerAngles.y"),curve_y);
				AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(bone_path,typeof(Transform),"localEulerAngles.z"),curve_z);
#else
				AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"localEulerAngles.x",curve_x);
				AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"localEulerAngles.y",curve_y);
				AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"localEulerAngles.z",curve_z);
#endif

			}
			catch (KeyNotFoundException)
			{
				//Debug.LogError("互換性のないボーンが読み込まれました:" + bone_path);
			}
		}
		//UnityのKeyframeに変換する（移動用）
		Keyframe[] ToKeyframesForLocation(FloatKeyframe[] custom_keys)
		{
			Keyframe[] result = custom_keys.Select(x=>new Keyframe(x.time, x.value)).ToArray();
			
			for (int i = 0, i_max = result.Length - 1; i < i_max; i++) {
				float base_tangent = GetLinearTangentForPosition(result[i], result[i+1]);
			
				CurveUtility_SetKeyBroken(ref result[i], true);
				CurveUtility_SetKeyTangentMode(ref result[i], true, CurveUtility_TangentMode.Editable);
				result[i].outTangent = custom_keys[i].in_vector.y / custom_keys[i].in_vector.x * base_tangent;
				
				CurveUtility_SetKeyBroken(ref result[i+1], false);
				CurveUtility_SetKeyTangentMode(ref result[i+1], false, CurveUtility_TangentMode.Editable);
				result[i+1].inTangent = (1.0f - custom_keys[i].out_vector.y) / (1.0f - custom_keys[i].out_vector.x) * base_tangent;
			}
			
			AddDummyKeyframe(ref result);
			return result;
		}
		// 移動のみの抽出
		void CreateKeysForLocation(MMD.VMD.VMDFormat format, AnimationClip clip, string current_bone, string bone_path, GameObject current_obj = null)
		{
			try
			{
				Vector3 default_position = Vector3.zero;
				if(current_obj != null)
					default_position = current_obj.transform.localPosition;
				
				List<MMD.VMD.VMDFormat.Motion> mlist = format.motion_list.motion[current_bone];
				
				int keyframeCountX = GetKeyframeCount(mlist, 0);
				int keyframeCountY = GetKeyframeCount(mlist, 1); 
				int keyframeCountZ = GetKeyframeCount(mlist, 2);
				
				FloatKeyframe[] lx_keys = new FloatKeyframe[keyframeCountX];
				FloatKeyframe[] ly_keys = new FloatKeyframe[keyframeCountY];
				FloatKeyframe[] lz_keys = new FloatKeyframe[keyframeCountZ];
				
				FloatKeyframe lx_prev_key=null;
				FloatKeyframe ly_prev_key=null;
				FloatKeyframe lz_prev_key=null;
				int ix=0;
				int iy=0;
				int iz=0;
				for (int i = 0; i < mlist.Count; i++)
				{
					float tick = mlist[i].flame_no * c_frame_to_time;
					
					FloatKeyframe lx_cur_key=new FloatKeyframe(tick,mlist[i].location.x * scale_ + default_position.x, GetBezierHandle(mlist[i].interpolation,0,0), GetBezierHandle(mlist[i].interpolation,0,1));
					FloatKeyframe ly_cur_key=new FloatKeyframe(tick,mlist[i].location.y * scale_ + default_position.y, GetBezierHandle(mlist[i].interpolation,1,0), GetBezierHandle(mlist[i].interpolation,1,1));
					FloatKeyframe lz_cur_key=new FloatKeyframe(tick,mlist[i].location.z * scale_ + default_position.z, GetBezierHandle(mlist[i].interpolation,2,0), GetBezierHandle(mlist[i].interpolation,2,1));
					
					// 各軸別々に補間が付いてる
					FloatKeyframe.AddBezierKeyframes(mlist[i].interpolation,0,lx_prev_key,lx_cur_key,ref lx_keys,ref ix);
					FloatKeyframe.AddBezierKeyframes(mlist[i].interpolation,1,ly_prev_key,ly_cur_key,ref ly_keys,ref iy);
					FloatKeyframe.AddBezierKeyframes(mlist[i].interpolation,2,lz_prev_key,lz_cur_key,ref lz_keys,ref iz);
					
					lx_prev_key=lx_cur_key;
					ly_prev_key=ly_cur_key;
					lz_prev_key=lz_cur_key;
				}
				
				// 回転ボーンの場合はデータが入ってないはず
				if (mlist.Count != 0)
				{
					AnimationCurve curve_x = new AnimationCurve(ToKeyframesForLocation(lx_keys));
					AnimationCurve curve_y = new AnimationCurve(ToKeyframesForLocation(ly_keys));
					AnimationCurve curve_z = new AnimationCurve(ToKeyframesForLocation(lz_keys));
#if !UNITY_4_2 //4.3以降
					AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(bone_path,typeof(Transform),"m_LocalPosition.x"),curve_x);
					AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(bone_path,typeof(Transform),"m_LocalPosition.y"),curve_y);
					AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(bone_path,typeof(Transform),"m_LocalPosition.z"),curve_z);
#else
					AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"m_LocalPosition.x",curve_x);
					AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"m_LocalPosition.y",curve_y);
					AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"m_LocalPosition.z",curve_z);
#endif
				}
			}
			catch (KeyNotFoundException)
			{
				//Debug.LogError("互換性のないボーンが読み込まれました:" + current_bone);
			}
		}

		void CreateKeysForSkin(MMD.VMD.VMDFormat format, AnimationClip clip)
		{
			foreach (var skin in format.skin_list.skin) {
				Keyframe[] keyframe = skin.Value.Select(x=>new Keyframe(x.flame_no * c_frame_to_time, x.weight)).ToArray();

				for (int i = 0, i_max = keyframe.Length - 1; i < i_max; i++) {
					float delta_time = keyframe[i+1].time - keyframe[i].time;
					float delta_value = keyframe[i+1].value - keyframe[i].value;
					float base_tangent = delta_value / delta_time;
					
					CurveUtility_SetKeyBroken(ref keyframe[i], true);
					CurveUtility_SetKeyTangentMode(ref keyframe[i], true, CurveUtility_TangentMode.Editable);
					keyframe[i].outTangent = base_tangent;
					
					CurveUtility_SetKeyBroken(ref keyframe[i+1], false);
					CurveUtility_SetKeyTangentMode(ref keyframe[i+1], false, CurveUtility_TangentMode.Editable);
					keyframe[i+1].inTangent = base_tangent;
				}
				
				AddDummyKeyframe(ref keyframe);

				// Z軸移動にキーフレームを打つ
				AnimationCurve curve = new AnimationCurve(keyframe);
#if !UNITY_4_2 //4.3以降
				AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("Expression/" + skin.Key,typeof(Transform),"m_LocalPosition.z"),curve);
#else
				AnimationUtility.SetEditorCurve(clip,"Expression/" + skin.Key,typeof(Transform),"m_LocalPosition.z",curve);
#endif

			}
		}
		
		// ボーンのパスを取得する
		string GetBonePath(Transform transform)
		{
			string buf;
			if (transform.parent == null)
				return transform.name;
			else 
				buf = GetBonePath(transform.parent);
			return buf + "/" + transform.name;
		}
		
		// ボーンの子供を再帰的に走査
		void FullSearchBonePath(Transform transform, Dictionary<string, string> dic)
		{
			int count = transform.childCount;
			for (int i = 0; i < count; i++)
			{
				Transform t = transform.GetChild(i);
				FullSearchBonePath(t, dic);
			}
			
			// オブジェクト名が足されてしまうので抜く
			string buf = "";
			string[] spl = GetBonePath(transform).Split('/');
			for (int i = 1; i < spl.Length-1; i++)
				buf += spl[i] + "/";
			buf += spl[spl.Length-1];

			try
			{
				dic.Add(transform.name, buf);
			}
			catch (System.ArgumentException arg)
			{
				Debug.Log(arg.Message);
				Debug.Log("An element with the same key already exists in the dictionary. -> " + transform.name);
			}

			// dicには全てのボーンの名前, ボーンのパス名が入る
		}
		
		void FullEntryBoneAnimation(MMD.VMD.VMDFormat format, AnimationClip clip, Dictionary<string, string> dic, Dictionary<string, GameObject> obj)
		{
			foreach (KeyValuePair<string, string> p in dic)	// keyはtransformの名前, valueはパス
			{
				// 互いに名前の一致する場合にRigidbodyが存在するか調べたい
				GameObject current_obj = null;
				if(obj.ContainsKey(p.Key)){
					current_obj = obj[p.Key];
					
					// Rigidbodyがある場合はキーフレの登録を無視する
					var rigid = current_obj.GetComponent<Rigidbody>();
					if (rigid != null && !rigid.isKinematic)
					{
						continue;
					}
				}
				
				// キーフレの登録
				CreateKeysForLocation(format, clip, p.Key, p.Value, current_obj);
				CreateKeysForRotation(format, clip, p.Key, p.Value);
			}
		}

		// とりあえず再帰的に全てのゲームオブジェクトを取得する
		void GetGameObjects(Dictionary<string, GameObject> obj, GameObject assign_pmd)
		{
			for (int i = 0; i < assign_pmd.transform.childCount; i++)
			{
				var transf = assign_pmd.transform.GetChild(i);
				try
				{
					obj.Add(transf.name, transf.gameObject);
				}
				catch (System.ArgumentException arg)
				{
					Debug.Log(arg.Message);
					Debug.Log("An element with the same key already exists in the dictionary. -> " + transf.name);
				}

				if (transf == null) continue;		// ストッパー
				GetGameObjects(obj, transf.gameObject);
			}
		}
		
		/// <summary>
		/// KeyframeのTangentMode値
		/// </summary>
		[System.Flags]
		enum CurveUtility_TangentMode
		{
			Editable = 0,
			Smooth = 1,
			Linear = 2,
			Stepped = 3
		}
		
		/// <summary>
		/// キーフレームのモード設定
		/// </summary>
		/// <param name='key'>設定するキーフレーム</param>
		/// <param name='is_right'>設定する方向は右か</param>
		/// <param name='mode'>設定するモード</param>
		static void CurveUtility_SetKeyTangentMode(ref Keyframe key, bool is_right, CurveUtility_TangentMode mode)
		{
#if false
			int left_right = ((is_right)? 1: 0);

			Types.GetType("UnityEditor.CurveUtility", "UnityEditor.dll")
				.InvokeMember("SetKeyTangentMode"
							, BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod
							, null
							, null
							, new object[]{key, left_right, mode}
				);
#else
			if ( !is_right ) {
				key.tangentMode &= -7;
				key.tangentMode |= (int)mode << 1;
			} else {
				key.tangentMode &= -25;
				key.tangentMode |= (int)mode << 3;
			}
#endif
		}
		
		/// <summary>
		/// キーフレームの破壊設定
		/// </summary>
		/// <param name='key'>設定するキーフレーム</param>
		/// <param name='is_right'>破壊されているか</param>
		static void CurveUtility_SetKeyBroken(ref Keyframe key, bool is_break)
		{
#if false
			System.Type t = Types.GetType("UnityEditor.CurveUtility", "UnityEditor.dll");
			MethodInfo mi = t.GetMethod("SetKeyBroken"
										, BindingFlags.Public | BindingFlags.Static
										);
			mi.Invoke(null, new object[]{key, is_break});
#else
			key.tangentMode &= -2;
			if (is_break) {
				key.tangentMode |= 1;
			}
#endif
		}

		/// <summary>
		/// アニメーションタイプの設定
		/// </summary>
		/// <param name="clip">設定するアニメーションクリップ.</param>
		/// <param name="engine">設定の為に参照するAnimatorを持つゲームオブジェクト</param>
		static void SetAnimationType(AnimationClip clip, GameObject game_object)
		{
			ModelImporterAnimationType animation_type;
			Animator animator = game_object.GetComponent<Animator>();
			if (null == animator) {
				animation_type = ModelImporterAnimationType.Legacy;
			} else if ((null == animator.avatar) && animator.avatar.isHuman) {
				animation_type = ModelImporterAnimationType.Human;
			} else {
				animation_type = ModelImporterAnimationType.Generic;
			}
			AnimationUtility.SetAnimationType(clip, animation_type);
		}
		
		private float scale_ = 1.0f;
		
		static readonly float c_frame_to_time = 1.0f / 30.0f;
	}
}
