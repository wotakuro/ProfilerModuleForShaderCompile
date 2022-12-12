using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using UnityEditorInternal;
//--Module--

namespace UTJ.Profiler.ShaderCompileModule
{
    [Serializable]
    [ProfilerModuleMetadata("ShaderCompile")]
    internal class ShaderCompileProfilerModule : ProfilerModule
    {
        private const string LogDir = "Library/profilermodule.shadercompile/logs/";
        static readonly ProfilerCounterDescriptor[] k_ChartCounters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor("ShaderCompile CreateGpuCount", ProfilerCategory.Scripts),
            new ProfilerCounterDescriptor("ShaderCompile CreateGpuTime", ProfilerCategory.Scripts),
            new ProfilerCounterDescriptor("ShaderCompile TotalCount", ProfilerCategory.Scripts),
            new ProfilerCounterDescriptor("ShaderCompile TotalTime", ProfilerCategory.Scripts),
        };

        private ShaderCompileRowUI shaderCompileRowUI = new ShaderCompileRowUI();

        private Config m_config;
        #region ACCESS_FROM_VIEW
        private ProfilerShaderCompileWatcher m_watcher;
        #endregion ACCESS_FROM_VIEW

        internal ProfilerShaderCompileWatcher watcher
        {
            get { return m_watcher; }
        }
        internal ShaderVariantCollection targetAsset
        {
            get { 
                return m_config.target; 
            }
            set {
                m_config.target = value;
                if (autoModeEnabled)
                {
                    this.watcher.SetTarget(value);
                }
            }
        }
        internal bool autoModeEnabled
        {
            get { 
                return m_config.autoEnabled; 
            }
            set
            {
                Debug.Log("autoModeEnabled!! " + value);
                m_config.autoEnabled = value;
                if (value)
                {
                    this.watcher.SetTarget(this.targetAsset);
                }
                else
                {
                    this.watcher.SetTarget(null);
                }
            }
        }
        internal bool logEnabled
        {
            get { 
                return m_config.logEnabled; 
            }
            set {
                Debug.Log("logEnabled!! " + value);
                this.watcher.SetLogEnabled(value);
                m_config.logEnabled = value;
            }
        }

        public ShaderCompileProfilerModule() : base(k_ChartCounters)
        {
            m_config = Config.GetConfig();

            m_watcher = new ProfilerShaderCompileWatcher();
            // UnityEngine.Debug.Log("CreateModule!!");
            EditorApplication.update += OnUpdate;

            ProfilerDriver.NewProfilerFrameRecorded += OnProiflerNewDataRecorded;

            this.watcher.SetLogFile(LogDir + GetUniqueFileName(), m_config.logEnabled);
        }

        private string GetUniqueFileName()
        {
            var now = System.DateTime.Now;
            return now.ToString("yyyyMMdd_HHmmss")+
                "_" + System.Guid.NewGuid().ToString() + ".log";
        }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new ShaderCompileModuleDetailsViewController(ProfilerWindow,this);
        }

        void OnUpdate()

        {
            m_watcher.ScanLatest();

            if (!this.ProfilerWindow)
            {

                ProfilerDriver.NewProfilerFrameRecorded -= OnProiflerNewDataRecorded;                    
                EditorApplication.update -= OnUpdate;
            }
        }

        public void OnClearData()
        {
            watcher.ClearData();
        }


        public void OnProfilerLoaded()
        {

            watcher.ClearData();
            watcher.ScanAll();
        }

        public void OnProiflerNewDataRecorded(int connectId,int frameIdx)
        {
            watcher.ScanLatest();

        }


        public List<ShaderCompileInfo> GetData(bool isOnlyFrame,long frameIdx,out bool shouldUpdate)
        {
            if (isOnlyFrame)
            {
                shouldUpdate = true;
                return watcher.GetFrameCompiles((int)frameIdx);
            }
            else
            {
                shouldUpdate = true;
                return watcher.allCompileInProfiler;
            }

        }

        public VisualElement GetShaderRowHeaderUI()
        {
            return shaderCompileRowUI.GetDefaultElement();
        }
        public VisualElement GetShaderCompileRowUI(ShaderCompileInfo compileInfo)
        {
            return shaderCompileRowUI.CreateNode(compileInfo);
        }
        public void ClearShaderCompileRowUI()
        {
            shaderCompileRowUI.ReleaseAllNodes();
        }

        public void OpenLogDir()
        {
            EditorUtility.RevealInFinder(LogDir);
        }

        public void ExportCsv()
        {
            string file = EditorUtility.SaveFilePanel("Export to csv", "", "shaderCompiles", "csv");
            if (!string.IsNullOrEmpty(file))
            {
                this.watcher.ExportToCsv(file);
            }
        }

        // [Warnning]access via Refection
        private bool IsActiveViaReflection()
        {
            try
            {
                var property = this.GetType().GetProperty("active", BindingFlags.NonPublic | BindingFlags.Instance);
                if (property == null)
                {
                    return true;
                }

                var val = property.GetValue(this);
                return (bool)val;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex);
            }
            return true;
        }
    }

}