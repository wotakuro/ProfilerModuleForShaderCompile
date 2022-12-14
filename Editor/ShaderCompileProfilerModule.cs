using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor.MPE;
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
            new ProfilerCounterDescriptor("ShaderCompile Count", ProfilerCategory.Scripts),
            new ProfilerCounterDescriptor("ShaderCompile Time", ProfilerCategory.Scripts),
            new ProfilerCounterDescriptor("ShaderCompile TotalCount", ProfilerCategory.Scripts),
            new ProfilerCounterDescriptor("ShaderCompile TotalTime", ProfilerCategory.Scripts),
        };

        private ShaderCompileRowUI shaderCompileRowUI = new ShaderCompileRowUI();

        private Config m_config;
        private ProfilerShaderCompileWatcher m_watcher;
        [NonSerialized]
        private bool isFirst = true;


        [NonSerialized]
        private long m_lastShowFrameIdx = -1;
        [NonSerialized]
        private bool m_lastShowIsOnlyFrame = false;
        [NonSerialized]
        private int m_lastShowCompileIdx = -1;
        private ShaderCompileModuleDetailsViewController m_targetController = null;

        #region ACCESS_FROM_VIEW

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
                this.watcher.SetLogEnabled(value);
                m_config.logEnabled = value;
            }
        }
        internal bool filterFrame
        {
            get
            {
                return m_config.filterFrame;
            }
            set
            {
                m_config.filterFrame = value;
            }
        }
        #endregion ACCESS_FROM_VIEW

        public ShaderCompileProfilerModule() : base(k_ChartCounters)
        {
            m_config = Config.GetConfig();

            m_watcher = new ProfilerShaderCompileWatcher();
            EditorApplication.update += OnUpdate;
            ProfilerDriver.NewProfilerFrameRecorded += OnProiflerNewDataRecorded;
            ProfilerDriver.profileLoaded += OnProfilerLoaded;
            ProfilerDriver.profileCleared += OnClearData;

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
            //if (ProcessService.level == ProcessLevel.Main){}
            this.m_targetController = new ShaderCompileModuleDetailsViewController(ProfilerWindow, this);

            return this.m_targetController;
        }

        void OnUpdate()
        {
            if (isFirst)
            {
                m_watcher.SetTarget(this.targetAsset);
                m_watcher.ScanLatest();

                if(this.m_targetController != null)
                {
                    m_targetController.OnFirstFrameInModule();
                }
                isFirst = false;
            }
            else
            {
                m_watcher.ScanLatest();
            }
            if (!this.ProfilerWindow)
            {

                ProfilerDriver.NewProfilerFrameRecorded -= OnProiflerNewDataRecorded;
                EditorApplication.update -= OnUpdate;
                ProfilerDriver.profileLoaded -= OnProfilerLoaded;
                ProfilerDriver.profileCleared -= OnClearData;
            }
        }

        public void OnClearData()
        {
            m_watcher.ClearData();
            if (m_targetController != null)
            {
                m_targetController.OnProfilerCleared();
            }
            this.m_lastShowCompileIdx = -1;
            this.m_lastShowFrameIdx = -1;
        }


        public void OnProfilerLoaded()
        {
            m_watcher.ScanAll();
            if (m_targetController != null)
            {
                m_targetController.OnProfilerLoaded();
            }

        }
        public void OnProiflerNewDataRecorded(int connectId, int frameIdx)
        {
            if (isFirst)
            {
                m_watcher.SetTarget(this.targetAsset);
                isFirst = false;
            }
            bool flag = m_watcher.RemoveOldFrames(ProfilerDriver.firstFrameIndex);
            if (flag)
            {
                this.m_lastShowCompileIdx = -1;
            }
            m_watcher.ScanLatest();
            if (m_targetController != null)
            {
                m_targetController.OnNewFrameRecorded(connectId,frameIdx);
            }

        }

        public void OnDisposeController()
        {
            this.m_lastShowCompileIdx = -1;
            this.m_lastShowFrameIdx = -1;
            this.m_targetController = null;

        }


        public List<ShaderCompileInfo> GetData(bool isOnlyFrame,long frameIdx,out bool shouldClear,out bool shouldAdd)
        {
            if (isOnlyFrame)
            {
                shouldClear = !m_lastShowIsOnlyFrame;
                shouldClear |= (m_lastShowFrameIdx != frameIdx);
                shouldAdd = shouldClear;

                m_lastShowIsOnlyFrame = true;
                m_lastShowFrameIdx = frameIdx;
                return watcher.GetFrameCompiles((int)frameIdx);
            }
            else
            {
                shouldAdd = m_lastShowIsOnlyFrame;
                shouldAdd |= (watcher.latestCompileFrameIdx != m_lastShowCompileIdx);
                // todo
                shouldClear = shouldAdd;

                m_lastShowIsOnlyFrame = false;
                m_lastShowCompileIdx = watcher.latestCompileFrameIdx;
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