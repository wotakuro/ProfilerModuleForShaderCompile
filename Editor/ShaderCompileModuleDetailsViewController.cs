using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Reflection;
using UnityEngine;
using System.Text;
using UnityEditor.Profiling;

//--Module--

//--Module Custom Drawing--

namespace UTJ.Profiler.ShaderCompileModule
{
    internal class ShaderCompileModuleDetailsViewController : ProfilerModuleViewController
    {
        const string k_UxmlResourceName = "Packages/com.utj.profilermodule.shadercompile/Editor/UXML/ShaderCompileModuleUI.uxml";

        static readonly ProfilerCounterDescriptor k_CounterDescriptor = new ProfilerCounterDescriptor("ShaderCompile CreateGpuCount", ProfilerCategory.Scripts);
        static readonly ProfilerCounterDescriptor k_TimeDescriptor = new ProfilerCounterDescriptor("ShaderCompile CreateGpuTime", ProfilerCategory.Scripts);
        static readonly ProfilerCounterDescriptor k_TotalCounterDescriptor = new ProfilerCounterDescriptor("ShaderCompile TotalCount", ProfilerCategory.Scripts);
        static readonly ProfilerCounterDescriptor k_TotalTimeDescriptor = new ProfilerCounterDescriptor("ShaderCompile TotalTime", ProfilerCategory.Scripts);

        private StringBuilder stringBuilder = new StringBuilder(128);


        #region UI_FIELD
        private Label m_CurrentCountLabel;
        private Label m_CurrentTimeLabel;
        private Label m_CurrentTotalCountLabel;
        private Label m_CurrentTotalLabel;

        private Label m_NextCountLabel;
        private Label m_NextTimeLabel;
        private Label m_NextTotalCountLabel;
        private Label m_NextTotalLabel;

        private ObjectField m_TargetAsset;
        private Button m_CreateNewTargetBtn;
        private Toggle m_AutoCreateEnabled;
        private Toggle m_LoggingEnabled;
        private Button m_OpenLogFolder;

        private Toggle m_ShowOnlyCurrentFrame;
        private ScrollView m_ShaderCompileList;
        private Button m_ExportCsv;
        #endregion

        ShaderCompileProfilerModule m_module;


        public ShaderCompileModuleDetailsViewController(ProfilerWindow profilerWindow, ShaderCompileProfilerModule module) : base(profilerWindow) 
        {
            this.m_module = module;
        }

        protected override VisualElement CreateView()
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlResourceName);
            var view = template.Instantiate();

            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;
            ProfilerDriver.profileCleared += OnProfilerCleared;
            ProfilerDriver.profileLoaded += OnProfilerLoaded;
            ProfilerDriver.NewProfilerFrameRecorded += OnNewFrameRecorded;


            m_CurrentCountLabel = view.Q<Label>("CurrentCreateGpuTime");
            m_CurrentTimeLabel = view.Q<Label>("CurrentTotalCount");
            m_CurrentTotalCountLabel = view.Q<Label>("CurrentTotalData");
            m_CurrentTotalLabel = view.Q<Label>("CurrentCreateGpuCount");

            m_NextCountLabel = view.Q<Label>("NextCreateGpuTime");
            m_NextTimeLabel = view.Q<Label>("NextTotalCount");
            m_NextTotalCountLabel = view.Q<Label>("NextTotalData");
            m_NextTotalLabel = view.Q<Label>("NextCreateGpuCount");


            m_TargetAsset = view.Q<ObjectField>("TargetShaderVariantCollection");
            m_CreateNewTargetBtn = view.Q<Button>("CreateNewTargetBtn");
            m_AutoCreateEnabled = view.Q<Toggle>("AutoModeEnable");
            m_LoggingEnabled = view.Q<Toggle>("LogOptionEnabled");
            m_OpenLogFolder = view.Q<Button>("LogOpenBtn");
            m_ShowOnlyCurrentFrame = view.Q<Toggle>("ShowOnlyCurrentFrame");
            m_ShaderCompileList = view.Q<ScrollView>("CompileList");
            m_ExportCsv = view.Q<Button>("ExportResultBtn");

            // setup
            m_TargetAsset.objectType = typeof(ShaderVariantCollection);
            m_TargetAsset.SetValueWithoutNotify( m_module.targetAsset );
            m_TargetAsset.RegisterValueChangedCallback(OnChangeTargetAsset);

            m_AutoCreateEnabled.SetValueWithoutNotify(m_module.autoModeEnabled);
            m_AutoCreateEnabled.RegisterCallback<ChangeEvent<bool>>(OnChangeAutoMode);

            m_LoggingEnabled.SetValueWithoutNotify(m_module.logEnabled);
            m_LoggingEnabled.RegisterCallback<ChangeEvent<bool>>(OnChangeLogEnable);

            //
            m_ShowOnlyCurrentFrame.SetValueWithoutNotify(true);
            m_ShowOnlyCurrentFrame.RegisterCallback<ChangeEvent<bool>>(OnChangeFilterCurrentFrame);

            // setup btn
            m_CreateNewTargetBtn.clicked += OnClickNewTargetButton;
            m_OpenLogFolder.clicked += OnClickOpenLogFolderButton;
            m_ExportCsv.clicked += OnClickExportCsv;

            SetupShaderInfo(ProfilerWindow.selectedFrameIndex);
            return view;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
            base.Dispose(disposing);
        }


        #region UI_EVENT
        private void OnClickExportCsv()
        {
            this.m_module.ExportCsv();
        }


        private void OnClickOpenLogFolderButton()
        {
            m_module.OpenLogDir();
        }
        private void OnClickNewTargetButton()
        {
            var asset = new ShaderVariantCollection();

            bool createFile = false;
            string file = null;
            for (int i = 0; i < 100; ++i)
            {
                if(i == 0)
                {
                    file = "Assets/VariantCollection.shadervariants";
                }
                else
                {
                    file = "Assets/VariantCollection_" + i+ ".shadervariants";
                }
                if (!System.IO.File.Exists(file))
                {
                    AssetDatabase.CreateAsset(asset, file);
                    createFile = true;
                    break;
                }
            }
            if (!createFile)
            {
                file = "Assets/VariantCollection_" + System.Guid.NewGuid().ToString() + ".shadervariants";
                AssetDatabase.CreateAsset(asset,file);
            }
            this.m_TargetAsset.value = asset;
        }

        private void OnChangeAutoMode(ChangeEvent<bool> evt)
        {
            var enableFlag = evt.newValue;
            m_module.autoModeEnabled = enableFlag;
        }
        private void OnChangeLogEnable(ChangeEvent<bool> evt)
        {
            m_module.logEnabled = evt.newValue;
        }

        private void OnChangeTargetAsset(ChangeEvent<UnityEngine.Object> evt)
        {
            var targetAsset = evt.newValue as ShaderVariantCollection;
            m_module.targetAsset = targetAsset;
        }

        private void OnChangeFilterCurrentFrame(ChangeEvent<bool> evt)
        {
            SetupShaderInfo(ProfilerWindow.selectedFrameIndex,true);
        }

        private void OnSelectedFrameIndexChanged(long selectedFrameIndex)
        {
            SetupCounterData(selectedFrameIndex , m_CurrentCountLabel, m_CurrentTimeLabel,
                m_CurrentTotalCountLabel, m_CurrentTotalLabel);
            SetupCounterData(selectedFrameIndex + 1, m_NextCountLabel, m_NextTimeLabel,
                m_NextTotalCountLabel, m_NextTotalLabel);


            if (this.m_ShowOnlyCurrentFrame.value)
            {
                SetupShaderInfo(selectedFrameIndex);
            }
        }
        #endregion UI_EVENT


        private void SetupCounterData(long selectedFrameIndex,Label compileCount,Label compileTime,Label totalCount,Label totalTime)
        {
            var selectedFrameIndexInt32 =(int)(selectedFrameIndex);
            using (var frameData = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(selectedFrameIndexInt32, 0))
            {
                SetupCounterLabel(compileCount, frameData, k_CounterDescriptor,false);
                SetupCounterLabel(compileTime, frameData, k_TimeDescriptor, true);
                SetupCounterLabel(totalCount, frameData, k_TotalCounterDescriptor, false);
                SetupCounterLabel(totalTime, frameData, k_TotalTimeDescriptor, true);
            }
        }
        private void SetupCounterLabel(Label label,RawFrameDataView frameData,ProfilerCounterDescriptor descripter,bool isTime)
        {
            stringBuilder.Clear();
            stringBuilder.Append(k_CounterDescriptor.Name).Append(" ");

            if (frameData != null && frameData.valid)
            {
                var val = frameData.GetCounterValueAsLong(frameData.GetMarkerId(descripter.Name));
                if (isTime)
                {
                    stringBuilder.Append( (double)val/1000000.0 ).Append(" ms");
                }
                else
                {
                    stringBuilder.Append(val);
                }
            }
            else
            {
                stringBuilder.Append("---");
            }

            label.text = stringBuilder.ToString();

        }


        #region PROFILER_EVENT
        private void OnProfilerCleared()
        {
            m_module.OnClearData();
            SetupShaderInfo(ProfilerWindow.selectedFrameIndex);
        }
        private void OnProfilerLoaded()
        {
            m_module.OnProfilerLoaded();
            SetupShaderInfo(ProfilerWindow.selectedFrameIndex);
        }
        private void OnNewFrameRecorded(int connectId, int frameIdx)
        {
            m_module.OnProiflerNewDataRecorded(connectId, frameIdx);
            if (!this.m_ShowOnlyCurrentFrame.value)
            {
                SetupShaderInfo(ProfilerWindow.selectedFrameIndex);
            }
        }

        #endregion PROFILER_EVENT
        private bool setHeader = false;

        private void SetupShaderInfo(long frameIdx,bool filterChange=false)
        {
            var watcher = this.m_module.watcher;
            List<ShaderCompileInfo> compileInfoList;
            bool shouldUpdate = true;

            compileInfoList = m_module.GetData(m_ShowOnlyCurrentFrame.value, frameIdx, out shouldUpdate);

            if ( !shouldUpdate )
            {
                return;
            }
            this.m_module.ClearShaderCompileRowUI();
            m_ShaderCompileList.Clear();

            if (compileInfoList != null)
            {
                foreach (var info in compileInfoList)
                {
                    m_ShaderCompileList.Add(this.m_module.GetShaderCompileRowUI(info));
                }
            }
            if (!setHeader)
            {
                var parent = m_ShaderCompileList.parent;
                int idx = parent.IndexOf(m_ShaderCompileList);
                parent.Insert(idx  , m_module.GetShaderRowHeaderUI());
                setHeader = true;
            }
        }
    }
}