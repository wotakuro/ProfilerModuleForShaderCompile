using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UTJ.Profiler.ShaderCompileModule
{
    internal class ShaderCompileRowUI
    {

        private class VisualElementData
        {
            public VisualElement element;
            public Label frameIdx;
            public Label shader;
            public Label time;
            public Label pass;
            public Label stage;
            public Label keyword;
        }

        const string k_UxmlRowResourceName = "Packages/com.utj.profilermodule.shadercompile/Editor/UXML/compileInfo.uxml";
        VisualTreeAsset template;

        private VisualElementData defaultData;
        private Stack<VisualElementData> elementsPool = new Stack<VisualElementData>();
        private List<VisualElementData> elementsActive = new List<VisualElementData>();

        public ShaderCompileRowUI()
        {

        }

        public void ReleaseAllNodes()
        {
            foreach(var active in elementsActive)
            {
                this.elementsPool.Push(active);
            }
            elementsActive.Clear();
        }

        public VisualElement CreateNode(ShaderCompileInfo info)
        {
            var node = GetOrCreateNode();
            elementsActive.Add(node);

            node.frameIdx.text = info.frameIdx.ToString();
            node.shader.text = info.shaderName;
            node.time.text = info.timeMs.ToString("0.00")+"ms";
            node.pass.text = info.pass;
            node.stage.text = info.stage;
            node.keyword.text = info.keyword;

            return node.element;
        }
        private void InitTemplate()
        {
            if (template == null)
            {
                template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlRowResourceName);
                this.defaultData = CreateData(template.Instantiate());
                this.SetUpWidth(this.defaultData);
                this.defaultData.element.style.height = 20;
                this.defaultData.element.style.flexShrink = 0.0f;
            }

        }
        public VisualElement GetDefaultElement()
        {
            this.InitTemplate();
            return this.defaultData.element;
        }

        private VisualElementData GetOrCreateNode()
        {
            if(elementsPool.Count > 0)
            {
                return elementsPool.Pop();
            }
            InitTemplate();

            var elem =  template.Instantiate();
            VisualElementData data = CreateData(elem);
            SetUpWidth(data);
            return data;
        }

        private VisualElementData CreateData(VisualElement elem)
        {
            VisualElementData data = new VisualElementData()
            {
                element = elem,
                frameIdx = elem.Q<Label>("frameIdx"),
                shader = elem.Q<Label>("shader"),
                time = elem.Q<Label>("time"),
                pass = elem.Q<Label>("pass"),
                stage = elem.Q<Label>("stage"),
                keyword = elem.Q<Label>("keyword"),
            };
            return data;
        }

        private void SetUpWidth(VisualElementData elementData)
        {
            elementData.frameIdx.style.width = 40;
            elementData.shader.style.width = 280;
            elementData.time.style.width = 70;
            elementData.pass.style.width = 120;
            elementData.stage.style.width = 50;

        }
    }
}
