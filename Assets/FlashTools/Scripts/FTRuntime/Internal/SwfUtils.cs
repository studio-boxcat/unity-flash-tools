using UnityEngine;

namespace FTRuntime.Internal {
	public static class SwfUtils {

		//
		// Shader properties
		//

		public static int TintShaderProp {
			get {
				ShaderPropsCache.LazyInitialize();
				return ShaderPropsCache.TintShaderProp;
			}
		}

		public static int MainTexShaderProp {
			get {
				ShaderPropsCache.LazyInitialize();
				return ShaderPropsCache.MainTexShaderProp;
			}
		}

		//
		// GetComponent<T>
		//

		public static T GetComponent<T>(GameObject obj, bool allow_create) where T : Component {
			var comp = obj.GetComponent<T>();
			if ( allow_create && !comp ) {
				comp = obj.AddComponent<T>();
			}
			return comp;
		}

		// ShaderPropsCache

		static class ShaderPropsCache {
			public static int TintShaderProp          = 0;
			public static int MainTexShaderProp       = 0;

			static bool _initialized = false;
			public static void LazyInitialize() {
				if ( !_initialized ) {
					_initialized            = true;
					TintShaderProp          = Shader.PropertyToID("_Tint");
					MainTexShaderProp       = Shader.PropertyToID("_MainTex");
				}
			}
		}

	}
}