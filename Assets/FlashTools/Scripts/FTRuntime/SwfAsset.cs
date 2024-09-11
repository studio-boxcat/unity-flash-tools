using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace FTRuntime {
	public class SwfAsset : ScriptableObject {
		[HideInInspector]
		public byte[]          Data;
		[HideInInspector]
		public string          Hash;
		[ReadOnly]
		public Texture2D       Atlas;
		[HideInInspector]
		public SwfSettingsData Settings;
		[LabelText("Settings")]
		public SwfSettingsData Overridden;

		void Reset() {
			Data       = Array.Empty<byte>();
			Hash       = string.Empty;
			Atlas      = null;
			Settings   = SwfSettingsData.identity;
			Overridden = SwfSettingsData.identity;
		}
	}
}