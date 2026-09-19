using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

[System.Serializable]
public class TensionRamp
{
    [Range(0, 1), Tooltip("Ceiling for this effect, reached at full tension.")]
    public float amount = 1f;

    [Range(0, 1), Tooltip("Tension at which the effect starts ramping in (Edge1).")]
    public float rampStarts = 0f;

    [Range(0, 1), Tooltip("Tension at which the effect is fully ramped in (Edge2).")]
    public float fullyIn = 1f;

    [Range(0, 1), Tooltip("Value below Edge1 (output floor).")]
    public float outputFloor = 0f;

    [Range(0, 1), Tooltip("Value above Edge2 (output ceiling).")]
    public float outputCeiling = 1f;

    public TensionRamp()
    {
    }

    public TensionRamp(float amount, float rampStarts, float fullyIn, float outputFloor, float outputCeiling)
    {
        this.amount = amount;
        this.rampStarts = rampStarts;
        this.fullyIn = fullyIn;
        this.outputFloor = outputFloor;
        this.outputCeiling = outputCeiling;
    }
}

public class ParanoiaVFXFeature : ScriptableRendererFeature
{
    [Tooltip("Material that uses the fullscreen paranoia shader")]
    public Material material;

    // can try before post processing if the ghost trails stand out too much
    public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;

    [Range(0, 1)] public float tension = 0.30f;

    [Header("Effect ramps")] public TensionRamp aberration = new TensionRamp(0.55f, 0.00f, 0.70f, 0.20f, 1.00f);
    public TensionRamp warp = new TensionRamp(0.35f, 0.10f, 0.80f, 0.15f, 1.00f);
    public TensionRamp grain = new TensionRamp(0.32f, 0.00f, 0.60f, 0.15f, 1.00f);
    public TensionRamp desat = new TensionRamp(0.55f, 0.00f, 1.00f, 0.20f, 1.00f);
    public TensionRamp cast = new TensionRamp(0.40f, 0.00f, 1.00f, 0.00f, 1.00f);
    public TensionRamp vignette = new TensionRamp(0.70f, 0.00f, 1.00f, 0.20f, 1.00f);
    public TensionRamp ghost = new TensionRamp(0.30f, 0.70f, 1.00f, 0.00f, 1.00f);

    [Header("Breath")] [Tooltip("Breath rate in radians per second at zero tension.")] [Range(0, 10)]
    public float breathRate = 1.2f;

    [Tooltip("Added to the breath rate at full tension.")] [Range(0, 10)]
    public float breathRatePerTension = 3.2f;

    [Header("Ghost trail snapshots")] [Tooltip("Seconds between scene snapshots.")] [Min(0.01f)]
    public float ghostInterval = 0.12f;

    [Tooltip("How many past snapshots are recorded.")] [Range(0, 3)]
    public int ghostTaps = 3;

    [Tooltip("Each older echo is this much weaker than the one before it.")] [Range(0, 1)]
    public float ghostFalloff = 0.65f;

    [Tooltip("Snapshot resolution divisor.")] [Range(1, 4)]
    public int ghostDownsample = 2;

    public static float? GlobalTension;
    public static void SetTension(float t) => GlobalTension = Mathf.Clamp01(t);
    public static void ClearTension() => GlobalTension = null;

    Material _instance;
    ParanoiaPass _pass;
    float _snapshotTimer;
    int _lastFrame = -1;
    float _breathPhase;

    public override void Create()
    {
        if (_instance != null)
        {
            CoreUtils.Destroy(_instance);
            _instance = null;
        }

        if (material != null) _instance = new Material(material);
        _pass = new ParanoiaPass { renderPassEvent = injectionPoint };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_instance == null) return;
        if (renderingData.cameraData.cameraType != CameraType.Game) return;

        float ten = GlobalTension ?? tension;
        _instance.SetFloat(ShaderIDs.Tension, ten);
        ShaderIDs.Aberr.Push(_instance, aberration);
        ShaderIDs.Warp.Push(_instance, warp);
        ShaderIDs.Grain.Push(_instance, grain);
        ShaderIDs.Desat.Push(_instance, desat);
        ShaderIDs.Cast.Push(_instance, cast);
        ShaderIDs.Vign.Push(_instance, vignette);
        ShaderIDs.Ghost.Push(_instance, ghost);

        _instance.SetFloat(ShaderIDs.GhostFalloff, ghostFalloff);

        bool snapshot = false;
        if (Time.frameCount != _lastFrame)
        {
            _lastFrame = Time.frameCount;

            _breathPhase += Time.deltaTime * (breathRate + ten * breathRatePerTension);
            _breathPhase = Mathf.Repeat(_breathPhase, 2f * Mathf.PI);

            _snapshotTimer += Time.deltaTime;
            if (_snapshotTimer >= ghostInterval)
            {
                _snapshotTimer = 0f;
                snapshot = true;
            }
        }

        _instance.SetFloat(ShaderIDs.BreathPhase, _breathPhase);

        _pass.Setup(_instance, snapshot, ghostTaps, ghostDownsample);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        _pass?.Dispose();
        if (_instance != null)
        {
            CoreUtils.Destroy(_instance);
            _instance = null;
        }
    }

    readonly struct RampIDs
    {
        readonly int _amount, _edge1, _edge2, _floor, _ceiling;

        public RampIDs(string prefix)
        {
            _amount = Shader.PropertyToID(prefix + "Amount");
            _edge1 = Shader.PropertyToID(prefix + "Edge1");
            _edge2 = Shader.PropertyToID(prefix + "Edge2");
            _floor = Shader.PropertyToID(prefix + "Floor");
            _ceiling = Shader.PropertyToID(prefix + "Ceiling");
        }

        public void Push(Material mat, TensionRamp ramp)
        {
            mat.SetFloat(_amount, ramp.amount);
            mat.SetFloat(_edge1, ramp.rampStarts);
            mat.SetFloat(_edge2, ramp.fullyIn);
            mat.SetFloat(_floor, ramp.outputFloor);
            mat.SetFloat(_ceiling, ramp.outputCeiling);
        }
    }

    static class ShaderIDs
    {
        public static readonly int
            Tension = Shader.PropertyToID("_Tension"),
            BreathPhase = Shader.PropertyToID("_BreathPhase"),
            GhostTapCount = Shader.PropertyToID("_GhostTapCount"),
            GhostFalloff = Shader.PropertyToID("_GhostFalloff");

        // newest echo first
        public static readonly int[] GhostTex =
        {
            Shader.PropertyToID("_GhostTex0"),
            Shader.PropertyToID("_GhostTex1"),
            Shader.PropertyToID("_GhostTex2"),
        };

        public static readonly RampIDs
            Aberr = new RampIDs("_Aberr"),
            Warp = new RampIDs("_Warp"),
            Grain = new RampIDs("_Grain"),
            Desat = new RampIDs("_Desat"),
            Cast = new RampIDs("_Cast"),
            Vign = new RampIDs("_Vign"),
            Ghost = new RampIDs("_Ghost");
    }


    class ParanoiaPass : ScriptableRenderPass
    {
        public const int MaxTaps = 3;
        const int Slots = MaxTaps + 1;

        Material _mat;
        readonly RTHandle[] _snaps = new RTHandle[Slots];
        int _newest; // slot holding the most recent snapshot
        int _valid; // snapshots taken since the last (re)allocation
        bool _snapshot;
        int _taps, _downsample;

        public void Setup(Material mat, bool snapshot, int taps, int downsample)
        {
            _mat = mat;
            _snapshot = snapshot;
            _taps = Mathf.Clamp(taps, 0, MaxTaps);
            _downsample = Mathf.Max(1, downsample);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_mat == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            if (resourceData.isActiveTargetBackBuffer) return;

            TextureHandle source = resourceData.activeColorTexture;

            var snapDesc = cameraData.cameraTargetDescriptor;
            snapDesc.depthBufferBits = 0;
            snapDesc.msaaSamples = 1;
            snapDesc.width = Mathf.Max(1, snapDesc.width / _downsample);
            snapDesc.height = Mathf.Max(1, snapDesc.height / _downsample);

            for (int i = 0; i < Slots; i++)
                if (RenderingUtils.ReAllocateHandleIfNeeded(ref _snaps[i], snapDesc, name: "_ParanoiaGhost" + i))
                    _valid = 0; // resolution changed, the snapshots we had are stale

            for (int t = 0; t < MaxTaps; t++)
            {
                int age = Mathf.Min(t, Mathf.Max(_valid - 1, 0));
                int slot = (_newest - age + Slots) % Slots;
                _mat.SetTexture(ShaderIDs.GhostTex[t], _snaps[slot]);
            }

            _mat.SetFloat(ShaderIDs.GhostTapCount, Mathf.Min(_taps, _valid));

            // temporary texture (dst) for this pass
            var dstDesc = renderGraph.GetTextureDesc(source);
            dstDesc.name = "_ParanoiaDst";
            dstDesc.clearBuffer = false;
            TextureHandle dst = renderGraph.CreateTexture(dstDesc);

            // render the camera into dst
            var blit = new RenderGraphUtils.BlitMaterialParameters(source, dst, _mat, 0);
            renderGraph.AddBlitPass(blit, passName: "Paranoia");

            // snapshot the scene, without any ghost trails
            if (_snapshot)
            {
                int write = (_newest + 1) % Slots;
                renderGraph.AddBlitPass(source, renderGraph.ImportTexture(_snaps[write]),
                    Vector2.one, Vector2.zero, passName: "Paranoia Ghost Snapshot");
                _newest = write;
                _valid = Mathf.Min(_valid + 1, Slots);
            }

            // render dst to the screen
            resourceData.cameraColor = dst;
        }

        public void Dispose()
        {
            for (int i = 0; i < Slots; i++)
            {
                _snaps[i]?.Release();
                _snaps[i] = null;
            }
        }
    }
}