using Assets.Scripts.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HotUpdate
{
    class TestMono : MonoBehaviour
    {

        private float timer = 0f;
        private Vector3 target;
        void Start()
        {
            UnityEngine.Debug.LogError("!!! TestMono::Start");
            transform.position = UnityEngine.Random.insideUnitSphere * 2;
            target = UnityEngine.Random.insideUnitSphere * 2;

            ILCodeManager.Instance.DoCoroutine(Coroutine());
        }

        void Awake()
        {

        }

        void Update()
        {
            timer += Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, target, 50f * Time.deltaTime);
            if (timer >= 1.0f)
            {
                target = UnityEngine.Random.insideUnitSphere * 5;
                timer = 0f;
            }
        }

        void TestQuit()
        {
            Time.timeScale = 0;
        }


        System.Collections.IEnumerator Coroutine()
        {
            Debug.Log("开始协程,t=" + Time.time);
            yield return new WaitForSeconds(3);
            Debug.Log("等待了3秒,t=" + Time.time);
            this.TestQuit();
        }

    }
}
