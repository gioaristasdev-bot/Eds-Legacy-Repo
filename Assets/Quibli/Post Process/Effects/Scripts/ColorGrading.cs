// Migracion Unity 6: se conserva solo el VolumeComponent. El renderer que lo acompanaba
// (ColorGradingRenderer : CompoundRenderer) dependia del framework de post-proceso de Quibli,
// que no compila en URP 17.5.
//
// Este efecto NO se ha portado a Render Graph a proposito: en el perfil de volumen que usan las
// escenas del juego (Main Camera Profile - City Demo) esta con active: 0, es decir, ya estaba
// desactivado y no se dibujaba. Se mantiene la clase para no romper esos perfiles.
// No renombrar esta clase ni su namespace: los perfiles de volumen la referencian.

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CompoundRendererFeature.PostProcess {
[Serializable, VolumeComponentMenu("Quibli/Stylized Color Grading")]
public class ColorGrading : VolumeComponent {
    [Tooltip("Controls the amount to which image colors are modified.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f, true);

    [Space]
    public ClampedFloatParameter blueShadows = new ClampedFloatParameter(0f, 0f, 1f, true);

    public ClampedFloatParameter greenShadows = new ClampedFloatParameter(0f, 0f, 1f, true);
    public ClampedFloatParameter redHighlights = new ClampedFloatParameter(0f, 0f, 1f, true);
    public ClampedFloatParameter contrast = new ClampedFloatParameter(0f, 0f, 1f, true);

    [Space]
    public ClampedFloatParameter vibrance = new ClampedFloatParameter(0f, 0f, 1f, true);

    public ClampedFloatParameter saturation = new ClampedFloatParameter(0f, 0f, 1f, true);
}
}
