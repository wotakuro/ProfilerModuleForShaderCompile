using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System.Reflection;

//--Module--

namespace UTJ.Profiler.ShaderCompileModule
{
    [Serializable]
    [ProfilerModuleMetadata("ShaderCompile")]
    internal class ShaderCompileProfilerModule : ProfilerModule
    {
        static readonly ProfilerCounterDescriptor[] k_ChartCounters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor("ShaderCompile CreateGpuCount", ProfilerCategory.Scripts),
            new ProfilerCounterDescriptor("ShaderCompile CreateGpuTime", ProfilerCategory.Scripts),
            new ProfilerCounterDescriptor("ShaderCompile TotalCount", ProfilerCategory.Scripts),
            new ProfilerCounterDescriptor("ShaderCompile TotalTime", ProfilerCategory.Scripts),
        };
        #region ACCESS_FROM_VIEW
        private ProfilerShaderCompileWatcher m_watcher;
        private ShaderVariantCollection m_targetAsset;
        private bool m_automodeEnabled;
        private bool m_logEnabled;
        #endregion ACCESS_FROM_VIEW

        internal ProfilerShaderCompileWatcher watcher
        {
            get { return m_watcher; }
        }
        internal ShaderVariantCollection targetAsset
        {
            get { return m_targetAsset; }
            set { m_targetAsset = value; }
        }
        internal bool autoModeEnabled
        {
            get { return m_automodeEnabled; }
            set { m_automodeEnabled = value; }
        }
        internal bool logEnabled
        {
            get { return m_logEnabled; }
            set { m_logEnabled = value; }
        }

        public ShaderCompileProfilerModule() : base(k_ChartCounters)
        {
            m_watcher = new ProfilerShaderCompileWatcher();
            // UnityEngine.Debug.Log("CreateModule!!");
            EditorApplication.update += OnUpdate;
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
                //UnityEngine.Debug.Log("DeleteModule!!");
                EditorApplication.update -= OnUpdate;
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