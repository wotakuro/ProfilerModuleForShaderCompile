using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine.Profiling;
using UnityEditor;


namespace UTJ.Profiler.ShaderCompileModule
{
    internal class ProfilerShaderCompileWatcher 
    {


        private int shaderCompileMakerId = FrameDataView.invalidMarkerId;

        private List<int> indexBuffer = new List<int>();
        private List<ShaderCompileInfo> allFrameBuffer = new List<ShaderCompileInfo>();
        private Dictionary<int, List<ShaderCompileInfo>> compileInfoByFrameIdx = new Dictionary<int, List<ShaderCompileInfo>>();
        private bool isDirty = true;
        private int latestFrameIndex = -1;


        public void ScanLatest()
        {
            int idx = latestFrameIndex;
            if( idx < ProfilerDriver.firstFrameIndex)
            {
                idx = ProfilerDriver.firstFrameIndex;
            }
            for (int i = idx; i < ProfilerDriver.lastFrameIndex; i++)
            {
                ScanFrame(i);
            }
        }

        public void ScanAll()
        {
            for (int i = ProfilerDriver.firstFrameIndex; i < ProfilerDriver.lastFrameIndex; i++)
            {
                ScanFrame(i);
            }
        }

        public List<ShaderCompileInfo> allCompileInProfiler
        {
            get
            {
                if (isDirty)
                {
                    allFrameBuffer.Clear();
                    foreach(var kvs in compileInfoByFrameIdx)
                    {
                        var frameCompiles = kvs.Value;
                        if(frameCompiles != null)
                        {
                            foreach(var compile in frameCompiles)
                            {
                                allFrameBuffer.Add(compile);
                            }
                        }
                    }
                }
                return allFrameBuffer;
            }
        }

        public bool isDirtyAllList
        {
            get { return isDirty; }
        }

        public List<ShaderCompileInfo> GetFrameCompiles(int frameIdx)
        {
            List<ShaderCompileInfo> list;
            if(compileInfoByFrameIdx.TryGetValue(frameIdx, out list))
            {
                return list;
            }
            return null;
        }

        public void ScanFrame(int frameIdx)
        {
            if(latestFrameIndex >= frameIdx) { return; }
            List<ShaderCompileInfo> buffer = null;
            for (int threadIndex = 0; ; ++threadIndex)
            {
                using (RawFrameDataView frameData = ProfilerDriver.GetRawFrameDataView(frameIdx, threadIndex))
                {
                    if (!frameData.valid)
                        break;


                    if (shaderCompileMakerId == FrameDataView.invalidMarkerId)
                    {
                        shaderCompileMakerId = frameData.GetMarkerId("Shader.CreateGPUProgram");
                        if (shaderCompileMakerId == FrameDataView.invalidMarkerId)
                            break;
                    }

                    int sampleCount = frameData.sampleCount;
                    for (int i = 0; i < sampleCount; ++i)
                    {
                        if (shaderCompileMakerId != frameData.GetSampleMarkerId(i))
                            continue;

                        var shaderName = frameData.GetSampleMetadataAsString(i, 0);
                        var pass = frameData.GetSampleMetadataAsString(i, 1);
                        var stage = frameData.GetSampleMetadataAsString(i, 2);
                        var keyword = frameData.GetSampleMetadataAsString(i, 3);

                        var compieInfo = new ShaderCompileInfo()
                        {
                            frameIdx = frameIdx,
                            shaderName = shaderName,
                            pass = pass,
                            stage = stage,
                            keyword = keyword,
                        };
                        if(buffer == null)
                        {
                            buffer = new List<ShaderCompileInfo>();
                        }
                        buffer.Add(compieInfo);
                    }
                }
            }

            if (buffer != null)
            {
                this.compileInfoByFrameIdx.Add(frameIdx, buffer);
                isDirty = true;
            }
            latestFrameIndex = frameIdx;
        }

        public void RemoveOldFrames(int frameIdx)
        {
            this.indexBuffer.Clear();
            foreach (var idx in this.compileInfoByFrameIdx.Keys)
            {
                if(idx < frameIdx)
                {
                    this.indexBuffer.Add(idx);
                }
            }
            foreach (var idx in this.indexBuffer)
            {
                this.compileInfoByFrameIdx.Remove(idx);
                isDirty = true;
            }
        }

        public void ClearData()
        {
            latestFrameIndex = 0;
            this.compileInfoByFrameIdx.Clear();
            isDirty = true;
        }
    }
}