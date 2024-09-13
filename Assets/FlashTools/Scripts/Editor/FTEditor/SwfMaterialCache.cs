using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

using System.IO;
using System.Collections.Generic;

namespace FTEditor {
	class SwfMaterialCache {

		const string SwfSimpleShaderName     = "SwfSimpleShader";
		const string SwfMaskedShaderName     = "SwfMaskedShader";
		const string SwfIncrMaskShaderName   = "SwfIncrMaskShader";
		const string SwfDecrMaskShaderName   = "SwfDecrMaskShader";

		static Dictionary<string, Shader> ShaderCache = new();
		static Shader GetShaderByName(string shader_name) {
			if ( !ShaderCache.TryGetValue(shader_name, out var shader) || !shader ) {
				shader = SafeLoadShader(shader_name);
				ShaderCache.Add(shader_name, shader);
			}
			shader.hideFlags = HideFlags.HideInInspector;
			return shader;
		}

		static Dictionary<string, Material> MaterialCache = new();
		static Material GetMaterialByPath(
			string                          material_path,
			Shader                          material_shader,
			System.Func<Material, Material> fill_material)
		{
			if ( !MaterialCache.TryGetValue(material_path, out var material) || !material ) {
				material = SafeLoadMaterial(material_path, material_shader, fill_material);
				MaterialCache.Add(material_path, material);
			}
			material.hideFlags = HideFlags.HideInInspector;
			return material;
		}

		// ---------------------------------------------------------------------
		//
		// Functions
		//
		// ---------------------------------------------------------------------

		public static Material Query(SwfInstanceData.Types type, SwfBlendModeData.Types blend_mode, int clip_depth)
		{
			return type switch
			{
				SwfInstanceData.Types.Mask => GetIncrMaskMaterial(),
				SwfInstanceData.Types.Group => GetSimpleMaterial(blend_mode),
				SwfInstanceData.Types.Masked => GetMaskedMaterial(blend_mode, clip_depth),
				SwfInstanceData.Types.MaskReset => GetDecrMaskMaterial(),
				_ => throw new UnityException($"Incorrect instance type: {type}")
			};
		}

		public static Material GetSimpleMaterial(
			SwfBlendModeData.Types blend_mode)
		{
			return LoadOrCreateMaterial(
				SelectShader(false, blend_mode),
				(dir_path, filename) => $"{dir_path}/{filename}_{blend_mode}.mat",
				material => FillMaterial(material, blend_mode, 0));
		}

		public static Material GetMaskedMaterial(
			SwfBlendModeData.Types blend_mode,
			int                    stencil_id)
		{
			return LoadOrCreateMaterial(
				SelectShader(true, blend_mode),
				(dir_path, filename) => $"{dir_path}/{filename}_{blend_mode}_{stencil_id}.mat",
				material => FillMaterial(material, blend_mode, stencil_id));
		}

		public static Material GetIncrMaskMaterial() {
			return LoadOrCreateMaterial(
				GetShaderByName(SwfIncrMaskShaderName),
				(dir_path, filename) => $"{dir_path}/{filename}.mat",
				material => material);
		}

		public static Material GetDecrMaskMaterial() {
			return LoadOrCreateMaterial(
				GetShaderByName(SwfDecrMaskShaderName),
				(dir_path, filename) => $"{dir_path}/{filename}.mat",
				material => material);
		}

		// ---------------------------------------------------------------------
		//
		// Private
		//
		// ---------------------------------------------------------------------

		static Shader SafeLoadShader(string shader_name) {
			var shader = Shader.Find("FlashTools/" + shader_name.Replace("Shader", ""));
			if ( !shader )
				throw new UnityException($"SwfMaterialCache. Shader not found: {shader_name}");
			return shader;
		}

		static Material SafeLoadMaterial(
			string                          material_path,
			Shader                          material_shader,
			System.Func<Material, Material> fill_material)
		{
			var material = AssetDatabase.LoadAssetAtPath<Material>(material_path);
			if ( !material ) {
				material = fill_material(new Material(material_shader));
				material.hideFlags = HideFlags.HideInInspector;
				AssetDatabase.CreateAsset(material, material_path);
			}
			return material;
		}

		static Material LoadOrCreateMaterial(
			Shader                              shader,
			System.Func<string, string, string> path_factory,
			System.Func<Material, Material>     fill_material)
		{
			var shader_path   = AssetDatabase.GetAssetPath(shader);
			var shader_dir    = Path.GetDirectoryName(shader_path);
			var generated_dir = Path.Combine(shader_dir, "Generated");
			if ( !AssetDatabase.IsValidFolder(generated_dir) ) {
				AssetDatabase.CreateFolder(shader_dir, "Generated");
			}
			var material_path = path_factory(generated_dir, Path.GetFileNameWithoutExtension(shader_path));
			return GetMaterialByPath(material_path, shader, fill_material);
		}

		static Shader SelectShader(bool masked, SwfBlendModeData.Types blend_mode)
		{
			var isKnownBlendMode = blend_mode
				is SwfBlendModeData.Types.Normal
				or SwfBlendModeData.Types.Layer
				or SwfBlendModeData.Types.Multiply
				or SwfBlendModeData.Types.Screen
				or SwfBlendModeData.Types.Lighten
				or SwfBlendModeData.Types.Add
				or SwfBlendModeData.Types.Subtract;
			return isKnownBlendMode
				? GetShaderByName(masked ? SwfMaskedShaderName : SwfSimpleShaderName)
				: throw new UnityException($"SwfMaterialCache. Incorrect blend mode: {blend_mode}");
		}

		static Material FillMaterial(
			Material               material,
			SwfBlendModeData.Types blend_mode,
			int                    stencil_id)
		{
			var (blendOp, srcBlend, dstBlend) = blend_mode switch
			{
				SwfBlendModeData.Types.Normal => (BlendOp: BlendOp.Add, SrcBlend: BlendMode.One, DstBlend: BlendMode.OneMinusSrcAlpha),
				SwfBlendModeData.Types.Layer => (BlendOp: BlendOp.Add, SrcBlend: BlendMode.One, DstBlend: BlendMode.OneMinusSrcAlpha),
				SwfBlendModeData.Types.Multiply => (BlendOp: BlendOp.Add, SrcBlend: BlendMode.DstColor, DstBlend: BlendMode.OneMinusSrcAlpha),
				SwfBlendModeData.Types.Screen => (BlendOp: BlendOp.Add, SrcBlend: BlendMode.OneMinusDstColor, DstBlend: BlendMode.One),
				SwfBlendModeData.Types.Lighten => (BlendOp: BlendOp.Max, SrcBlend: BlendMode.One, DstBlend: BlendMode.OneMinusSrcAlpha),
				SwfBlendModeData.Types.Add => (BlendOp: BlendOp.Add, SrcBlend: BlendMode.One, DstBlend: BlendMode.One),
				SwfBlendModeData.Types.Subtract => (BlendOp: BlendOp.ReverseSubtract, SrcBlend: BlendMode.One, DstBlend: BlendMode.One),
				_ => throw new UnityException($"SwfMaterialCache. Incorrect blend mode=> {blend_mode}"),
			};
			material.SetInt("_BlendOp", (int)blendOp);
			material.SetInt("_SrcBlend", (int)srcBlend);
			material.SetInt("_DstBlend", (int)dstBlend);
			material.SetInt("_StencilID", stencil_id);
			return material;
		}
	}
}