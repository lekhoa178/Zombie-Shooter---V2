using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeslaFXController : MonoBehaviour
{
    public Transform start, end;
    public GameObject disableController;
    private bool active = true;
    private float maxDistance = 12f;
    public float destroyMeTime;
    private float duration, timeActive;
    private Transform spherePos;
    private void Update()
    {
        if (active)
        {
            start.position = spherePos.position;

            float dis = Vector3.Distance(start.position, end.position);
            timeActive += Time.deltaTime;
            if (dis > maxDistance || timeActive > duration)
            {
                disableController.SetActive(false);
                active = false;

                StartCoroutine(KillMyself(2));
            }
        }
    }


    IEnumerator KillMyself(float t)
    {
        yield return new WaitForSeconds(t);

        Destroy(this.gameObject);
    }
    public void InitializeEffect(Transform transstart, Vector3 transend, float maxRange = 12f, float dur = 3f)
    {
        spherePos = transstart;
        start.position = spherePos.position;
        end.position = transend;

        maxDistance = maxRange;
        duration = dur;
    }
}
