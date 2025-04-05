using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeslaSpawner : MonoBehaviour
{
    public float spawnRange, maxDistance = 8f;

    public float spawnRate = 0.3f;
    private float timemissing;
    public GameObject fxGraph;

    public Vector2 durationMinMax;
    void Update()
    {
        timemissing += Time.deltaTime;
        if (timemissing >= spawnRate)
        {
            SpawnLightning();
            timemissing = 0;
        }
    }

    void SpawnLightning()
    {
        Vector3 randomdir = Random.insideUnitSphere.normalized;

        RaycastHit hit;
        Transform spawnpos = this.transform;
        if (Physics.Raycast(spawnpos.position, randomdir, out hit, spawnRange))
        {
            float dur = Random.Range(durationMinMax.x, durationMinMax.y);
            GameObject spawned = Instantiate<GameObject>(fxGraph);
            spawned.GetComponent<TeslaFXController>().InitializeEffect(spawnpos, hit.point, maxDistance, dur);
            Debug.DrawLine(spawnpos.position, hit.point, Color.blue, 3f);
        }
    }
}
