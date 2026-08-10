// Port a Render Graph del efecto "Stylized Detail" de Quibli (Dustyroom).
//
// Por que existe este archivo:
//   URP 17.5 (Unity 6000.4+) elimino ScriptableRenderPass.Execute(ScriptableRenderContext, ref
//   RenderingData) y ScriptableRendererFeature.SetupRenderPasses(...), y el Compatibility Mode
//   dejo de existir (RenderGraphSettings.enableRenderCompatibilityMode => false). El framework de
//   post-proceso de Quibli (CompoundPass / CompoundRenderer / QuibliPostProcess) no compila en esa
//   version.
//
//   De los dos efectos de Quibli que habia en el perfil de volumen del juego, ColorGrading estaba
//   en active: 0. El unico vivo era Stylized Detail, asi que se porta solo ese en vez de mantener
//   un fork del framework generico completo.
//
// Se conserva el shader original sin tocar: "Hidden/CompoundRendererFeature/StylizedDetail".
// Se conserva tambien el VolumeComponent CompoundRendererFeature.PostProcess.StylizedDetail, para
// que los perfiles de volumen existentes sigan funcionando sin reconfigurar nada.
//
// Vive fuera de Assets/Quibli/ a proposito: actualizar Quibli desde el Asset Store no debe borrarlo.

using CompoundRendererFeature.PostProcess;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Nabhi.Rendering {
public class StylizedDetailFeature : ScriptableRendererFeature {
    private const string ShaderName = "Hidden/CompoundRendererFeature/StylizedDetail";

    [SerializeField]
    [Tooltip("Punto de inyeccion. El framework de Quibli usaba BeforeRenderingPostProcessing.")]
    private RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

    private StylizedDetailPass _pass;
    private Material _material;

    public override void Create() {
        if (_material == null) {
            _material = CoreUtils.CreateEngineMaterial(ShaderName);
        }

        _pass = new StylizedDetailPass(_material) { renderPassEvent = injectionPoint };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if (_material == null || _pass == null) return;
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing) {
        CoreUtils.Destroy(_material);
        _material = null;
        _pass = null;
    }
}

internal class StylizedDetailPass : ScriptableRenderPass {
    private readonly Material _material;

    private static class PropertyIDs {
        internal static readonly int Input = Shader.PropertyToID("_MainTex");
        internal static readonly int BlurStrength = Shader.PropertyToID("_BlurStrength");
        internal static readonly int Blur1 = Shader.PropertyToID("_BlurTex1");
        internal static readonly int Blur2 = Shader.PropertyToID("_BlurTex2");
        internal static readonly int Intensity = Shader.PropertyToID("_Intensity");
        internal static readonly int DownSampleScaleFactor = Shader.PropertyToID("_DownSampleScaleFactor");
        internal static readonly int CoCParams = Shader.PropertyToID("_CoCParams");
        internal static readonly int SourceSize = Shader.PropertyToID("_SourceSize");
    }

    private class PassData {
        internal Material material;
        internal TextureHandle source;
        internal TextureHandle ping;
        internal TextureHandle blur1;
        internal TextureHandle blur2;
        internal TextureHandle composite;
        internal float intensity;
        internal float blurRadius;
        internal float edgePreserve;
        internal Vector4 cocParams;
        internal Vector4 sourceSize;
        internal Vector4 downSampleScaleFactor;
    }

    public StylizedDetailPass(Material material) {
        _material = material;
        // El renderer original declaraba estas entradas via CompoundRenderer.input.
        ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
        if (_material == null) return;

        var cameraData = frameData.Get<UniversalCameraData>();
        if (cameraData.cameraType == CameraType.Preview) return;
        if (!cameraData.postProcessEnabled) return;

        var volume = VolumeManager.instance.stack.GetComponent<StylizedDetail>();
        if (volume == null || volume.intensity.value <= 0f) return;

        var resourceData = frameData.Get<UniversalResourceData>();
        // No se puede muestrear el backbuffer como textura.
        if (resourceData.isActiveTargetBackBuffer) return;

        TextureHandle source = resourceData.activeColorTexture;
        if (!source.IsValid()) return;

        const int downSample = 1;

        var descriptor = cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
        descriptor.msaaSamples = 1;

        int wh = descriptor.width / downSample;
        int hh = descriptor.height / downSample;
        if (wh <= 0 || hh <= 0) return;

        // Un radio de 1 equivale a 1 a 1080p. Se acota porque el kernel gaussiano se degrada
        // mucho a resoluciones altas (4K+). Igual que en el original.
        float blurRadius = Mathf.Min(volume.blur.value * (wh / 1080f), 2f);
        float edgePreserve = Mathf.Min(volume.edgePreserve.value * (wh / 1080f), 2f);

        float rangeStart = volume.rangeStart.overrideState ? volume.rangeStart.value : 0f;
        float rangeEnd = volume.rangeEnd.overrideState ? volume.rangeEnd.value : -1f;

        var blurDescriptor = descriptor;
        blurDescriptor.width = wh;
        blurDescriptor.height = hh;

        TextureHandle ping = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, blurDescriptor, "_PingTexture", false, FilterMode.Bilinear);
        TextureHandle blur1 = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, blurDescriptor, "_BlurTex1", false, FilterMode.Bilinear);
        TextureHandle blur2 = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, blurDescriptor, "_BlurTex2", false, FilterMode.Bilinear);
        // El source es tambien el destino final, y no se puede leer y escribir a la vez:
        // se compone en un intermedio y luego se copia de vuelta.
        TextureHandle composite = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, descriptor, "_StylizedDetailComposite", false, FilterMode.Bilinear);

        float width = descriptor.width;
        float height = descriptor.height;

        using (var builder = renderGraph.AddUnsafePass<PassData>("Nabhi Stylized Detail", out var passData)) {
            passData.material = _material;
            passData.source = source;
            passData.ping = ping;
            passData.blur1 = blur1;
            passData.blur2 = blur2;
            passData.composite = composite;
            passData.intensity = volume.intensity.value;
            passData.blurRadius = blurRadius;
            passData.edgePreserve = edgePreserve;
            passData.cocParams = new Vector4(rangeStart, rangeEnd, 0f, 0f);
            passData.sourceSize = new Vector4(width, height, 1f / width, 1f / height);
            passData.downSampleScaleFactor =
                new Vector4(1f / downSample, 1f / downSample, downSample, downSample);

            // ReadWrite: se lee como fuente y, al final del pase, se copia el resultado encima.
            builder.UseTexture(source, AccessFlags.ReadWrite);
            builder.UseTexture(ping, AccessFlags.ReadWrite);
            builder.UseTexture(blur1, AccessFlags.ReadWrite);
            builder.UseTexture(blur2, AccessFlags.ReadWrite);
            builder.UseTexture(composite, AccessFlags.ReadWrite);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc<PassData>(static (data, ctx) => ExecutePass(data, ctx));
        }
    }

    private static void ExecutePass(PassData data, UnsafeGraphContext ctx) {
        CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

        data.material.SetVector(PropertyIDs.CoCParams, data.cocParams);
        data.material.SetFloat(PropertyIDs.Intensity, data.intensity);

        cmd.SetGlobalVector(PropertyIDs.SourceSize, data.sourceSize);
        cmd.SetGlobalVector(PropertyIDs.DownSampleScaleFactor, data.downSampleScaleFactor);

        // Pre-blur que preserva bordes: horizontal (pasada 1) + vertical (pasada 2).
        cmd.SetGlobalFloat(PropertyIDs.BlurStrength, data.edgePreserve);
        cmd.SetGlobalTexture(PropertyIDs.Input, data.source);
        CoreUtils.DrawFullScreen(cmd, data.material, data.ping, null, 1);
        cmd.SetGlobalTexture(PropertyIDs.Input, data.ping);
        CoreUtils.DrawFullScreen(cmd, data.material, data.blur1, null, 2);

        // Blur ancho, sobre el resultado anterior.
        cmd.SetGlobalFloat(PropertyIDs.BlurStrength, data.blurRadius);
        cmd.SetGlobalTexture(PropertyIDs.Input, data.blur1);
        CoreUtils.DrawFullScreen(cmd, data.material, data.ping, null, 1);
        cmd.SetGlobalTexture(PropertyIDs.Input, data.ping);
        CoreUtils.DrawFullScreen(cmd, data.material, data.blur2, null, 2);

        // Composicion (pasada 0). El shader lee _BlurTex1 y _BlurTex2 como globales.
        // En el codigo original se vinculaban solas porque cmd.GetTemporaryRT(nameID, ...) ata el
        // RT temporal a la propiedad global con ese nombre. Render Graph no hace eso, asi que hay
        // que vincularlas explicitamente o la composicion leeria basura.
        cmd.SetGlobalTexture(PropertyIDs.Blur1, data.blur1);
        cmd.SetGlobalTexture(PropertyIDs.Blur2, data.blur2);
        cmd.SetGlobalTexture(PropertyIDs.Input, data.source);
        CoreUtils.DrawFullScreen(cmd, data.material, data.composite, null, 0);

        // El source es tambien el destino final. No se puede componer directamente sobre el
        // porque se esta leyendo como _MainTex, asi que se copia de vuelta al terminar.
        // (RenderGraphUtils.AddCopyPass es internal en URP 17.5; Blitter si es publico.)
        Blitter.BlitCameraTexture(cmd, data.composite, data.source);
    }
}
}
