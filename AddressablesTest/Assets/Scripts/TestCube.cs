using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCube : MonoBehaviour
{
    // Start is called before the first frame update
    private float timer = 0f;
    private Vector3 target;

    void Start()
    {
        transform.position = Random.insideUnitSphere * 2;
        target = Random.insideUnitSphere * 2;
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, target, 0.5f * Time.deltaTime);
        if (timer >= 2.0f)
        {
            target = Random.insideUnitSphere * 2;
        }
    }
}