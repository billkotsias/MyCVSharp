using UnityEngine;

namespace MyCVSharp
{
	// Abstracted to support both Texture2D and WebCamTexture
	public class TextureData
	{
		public int width;
		public int height;
		public Color32[] pixels;

		public TextureData( Texture2D texture )
		{
			width = texture.width;
			height = texture.height;
			pixels = texture.GetPixels32();
		}

		public TextureData( WebCamTexture texture )
		{
			width = texture.width;
			height = texture.height;
			pixels = texture.GetPixels32();
		}
	}
}
