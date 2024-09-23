using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

using System.IO;
using System.Collections.Generic;
using FTSwfTools.SwfTypes;
using JetBrains.Annotations;

namespace FTEditor {
	static class SwfMaterialCache {
		const string MaterialDir = "Packages/com.boxcat.flashtools/Materials/Generated";

		const string SwfSimpleShaderName     = "SwfSimpleShader";
		const string SwfMaskedShaderName     = "SwfMaskedShader";
		const string SwfIncrMaskShaderName   = "SwfIncrMaskShader";
		const string SwfDecrMaskShaderName   = "SwfDecrMaskShader";

		static readonly Dictionary<string, Shader> ShaderCache = new();
		static Shader GetShaderByFileName(string filename) {
			if (ShaderCache.TryGetValue(filename, out var shader))
				return shader;

			shader = Shader.Find("FlashTools/" + filename.Replace("Shader", ""));
			if ( !shader ) throw new UnityException($"SwfMaterialCache. Shader not found: {filename}");
			ShaderCache.Add(filename, shader);
			return shader;
		}

		static readonly Dictionary<string, Material> MaterialCache = new();

		static Material LoadOrCreateMaterial(
			string               shader_filename,
			string               material_name,
			[CanBeNull] Action<Material> material_init)
		{
			var material_path = Path.Combine(MaterialDir, material_name + ".mat");
			if (MaterialCache.TryGetValue(material_path, out var material))
				return material;

			material = AssetDatabase.LoadAssetAtPath<Material>(material_path);
			if (material) {
				MaterialCache.Add(material_path, material);
				return material;
			}

			var shader = GetShaderByFileName(shader_filename);
			material = new Material(shader);
			material_init?.Invoke(material);
			AssetDatabase.CreateAsset(material, material_path);
			MaterialCache.Add(material_path, material);
			return material;
		}

		// ---------------------------------------------------------------------
		//
		// Functions
		//
		// ---------------------------------------------------------------------

		public static Material Query(MaterialKey key)
		{
			var (type, blend_mode, clip_depth) = key;

			ValueTuple<string, string, Action<Material>> tuple = type switch
			{
				SwfInstanceData.Types.Simple => (SwfSimpleShaderName, $"_{blend_mode}", material => SetMaterialProperties(material, blend_mode, 0)),
				SwfInstanceData.Types.Masked => (SwfMaskedShaderName, $"_{blend_mode}_{(int) clip_depth}", material => SetMaterialProperties(material, blend_mode, clip_depth)),
				SwfInstanceData.Types.MaskIn => (SwfIncrMaskShaderName, "", null),
				SwfInstanceData.Types.MaskOut => (SwfDecrMaskShaderName, "", null),
				_ => throw new UnityException($"Incorrect instance type: {type}")
			};

			var (shader_filename, material_suffix, material_init) = tuple;
			var material_name = shader_filename + material_suffix;
			return LoadOrCreateMaterial(shader_filename, material_name, material_init);
		}

		// ---------------------------------------------------------------------
		//
		// Private
		//
		// ---------------------------------------------------------------------

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