// Migracion Unity 6: se conserva solo el VolumeComponent. El renderer que lo acompanaba
// (StylizedDetailRenderer : CompoundRenderer) dependia del framework de post-proceso de Quibli,
// que no compila en URP 17.5. El efecto lo dibuja ahora Nabhi.Rendering.StylizedDetailFeature,
// en Assets/Settings/Rendering/StylizedDetailFeature.cs, sobre Render Graph.
// No renombrar esta clase ni su namespace: los perfiles de volumen la referencian.

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CompoundRendererFeature.PostProcess {
[Serializable, VolumeComponentMenu("Quibli/Stylized Detail")]
public class StylizedDetail : VolumeComponent {
    [Tooltip("Controls the amount of contrast added to the image details.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 3f, true);

    [Tooltip("Controls smoothing amount.")]
    public ClampedFloatParameter blur = new ClampedFloatParameter(1f, 0, 2, true);

    [Tooltip("Controls structure within the image.")]
    public ClampedFloatParameter edgePreserve = new ClampedFloatParameter(1.25f, 0, 2, true);

    [Tooltip("The distance from the camera at which the effect starts."), Space]
    public MinFloatParameter rangeStart = new MinFloatParameter(10f, 0f);

    [Tooltip("The distance from the camera at which the effect reaches its maximum radius.")]
    public MinFloatParameter rangeEnd = new MinFloatParameter(30f, 0f);
}
}
