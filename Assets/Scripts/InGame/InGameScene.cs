using UnityEngine;

public class InGameScene : MonoBehaviour
{
    [SerializeField] private MonsterManager m_MonsterManager;
    void Start()
    {
        m_MonsterManager.Init();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
