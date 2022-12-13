using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace UTJ.Profiler.ShaderCompileModule
{
    [System.Serializable]
    internal class Config
    {
        private const string ConfigFile = "Library/profilermodule.shadercompile/config.json";

        [SerializeField]
        private string m_targetPath;
        [SerializeField]
        private bool m_autoEnabled;
        [SerializeField]
        private bool m_logEnabled;
        [SerializeField]
        private bool m_filterFrame;

        public ShaderVariantCollection target
        {
            set
            {
                var path = AssetDatabase.GetAssetPath(value);

                this.m_targetPath = path;
                Save();
            }
            get
            {
                return AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(this.m_targetPath);
            }
        }

        public bool autoEnabled
        {
            get
            {
                return m_autoEnabled;
            }
            set
            {
                m_autoEnabled = value;
                this.Save();
            }
        }
        public bool logEnabled
        {
            get
            {
                return m_logEnabled;
            }
            set
            {
                m_logEnabled = value;
                this.Save();
            }
        }
        public bool filterFrame
        {
            get
            {
                return m_filterFrame;
            }
            set
            {
                m_filterFrame = value;
                this.Save();
            }
        }


        public static Config GetConfig()
        {
            if (!File.Exists(ConfigFile))
            {
                return GetDefault();
            }
            try
            {
                var str = File.ReadAllText(ConfigFile);
                return JsonUtility.FromJson<Config>(str);
            }catch(System.Exception e)
            {
                Debug.LogError(e);
            }
            return GetDefault();
        }
        private static Config GetDefault()
        {
            return new Config()
            {
                m_targetPath = "",
                m_autoEnabled = true,
                m_logEnabled = true,
                m_filterFrame = true,
            };
        }

        public void Save()
        {
            string dir = System.IO.Path.GetDirectoryName(ConfigFile);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            var str = JsonUtility.ToJson(this);
            File.WriteAllText(ConfigFile, str);
        }
    }
}