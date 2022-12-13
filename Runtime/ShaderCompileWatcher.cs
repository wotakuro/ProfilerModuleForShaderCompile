#if ENABLE_PROFILER

using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Profiling;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]
namespace UTJ.Profiling.ShaderCompileModule
{
    [Preserve]
    internal class ShaderCompileWatcher
    {
        static ShaderCompileWatcher instance;

        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void InitPlayerLoop()
        {
            instance = new ShaderCompileWatcher();
            var current = PlayerLoop.GetCurrentPlayerLoop();

            var checkSystem = new PlayerLoopSystem()
            {
                type = typeof(ShaderCompileWatcher),
                updateDelegate = instance.Update
            };
            var currentSubSystemList = current.subSystemList;
            var newSubSystem = new PlayerLoopSystem[currentSubSystemList.Length];
            for (int i = 0; i < currentSubSystemList.Length; ++i)
            {
                var subSystem = currentSubSystemList[i];
                if (subSystem.type == typeof(PostLateUpdate))
                {
                    var subsubSystem = subSystem.subSystemList;
                    var newSubSubSystem = new PlayerLoopSystem[subsubSystem.Length + 1];

                    int addIdx = 0;
                    for (int j = 0; j < subsubSystem.Length; ++j)
                    {
                        newSubSubSystem[j + addIdx] = subsubSystem[j];
                        if (subsubSystem[j].type == typeof(PostLateUpdate.ProfilerEndFrame))
                        {
                            newSubSubSystem[j + 1] = checkSystem;
                            addIdx = 1;
                        }

                    }
                    subSystem.subSystemList = newSubSubSystem;
                }
                newSubSystem[i] = subSystem;
            }
            current.subSystemList = newSubSystem;
            PlayerLoop.SetPlayerLoop(current);
        }

        ProfilerRecorder createGpuRecord;


        [NativeDisableUnsafePtrRestriction]
        [NonSerialized]
        System.IntPtr currentCountHandle;
        [NativeDisableUnsafePtrRestriction]
        [NonSerialized]
        System.IntPtr currentTimeHandle;


        [NativeDisableUnsafePtrRestriction]
        [NonSerialized]
        System.IntPtr totalCountHandle;
        [NativeDisableUnsafePtrRestriction]
        [NonSerialized]
        System.IntPtr totalTimeHandle;


        [NativeDisableUnsafePtrRestriction]
        [NonSerialized]
        System.IntPtr platformCodeHandle;

        private int totalCount = 0;
        private long totalNanosec = 0;

        private int currentCount = 0;
        private long currentNanosec = 0;
        private int platformCode;

        private ShaderCompileWatcher()
        {
            Intiialize();
        }

        // Start is called before the first frame update
        private void Intiialize()
        {
            UnityEngine.Profiling.Profiler.enabled = true;
            //"Shader.CreateGPUProgram"
            this.createGpuRecord = ProfilerRecorder.StartNew(
                new ProfilerMarker(ProfilerCategory.Render, "Shader.CreateGPUProgram"), 1,
                ProfilerRecorderOptions.Default);
            unsafe
            {
                currentCountHandle = ProfilerUnsafeUtility.CreateMarker(
                    "ShaderCompile Count",
                    ProfilerUnsafeUtility.CategoryScripts, MarkerFlags.Counter, 1);
                ProfilerUnsafeUtility.SetMarkerMetadata(currentCountHandle, 0, null,
                    (byte)ProfilerMarkerDataType.UInt32, (byte)ProfilerMarkerDataUnit.Count);

                currentTimeHandle = ProfilerUnsafeUtility.CreateMarker(
                    "ShaderCompile Time",
                    ProfilerUnsafeUtility.CategoryScripts, MarkerFlags.Counter, 1);
                ProfilerUnsafeUtility.SetMarkerMetadata(currentTimeHandle, 0, null,
                    (byte)ProfilerMarkerDataType.Int64, (byte)ProfilerMarkerDataUnit.TimeNanoseconds);

                // total
                totalCountHandle = ProfilerUnsafeUtility.CreateMarker(
                    "ShaderCompile TotalCount",
                    ProfilerUnsafeUtility.CategoryScripts, MarkerFlags.Counter, 1);
                ProfilerUnsafeUtility.SetMarkerMetadata(totalCountHandle, 0, null,
                    (byte)ProfilerMarkerDataType.UInt32, (byte)ProfilerMarkerDataUnit.Count);

                totalTimeHandle = ProfilerUnsafeUtility.CreateMarker(
                    "ShaderCompile TotalTime",
                    ProfilerUnsafeUtility.CategoryScripts, MarkerFlags.Counter, 1);
                ProfilerUnsafeUtility.SetMarkerMetadata(totalTimeHandle, 0, null,
                    (byte)ProfilerMarkerDataType.Int64, (byte)ProfilerMarkerDataUnit.TimeNanoseconds);

                // platform ( hidden)
                platformCodeHandle = ProfilerUnsafeUtility.CreateMarker(
                    "ShaderCompile Platform",
                    ProfilerUnsafeUtility.CategoryScripts, MarkerFlags.Counter, 1);
                ProfilerUnsafeUtility.SetMarkerMetadata(platformCodeHandle, 0, null,
                    (byte)ProfilerMarkerDataType.Int32, (byte)ProfilerMarkerDataUnit.Undefined);

                //
                this.platformCode = (int)Application.platform;
            }
            this.CommitPlatformCode();

        }



        // Update is called once per frame
        void Update()
        {
            this.currentCount = 0;
            this.currentNanosec = 0;
            unsafe
            {
                if (createGpuRecord.Count > 0)
                {
                    var currentSample = createGpuRecord.GetSample(0);
                    this.currentCount += (int)currentSample.Count;
                    this.currentNanosec += currentSample.Value;
                }
            }
            this.totalCount += this.currentCount;
            this.totalNanosec += this.currentNanosec;

            unsafe
            {
                if (this.currentCountHandle != IntPtr.Zero)
                {
                    unsafe
                    {
                        var data = new ProfilerMarkerData
                        {
                            Type = (byte)ProfilerMarkerDataType.UInt32,
                            Size = sizeof(uint),
                            Ptr = UnsafeUtility.AddressOf(ref this.currentCount)
                        };
                        ProfilerUnsafeUtility.SingleSampleWithMetadata(this.currentCountHandle, 1, &data);
                    }
                }
                if (this.currentTimeHandle != IntPtr.Zero)
                {
                    unsafe
                    {
                        var data = new ProfilerMarkerData
                        {
                            Type = (byte)ProfilerMarkerDataType.Int64,
                            Size = sizeof(long),
                            Ptr = UnsafeUtility.AddressOf(ref this.currentNanosec)
                        };
                        ProfilerUnsafeUtility.SingleSampleWithMetadata(this.currentTimeHandle, 1, &data);
                    }
                }
                // total
                if (this.totalCountHandle != IntPtr.Zero)
                {
                    unsafe
                    {
                        var data = new ProfilerMarkerData
                        {
                            Type = (byte)ProfilerMarkerDataType.UInt32,
                            Size = sizeof(uint),
                            Ptr = UnsafeUtility.AddressOf(ref this.totalCount)
                        };
                        ProfilerUnsafeUtility.SingleSampleWithMetadata(this.totalCountHandle, 1, &data);
                    }
                }
                if (this.totalTimeHandle != IntPtr.Zero)
                {
                    unsafe
                    {
                        var data = new ProfilerMarkerData
                        {
                            Type = (byte)ProfilerMarkerDataType.Int64,
                            Size = sizeof(long),
                            Ptr = UnsafeUtility.AddressOf(ref this.totalNanosec)
                        };
                        ProfilerUnsafeUtility.SingleSampleWithMetadata(this.totalTimeHandle, 1, &data);
                    }
                }
                this.CommitPlatformCode();

            }

        }
        private void CommitPlatformCode()
        {
            // hidden platform
            if (this.platformCodeHandle != IntPtr.Zero)
            {
                unsafe
                {
                    var data = new ProfilerMarkerData
                    {
                        Type = (byte)ProfilerMarkerDataType.Int32,
                        Size = sizeof(int),
                        Ptr = UnsafeUtility.AddressOf(ref this.platformCode)
                    };
                    ProfilerUnsafeUtility.SingleSampleWithMetadata(this.platformCodeHandle, 1, &data);
                }
            }

        }
    }
}
#endif