using System;
using PowerTree;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace DungeonGeneration
{
    public class StageBaseManager : MonoBehaviour
    {
        [Header("Player Settings")] [SerializeField]
        protected GameObject playerPrefab;
        [SerializeField] protected string playerSpawnPointTag = "PlayerSpawn";

        [Header("HUD Settings")] [SerializeField]
        protected GameObject hudPrefab;
        
        [Header("Stage Settings")]
        [SerializeField] protected int coinEarnAmount = 25;
        [SerializeField] protected float shardsEarnProbability = 0.1f;
        
        public int CoinEarnAmount => coinEarnAmount;

        public UnityEvent onStageCompleted;

        private GameObject currentPlayer;
        private GameObject currentHUD;

        protected virtual void Start()
        {
            InitializeStage();
        }

        public void InitializeStage()
        {
            SpawnPlayerAtSpawnPoint();
            CreateHUD();
            
            onStageCompleted.AddListener(() =>
            {
                PlayerStats.Instance?.AddDroppedGold(coinEarnAmount);
                if (UnityEngine.Random.value < shardsEarnProbability)
                {
                    PowerTreeManager.Instance?.AddAetherShards(1);
                }
            });
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
}