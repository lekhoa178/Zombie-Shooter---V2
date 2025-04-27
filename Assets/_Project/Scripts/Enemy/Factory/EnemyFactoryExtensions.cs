using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extension methods for working with EnemyFactory and BaseEnemy
/// </summary>
public static class EnemyFactoryExtensions
{
    /// <summary>
    /// Spawns an enemy at the position after a telegraph effect, with an optional callback when spawned
    /// </summary>
    public static BaseEnemy SpawnEnemyAt(this GameObject _enemyPrefab, Vector3 _position, System.Action<BaseEnemy> _onSpawned = null)
    {
        if (!_enemyPrefab) return null;
        
        // Register callback if needed
        if (_onSpawned != null)
        {
            // Use a local function to handle the event once
            void OnEnemySpawnedHandler(BaseEnemy spawnedEnemy)
            {
                if (spawnedEnemy != null && spawnedEnemy.gameObject.name.StartsWith(_enemyPrefab.name))
                {
                    _onSpawned.Invoke(spawnedEnemy);
                    EnemyFactory.Instance.OnEnemySpawned.RemoveListener(OnEnemySpawnedHandler);
                }
            }
            
            EnemyFactory.Instance.OnEnemySpawned.AddListener(OnEnemySpawnedHandler);
        }
        
        // Spawn the enemy using the factory
        return EnemyFactory.Instance.SpawnEnemy(_enemyPrefab, _position, Quaternion.identity);
    }
    
    /// <summary>
    /// Spawns an enemy at the position immediately without telegraph, with an optional callback when spawned
    /// </summary>
    public static BaseEnemy SpawnEnemyImmediateAt(this GameObject _enemyPrefab, Vector3 _position)
    {
        if (_enemyPrefab == null) return null;
        
        // Spawn the enemy using the factory
        return EnemyFactory.Instance.SpawnEnemyImmediate(_enemyPrefab, _position, Quaternion.identity);
    }
    
    /// <summary>
    /// Spawns multiple enemies in a grid pattern
    /// </summary>
    public static void SpawnEnemiesInGrid(this GameObject _enemyPrefab, Vector3 _centerPosition, int _rows, int _columns, float _spacing)
    {
        if (_enemyPrefab == null) return;
        
        float startX = _centerPosition.x - ((_columns - 1) * _spacing * 0.5f);
        float startZ = _centerPosition.z - ((_rows - 1) * _spacing * 0.5f);
        
        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _columns; col++)
            {
                Vector3 spawnPos = new Vector3(
                    startX + (col * _spacing),
                    _centerPosition.y,
                    startZ + (row * _spacing)
                );
                
                EnemyFactory.Instance.SpawnEnemy(_enemyPrefab, spawnPos, Quaternion.identity);
            }
        }
    }
    
    /// <summary>
    /// Spawns multiple enemies in a circle pattern
    /// </summary>
    public static void SpawnEnemiesInCircle(this GameObject _enemyPrefab, Vector3 _centerPosition, int _count, float _radius)
    {
        if (_enemyPrefab == null || _count <= 0) return;
        
        float angleStep = 360f / _count;
        
        for (int i = 0; i < _count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 spawnPos = _centerPosition + new Vector3(
                Mathf.Cos(angle) * _radius,
                0,
                Mathf.Sin(angle) * _radius
            );
            
            Quaternion rotation = Quaternion.LookRotation((_centerPosition - spawnPos).normalized);
            EnemyFactory.Instance.SpawnEnemy(_enemyPrefab, spawnPos, rotation);
        }
    }
    
    /// <summary>
    /// Get the type/name of the enemy (extracted from prefab name)
    /// </summary>
    public static string GetEnemyType(this BaseEnemy _enemy)
    {
        if (_enemy == null) return string.Empty;
        
        string enemyName = _enemy.gameObject.name;
        
        // Remove the "(Clone)" suffix and any numbers from object pooling
        int cloneIndex = enemyName.IndexOf("(Clone)");
        if (cloneIndex >= 0)
        {
            enemyName = enemyName.Substring(0, cloneIndex);
        }
        
        // Remove numbers from pool naming
        int dashIndex = enemyName.LastIndexOf("-");
        if (dashIndex >= 0 && dashIndex < enemyName.Length - 1)
        {
            // Check if the rest of the string after the dash is a number
            if (int.TryParse(enemyName.Substring(dashIndex + 1), out _))
            {
                enemyName = enemyName.Substring(0, dashIndex);
            }
        }
        
        return enemyName.Trim();
    }
    
    /// <summary>
    /// Extension method to check if an enemy is of a specific type (by name)
    /// </summary>
    public static bool IsEnemyType(this BaseEnemy _enemy, string _typeName)
    {
        if (_enemy == null) return false;
        
        return _enemy.GetEnemyType().Equals(_typeName, System.StringComparison.OrdinalIgnoreCase);
    }
} 