using UnityEngine;

public class InGameScene : MonoBehaviour
{
    [SerializeField] private MonsterManager m_MonsterManager;
    [SerializeField] private SpawnManager m_SpawnManager;
    void Start()
    {
        m_MonsterManager.Init();
        m_SpawnManager.Init();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
