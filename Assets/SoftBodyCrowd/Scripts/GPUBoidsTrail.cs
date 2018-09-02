using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace irishoak.SoftBodyCrowd
{
    [RequireComponent(typeof(GPUBoids))]
    public class GPUBoidsTrail : MonoBehaviour
    {

        GPUBoids _boidScript;

        int _trailNum = 1;

        [SerializeField]
        int _trailHistoryNum = 1;
        
        RenderTexture[] _positionBuffer;
        RenderTexture[] _normalBuffer;
        RenderTexture[] _binormalBuffer;
        
        public RenderTexture GetPositionBuffer()
        {
            return _positionBuffer[0] ?? null;
        }
        public RenderTexture GetNormalBuffer()
        {
            return _normalBuffer[0] ?? null;
        }
        public RenderTexture GetBinormalBuffer()
        {
            return _binormalBuffer[0] ?? null;
        }

        [SerializeField]
        Shader _trailKernelShader;
        Material _trailKernelMat;
        Material trailKernelMat
        {
            get
            {
                if (_trailKernelMat == null)
                    _trailKernelMat = new Material(_trailKernelShader) { hideFlags = HideFlags.DontSave };
                return _trailKernelMat;
            }
        }

        RenderBuffer[] _mrt;

        [SerializeField]
        bool _showDebugTexOnGUI = false;

        void Awake()
        {
            _boidScript = GetComponent<GPUBoids>();    
        }

        void Start()
        {
            _trailNum = _boidScript.GetMaxObjectNum();

            CreateBuffer(ref _positionBuffer);
            CreateBuffer(ref _normalBuffer);
            CreateBuffer(ref _binormalBuffer);

            _mrt = new RenderBuffer[2];
        }
        
        void Update()
        {
            
            // Update Position
            trailKernelMat.SetTexture("_PositionBuffer", _positionBuffer[0]);
            trailKernelMat.SetBuffer("_BoidDataBuffer", _boidScript.GetBoidDataBuffer());
            trailKernelMat.SetInt("_TrailNum", _trailNum);
            Graphics.Blit(null, _positionBuffer[1], trailKernelMat, 0);


            // Reconstruct Vector
            _mrt[0] = _normalBuffer[1].colorBuffer;
            _mrt[1] = _binormalBuffer[1].colorBuffer;
            Graphics.SetRenderTarget(_mrt, _positionBuffer[1].depthBuffer);
            trailKernelMat.SetTexture("_PositionBuffer", _positionBuffer[1]);
            trailKernelMat.SetTexture("_NormalBuffer", _normalBuffer[0]);
            trailKernelMat.SetTexture("_BinormalBuffer", _binormalBuffer[0]);
            Graphics.Blit(null, trailKernelMat, 1);

            SwapBuffer(ref _positionBuffer[0], ref _positionBuffer[1]);
            SwapBuffer(ref _normalBuffer[0], ref _normalBuffer[1]);
            SwapBuffer(ref _binormalBuffer[0], ref _binormalBuffer[1]);
        }
        
        void OnDestroy()
        {
            if (_positionBuffer != null)
                for (var i = 0; i < 2; i++)
                    RenderTexture.DestroyImmediate(_positionBuffer[i]);

            if (_normalBuffer != null)
                for (var i = 0; i < 2; i++)
                    RenderTexture.DestroyImmediate(_normalBuffer[i]);
            
            if (_binormalBuffer != null)
                for (var i = 0; i < 2; i++)
                    RenderTexture.DestroyImmediate(_binormalBuffer[i]);

            _mrt = null;
        }

        void OnGUI()
        {
            if (!_showDebugTexOnGUI)
                return;

            var w = _trailHistoryNum * 1;
            if (_positionBuffer != null)
            {
                var r00 = new Rect(w * 0, _trailNum * 0, w, _trailNum);
                GUI.DrawTexture(r00, _positionBuffer[0]);
            }
            if (_normalBuffer != null)
            {
                var r10 = new Rect(w * 1, _trailNum * 0, w, _trailNum);
                GUI.DrawTexture(r10, _normalBuffer[0]);
            }
            if (_binormalBuffer != null)
            {
                var r20 = new Rect(w * 2, _trailNum * 0, w, _trailNum);
                GUI.DrawTexture(r20, _binormalBuffer[0]);
            }
        }

        void CreateBuffer(ref RenderTexture[] buffer)
        {
            buffer = new RenderTexture[2];
            {
                buffer[0] = new RenderTexture(_trailHistoryNum, _trailNum, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                buffer[0].hideFlags = HideFlags.DontSave;
                buffer[0].filterMode = FilterMode.Bilinear;
                buffer[0].wrapMode = TextureWrapMode.Clamp;
                buffer[0].enableRandomWrite = true;
                buffer[0].Create();
                buffer[1] = new RenderTexture(_trailHistoryNum, _trailNum, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                buffer[1].hideFlags = HideFlags.DontSave;
                buffer[1].filterMode = FilterMode.Bilinear;
                buffer[1].wrapMode = TextureWrapMode.Clamp;
                buffer[1].enableRandomWrite = true;
                buffer[1].Create();
            }

            RenderTexture store = RenderTexture.active;
            {
                RenderTexture.active = buffer[0];
                GL.Clear(false, true, Color.clear);
                RenderTexture.active = buffer[1];
                GL.Clear(false, true, Color.clear);
            }
            RenderTexture.active = store;
        }

        void SwapBuffer(ref RenderTexture ping, ref RenderTexture pong)
        {
            RenderTexture temp = ping;
            ping = pong;
            pong = temp;
        }
    }
}