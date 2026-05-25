using UnityEngine;

/// <summary>
/// Holds the Go/Arrived models and exposes ShowModel2() / ShowModel1() to be
/// called by Meta Interaction events. Wire the Go model's
/// PointableUnityEventWrapper.WhenSelect -> ShowModel2() in the Inspector.
/// Works with BOTH controller ray and hand pinch (the Meta RayInteractors drive
/// the RayInteractable). No raycast code here — interaction is fully Meta-native.
/// </summary>
public class ModelSwapController : MonoBehaviour
{
    [Header("Model Groups")]
    [Tooltip("Shown first (the 'Go' model). Needs Collider + ColliderSurface + RayInteractable + PointableUnityEventWrapper.")]
    public GameObject model1Group;
    [Tooltip("Shown after Go is selected (the 'Arrived' model).")]
    public GameObject model2Group;

    private void Start()
    {
        if (model1Group != null) model1Group.SetActive(true);
        if (model2Group != null) model2Group.SetActive(false);
    }

    /// <summary>Wire this to the Go model's PointableUnityEventWrapper.WhenSelect.</summary>
    public void ShowModel2()
    {
        Debug.Log($"[ModelSwap] ShowModel2 called — m1={model1Group?.name ?? "NULL"} m2={model2Group?.name ?? "NULL"}");
        if (model1Group != null) model1Group.SetActive(false);
        if (model2Group != null) model2Group.SetActive(true);
        Debug.Log("[ModelSwap] -> Arrived");
    }

    /// <summary>Optional: wire to Arrived's WhenSelect for a toggle back.</summary>
    public void ShowModel1()
    {
        if (model2Group != null) model2Group.SetActive(false);
        if (model1Group != null) model1Group.SetActive(true);
        Debug.Log("[ModelSwap] -> Go");
    }
}
