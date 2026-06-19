using UnityEngine;
using UnityEngine.InputSystem;

public class BattleDebugHotkeys : MonoBehaviour
{
    [Header("References")]
    public BattleManager battleManager;
    public GameObject battleManagerObject;

    [Header("Hotkeys")]
    public Key killAllEnemiesKey = Key.K;
    public Key enableBattleManagerKey = Key.B;

    [Header("Settings")]
    public int debugDamage = 9999;

    [Header("Debug")]
    public bool enableDebugHotkeys = true;

    private void Awake()
    {
        AutoFindRefs();
    }

    private void Update()
    {
        if (!enableDebugHotkeys)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard[killAllEnemiesKey] != null &&
            keyboard[killAllEnemiesKey].wasPressedThisFrame)
        {
            KillAllEnemies();
        }

        if (keyboard[enableBattleManagerKey] != null &&
            keyboard[enableBattleManagerKey].wasPressedThisFrame)
        {
            EnableBattleManagerObject();
        }
    }

    private void AutoFindRefs()
    {
        if (battleManager == null)
            battleManager = FindFirstObjectByType<BattleManager>(FindObjectsInactive.Include);

        if (battleManagerObject == null && battleManager != null)
            battleManagerObject = battleManager.gameObject;
    }

    [ContextMenu("Debug Kill All Enemies")]
    public void KillAllEnemies()
    {
        AutoFindRefs();

        EnemyUnit[] enemies = FindObjectsByType<EnemyUnit>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Debug.Log($"[BattleDebugHotkeys] 對場景所有怪物造成 {debugDamage} 傷害，數量 = {enemies.Length}");

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyUnit enemy = enemies[i];

            if (enemy == null)
                continue;

            if (!enemy.gameObject.activeInHierarchy)
                continue;

            if (enemy.currentHp <= 0)
                continue;

            enemy.TakeDamage(debugDamage);
        }

        if (battleManager != null && battleManager.gameObject.activeInHierarchy)
        {
            battleManager.RequestCheckBattleEnd();
        }
    }

    [ContextMenu("Debug Enable BattleManager")]
    public void EnableBattleManagerObject()
    {
        AutoFindRefs();

        if (battleManagerObject == null)
        {
            Debug.LogWarning("[BattleDebugHotkeys] battleManagerObject 沒有指定");
            return;
        }

        if (battleManagerObject.activeSelf)
        {
            Debug.Log("[BattleDebugHotkeys] BattleManager 物件已經是開啟狀態");
            return;
        }

        Debug.Log("[BattleDebugHotkeys] 開啟 BattleManager 物件");

        battleManagerObject.SetActive(true);
    }
}