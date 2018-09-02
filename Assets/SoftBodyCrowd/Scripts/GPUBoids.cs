using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace irishoak.SoftBodyCrowd
{
    public class GPUBoids : MonoBehaviour
    {

        struct BoidData
        {
            public Vector3 Velocity;
            public Vector3 Position;
        };

        const int SIMULATION_BLOCK_SIZE = 256;

        [SerializeField, Range(256, 16384)]
        int _maxObjectNum = 2048;

        [SerializeField]
        float _cohesionNeighborhoodRadius  = 2.0f;

        [SerializeField]
        float _alignmentNeighborhoodRadius = 2.0f;

        [SerializeField]
        float _separateNeighborhoodRadius  = 1.0f;

        [SerializeField]
        float _maxSpeed = 5.0f;

        [SerializeField]
        float _maxSteerForce = 0.5f;

        [SerializeField]
        float _cohesionWeight = 1.0f;

        [SerializeField]
        float _alignmentWeight = 1.0f;

        [SerializeField]
        float _separateWeight = 3.0f;

        [SerializeField]
        float _avoidWallWeight = 10.0f;

        [SerializeField]
        Vector3 _simulationAreaCenter = Vector3.zero;

        [SerializeField]
        Vector3 _simulationAreaSize   = new Vector3(32.0f, 32.0f, 32.0f);

        [SerializeField]
        ComputeShader _boidsCS;

        [SerializeField]
        ComputeShader _boidsResetVelocityCS;

        ComputeBuffer _boidForceBuffer;
        ComputeBuffer _boidDataBuffer;


        public ComputeBuffer GetBoidDataBuffer()
        {
            return _boidDataBuffer ?? null;
        }

        public int GetMaxObjectNum()
        {
            return _maxObjectNum;
        }

        public Vector3 GetSimulationAreaCenter()
        {
            return _simulationAreaCenter;
        }

        public Vector3 GetSimulationAreaSize()
        {
            return _simulationAreaSize;
        }

        void Start()
        {
            Init();
        }

        void Update()
        {
            for (var i = 1; i <= 7; i++)
            {
                if (Input.GetKeyUp(i.ToString("0")))
                {
                    ResetVelocity(i);
                }
            }

            Simulation();
        }

        void OnDestroy()
        {
            Cleanup();
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(_simulationAreaCenter, _simulationAreaSize);
        }

        void Init()
        {
            _boidDataBuffer  = new ComputeBuffer(_maxObjectNum, Marshal.SizeOf(typeof(BoidData)));
            _boidForceBuffer = new ComputeBuffer(_maxObjectNum, Marshal.SizeOf(typeof(Vector3)));

            var forceArr    = new Vector3 [_maxObjectNum];
            var boidDataArr = new BoidData[_maxObjectNum];
            for (var i = 0; i < _maxObjectNum; i++)
            {
                forceArr[i] = Vector3.zero;
                boidDataArr[i].Position = Random.insideUnitSphere * 1.0f;
                boidDataArr[i].Velocity = Random.insideUnitSphere * 0.1f;
            }
            _boidForceBuffer.SetData(forceArr);
            _boidDataBuffer.SetData(boidDataArr);

            forceArr    = null;
            boidDataArr = null;
        }

        void Simulation()
        {
            ComputeShader cs = _boidsCS;

            var id = -1;

            var threadGroupSize = Mathf.CeilToInt(_maxObjectNum / SIMULATION_BLOCK_SIZE);

            id = cs.FindKernel("ForceCS");
            cs.SetInt("_MaxBoidObjectNum", _maxObjectNum);
            cs.SetFloat("_CohesionNeighborhoodRadius",  _cohesionNeighborhoodRadius);
            cs.SetFloat("_AlignmentNeighborhoodRadius", _alignmentNeighborhoodRadius);
            cs.SetFloat("_SeparateNeighborhoodRadius", _separateNeighborhoodRadius);
            cs.SetFloat("_MaxSpeed", _maxSpeed);
            cs.SetFloat("_MaxSteerForce", _maxSteerForce);
            cs.SetFloat("_SeparateWeight", _separateWeight);
            cs.SetFloat("_CohesionWeight", _cohesionWeight);
            cs.SetFloat("_AlignmentWeight", _alignmentWeight);
            cs.SetVector("_WallCenter", _simulationAreaCenter);
            cs.SetVector("_WallSize",   _simulationAreaSize);
            cs.SetFloat("_AvoidWallWeight", _avoidWallWeight);
            cs.SetBuffer(id, "_BoidDataBufferRO", _boidDataBuffer);
            cs.SetBuffer(id, "_BoidForceBuffer", _boidForceBuffer);
            cs.Dispatch(id, threadGroupSize, 1, 1);

            id = cs.FindKernel("IntegrateCS");
            cs.SetFloat("_DeltaTime", Time.deltaTime);
            cs.SetBuffer(id, "_BoidForceBufferRO", _boidForceBuffer);
            cs.SetBuffer(id, "_BoidDataBuffer", _boidDataBuffer);
            cs.Dispatch(id, threadGroupSize, 1, 1);
        }

        void ResetVelocity(int pattern)
        {
            var cs = _boidsResetVelocityCS;
            var groupThreadsX = Mathf.CeilToInt(_maxObjectNum / (1.0f * SIMULATION_BLOCK_SIZE));
            var id = -1;

            cs.SetFloat("_Time", Time.time);
            cs.SetVector("_WallCenter", _simulationAreaCenter);

            switch (pattern)
            {
                case 1: // CSResetVelocityRandomUniform
                    id = cs.FindKernel("CSResetVelocityRandomUniform");
                    break;

                case 2: // CSResetVelocityRandomDirectional
                    id = cs.FindKernel("CSResetVelocityRandomDirectional");
                    cs.SetVector("_RandomDirection", Random.insideUnitSphere);
                    break;

                case 3: // CSResetVelocityRadialInner
                    id = cs.FindKernel("CSResetVelocityRadialInner");
                    break;

                case 4: // CSResetVelocityRadialOuter
                    id = cs.FindKernel("CSResetVelocityRadialOuter");
                    break;

                case 5: // CSResetVelocityHorizontalCircleP
                    id = cs.FindKernel("CSResetVelocityHorizontalCircleP");
                    break;

                case 6: // CSResetVelocityHorizontalCircleN
                    id = cs.FindKernel("CSResetVelocityHorizontalCircleN");
                    break;

                case 7: // CSResetVelocitySimplexNoise
                    id = cs.FindKernel("CSResetVelocitySimplexNoise");
                    break;
                default:
                    break;

            }
            cs.SetBuffer(id, "_BoidDataBuffer", _boidDataBuffer);
            cs.Dispatch(id, groupThreadsX, 1, 1);

        }

        void Cleanup()
        {
            if (_boidForceBuffer != null)
            {
                _boidForceBuffer.Release();
                _boidForceBuffer = null;
            }

            if (_boidDataBuffer != null)
            {
                _boidDataBuffer.Release();
                _boidDataBuffer = null;
            }
        }
    }
}