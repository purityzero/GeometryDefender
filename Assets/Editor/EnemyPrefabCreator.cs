using UnityEngine;
using UnityEditor;
using System.IO;

public class EnemyPrefabCreator
{
    private static readonly string PREFAB_OUTPUT_PATH = "Assets/Resources/Prefabs/Monster";
    private static readonly string SPRITE_BASE_PATH = "Assets/Resources/Image";

    [MenuItem("Tools/Create Enemy Prefabs")]
    public static void CreateEnemyPrefabs()
    {
        if (Directory.Exists(PREFAB_OUTPUT_PATH) == false)
            Directory.CreateDirectory(PREFAB_OUTPUT_PATH);

        string[] shapeNames = { "Triangle", "Circle", "Square", "Diamond", "Pentagon", "Star" };

        for (int i = 0; i < shapeNames.Length; ++i)
        {
            CreatePrefab(shapeNames[i]);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[EnemyPrefabCreator] {shapeNames.Length}개 프리팹 생성 완료 → {PREFAB_OUTPUT_PATH}");
    }

    private static void CreatePrefab(string _shapeName)
    {
        string spritePath = $"{SPRITE_BASE_PATH}/shape_{_shapeName.ToLower()}.png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        if (sprite == null)
        {
            Debug.Log($"[EnemyPrefabCreator] 스프라이트를 찾을 수 없음: {spritePath}");
            return;
        }

        GameObject gameObject = new GameObject(_shapeName);

        SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;

        ActorMonster actorMonster = gameObject.AddComponent<ActorMonster>();

        SerializedObject serializedActorMonster = new SerializedObject(actorMonster);
        SerializedProperty rendererProperty = serializedActorMonster.FindProperty("m_Renderer");
        rendererProperty.objectReferenceValue = spriteRenderer;
        serializedActorMonster.ApplyModifiedProperties();

        string prefabPath = $"{PREFAB_OUTPUT_PATH}/{_shapeName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPath);

        Object.DestroyImmediate(gameObject);

        Debug.Log($"[EnemyPrefabCreator] 생성: {prefabPath}");
    }
}
