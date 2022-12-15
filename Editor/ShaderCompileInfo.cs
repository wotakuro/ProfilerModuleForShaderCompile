using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UTJ.Profiler.ShaderCompileModule
{
    internal class ShaderCompileInfo
    {
        private static Dictionary<string,Shader> s_ShaderCache = new Dictionary<string,Shader>();

        public int frameIdx;
        public string shaderName;
        public string pass;
        public string stage;
        public string keyword;
        public float timeMs;
        public RuntimePlatform platform;

        public ShaderVariantCollection.ShaderVariant GetShaderariant()
        {
            var variant = new ShaderVariantCollection.ShaderVariant();
            var shader = GetShader(); ;
            variant.shader = shader;
            variant.keywords = GetKeywordArray(keyword);
            string lightMode = ShaderPassLightModeConverter.GetLightModeByPasssName(shader, pass);
            variant.passType = GetPassType(lightMode);

            //Debug.Log("PassConvert " + pass + " -> " + lightMode);
            return variant;
        }

        private Shader GetShader()
        {
            Shader shader;
            if ( s_ShaderCache.TryGetValue(shaderName,out shader)){
                return shader;
            }
            shader = Shader.Find(shaderName);
            s_ShaderCache.Add(shaderName, shader);
            return shader;
        }

        private string[] GetKeywordArray(string keywords)
        {

            string[] keywordArray;
            if (string.IsNullOrEmpty(keywords) || keywords == "<no keywords>")
            {
                keywordArray = new string[] { "" };
            }
            else
            {
                keywordArray = keywords.Split(' ');
            }
            return keywordArray;
        }
        private static PassType GetPassType(string str)
        {
            if(str == null)
            {
                return PassType.Normal;
            }
            str = str.ToUpper();
            switch (str)
            {
                case "":
                case "ALWAYS":
                    return PassType.Normal;
                case "VERTEX":
                    return PassType.Vertex;
                case "VERTEXLM":
                    return PassType.VertexLM;
                case "VERTEXLMRGBM":
                    return PassType.ForwardBase;
                case "FORWARDADD":
                    return PassType.ForwardAdd;
                case "PREPASSBASE":
                    return PassType.LightPrePassBase;
                case "PREPASSFINAL":
                    return PassType.LightPrePassFinal;
                case "SHADOWCASTER":
                    return PassType.ShadowCaster;
                case "DEFERRED":
                    return PassType.Deferred;
                case "META":
                    return PassType.Meta;
                case "MOTIONVECTORS":
                    return PassType.MotionVectors;
                case "SRPDEFAULTUNLIT":
                    return PassType.ScriptableRenderPipelineDefaultUnlit;
            }
            //                PassType.ScriptableRenderPipelineDefaultUnlit
            return PassType.ScriptableRenderPipeline;
        }


    }
}