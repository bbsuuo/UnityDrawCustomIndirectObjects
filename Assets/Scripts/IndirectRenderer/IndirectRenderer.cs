using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace JustEngine.JustGraphcis
{
    //[ExecuteAlways]
    public abstract class IndirectRenderer : MonoBehaviour
    {
        public bool drawBySelf = true;
        [SerializeField]
        protected Mesh instanceMesh;
        [SerializeField]
        protected int subMeshIndex;
        [SerializeField]
        public bool createMaterialByShader;
        [SerializeField]
        protected Material createByShaderMaterials;
        [SerializeField]
        protected Shader materialShader;
        [SerializeField]
        protected Material instanceMaterials;
        [SerializeField, Range(8, 20000)]
        protected int instanceCount;

        protected ComputeBuffer argsBuffer;
        protected Material material 
        {
            get 
            {
                if (createMaterialByShader)
                {
                    return createByShaderMaterials;
                }
                else 
                {
                    return instanceMaterials;
                }
            }
        }

        [SerializeField]
        protected Camera cullingCamera;
        [SerializeField]
        private bool frustumCulling;
        [SerializeField]
        private bool hzCulling;
 
        public int InstanceCount
        {
            get
            {
                return instanceCount;
            }
            set
            {
                instanceCount = value; 
                UpdateBuffers();
            }
        }
        protected bool FrustumCulling { get => frustumCulling; }
        protected bool HzCulling { get => hzCulling; }
        protected virtual void Start()
        {

        }

        protected virtual void OnDestroy()
        {
            ReleaseBuffers();
            if (createMaterialByShader && createByShaderMaterials)
            {
                GameObject.DestroyImmediate(createByShaderMaterials, true);
            }
        }

        protected virtual void OnEnable()
        {
            UpdateBuffers();
            IndirectRenderStack.Register(this);
        }

        protected virtual void OnDisable()
        {
            IndirectRenderStack.UnRegister(this);
            ReleaseBuffers();
        }

        public void CallRender(CommandBuffer cmd)
        {
            if (PrepareProperty())
            {
                UpdateRender(cmd);
            }
        }

        private void OnValidate()
        {
            UpdateBuffers();
        }


        protected virtual void OnDrawGizmosSelected()
        {

        }

        /// <summary>
        /// if config change,will update buffers
        /// </summary>
        protected virtual void UpdateBuffers()
        {
            if (createMaterialByShader) 
            {
                if (createByShaderMaterials != null && (createByShaderMaterials.shader != materialShader))
                {
                    GameObject.DestroyImmediate(createByShaderMaterials, true);
                    createByShaderMaterials = null;
                }
                if (createByShaderMaterials == null && materialShader)
                    createByShaderMaterials = new Material(materialShader);
            }


            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount);



            if (argsBuffer == null || !argsBuffer.IsValid())
                argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            //init args
            if (argsBuffer != null && argsBuffer.IsValid())
            {
                uint[] args = new uint[5]
                {
                    (uint)instanceMesh.GetIndexCount(subMeshIndex),
                    (uint)instanceCount,
                    (uint)instanceMesh.GetIndexStart(subMeshIndex),
                    (uint)instanceMesh.GetBaseVertex(subMeshIndex),
                    0
                };

                argsBuffer.SetData(args);
            }

        }

        protected virtual void UpdateRender(CommandBuffer cmd)
        {
            material.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
            if (cmd == null)
            {
                Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, material, CalculateBounds(), argsBuffer);
            }
            else
            {
                cmd.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, material, 0, argsBuffer);
            }
        }

        protected virtual Bounds CalculateBounds()
        {
            return new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f));
        }

        protected virtual bool PrepareProperty()
        {
            bool meshPrepare = instanceMesh != null;
            bool meshCountPrepare = instanceCount != 0;
            bool materialPrepare = material != null;
            bool argsBufferPreapare = argsBuffer != null && argsBuffer.IsValid();
            return meshPrepare && meshCountPrepare && materialPrepare && argsBufferPreapare;
        }

        public void SetInstanceCount(float count)
        {
            this.instanceCount = (int)count;
            UpdateBuffers();
        }

        protected virtual void ReleaseBuffers()
        {
            if (argsBuffer != null)
            {
                argsBuffer.Release();
                argsBuffer = null;
            }
        }
    }



}

