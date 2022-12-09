using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEngine.UIElements;
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

        // Specify a list of Profiler category names, which should be auto-enabled when the module is active.
        static readonly string[] k_AutoEnabledCategoryNames = new string[]
        {
        ProfilerCategory.Memory.Name,
        };

        public ShaderCompileProfilerModule() : base(k_ChartCounters, autoEnabledCategoryNames: k_AutoEnabledCategoryNames)
        {
            // UnityEngine.Debug.Log("CreateModule!!");
            EditorApplication.update += OnUpdate;
        }
        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new ShaderCompileModuleDetailsViewController(ProfilerWindow,this);
        }

        void OnUpdate()
        {
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