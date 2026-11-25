using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class ReloadSmoke : MonoBehaviour
{
    [SerializeField] private ParticleSystem smokePrefab;
    [SerializeField] private Shader overlayShader;
    [SerializeField, ColorUsage(false)] private Color coverColor = new Color(0.33f, 0.25f, 0.34f, 1f);
    [SerializeField, Min(0.1f)] private float coverDuration = 0.55f;
    [SerializeField, Min(0f)] private float holdDuration = 2f;
    [SerializeField, Min(0.1f)] private float revealDuration = 0.8f;

    private static ReloadSmoke activeTransition;

    private Camera headset;
    private Material coverMaterial;
    private Material smokeMaterial;
    private Color smokeColor;

    public void Reload(int sceneIndex)
    {
        if (activeTransition != null)
            return;

        if (sceneIndex < 0 || smokePrefab == null || overlayShader == null)
        {
            Debug.LogError("ReloadSmoke requires a build scene, smoke prefab and overlay shader.", this);
            return;
        }

        // The scene component keeps its input binding. Only the temporary copy survives the load.
        var effect = new GameObject("Reload Smoke").AddComponent<ReloadSmoke>();
        effect.smokePrefab = smokePrefab;
        effect.overlayShader = overlayShader;
        effect.coverColor = coverColor;
        effect.coverDuration = coverDuration;
        effect.holdDuration = holdDuration;
        effect.revealDuration = revealDuration;

        activeTransition = effect;
        DontDestroyOnLoad(effect.gameObject);
        effect.StartCoroutine(effect.Play(sceneIndex));
    }

    private IEnumerator Play(int sceneIndex)
    {
        SceneManager.sceneLoaded += FindHeadset;
        Application.onBeforeRender += FollowHeadset;
        FollowHeadset();

        CreateOverlay();
        yield return Fade(0f, 1f, coverDuration);
        yield return null; // Present a fully covered frame before activating the new scene.
        yield return SceneManager.LoadSceneAsync(sceneIndex);
        yield return new WaitForSecondsRealtime(holdDuration);
        yield return Fade(1f, 0f, revealDuration);
        Destroy(gameObject);
    }

    private void CreateOverlay()
    {
        var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var collider = cover.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        cover.name = "Stereo Cover";
        cover.transform.SetParent(transform, false);
        cover.transform.localScale = Vector3.one * 3f;
        var coverRenderer = cover.GetComponent<MeshRenderer>();
        coverRenderer.shadowCastingMode = ShadowCastingMode.Off;
        // Draw after the headset HUD, including the existing paint splatter canvas.
        coverRenderer.sortingOrder = 32000;
        coverMaterial = new Material(overlayShader) { renderQueue = 4900 };
        coverMaterial.SetFloat("_Cull", (float)CullMode.Front);
        coverRenderer.sharedMaterial = coverMaterial;

        var smoke = Instantiate(smokePrefab, transform);
        smoke.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        smoke.gameObject.SetActive(true);
        smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var renderer = smoke.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = coverRenderer.sortingOrder + 1;
        var source = renderer.sharedMaterial;
        smokeColor = source.color;
        smokeMaterial = new Material(overlayShader)
        {
            mainTexture = source.mainTexture,
            renderQueue = 4901
        };
        smokeMaterial.SetFloat("_UseVertexColor", 1f);
        renderer.sharedMaterial = smokeMaterial;
        renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
        {
            ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV
        });

        var main = smoke.main;
        main.duration = 3f;
        main.loop = true;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.38f, 0.68f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.035f, 0.11f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
        var shape = smoke.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.85f;
        shape.radiusThickness = 0f;
        var emission = smoke.emission;
        emission.enabled = true;
        emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());
        emission.rateOverTime = 48f;
        emission.rateOverDistance = 0f;
        SetOpacity(0f);
        smoke.Play();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            SetOpacity(Mathf.SmoothStep(from, to, elapsed / duration));
            yield return null;
        }

        SetOpacity(to);
    }

    private void SetOpacity(float opacity)
    {
        coverMaterial.color = new Color(coverColor.r, coverColor.g, coverColor.b, opacity);
        smokeMaterial.color = new Color(smokeColor.r, smokeColor.g, smokeColor.b, smokeColor.a * opacity);
    }

    private void FindHeadset(Scene scene, LoadSceneMode mode)
    {
        headset = Camera.main;
        FollowHeadset();
    }

    private void LateUpdate()
    {
        if (activeTransition == this)
            FollowHeadset();
    }

    [BeforeRenderOrder(10000)]
    private void FollowHeadset()
    {
        if (headset == null || !headset.isActiveAndEnabled)
            headset = Camera.main;

        if (headset != null)
            transform.position = headset.transform.position;
    }

    private void OnDestroy()
    {
        if (activeTransition != this)
            return;

        SceneManager.sceneLoaded -= FindHeadset;
        Application.onBeforeRender -= FollowHeadset;
        Destroy(coverMaterial);
        Destroy(smokeMaterial);
        activeTransition = null;
    }
}
