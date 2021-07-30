using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class GameManager : MonoSingleton<GameManager>
{
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
    }

    // Start is called before the first frame update
    string key = "Cube";
    void Start()
    {
    }

    public async Task checkDownLoadSize()
    {
        var handle = Addressables.GetDownloadSizeAsync(key);
        await handle.Task;
        Debug.Log(handle.Result);
    }



    IEnumerator DownLoadAA(string key)
    {
        var downloadsize = Addressables.GetDownloadSizeAsync(key);
        yield return downloadsize;
        Debug.Log("start download:" + downloadsize.Result / 1024 + "KB");

        var handle = Addressables.DownloadDependenciesAsync(key);
        handle.Completed += OnDownLoaded;
        while (!handle.IsDone)
        {
            var status = handle.GetDownloadStatus();

            float progress = status.Percent;
            Debug.Log(progress);
        }


        yield return null;

    }

    /// <summary>
    /// 获取所有资源的大小,如果资源数过多，直接指定key更好，具体提升待定
    /// </summary>
    /// <returns></returns>
    IEnumerator checkUpdateList()
    {
        yield return Addressables.InitializeAsync();


        IList<object> keysList = new List<object>();
        foreach (IResourceLocator locator in Addressables.ResourceLocators)
        {
            var map = locator as ResourceLocationMap;
            if (map == null)
                continue;

            keysList = map.Keys.ToList();
        }
        var dls = Addressables.GetDownloadSizeAsync(keysList as IEnumerable);
        dls.WaitForCompletion();

        Debug.Log("start download:" + dls.Result / 1024 + "KB");

        if (dls.Result > 0)
        {
            Debug.Log("start download:" + dls.Result / 1024 + "KB");
            var handle = Addressables.DownloadDependenciesAsync("test");
            handle.Completed += OnDownLoaded;
            while (!handle.IsDone)
            {
                var status = handle.GetDownloadStatus();

                float progress = status.Percent;
                Debug.Log(progress);
                yield return null;
            }
            //Addressables.InstantiateAsync("Sphere", Vector3.zero, Quaternion.identity).Completed += OnInited;

            //Addressables.InstantiateAsync("Sphere", Vector3.zero, Quaternion.identity);

            //Addressables.InstantiateAsync("dese", Vector3.one * 20, Quaternion.identity);

            Addressables.LoadAssetAsync<GameObject>("dese").Completed += OnInited;

        }


    }

    IEnumerator checkUpdate()
    {
        //初始化Addressable
        var init = Addressables.InitializeAsync();
        yield return init;
        //开始连接服务器检查更新
        AsyncOperationHandle<List<string>> checkHandle = Addressables.CheckForCatalogUpdates(false);
        //检查结束，验证结果 
        yield return checkHandle;
        if (checkHandle.Status == AsyncOperationStatus.Succeeded)
        {
            List<string> catalogs = checkHandle.Result;
            if (catalogs != null && catalogs.Count > 0)
            {
                Debug.Log("download start");
                var updateHandle = Addressables.UpdateCatalogs(catalogs, false);
                yield return updateHandle;
                Debug.Log("download finish");
            }
        }
        Addressables.Release(checkHandle);
    }


    private void OnDownLoaded(AsyncOperationHandle obj)
    {
        //GameObject go = obj.Result;
        //Instantiate(go);
        Debug.Log(obj.Result);
    }

    private void Dls_Completed(AsyncOperationHandle<long> obj)
    {
        Debug.Log(obj);
    }

    private void OnResLoadedHandler(AsyncOperationHandle<GameObject> obj)
    {
        GameObject go = obj.Result;
        Instantiate(go);
    }

    private void OnInited(AsyncOperationHandle<GameObject> obj)
    {

        // Addressables.ReleaseInstance(go);
        for (int i = 0; i < 5; i++)
        {
            Vector3 position = UnityEngine.Random.insideUnitSphere * 5;
            GameObject go = obj.Result;
            Instantiate(go, position, Quaternion.identity);
        }

        //Addressables.Release(obj);
    }


    // Update is called once per frame
    void Update()
    {

    }
    void OnLoaded(AsyncOperationHandle<SceneInstance> handle)
    {

    }


}
