using Assets.Scripts.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HotUpdate
{
    public class GameController
    {
        public GameController(String a)
        {
            UnityEngine.Debug.Log("!!! GameController:实例化|" + a);
        }

        public static void Initialize()
        {
            UnityEngine.Debug.Log("!!! GameController::Initialize()");
            ILCodeManager.LoadFinishedAction = OnFinshLoad;
            ILCodeManager.Instance.LoadGoByAddressale("Cube");
        }


        static void OnFinshLoad(GameObject go)
        {

            GameObject newGo = GameObject.Instantiate(go);
            UnityEngine.Debug.LogError("!!! GameController::OnFinshLoad" + go.transform.position);
            newGo.AddComponent<TestMono>();
        }
    }
}
