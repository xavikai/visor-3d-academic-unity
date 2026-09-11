using UnityEngine;

/// <summary>One reusable object, with explicit geometry variants.</summary>
[DisallowMultipleComponent]
public class AcademicModel : MonoBehaviour
{
    public string displayName;
    public GameObject lowpoly;
    public GameObject highpoly;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim();

    public bool IsValid => (lowpoly != null || highpoly != null)
        && IsChild(lowpoly) && IsChild(highpoly)
        && (lowpoly == null || highpoly == null ||
            (lowpoly != highpoly && !lowpoly.transform.IsChildOf(highpoly.transform)
             && !highpoly.transform.IsChildOf(lowpoly.transform)));

    private bool IsChild(GameObject variant) =>
        variant == null || (variant != gameObject && variant.transform.IsChildOf(transform));
}
