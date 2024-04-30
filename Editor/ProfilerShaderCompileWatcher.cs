using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine.Profiling;
using UnityEditor;
using System.Text;
using System.IO;
using Unity.Profiling.Editor;
using Unity.Profiling;

namespace UTJ.Profiler.ShaderCompileModule
{
    internal class ProfilerShaderCompileWatcher 
    {
        private const string csvHeader = "frameIdx,Shader,exec(ms),isWarmupCall,pass,stage,keyword,platform,\n";
        static readonly ProfilerCounterDescriptor k_PlatformCoderDescriptor = new ProfilerCounterDescriptor("ShaderCompile Platform", ProfilerCategory.Scripts);

        private int m_shaderCompileMakerId = FrameDataView.invalidMarkerId;

        private List<int> m_indexBuffer = new List<int>();
        private List<ShaderCompileInfo> m_allFrameBuffer = new List<ShaderCompileInfo>();
        private Dictionary<int, List<ShaderCompileInfo>> m_compileInfoByFrameIdx = new Dictionary<int, List<ShaderCompileInfo>>();
        private bool m_isDirty = true;
        private int m_latestFrameIndex = -1;
        private int m_latestCompileFrameIdx = -1;

        private ShaderVariantCollection m_targetAsset = null;
        private StringBuilder m_stringBuilder = new StringBuilder();
        private string m_logFile;
        private long m_lastLogFrameIdx = -1;
        private bool m_enableLog = false;

        public int latestCompileFrameIdx
        {
            get
            {
                return m_latestCompileFrameIdx;
            }
        }

        public void SetLogEnabled(bool flag)
        {
            m_enableLog = flag;
            if (flag)
            {
                m_indexBuffer.Clear();
                foreach (var frameIdx in m_compileInfoByFrameIdx.Keys)
                {
                    m_indexBuffer.Add(frameIdx);
                }
                m_indexBuffer.Sort();
                foreach(var idx in m_indexBuffer)
                {
                    if(idx > this.m_lastLogFrameIdx)
                    {
                        this.AddToLogFile(m_compileInfoByFrameIdx[idx], idx);
                    }
                }
            }
        }


        public void ScanLatest()
        {
            int idx = m_latestFrameIndex;
            if( idx < ProfilerDriver.firstFrameIndex)
            {
                idx = ProfilerDriver.firstFrameIndex;
            }
            for (int i = idx; i < ProfilerDriver.lastFrameIndex; i++)
            {
                if( !ScanFrame(i))
                {
                    break;
                }
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
                if (m_isDirty)
                {
                    m_allFrameBuffer.Clear();
                    foreach(var kvs in m_compileInfoByFrameIdx)
                    {
                        var frameCompiles = kvs.Value;
                        if(frameCompiles != null)
                        {
                            foreach(var compile in frameCompiles)
                            {
                                m_allFrameBuffer.Add(compile);
                            }
                        }
                    }
                    m_isDirty = false;
                }
                return m_allFrameBuffer;
            }
        }

        private bool isDirtyAllList
        {
            get { return m_isDirty; }
        }

        public List<ShaderCompileInfo> GetFrameCompiles(int frameIdx)
        {
            List<ShaderCompileInfo> list;
            if(m_compileInfoByFrameIdx.TryGetValue(frameIdx, out list))
            {
                return list;
            }
            return null;
        }

        public bool ScanFrame(int frameIdx)
        {

            RuntimePlatform platform = RuntimePlatform.WindowsEditor;

            if (m_latestFrameIndex >= frameIdx) { return true; }
            List<ShaderCompileInfo> buffer = null;
            for (int threadIndex = 0; ; ++threadIndex)
            {
                using (RawFrameDataView frameData = ProfilerDriver.GetRawFrameDataView(frameIdx, threadIndex))
                {
                    if (!frameData.valid)
                    {
                        //Debug.LogError("frameData invalid " + frameIdx +"--" + threadIndex);
                        //return true;
                        if (threadIndex == 0)
                        {
                            return false;
                        }
                        else { 
                            break;
                        }
                    }

                    // setup platform code
                    if(threadIndex == 0)
                    {
                        var maker = frameData.GetMarkerId(k_PlatformCoderDescriptor.Name);
                        int platformCode = (int)frameData.GetCounterValueAsInt(maker);
                        platform = (RuntimePlatform)platformCode;

                    }


                    if (m_shaderCompileMakerId == FrameDataView.invalidMarkerId)
                    {
                        m_shaderCompileMakerId = frameData.GetMarkerId("Shader.CreateGPUProgram");
                        if (m_shaderCompileMakerId == FrameDataView.invalidMarkerId)
                            break;
                    }

                    int sampleCount = frameData.sampleCount;
                    for (int i = 0; i < sampleCount; ++i)
                    {
                        if (m_shaderCompileMakerId != frameData.GetSampleMarkerId(i))
                            continue;
                        if(frameData.GetSampleMetadataCount(i) < 4)
                        {
                            m_shaderCompileMakerId = FrameDataView.invalidMarkerId;
                            continue;
                        }

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
                            platform = platform,
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
                this.m_compileInfoByFrameIdx.Add(frameIdx, buffer);
                m_isDirty = true;
                m_latestCompileFrameIdx = frameIdx;
            }
            m_latestFrameIndex = frameIdx;

            // Set to ShaderVariantCollection
            AddToShaderVariantCollection(buffer);
            if (m_enableLog && frameIdx > this.m_lastLogFrameIdx)
            {
                AddToLogFile(buffer, frameIdx);
            }
            return true;
        }

        public bool RemoveOldFrames(int frameIdx)
        {
            this.m_indexBuffer.Clear();
            foreach (var idx in this.m_compileInfoByFrameIdx.Keys)
            {
                if(idx < frameIdx)
                {
                    this.m_indexBuffer.Add(idx);
                }
            }
            foreach (var idx in this.m_indexBuffer)
            {
                this.m_compileInfoByFrameIdx.Remove(idx);
                m_isDirty = true;
            }
            return m_isDirty;
        }

        public void ClearData()
        {
            this.m_latestFrameIndex = -1;
            this.m_latestCompileFrameIdx = -1;
            this.m_compileInfoByFrameIdx.Clear();
            this.m_isDirty = true;
            this.m_lastLogFrameIdx = -1;
            this.m_shaderCompileMakerId = FrameDataView.invalidMarkerId;
        }
        public void ClearMakerId()
        {
            this.m_shaderCompileMakerId = FrameDataView.invalidMarkerId;
        }

        private void AddToShaderVariantCollection(List<ShaderCompileInfo> compileInfoList)
        {
            if(m_targetAsset == null) {
                return;
            }
            if(compileInfoList == null) { return; }
            foreach (var info in compileInfoList)
            {
                var variant = info.GetShaderariant();
                if(variant.shader == null)
                {
                    Debug.LogError("[AddToShaderVariantCollection] can not find shader " + info.shaderName);
                    continue;
                }
                if ( !m_targetAsset.Contains(variant))
                {
                    m_targetAsset.Add(variant);
                }
            }
        }
        private void AddToLogFile(List<ShaderCompileInfo> compileInfoList,long frameIdx)
        {
            if (compileInfoList == null) { return; }
            if (string.IsNullOrEmpty(this.m_logFile))
            {
                return;
            }
            if(compileInfoList.Count == 0)
            {
                return;
            }
            m_stringBuilder.Clear();

            if (m_lastLogFrameIdx == -1)
            {
                if (!System.IO.File.Exists(this.m_logFile))
                {
                    string dir = System.IO.Path.GetDirectoryName(this.m_logFile);
                    if (!System.IO.Directory.Exists(dir))
                    {
                        System.IO.Directory.CreateDirectory(dir);
                    }
                    System.IO.File.WriteAllText(m_logFile, csvHeader);
                }
            }

            foreach (var info in compileInfoList)
            {
                m_stringBuilder.Append(info.frameIdx).Append(",").
                    Append(info.shaderName).Append(",").
                    Append(info.timeMs).Append(",unknown,").
                    Append(info.pass).Append(",").
                    Append(info.stage).Append(",").
                    Append(info.keyword).Append(",").
                    Append(info.platform.ToString()).Append(",\n");
            }
            this.m_lastLogFrameIdx = frameIdx;
            File.AppendAllText(m_logFile, m_stringBuilder.ToString());
        }

        public void SetLogFile(string file,bool enable)
        {
            this.m_lastLogFrameIdx = -1;
            this.m_logFile = file;
            this.m_enableLog = enable;
        }

        public void SetTarget(ShaderVariantCollection collection)
        {
            this.m_targetAsset = collection;
            if (collection != null)
            {
                foreach (var buffer in this.m_compileInfoByFrameIdx.Values)
                {
                    AddToShaderVariantCollection(buffer);
                }

            }
        }

        public void ExportToCsv(string file)
        {
            var sb = new StringBuilder();
            sb.Append(csvHeader);
            foreach(var info in this.allCompileInProfiler)
            {
                sb.Append(info.frameIdx).Append(",").
                    Append(info.shaderName).Append(",").
                    Append(info.timeMs).Append(",unknown,").
                    Append(info.pass).Append(",").
                    Append(info.stage).Append(",").
                    Append(info.keyword).Append(",").
                    Append(info.platform.ToString()).Append(",\n");
            }
            File.WriteAllText(file, sb.ToString());
        }
    }
}