using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEngine.UIElements;
using System.Reflection;

//--Module--

//--Module Custom Drawing--

namespace UTJ.Profiler.ShaderCompileModule
{
    internal class ShaderCompileModuleDetailsViewController : ProfilerModuleViewController
    {
        const string k_UxmlResourceName = "Packages/com.utj.profilermodule.shadercompile/Editor/UXML/ShaderCompileModuleUI.uxml";

        static readonly ProfilerCounterDescriptor k_GcReservedMemoryCounterDescriptor = new ProfilerCounterDescriptor("GC Reserved Memory", ProfilerCategory.Memory);
        static readonly ProfilerCounterDescriptor k_GcUsedMemoryCounterDescriptor = new ProfilerCounterDescriptor("GC Used Memory", ProfilerCategory.Memory);

        VisualElement m_BarFill;
        Label m_BarLabel;

        public ShaderCompileModuleDetailsViewController(ProfilerWindow profilerWindow, ShaderCompileProfilerModule module) : base(profilerWindow) 
        {
        }

        protected override VisualElement CreateView()
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlResourceName);
            UnityEngine.Debug.Log(template == null);
            var view = template.Instantiate();

            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

            UnityEngine.Debug.Log("Module�F�FCreateView");
            return view;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            UnityEngine.Debug.Log("Module�F�FDispose");
            ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
            base.Dispose(disposing);
        }

        void OnSelectedFrameIndexChanged(long selectedFrameIndex)
        {
            //ReloadData(selectedFrameIndex);
        }

        /*
        void ReloadData(long selectedFrameIndex)
        {
            long gcReservedBytes = 0;
            long gcUsedBytes = 0;

            var selectedFrameIndexInt32 = Convert.ToInt32(selectedFrameIndex);
            using (var frameData = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(selectedFrameIndexInt32, 0))
            {
                if (frameData == null || !frameData.valid)
                    return;

                var gcReservedBytesMarkerId = frameData.GetMarkerId(k_GcReservedMemoryCounterDescriptor.Name);
                gcReservedBytes = frameData.GetCounterValueAsLong(gcReservedBytesMarkerId);

                var gcUsedBytesMarkerId = frameData.GetMarkerId(k_GcUsedMemoryCounterDescriptor.Name);
                gcUsedBytes = frameData.GetCounterValueAsLong(gcUsedBytesMarkerId);
            }

            float gcUsedBytesScalar = (float)gcUsedBytes / gcReservedBytes;
            m_BarFill.style.width = new Length(gcUsedBytesScalar * 100, LengthUnit.Percent);
            m_BarLabel.text = $"{EditorUtility.FormatBytes(gcUsedBytes)} / {EditorUtility.FormatBytes(gcReservedBytes)}";
        }
        */
    }
}