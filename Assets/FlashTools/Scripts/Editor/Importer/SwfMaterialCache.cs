using System;
using System.Collections.Generic;
using System.IO;
using FTSwfTools;
using UnityEngine;
using UnityEditor;

namespace FTEditor.Importer
{
	static class SwfMaterialCache {
		static readonly Dictionary<string, Shader> _shaderCache = new();

		static Shader GetShaderByFileName(string filename) {
			if (_shaderCache.TryGetValue(filename, out var shader))
				return shader;

			shader = Shader.Find("FlashTools/" + filename.Replace("Shader", ""));
			if ( !shader ) throw new UnityException($"SwfMaterialCache. Shader not found: {filename}");
			_shaderCache.Add(filename, shader);
			return shader;
		}

		const string _materialDir = "Packages/com.boxcat.flashtools/Materials/Generated";
		static readonly Dictionary<MaterialKey, Material> _materialCache = new(MaterialKey.Comparer.Instance);

		public static Material Load(MaterialKey key)
		{
			if (_materialCache.TryGetValue(key, out var material))
				return material;

			var (type, blend_mode, clip_depth) = key;

			ValueTuple<string, string, Action<Material>> tuple = type switch
			{
				SwfInstanceData.Types.Simple => ("SwfSimpleShader", $"_{blend_mode}", material => SetMaterialProperties(material, blend_mode, 0)),
				SwfInstanceData.Types.Masked => ("SwfMaskedShader", $"_{blend_mode}_{(int) clip_depth}", material => SetMaterialProperties(material, blend_mode, clip_depth)),
				SwfInstanceData.Types.MaskIn => ("SwfIncrMaskShader", "", null),
				SwfInstanceData.Types.MaskOut => ("SwfDecrMaskShader", "", null),
				_ => throw new UnityException($"Incorrect instance type: {type}")
			};

			var (shader_filename, material_suffix, material_init) = tuple;
			var material_path = Path.Combine(_materialDir, $"{shader_filename}{material_suffix}.mat");
			material = AssetDatabase.LoadAssetAtPath<Material>(material_path);
			if (material) {
				_materialCache.Add(key, material);
				return material;
			}

			var shader = GetShaderByFileName(shader_filename);
			material = new Material(shader);
			material_init?.Invoke(material);
			AssetDatabase.CreateAsset(material, material_path);
			_materialCache.Add(key, material);
			return material;


			static void SetMaterialProperties(
				Material               material,
				SwfBlendMode           blend_mode,
				Depth                  stencil_id)
			{
				var (blendOp, srcBlend, dstBlend) = blend_mode.GetMaterialiProperties();
				material.SetFloat("_BlendOp", (int)blendOp);
				material.SetFloat("_SrcBlend", (int)srcBlend);
				material.SetFloat("_DstBlend", (int)dstBlend);
				material.SetFloat("_StencilID", (int)stencil_id);
			}
		}
	}
}