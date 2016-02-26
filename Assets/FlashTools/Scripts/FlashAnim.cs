﻿using UnityEngine;
using System.Collections.Generic;

namespace FlashTools {
	[ExecuteInEditMode]
	public class FlashAnim : MonoBehaviour {
		public FlashAnimAsset Asset = null;

		int _current_frame = 0;
		float _frame_timer = 0.0f;

		List<Vector3> _vertices  = new List<Vector3>();
		List<int>     _triangles = new List<int>();
		List<Vector2> _uvs       = new List<Vector2>();

		Mesh      _mesh          = null;
		Vector3[] _vertices_arr  = new Vector3[0];
		int[]     _triangles_arr = new int[0];
		Vector2[] _uvs_arr       = new Vector2[0];

		public void Play() {
		}

		public void Stop() {
		}

		public void Pause() {
		}

		public void GoToFrame(int frame) {
		}

		public int frameCount {
			get {
				int frames = 0;
				if ( Asset ) {
					var layers = GetCurrentSymbol().Layers;
					for ( var i = 0; i < layers.Count; ++i ) {
						var layer = layers[i];
						frames = Mathf.Max(frames, layer.Frames.Count);
					}
				}
				return frames;
			}
		}

		FlashAnimSymbolData GetCurrentSymbol() {
			//return Asset.Data.Library.Symbols[0];
			return Asset.Data.Stage;
		}

		int GetNumFrameByNum(FlashAnimLayerData layer, int num) {
			return num % layer.Frames.Count;
		}

		FlashAnimFrameData GetFrameByNum(FlashAnimLayerData layer, int num) {
			var frame_num = GetNumFrameByNum(layer, num);
			if ( frame_num >= 0 && frame_num < layer.Frames.Count ) {
				return layer.Frames[frame_num];
			}
			return null;
		}

		FlashAnimSymbolData FindSymbol(FlashAnimLibraryData library, string symbol_id) {
			for ( var i = 0; i < library.Symbols.Count; ++i ) {
				var symbol = library.Symbols[i];
				if ( symbol.Id == symbol_id ) {
					return symbol;
				}
			}
			return null;
		}

		FlashAnimBitmapData FindBitmap(FlashAnimLibraryData library, string bitmap_id) {
			for ( var i = 0; i < library.Bitmaps.Count; ++i ) {
				var bitmap = library.Bitmaps[i];
				if ( bitmap.Id == bitmap_id ) {
					return bitmap;
				}
			}
			return null;
		}

		void RenderInstance(FlashAnimInstData elem_data, int frame_num, Matrix4x4 matrix) {
			if ( elem_data.Type == FlashAnimInstType.Bitmap ) {
				var bitmap = Asset ? FindBitmap(Asset.Data.Library, elem_data.Asset) : null;
				if ( bitmap != null ) {
					var width  = bitmap.RealSize.x;
					var height = bitmap.RealSize.y;

					var v0 = new Vector3(     0,       0, 0);
					var v1 = new Vector3( width,       0, 0);
					var v2 = new Vector3( width,  height, 0);
					var v3 = new Vector3(     0,  height, 0);

					_vertices.Add(matrix.MultiplyPoint3x4(v0));
					_vertices.Add(matrix.MultiplyPoint3x4(v1));
					_vertices.Add(matrix.MultiplyPoint3x4(v2));
					_vertices.Add(matrix.MultiplyPoint3x4(v3));

					_triangles.Add(_vertices.Count - 4 + 2);
					_triangles.Add(_vertices.Count - 4 + 1);
					_triangles.Add(_vertices.Count - 4 + 0);
					_triangles.Add(_vertices.Count - 4 + 0);
					_triangles.Add(_vertices.Count - 4 + 3);
					_triangles.Add(_vertices.Count - 4 + 2);

					var source_rect = bitmap.SourceRect;
					_uvs.Add(new Vector2(source_rect.xMin, source_rect.yMax));
					_uvs.Add(new Vector2(source_rect.xMax, source_rect.yMax));
					_uvs.Add(new Vector2(source_rect.xMax, source_rect.yMin));
					_uvs.Add(new Vector2(source_rect.xMin, source_rect.yMin));
				}
			} else if ( elem_data.Type == FlashAnimInstType.Symbol ) {
				var symbol = Asset ? FindSymbol(Asset.Data.Library, elem_data.Asset) : null;
				if ( symbol != null ) {
					RenderSymbol(symbol, frame_num, matrix);
				}
			}
		}

		void RenderSymbol(FlashAnimSymbolData symbol, int frame_num, Matrix4x4 matix) {
			for ( var i = 0; i < symbol.Layers.Count; ++i ) {
				var layer = symbol.Layers[i];
				if ( layer.LayerType != FlashAnimLayerType.Mask ) {
					var frame = GetFrameByNum(layer, frame_num);
					if ( frame != null ) {
						for ( var j = 0; j < frame.Elems.Count; ++j ) {
							var elem = frame.Elems[j];
							if ( elem.Instance != null ) {
								RenderInstance(
									elem.Instance, frame_num, matix * elem.Matrix);
							}
						}
					}
				}
			}
		}

		void Update() {
			_frame_timer += 25.0f * Time.deltaTime;
			while ( _frame_timer > 1.0f ) {
				_frame_timer -= 1.0f;
				++_current_frame;
				if ( _current_frame > frameCount - 1 ) {
					_current_frame = 0;
				}
				//Debug.LogFormat("Cur frame: {0}", _current_frame);
			}
		}

		void OnRenderObject() {
			if ( Asset ) {
				_vertices.Clear();
				_triangles.Clear();
				_uvs.Clear();
				RenderSymbol(
					GetCurrentSymbol(),
					_current_frame,
					Matrix4x4.Scale(new Vector3(1,-1,1)));

				/*
				if ( _vertices_arr.Length < _vertices.Count ) {
					_vertices_arr = _vertices.ToArray();
				} else {
					_vertices.CopyTo(_vertices_arr);
				}
				if ( _triangles_arr.Length < _triangles.Count ) {
					_triangles_arr = _triangles.ToArray();
				} else {
					_triangles.CopyTo(_triangles_arr);
				}
				if ( _uvs_arr.Length < _uvs.Count ) {
					_uvs_arr = _uvs.ToArray();
				} else {
					_uvs.CopyTo(_uvs_arr);
				}

				var mesh       = new Mesh();
				mesh.vertices  = _vertices_arr;
				mesh.triangles = _triangles_arr;
				mesh.uv        = _uvs_arr;
				mesh.RecalculateNormals();
				GetComponent<MeshFilter>().mesh = mesh;*/

				if ( !_mesh ) {
					_mesh = new Mesh();
				}

				if ( _mesh ) {
					_mesh.Clear();
					_mesh.SetVertices(_vertices);
					_mesh.SetTriangles(_triangles, 0);
					_mesh.SetUVs(0, _uvs);
					_mesh.RecalculateNormals();
					GetComponent<MeshFilter>().mesh = _mesh;
				}
			}
		}
	}
}