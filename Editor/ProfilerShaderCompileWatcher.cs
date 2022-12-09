using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine.Profiling;
using UnityEditor;
using System.Text;

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

        private ShaderVariantCollection targetAsset = null;
        private StringBuilder stringBuilder = new StringBuilder();
        private string logFile;
        private bool isFirstLog = true;


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
                        var timeMs = frameData.GetSampleTimeMs(i);
                        var compieInfo = new ShaderCompileInfo()
                        {
                            frameIdx = frameIdx,
                            shaderName = shaderName,
                            pass = pass,
                            stage = stage,
                            keyword = keyword,
                            timeMs = timeMs,
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

            // Set to ShaderVariantCollection
            AddToShaderVariantCollection(buffer);
            AddToLogFile(buffer);
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

        private void AddToShaderVariantCollection(List<ShaderCompileInfo> compileInfoList)
        {
            if(targetAsset == null) { return; }
            if(compileInfoList == null) { return; }
            foreach (var info in compileInfoList)
            {
                var variant = info.GetShaderariant();
                if (!targetAsset.Contains(variant))
                {
                    targetAsset.Add(variant);
                }
            }
        }
        private void AddToLogFile(List<ShaderCompileInfo> compileInfoList)
        {
            if (compileInfoList == null) { return; }
            if (string.IsNullOrEmpty(this.logFile))
            {
                return;
            }
            if(compileInfoList.Count == 0)
            {
                return;
            }
            stringBuilder.Clear();

            if (isFirstLog)
            {
                if (!System.IO.File.Exists(this.logFile))
                {
                    string dir = System.IO.Path.GetDirectoryName(this.logFile);
                    if (!System.IO.Directory.Exists(dir))
                    {
                        System.IO.Directory.CreateDirectory(dir);
                    }
                    string header = "frameIdx,Shader,exec(ms),isWarmupCall,pass,stage,keyword,\n";
                    System.IO.File.WriteAllText(logFile, header);
                }
                isFirstLog = false;
            }

            foreach (var info in compileInfoList)
            {
                stringBuilder.Append(info.frameIdx).Append(",").
                    Append(info.shaderName).Append(",").
                    Append(info.timeMs).Append(",unknown,").
                    Append(info.pass).Append(",").
                    Append(info.stage).Append(",").
                    Append(info.keyword).Append(",\n");
            }
            System.IO.File.AppendAllText(logFile, stringBuilder.ToString());
        }

        public void SetLogFile(string file)
        {
            this.isFirstLog = (this.logFile != file);
            this.logFile = file;
        }

        public void SetTarget(ShaderVariantCollection collection)
        {
            this.targetAsset = collection;
            if (collection != null)
            {
                foreach (var buffer in this.compileInfoByFrameIdx.Values)
                {
                    AddToShaderVariantCollection(buffer);
                }

            }
        }
    }
}