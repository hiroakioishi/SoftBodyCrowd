using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace irishoak.SoftBodyCrowd
{
    [RequireComponent(typeof(GPUBoids))]
    [RequireComponent(typeof(GPUBoidsTrail))]
    public class GPUBoidsRender : MonoBehaviour
    {
        [SerializeField]
        float _pathScale = 1.0f;
        [SerializeField]
        float _pathOffset = 0.0f;
        [SerializeField]
        float _thickness = 1.0f;

        GPUBoids _boidsScript;
        GPUBoidsTrail _boidsTrailScript;

        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        ComputeBuffer _argsBuffer;

        [SerializeField]
        Material _renderMat;
        
        [SerializeField]
        Mesh _instanceMesh;

        void Awake()
        {
            _boidsScript      = GetComponent<GPUBoids>();
            _boidsTrailScript = GetComponent<GPUBoidsTrail>();
        }

        void Start()
        {
            _argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        }

        void Update()
        {
            RenderInstancedMesh();
        }

        void OnDestroy()
        {
            if (_argsBuffer != null)
                _argsBuffer.Release();
            _argsBuffer = null;
        }

        void RenderInstancedMesh()
        {
            if (_renderMat == null || _boidsScript == null || _boidsTrailScript == null || !SystemInfo.supportsInstancing)
                return;

            uint numIndices = (_instanceMesh != null) ? (uint)_instanceMesh.GetIndexCount(0) : 0;
            args[0] = numIndices;
            args[1] = (uint)_boidsScript.GetMaxObjectNum(); 
            _argsBuffer.SetData(args);

            _renderMat.SetBuffer("_BoidDataBuffer", _boidsScript.GetBoidDataBuffer());

            _renderMat.SetTexture("_PositionBuffer", _boidsTrailScript.GetPositionBuffer());
            _renderMat.SetTexture("_NormalBuffer", _boidsTrailScript.GetNormalBuffer());
            _renderMat.SetTexture("_BinormalBuffer", _boidsTrailScript.GetBinormalBuffer());

            _renderMat.SetFloat("_PathOffset", _pathOffset);
            _renderMat.SetFloat("_PathScale", _pathScale);
            _renderMat.SetFloat("_Thickness", _thickness);

            
            var bounds = new Bounds
            (
                _boidsScript.GetSimulationAreaCenter(),
                _boidsScript.GetSimulationAreaSize()
            );

            Graphics.DrawMeshInstancedIndirect
            (
                _instanceMesh,
                0,
                _renderMat,
                bounds,
                _argsBuffer
            );
        }
    }
}