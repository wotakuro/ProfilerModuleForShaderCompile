using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine.Profiling;
using UnityEditor;


namespace UTJ.Profiler.ShaderCompileModule
{
    internal class ProfilerShaderCompileWatcher : EditorWindow
    {
        private class ShaderCompileInfo
        {
            public int frameIdx;
            public string shaderName;
            public string pass;
            public string stage;
            public string keyword;

        }

        [MenuItem("Tools/ProfilerShaderCompileWatcher")]
        public static void Create()
        {
            ProfilerShaderCompileWatcher.GetWindow<ProfilerShaderCompileWatcher>();
        }

        int shaderCompileMakerId = FrameDataView.invalidMarkerId;
        private Vector2 scrollPos;
        List<ShaderCompileInfo> compileInfos = new List<ShaderCompileInfo>();

        private void OnGUI()
        {
            if (GUILayout.Button("Scan"))
            {
                Scan();
            }
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var compileInfo in compileInfos)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(compileInfo.frameIdx.ToString(), GUILayout.Width(40));
                EditorGUILayout.LabelField(compileInfo.shaderName, GUILayout.Width(250));
                EditorGUILayout.LabelField(compileInfo.pass, GUILayout.Width(80));
                EditorGUILayout.LabelField(compileInfo.stage, GUILayout.Width(60));
                EditorGUILayout.LabelField(compileInfo.keyword);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        void Scan()
        {
            for (int i = ProfilerDriver.firstFrameIndex; i < ProfilerDriver.lastFrameIndex; i++)
            {
                Exec(i);
            }
        }

        void Exec(int frameIdx)
        {
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
                        compileInfos.Add(compieInfo);
                    }
                }
            }
        }
    }
}