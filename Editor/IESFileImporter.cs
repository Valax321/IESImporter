using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Valax321.IESImporter
{
    [ScriptedImporter(1, "ies", AllowCaching = true)]
    public class IESFileImporter : ScriptedImporter
    {
        private static class Tooltips
        {
            public const string CookieType = @"The type of light cookie to import.

Point: A cubemap texture to be used on point lights. This can represent a much wider area of the light than spot cookies.

Spot: A 2D texture to be used on spot lights. This will look less physically correct unless the spot angle matches the IES perfectly, but spot lights are cheaper than point lights if shadows are needed.";

            public const string TextureSize =
                @"The size of the generated light cookie. Higher values will look better at the cost of texture memory and disk space.

For point mode, this is the width/height of one cube face. For spot mode, this is the width/height of the generated texture.";
        }
        
        [Tooltip(Tooltips.CookieType)]
        [SerializeField] private CookieType m_CookieType = CookieType.Point;

        [Tooltip(Tooltips.TextureSize)]
        [SerializeField] private int m_TextureSize = 512;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var iesFile = ReadIESFile();

            Texture tex;
            if (m_CookieType == CookieType.Point)
            {
                tex = GenerateCubeTexture(iesFile);
            }
            else
            {
                tex = GenerateSpotTexture(iesFile);
            }
        }

        private Texture GenerateSpotTexture(IESParser iesData)
        {
            var t2d = new Texture2D(m_TextureSize, m_TextureSize, DefaultFormat.HDR, TextureCreationFlags.None);

            var desc =
                new RenderTextureDescriptor(m_TextureSize, m_TextureSize, RenderTextureFormat.Default, 0, 0)
                {
                    enableRandomWrite = true
                };
            var rt = RenderTexture.GetTemporary(desc);
            
            var iesDataTexture = new Texture2D(iesData.horizontalAngleCount, iesData.verticalAngleCount,
                TextureFormat.RGBAFloat, false);
            var bufferData =
                new NativeArray<Color>(iesData.horizontalAngleCount * iesData.verticalAngleCount, Allocator.Temp);
            
            var i = 0;
            
            for (var v = 0; v < iesData.verticalAngleCount; v++)
            {
                for (var h = 0; h < iesData.horizontalAngleCount; h++)
                {
                    var sample = iesData.samples[h, v];
                    bufferData[i] = new Color(sample.VerticalAngle, sample.HorizontalAngle, sample.Intensity / iesData.maxIntensity, 0);
                    i++;
                }
            }
            iesDataTexture.SetPixelData(bufferData, 0);
            iesDataTexture.Apply();
            bufferData.Dispose();

            var csShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                "Packages/com.valax321.iesimporter/Shaders/GenerateIESSpot.compute");
            
            csShader.SetTexture(0, Shader.PropertyToID("DataTexture"), iesDataTexture);
            csShader.SetTexture(0, Shader.PropertyToID("Result"), rt, 0, RenderTextureSubElement.Color);
            csShader.SetVector(Shader.PropertyToID("ResultSize"), new Vector4(rt.width, rt.height));
            csShader.SetVector(Shader.PropertyToID("DataSize"), new Vector4(iesData.horizontalAngleCount, iesData.verticalAngleCount));
            csShader.SetVector(Shader.PropertyToID("AngleRanges"), 
                new Vector4(iesData.samples[0, 0].VerticalAngle, 
                    iesData.samples[0, iesData.verticalAngleCount - 1].VerticalAngle, 
                    iesData.samples[0, 0].HorizontalAngle, 
                    iesData.samples[iesData.horizontalAngleCount - 1, 0].VerticalAngle));

            csShader.Dispatch(0, m_TextureSize / 8, m_TextureSize / 8, 1);

            RenderTexture.active = rt;
            t2d.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            t2d.Apply();
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(Path.ChangeExtension(assetPath, ".exr"), t2d.EncodeToEXR());
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(assetPath), $"{Path.GetFileNameWithoutExtension(assetPath)}_IESData.tga"), iesDataTexture.EncodeToTGA());

            return t2d;
        }

        private Texture GenerateCubeTexture(IESParser iesData)
        {
            var cube = new Cubemap(m_TextureSize, TextureFormat.Alpha8, true);
            return cube;
        }

        private IESParser ReadIESFile()
        {
            using var reader = File.OpenText(assetPath);
            return IESParser.Read(reader);
        }
    }
}
