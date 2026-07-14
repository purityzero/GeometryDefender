using UnityEngine;
using UnityEditor;
using System.IO;

public class EnemyMaterialCreator
{
    private static readonly string MAT_OUTPUT_PATH = "Assets/Resources/Mat/Enemy";
    private static readonly string TEMPLATE_MAT_PATH = "Assets/Resources/Mat/TestMat.mat";
    private static readonly string SPRITE_BASE_PATH = "Assets/Resources/Image";

    private static readonly (string shapeName, string variantName, string colorHex)[] COMBINATIONS =
    {
        ("Triangle", "Normal", "#ff3355"),
        ("Circle",   "Normal", "#ff3355"),
        ("Square",   "Normal", "#ff3355"),
        ("Diamond",  "Normal", "#ff3355"),
        ("Pentagon", "Normal", "#ff3355"),
        ("Triangle", "Elite",  "#ff00aa"),
        ("Circle",   "Elite",  "#ff00aa"),
        ("Square",   "Elite",  "#ff00aa"),
        ("Diamond",  "Elite",  "#ff00aa"),
        ("Pentagon", "Elite",  "#ff00aa"),
        ("Star",     "Boss",   "#ffd600"),
    };

    [MenuItem("Tools/Create Enemy Glow Materials")]
    public static void CreateEnemyGlowMaterials()
    {
        Material templateMaterial = AssetDatabase.LoadAssetAtPath<Material>(TEMPLATE_MAT_PATH);
        if (templateMaterial == null)
        {
            Debug.Log("[EnemyMaterialCreator] TestMat을 찾을 수 없음: " + TEMPLATE_MAT_PATH);
            return;
        }

        if (Directory.Exists(MAT_OUTPUT_PATH) == false)
            Directory.CreateDirectory(MAT_OUTPUT_PATH);

        for (int i = 0; i < COMBINATIONS.Length; ++i)
        {
            CreateMaterial(templateMaterial, COMBINATIONS[i].shapeName, COMBINATIONS[i].variantName, COMBINATIONS[i].colorHex);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[EnemyMaterialCreator] {COMBINATIONS.Length}개 Glow 머티리얼 생성 완료 → {MAT_OUTPUT_PATH}");
    }

    private static void CreateMaterial(Material _template, string _shapeName, string _variantName, string _colorHex)
    {
        Material material = new Material(_template);

        string spritePath = $"{SPRITE_BASE_PATH}/shape_{_shapeName.ToLower()}.png";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);

        if (texture == null)
        {
            Debug.Log($"[EnemyMaterialCreator] 텍스처를 찾을 수 없음: {spritePath}");
        }
        else
        {
            material.SetTexture("_MainTexture", texture);
        }

        if (ColorUtility.TryParseHtmlString(_colorHex, out Color color) == true)
            material.SetColor("_Color", color);
        else
            Debug.Log($"[EnemyMaterialCreator] 색상 파싱 실패: {_colorHex}");

        string matPath = $"{MAT_OUTPUT_PATH}/GlowMat_{_shapeName}_{_variantName}.mat";
        AssetDatabase.CreateAsset(material, matPath);

        Debug.Log($"[EnemyMaterialCreator] 생성: {matPath}");
    }
}
