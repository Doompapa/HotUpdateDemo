using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Manager;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AddressableUpdater : MonoSingleton<AddressableUpdater>
{
    [SerializeField] private Text statusText;
    [SerializeField] private GameObject downLoad;
    [SerializeField] private Button startUp;

    private bool checkingUpdate;
    private bool needUpdate;
    private bool isUpdating;

    private float checkUpdateTime = 0;
    private const float CHECKTIMEMAX = 5;

    private List<string> needUpdateCatalogs;
    private AsyncOperationHandle<List<IResourceLocator>> updateHandle;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        startUp.onClick.AddListener(StartDownLoad);
        startUp.gameObject.SetActive(false);
        StartCheckUpdate();
    }

    public void StartCheckUpdate()
    {
        statusText.text = "正在检测资源更新...";
        Debug.Log("正在检测资源更新...");
        StartCoroutine(checkUpdate());
    }

    public void StartDownLoad()
    {
        if (needUpdate)
        {
            StartCoroutine(download());
        }
    }

    IEnumerator checkUpdate()
    {
        checkingUpdate = true;
        //初始化Addressable
        var init = Addressables.InitializeAsync();
        yield return init;

        var start = DateTime.Now;
        //开始连接服务器检查更新
        AsyncOperationHandle<List<string>> handle = Addressables.CheckForCatalogUpdates(false);
        //检查结束，验证结果
        checkingUpdate = false;
        Debug.Log(string.Format("CheckIfNeededUpdate use {0}ms", (DateTime.Now - start).Milliseconds));
        yield return handle;
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            List<string> catalogs = handle.Result;
            if (catalogs != null && catalogs.Count > 0)
            {
                needUpdate = true;
                needUpdateCatalogs = catalogs;
            }
        }

        if (needUpdate)
        {
            //检查到有资源需要更新
            statusText.text = "有资源需要更新";
            Debug.Log("有资源需要更新");
            downLoad.SetActive(true);
            startUp.gameObject.SetActive(true);

            // var checkSizeHandle = Addressables.GetDownloadSizeAsync(needUpdateCatalogs as IEnumerable);
            // yield return checkSizeHandle;
            // statusText.text = "有" + checkSizeHandle.Result / 1024 + "KB需要下载";
            //StartDownLoad();
        }
        else
        {
            statusText.text = "最新版本无需更新";
            Debug.Log($"Loading...");
            //没有资源需要更新，或者连接服务器失败
            startUp.onClick.RemoveListener(StartDownLoad);
            startUp.onClick.AddListener(Skip);
            startUp.transform.Find("Text").GetComponent<Text>().text = "Start";
            startUp.gameObject.SetActive(true);
        }

        Addressables.Release(handle);
    }

    IEnumerator download()
    {
        var start = DateTime.Now;
        //开始下载资源
        isUpdating = true;
        updateHandle = Addressables.UpdateCatalogs(needUpdateCatalogs, false);
        yield return updateHandle;
        Debug.Log(string.Format("UpdateFinish use {0}ms", (DateTime.Now - start).Milliseconds));
        Debug.Log($"下载完成");
        //下载完成
        isUpdating = false;
        Addressables.Release(updateHandle);
        Skip();
    }

    public void Skip()
    {
        SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive).completed += operation =>
        {
            SceneManager.UnloadSceneAsync(0);
            ILCodeManager.Instance.LoadUpdateDllAsync();
        };
    }

    private void Update()
    {
        if (checkingUpdate)
        {
            checkUpdateTime += Time.deltaTime;
            if (checkUpdateTime > CHECKTIMEMAX)
            {
                //自测连接超时
                checkingUpdate = false;
                StopAllCoroutines();
                startUp.onClick.RemoveListener(StartDownLoad);
                startUp.onClick.AddListener(Skip);
                startUp.transform.Find("Text").GetComponent<Text>().text = "Start";
                startUp.gameObject.SetActive(true);
                Debug.Log(string.Format("Connect Timed Out"));
            }
        }

        if (isUpdating)
        {
            int progress = (int) (updateHandle.PercentComplete * 100);
            statusText.text = $"正在更新资源... {progress}%";
            Debug.Log($"正在更新资源... {progress}%");
        }
    }

    private void OnInited(AsyncOperationHandle<GameObject> obj)
    {
        // Addressables.ReleaseInstance(go);
        // for (int i = 0; i < 2; i++)
        // {
        //     Vector3 position = UnityEngine.Random.insideUnitSphere * 5;
        //     GameObject go = obj.Result;
        //     Instantiate(go, position, Quaternion.identity);
        // }

        //Addressables.Release(obj);
    }
}