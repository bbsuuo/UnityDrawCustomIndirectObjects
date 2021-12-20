using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JustEngine.JustGraphcis
{
    [ExecuteAlways]
    public class BoundsIndirectRenderer : IndirectRenderer
    {
        [SerializeField]
        protected ComputeShader indirectComputerShader;
        [SerializeField]
        private int randomSeed;
        [SerializeField]
        private Bounds bounds;
        [SerializeField]
        private Vector2 minMaxSize = Vector2.one;
        [SerializeField]
        private Vector2 minMaxRotationX;
        [SerializeField]
        private Vector2 minMaxRotationY;
        [SerializeField]
        private Vector2 minMaxRotationZ;

        private Bounds viewBounds;

        private ComputeBuffer positionBuffer;
        private ComputeBuffer visualbleBuffer;

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        private void LateUpdate()
        {
            if (drawBySelf) 
            {
                UpdateRender(null);
            }
        }


        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = Color.white;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
        protected override void UpdateBuffers()
        {
            base.UpdateBuffers();
            bool needUpdateTransformBuffer = (positionBuffer == null || positionBuffer.count != instanceCount);
            if (needUpdateTransformBuffer)
            {
                if (visualbleBuffer != null)
                {
                    visualbleBuffer.Release();
                    visualbleBuffer = null;
                }

                if (positionBuffer != null)
                {
                    positionBuffer.Release();
                    positionBuffer = null;
                }
                int positionStride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Matrix4x4));
                positionBuffer = new ComputeBuffer(InstanceCount, positionStride);
                visualbleBuffer = new ComputeBuffer(InstanceCount, sizeof(uint), ComputeBufferType.Append);
            }

            //Set Buffers
            if (indirectComputerShader)
            {
                int kernelIndex = indirectComputerShader.FindKernel("PositionCalculate");
                indirectComputerShader.SetBuffer(kernelIndex, "_Positions", positionBuffer);
                var min = bounds.min;
                var max = bounds.max;
                indirectComputerShader.SetInt("randomState", Mathf.Abs(randomSeed));
                indirectComputerShader.SetVector("minBound", new Vector4(min.x, min.y, min.z, minMaxSize.x));
                indirectComputerShader.SetVector("maxBound", new Vector4(max.x, max.y, max.z, minMaxSize.y));
                indirectComputerShader.SetVector("rotationX",minMaxRotationX);
                indirectComputerShader.SetVector("rotationY", minMaxRotationY);
                indirectComputerShader.SetVector("rotationZ", minMaxRotationZ);
                if (instanceMesh)
                {
                    indirectComputerShader.SetVector("meshBoundsCenter", instanceMesh.bounds.center);
                    indirectComputerShader.SetVector("meshBoundsExtents", instanceMesh.bounds.extents);
                }
                //Calculate Positions
                indirectComputerShader.Dispatch(kernelIndex, instanceCount / 8, 1, 1);

                if (FrustumCulling)
                {
                    indirectComputerShader.EnableKeyword("EnableCulling");
                }
                else
                {
                    indirectComputerShader.DisableKeyword("EnableCulling");
                }
                if (HzCulling)
                {
                    indirectComputerShader.EnableKeyword("EnableWithHZCulling");
                }
                else
                {
                    indirectComputerShader.DisableKeyword("EnableWithHZCulling");
                }

            }



        }
        protected override void UpdateRender(CommandBuffer cmd)
        {
            if (material == null) return;
            if (cullingCamera == null)
            {
                cullingCamera = Camera.main;
            }
            if (cmd == null)
            {
                Matrix4x4 v = cullingCamera.worldToCameraMatrix;
                Matrix4x4 p = cullingCamera.projectionMatrix;


                indirectComputerShader.SetVector("_CameraPos", cullingCamera.transform.position);
                indirectComputerShader.SetMatrix("_VPMatrix", p * v);
                indirectComputerShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
                indirectComputerShader.SetMatrix("_WorldToLocal",transform.worldToLocalMatrix);
                visualbleBuffer.SetCounterValue(0);

                int cullingKernelIndex = indirectComputerShader.FindKernel("CullingCalculate");
                indirectComputerShader.SetBuffer(cullingKernelIndex, "_Positions", positionBuffer);
                indirectComputerShader.SetBuffer(cullingKernelIndex, "_VisibleInstanceIds", visualbleBuffer);
                indirectComputerShader.Dispatch(cullingKernelIndex, instanceCount / 8, 1, 1);
                ComputeBuffer.CopyCount(visualbleBuffer, argsBuffer, 4);
                material.SetBuffer("_VisibleInstanceIds", visualbleBuffer);
                material.SetBuffer("_Positions", positionBuffer);
            }
            else
            {
                Matrix4x4 v = cullingCamera.worldToCameraMatrix;
                Matrix4x4 p = cullingCamera.projectionMatrix;
                cmd.SetComputeVectorParam(indirectComputerShader, "_CameraPos", cullingCamera.transform.position);
                cmd.SetComputeMatrixParam(indirectComputerShader, "_VPMatrix", p * v);
                cmd.SetComputeMatrixParam(indirectComputerShader, "_LocalToWorld", transform.localToWorldMatrix);
                cmd.SetComputeMatrixParam(indirectComputerShader, "_WorldToLocal", transform.worldToLocalMatrix);
#if UNITY_2021_1_OR_NEWER
                cmd.SetBufferCounterValue(visualbleBuffer, 0);
#else
                cmd.SetComputeBufferCounterValue(visualbleBuffer, 0);
#endif
                int cullingKernelIndex = indirectComputerShader.FindKernel("CullingCalculate");
                cmd.SetComputeBufferParam(indirectComputerShader, cullingKernelIndex, "_Positions", positionBuffer);
                cmd.SetComputeBufferParam(indirectComputerShader, cullingKernelIndex, "_VisibleInstanceIds", visualbleBuffer);
                cmd.DispatchCompute(indirectComputerShader, cullingKernelIndex, instanceCount / 8, 1, 1);
                cmd.CopyCounterValue(visualbleBuffer, argsBuffer, 4);
                material.SetBuffer("_VisibleInstanceIds", visualbleBuffer);
                material.SetBuffer("_Positions", positionBuffer);
            }
            //Set Buffer
            base.UpdateRender(cmd);
        }
        protected override Bounds CalculateBounds()
        {
            viewBounds = new Bounds();
            var min = bounds.min;
            var max = bounds.max;
            viewBounds.Encapsulate(transform.TransformPoint(min));
            viewBounds.Encapsulate(transform.TransformPoint(max));
            return viewBounds;
        }
        protected override bool PrepareProperty()
        {
            bool positionPrepare = positionBuffer != null && positionBuffer.IsValid();
            return base.PrepareProperty() && positionPrepare && indirectComputerShader != null;
        }
        protected override void ReleaseBuffers()
        {
            base.ReleaseBuffers();
            if (visualbleBuffer != null)
            {
                visualbleBuffer.Release();
                visualbleBuffer = null;
            }

            if (positionBuffer != null)
            {
                positionBuffer.Release();
                positionBuffer = null;
            }
        }



    }

#if UNITY_EDITOR
    [CustomEditor(typeof(BoundsIndirectRenderer))]
    public class BoundsIndirectRendererEditor : Editor
    {
        SerializedProperty drawBySelfProp;
        SerializedProperty subMeshIndexProp;
        SerializedProperty instanceCountProp;
        SerializedProperty computerShaderProp;

        SerializedProperty instanceMeshProp;
        SerializedProperty autoCreateMaterialProp;
        SerializedProperty instanceMaterialProp;
        SerializedProperty createMaterialShaderProp;
        SerializedProperty createMaterialProp;

        SerializedProperty cullingCameraProp;
        SerializedProperty frustumCullingProp;
        SerializedProperty hzCullingProp;

        SerializedProperty randomSeedProp;
        SerializedProperty boundsProp;
        SerializedProperty minMaxScaleProp;
        SerializedProperty minMaxRXProp;
        SerializedProperty minMaxRYProp;
        SerializedProperty minMaxRZProp;

        private void OnEnable()
        {
            drawBySelfProp = serializedObject.FindProperty("drawBySelf");
            subMeshIndexProp = serializedObject.FindProperty("subMeshIndex");
            computerShaderProp = serializedObject.FindProperty("indirectComputerShader");
            instanceCountProp = serializedObject.FindProperty("instanceCount");
            instanceMeshProp = serializedObject.FindProperty("instanceMesh");

            autoCreateMaterialProp = serializedObject.FindProperty("createMaterialByShader");
            createMaterialShaderProp = serializedObject.FindProperty("materialShader");
            createMaterialProp = serializedObject.FindProperty("createByShaderMaterials");
            instanceMaterialProp = serializedObject.FindProperty("instanceMaterials");

            cullingCameraProp = serializedObject.FindProperty("cullingCamera");
            frustumCullingProp = serializedObject.FindProperty("frustumCulling");
            hzCullingProp = serializedObject.FindProperty("hzCulling");

            randomSeedProp = serializedObject.FindProperty("randomSeed");
            boundsProp = serializedObject.FindProperty("bounds");
            minMaxScaleProp = serializedObject.FindProperty("minMaxSize");
            minMaxRXProp = serializedObject.FindProperty("minMaxRotationX");
            minMaxRYProp = serializedObject.FindProperty("minMaxRotationY");
            minMaxRZProp = serializedObject.FindProperty("minMaxRotationZ");
        }

        private void OnDisable()
        {
            ReleaseMaterial();
        }

        bool mainConfig = true;
        bool materialConfig = true;
        bool cullingConfig = true;
        bool positionConfig = true;

        GUIContent drawBySelfContent = new GUIContent("自身调用渲染(不同的渲染流程,开启无hz但是也不会产生多余的计算)");
        GUIContent subMeshIndexPropContent = new GUIContent("子网格渲染索引");
        GUIContent computerShaderPropContent = new GUIContent("ComputeShader");
        GUIContent instanceCountPropContent = new GUIContent("渲染数量");
        GUIContent instanceMeshPropContent = new GUIContent("渲染网格");
        GUIContent createShaderPropContent = new GUIContent("Shader");

        GUIContent cullingCameraPropContent = new GUIContent("剪裁相机");
        GUIContent frustumCullingPropPropContent = new GUIContent("视锥剪裁");
        GUIContent hzCullingPropContent = new GUIContent("Hz剪裁(需要配置Feature)");

        string[] materialMode = new string[2]{ "Shader创建实例","自定义材质球" };
        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            DrawScriptArea();
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("因为仅在MainCamera下计算,因此未开启游戏时编辑器下可能会有异常显示");
            if (mainConfig = EditorGUILayout.BeginToggleGroup("基础设置", mainConfig)) 
            {
                EditorGUILayout.PropertyField(drawBySelfProp, drawBySelfContent);
                EditorGUILayout.PropertyField(subMeshIndexProp, subMeshIndexPropContent);
                EditorGUILayout.PropertyField(computerShaderProp, computerShaderPropContent);
                EditorGUILayout.PropertyField(instanceMeshProp, instanceMeshPropContent);
                EditorGUILayout.PropertyField(instanceCountProp, instanceCountPropContent);
            }
            EditorGUILayout.EndToggleGroup();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(EditorStyles.helpBox);
            if (materialConfig = EditorGUILayout.BeginToggleGroup("材质设置", materialConfig)) 
            {

                //autoCreateMaterialProp
                int materialModeSelect = autoCreateMaterialProp.boolValue ? 0 : 1;
                bool currentModeIsShaderMode =  GUILayout.Toolbar(materialModeSelect, materialMode) == 0;
                if (currentModeIsShaderMode != autoCreateMaterialProp.boolValue) 
                {
                    autoCreateMaterialProp.boolValue = currentModeIsShaderMode;
                }
                if (currentModeIsShaderMode)
                {
                    EditorGUILayout.PropertyField(createMaterialShaderProp, createShaderPropContent);
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(createMaterialProp);
                    EditorGUI.EndDisabledGroup();
                }
                else 
                {
                    EditorGUILayout.PropertyField(instanceMaterialProp);
                }
            }
            EditorGUILayout.EndToggleGroup();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(EditorStyles.helpBox);
            if (cullingConfig = EditorGUILayout.BeginToggleGroup("剪裁设置", cullingConfig))
            {
                EditorGUILayout.PropertyField(cullingCameraProp, cullingCameraPropContent);
                EditorGUILayout.PropertyField(frustumCullingProp, frustumCullingPropPropContent);
                EditorGUILayout.PropertyField(hzCullingProp, hzCullingPropContent);
 
            }
            EditorGUILayout.EndToggleGroup();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(EditorStyles.helpBox);
            if (positionConfig = EditorGUILayout.BeginToggleGroup("位置计算", positionConfig)) 
            {
                EditorGUILayout.PropertyField(randomSeedProp);
                EditorGUILayout.PropertyField(boundsProp);

                GUILayout.Label("缩放",EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(minMaxScaleProp);

                GUILayout.Label("旋转", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(minMaxRXProp);
                EditorGUILayout.PropertyField(minMaxRYProp);
                EditorGUILayout.PropertyField(minMaxRZProp);
            }
            EditorGUILayout.EndToggleGroup();

            GUILayout.EndVertical();

            if (serializedObject.ApplyModifiedProperties()) 
            {
                ReleaseMaterial();
            }

            GUILayout.Space(25);
            GUILayout.FlexibleSpace();
             var mEditor =  GetMaterialEditor(autoCreateMaterialProp.boolValue);
            if (mEditor) 
            {
                mEditor.DrawHeader();
                mEditor.OnInspectorGUI();
            }
        }

        void DrawScriptArea()
        {
            EditorGUI.BeginDisabledGroup(true);
            SerializedProperty property = serializedObject.GetIterator();
            property.NextVisible(true);
            EditorGUILayout.PropertyField(property, true);
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(3f);
        }

        MaterialEditor createByShaderMaterialEditor;
        MaterialEditor instanceMaterialEditor;

        MaterialEditor GetMaterialEditor(bool shaderMode) 
        {
            if (shaderMode)
            {
                if (createByShaderMaterialEditor == null) 
                {
                    Material material = createMaterialProp.objectReferenceValue as Material;
                    if(material) createByShaderMaterialEditor = (MaterialEditor)CreateEditor(material);
                }
                return createByShaderMaterialEditor;
            }
            else 
            {
                if (instanceMaterialEditor == null)
                {
                    Material material = instanceMaterialProp.objectReferenceValue as Material;
                    if (material) instanceMaterialEditor = (MaterialEditor)CreateEditor(material);
                }
                return instanceMaterialEditor;
            }
        }

        void ReleaseMaterial() 
        {
            if (createByShaderMaterialEditor) 
            {
                DestroyImmediate(createByShaderMaterialEditor);
                createByShaderMaterialEditor = null;
            }

            if (instanceMaterialEditor) 
            {
                DestroyImmediate(instanceMaterialEditor);
                instanceMaterialEditor = null;
            }
        }

    }

#endif
}