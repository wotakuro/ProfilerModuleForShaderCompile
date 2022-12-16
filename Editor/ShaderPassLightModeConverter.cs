using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace UTJ.Profiler.ShaderCompileModule
{

    public class ShaderPassLightModeConverter
    {
        private static Dictionary<Shader, ShaderPassLightModeDictionary> m_ShaderDictionary;

        public static string GetLightModeByPasssName(Shader shader, string pass)
        {
            if (m_ShaderDictionary == null)
            {
                m_ShaderDictionary = new Dictionary<Shader, ShaderPassLightModeDictionary>();
            }
            ShaderPassLightModeDictionary dictionary;
            if (m_ShaderDictionary.TryGetValue(shader, out dictionary))
            {
                return dictionary.GetLightMode(pass);
            }
            dictionary = new ShaderPassLightModeDictionary(shader);
            m_ShaderDictionary.Add(shader, dictionary);

            return dictionary.GetLightMode(pass);
        }

    }


    internal class ShaderPassLightModeDictionary
    {
        private Shader m_targetShader;
        private Dictionary<string, string> m_passToLightMode;
        private List<Dictionary<string, string>> m_passBySubShader;
        private Dictionary<string, string> m_currentSubShaderWork;

        public string GetLightMode(string pass)
        {
            if (m_passToLightMode == null) { return ""; }
            string result;
            if (m_passToLightMode.TryGetValue(pass, out result))
            {
                return result;
            }
            return "";
        }


        public ShaderPassLightModeDictionary(Shader shader)
        {
            Init(shader);
        }

        private void Init(Shader shader)
        {
            this.m_targetShader = shader;
            this.m_passToLightMode = new Dictionary<string, string>();
            this.m_passBySubShader = new List<Dictionary<string, string>>();
            if (m_targetShader)
            {
                var serializedObject = new SerializedObject(m_targetShader);
                SerializedProperty subShadersProp = serializedObject.FindProperty("m_ParsedForm.m_SubShaders");
                int subShaderNum = subShadersProp.arraySize;
                for (int i = 0; i < subShaderNum; ++i)
                {
                    this.m_currentSubShaderWork = new Dictionary<string, string>();
                    var currentSubShaderProp = subShadersProp.GetArrayElementAtIndex(i);
                    ExecSubShaderProp(currentSubShaderProp, i);
                    this.m_passBySubShader.Add(m_currentSubShaderWork);
                }
            }
        }

        private void ExecSubShaderProp(SerializedProperty prop, int subIdx)
        {
            if (prop == null) { return; }
            var passesProp = prop.FindPropertyRelative("m_Passes");
            int passCnt = passesProp.arraySize;

            for (int i = 0; i < passCnt; ++i)
            {
                var currentPassProp = passesProp.GetArrayElementAtIndex(i);
                ExecPassProp(currentPassProp, subIdx, i);
            }
        }
        private void ExecPassProp(SerializedProperty prop, int subIdx, int passIdx)
        {
            if (prop == null) { return; }
            var lightMode = GetLightMode(prop);
            var pass = GetPassName(this.m_targetShader, subIdx, passIdx);
            if (!m_passToLightMode.TryAdd(pass, lightMode))
            {
                //Debug.Log("already::" + pass + " lightmode:" + lightMode);
            }
            if (!this.m_currentSubShaderWork.TryAdd(pass, lightMode))
            {
                string alreadyLightmode;
                if (m_currentSubShaderWork.TryGetValue(pass, out alreadyLightmode))
                {
                    var sb = new System.Text.StringBuilder(128);
                    sb.Append("[ShaderPassLightModeConverter] alreadyIn:")
                        .Append(this.m_targetShader.name).Append(" subShader:")
                        .Append(subIdx).Append(" pass ").Append(passIdx).Append(": ")
                        .Append(pass).Append(" lightmode:")
                        .Append(alreadyLightmode).Append(" new:").Append(lightMode);
                    Debug.LogWarning(sb.ToString());
                }

            }

        }

        private string GetPassName(Shader shader, int subidx, int passIdx)
        {
            var data = ShaderUtil.GetShaderData(shader);
            if (data == null) { return ""; }
            if(data.SubshaderCount <= subidx)
            {
                Debug.LogWarning("subshader not found " + shader.name + "  " + subidx);
                return "";
            }
            var sub = data.GetSubshader(subidx);
            if (sub == null) { return ""; }
            if(sub.PassCount <= passIdx)
            {
                Debug.LogWarning("pass not found " + shader.name + "  " + subidx +":" + passIdx);
                return "";
            }

            var pass = sub.GetPass(passIdx);
            if (pass == null) { return ""; }

            return pass.Name;
        }

        private string GetLightMode(SerializedProperty prop)
        {

            string str;
            str = GetTagValue(prop.FindPropertyRelative("m_Tags.tags"), "LIGHTMODE");
            if (!string.IsNullOrEmpty(str))
            {
                return str;
            }
            return GetTagValue(prop.FindPropertyRelative("m_State.m_Tags.tags"), "LIGHTMODE");
        }

        private string GetTagValue(SerializedProperty prop, string key)
        {
            if (prop == null) { return null; }
            key = key.ToUpper();
            int tagsCount = prop.arraySize;
            for (int i = 0; i < tagsCount; ++i)
            {
                var tagInfo = prop.GetArrayElementAtIndex(i);

                var firstProp = tagInfo.FindPropertyRelative("first");
                if (firstProp.stringValue.ToUpper().Trim() == key)
                {
                    var secondProp = tagInfo.FindPropertyRelative("second");
                    return secondProp.stringValue;
                }
            }
            return null;

        }
    }


}