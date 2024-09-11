using UnityEngine;

namespace FTRuntime {
	[System.Serializable]
	public struct SwfSettingsData {
		[Range(0, int.MaxValue)]
		public int         AtlasPadding;
		[Range(float.Epsilon, 500)]
		public float       PixelsPerUnit;
		public bool        BitmapTrimming;

		public static SwfSettingsData identity {
			get {
				return new SwfSettingsData{
					PixelsPerUnit      = 100.0f,
					BitmapTrimming     = true};
			}
		}

		public bool CheckEquals(SwfSettingsData other)
		{
			return
				AtlasPadding == other.AtlasPadding &&
				Mathf.Approximately(PixelsPerUnit, other.PixelsPerUnit) &&
				BitmapTrimming == other.BitmapTrimming;
		}
	}

	[CreateAssetMenu(
		fileName = "SwfSettings",
		menuName = "FlashTools/SwfSettings",
		order = 100)]
	public class SwfSettings : ScriptableObject {
		public SwfSettingsData Settings;

		void Reset() {
			Settings = SwfSettingsData.identity;
		}
	}
}