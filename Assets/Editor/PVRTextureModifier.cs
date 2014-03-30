using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

class PVRTextureModifier : AssetPostprocessor
{
	struct Position2 {
		public int x,y;
		public Position2(int p1, int p2)
		{
			x = p1;
			y = p2;
		}
	}

	readonly static List<List<Position2>> bleedTable;
	static PVRTextureModifier(){
		bleedTable=new List<List<Position2>>();
		for(int i=1;i<=12;i++){
			var bT=new List<Position2>();
			for(int x=-i;x<=i;x++){
				bT.Add(new Position2(x,i));
				bT.Add(new Position2(-x,-i));
			}
			for(int y=-i+1;y<=i-1;y++){
				bT.Add(new Position2(i,y));
				bT.Add(new Position2(-i,-y));
			}
			bleedTable.Add(bT);
		}
	}
	
	public override int GetPostprocessOrder(){
//		return int.MaxValue;
		return 0;
	}

	void OnPreprocessTexture ()
    {
        var importer = (assetImporter as TextureImporter);

		if (importer.textureType==TextureImporterType.GUI
		 &&(assetPath.EndsWith ("PMA.png")
		 || assetPath.EndsWith ("Bleed.png")
		 || assetPath.EndsWith ("PVR.png"))) {
			importer.alphaIsTransparency=false;
			importer.compressionQuality = (int)TextureCompressionQuality.Best;
			if(importer.textureFormat==TextureImporterFormat.RGB16)
				importer.textureFormat = TextureImporterFormat.RGB24;
			if(importer.textureFormat==TextureImporterFormat.RGBA16)
				importer.textureFormat = TextureImporterFormat.RGBA32;
			if(importer.textureFormat==TextureImporterFormat.ARGB16)
				importer.textureFormat = TextureImporterFormat.ARGB32;
		}
	}

    void OnPostprocessTexture (Texture2D texture)
    {
		var pixels = texture.GetPixels ();
		var height = texture.height;
		var width = texture.width;
//		Debug.Log(texture.format);
		var importer = (assetImporter as TextureImporter);
		if(importer.textureType!=TextureImporterType.GUI)
			return;
		if (assetPath.EndsWith ("PMA.png")) {
			for (var y = 0; y < height; y++) {
				for (var x = 0; x < width; x++) {
					int position=y*width+x;
					pixels [position]=new Color(
						pixels [position].r*pixels [position].a,
						pixels [position].g*pixels [position].a,
						pixels [position].b*pixels [position].a,
						pixels [position].a);
				}
			}
		}else if (assetPath.EndsWith ("Bleed.png")) {
			for (var y = 0; y < height; y++) {
				for (var x = 0; x < width; x++) {
					int position=y*width+x;
					if (pixels [position].a <= 0.0666666f) {
						float a=pixels[position].a;
						pixels[position]=new Color(0.5f,0.5f,0.5f,a);
						int index=1;
						foreach(var bt in bleedTable){
							float r=0.0f;
							float g=0.0f;
							float b=0.0f;
							float c=0.0f;
							foreach(var pt in bt){
								int xp=x+pt.x;
								int yp=y+pt.y;
								if (xp >= 0 && xp < width && yp >= 0 && yp < height)
								{
									int pos=yp*width+xp;
									float ad=pixels[pos].a;
									if(ad>0.0666666f){
										r+=pixels[pos].r*ad;	
										g+=pixels[pos].g*ad;	
										b+=pixels[pos].b*ad;
										c+=ad;
									}
								}
							}
							if(c>0.0f){
								float fac=Mathf.Min (1.0f,(float)(13-index)/6.0f);
								pixels[position]=
									new Color(r/c*fac+pixels[position].r*(1.0f-fac)
									         ,g/c*fac+pixels[position].g*(1.0f-fac)
									         ,b/c*fac+pixels[position].b*(1.0f-fac),a);
								break;
							}
							index++;
						}
					}
				}
			}
		}else if (assetPath.EndsWith ("PVR.png")) {
			for (var i = 0; i < pixels.Length; i++) {
	           if (pixels [i].a == 0.0f) {
	                pixels [i] = new Color (0.5f, 0.5f, 0.5f, 0.0f);
	            }
			}
        }
        texture.SetPixels (pixels);
//      EditorUtility.CompressTexture (texture, TextureFormat.PVRTC_RGBA4, TextureCompressionQuality.Best);
    }
}