using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Reflection;
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
        private Toggle m_AutoCreateEnabled;
        private Toggle m_LoggingEnabled;
        private Button m_OpenLogFolder;

        private Toggle m_ShowOnlyCurrentFrame;
        private ScrollView m_ShaderCompileList;
        private Button m_ExportCsv;
        #endregion

        public ShaderCompileModuleDetailsViewController(ProfilerWindow profilerWindow, ShaderCompileProfilerModule module) : base(profilerWindow) 
        {
        }

        protected override VisualElement CreateView()
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlResourceName);
            var view = template.Instantiate();

            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

            UnityEngine.Debug.Log("ModuleÅFÅFCreateView");

            m_CurrentCountLabel = view.Q<Label>("CurrentCreateGpuTime");
            m_CurrentTimeLabel = view.Q<Label>("CurrentTotalCount");
            m_CurrentTotalCountLabel = view.Q<Label>("CurrentTotalData");
            m_CurrentTotalLabel = view.Q<Label>("CurrentCreateGpuCount");

            m_NextCountLabel = view.Q<Label>("NextCreateGpuTime");
            m_NextTimeLabel = view.Q<Label>("NextTotalCount");
            m_NextTotalCountLabel = view.Q<Label>("NextTotalData");
            m_NextTotalLabel = view.Q<Label>("NextCreateGpuCount");


            m_TargetAsset = view.Q<ObjectField>("TargetShaderVariantCollection");
            m_AutoCreateEnabled = view.Q<Toggle>("AutoModeEnable");
            m_LoggingEnabled = view.Q<Toggle>("LogOptionEnabled");
            m_OpenLogFolder = view.Q<Button>("LogOpenBtn");
            m_ShowOnlyCurrentFrame = view.Q<Toggle>("ShowOnlyCurrentFrame");
            m_ShaderCompileList = view.Q<ScrollView>("CompileList");
            m_ExportCsv = view.Q<Button>("ExportResultBtn");
            return view;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
            base.Dispose(disposing);
        }

        private void OnSelectedFrameIndexChanged(long selectedFrameIndex)
        {
            SetupCounterData(selectedFrameIndex , m_CurrentCountLabel, m_CurrentTimeLabel,
                m_CurrentTotalCountLabel, m_CurrentTotalLabel);
            SetupCounterData(selectedFrameIndex + 1, m_NextCountLabel, m_NextTimeLabel,
                m_NextTotalCountLabel, m_NextTotalLabel);
        }


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

    }
}