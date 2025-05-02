using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameModeManager : MonoBehaviour
{
    // Singleton instance
    private static GameModeManager _instance;
    public static GameModeManager Instance { get { return _instance; } }

    [Header("Player Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private string playerSpawnPointTag = "PlayerSpawn";

    [Header("HUD Settings")]
    [SerializeField] private GameObject hudPrefab;

    // Runtime references
    private GameObject currentPlayer;
    private GameObject currentHUD;

    private void Awake()
    {
        // Singleton implementation
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Subscribe to scene load events
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SpawnPlayerAtSpawnPoint();
        CreateHUD();
    }

    public void InitializeGameMode()
    {
        
    }

    public void SpawnPlayerAtSpawnPoint()
    {
        GameObject spawnPoint = FindSpawnPoint();
        
        if (spawnPoint != null)
        {
            SpawnPlayer(spawnPoint.transform.position, spawnPoint.transform.rotation);
        }
        else
        {
            Debug.LogWarning("No spawn point found. Spawning player at origin.");
            SpawnPlayer(Vector3.zero, Quaternion.identity);
        }
    }

    public GameObject SpawnPlayer(Vector3 position, Quaternion rotation)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab not set in GameModeManager!");
            return null;
        }

        // Destroy existing player if there is one
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
        }

        currentPlayer = Instantiate(playerPrefab, position, rotation);
        return currentPlayer;
    }

    public void CreateHUD()
    {
        if (hudPrefab == null)
        {
            Debug.LogWarning("HUD prefab not set in GameModeManager!");
            return;
        }

        if (currentHUD != null)
        {
            Destroy(currentHUD);
        }

        currentHUD = Instantiate(hudPrefab);
    }

    private GameObject FindSpawnPoint()
    {
        var spawnPoint = GameObject.FindWithTag(playerSpawnPointTag);
        
        if (spawnPoint == null)
        {
            spawnPoint = FindAnyObjectByType<PlayerSpawnPoint>().gameObject;
        }
        
        return spawnPoint;
    }

    public GameObject GetCurrentPlayer()
    {
        return currentPlayer;
    }

    public GameObject GetCurrentHUD()
    {
        return currentHUD;
    }
}