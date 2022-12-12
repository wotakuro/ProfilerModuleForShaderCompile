using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace UTJ.Profiler.ShaderCompileModule
{
    [System.Serializable]
    internal class Config
    {
        private const string ConfigFile = "Library/profilermodule.shadercompile/config.json";

        [SerializeField]
        private string targetPath;
        [SerializeField]
        private bool autoEnabled;
        [SerializeField]
        private bool logEnabled;

        public void Load()
        {

        }
        public void Save()
        {
            var str = JsonUtility.ToJson(this);
            File.WriteAllText(ConfigFile, str);
        }
    }
}