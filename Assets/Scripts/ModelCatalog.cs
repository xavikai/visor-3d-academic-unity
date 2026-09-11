using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Visor 3D/Model Catalog")]
public class ModelCatalog : ScriptableObject
{
    public List<AcademicModel> prefabs = new List<AcademicModel>();
    [HideInInspector] public string sourceScenePath;
}
