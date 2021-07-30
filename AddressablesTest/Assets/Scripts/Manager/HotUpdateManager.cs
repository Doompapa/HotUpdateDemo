using Assets.Scripts.Manager;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.AddressableAssets.Addressables;
using static UnityEngine.UI.Button;

public class HotUpdateManager : MonoSingleton<HotUpdateManager>
{
    [SerializeField] private Text statusText;
    [SerializeField] private Slider downLoad;
    [SerializeField] private Button downLoadButton;

    private AsyncOperationHandle<List<IResourceLocator>> updateHandle;

    private AsyncOperationHandle<long> dls;

    private IList<object> keysList = new List<object>();
    
    private IList<IResourceLocator> _updateKeys = new List<IResourceLocator>();

    protected override void Awake()
    {
        base.Awake();
    }

    // Start is called before the first frame update
    void Start()
    {
        //clear cache
        //Caching.ClearCache();

        StartCoroutine(checkUpdate());
    }
    

    IEnumerator checkUpdate()
    {
        Addressables.CheckForCatalogUpdates();
        
        
        downLoadButton.gameObject.SetActive(false);
        downLoadButton.onClick.AddListener(delegate ()
        {
            StartCoroutine(download());
        });

        //init addressable function
        yield return Addressables.InitializeAsync();
        //get update list
        foreach (IResourceLocator locator in Addressables.ResourceLocators)
        {
            var map = locator as ResourceLocationMap;
            if (map == null)
                continue;

            keysList = map.Keys.ToList();
        }

        dls = Addressables.GetDownloadSizeAsync(keysList as IEnumerable);
        dls.WaitForCompletion();
        Debug.Log("check download:" + dls.Result / 1024 + "KB");
        statusText.text = dls.Result / 1024 + "KB " + "package to be update";

        downLoadButton.gameObject.SetActive(true);
    }


    IEnumerator download()
    {
        if (dls.Result > 0)
        {

            var handle = Addressables.DownloadDependenciesAsync(keysList as IEnumerable, MergeMode.None);
            handle.Completed += OnDownLoaded;
            while (!handle.IsDone)
            {
                var status = handle.GetDownloadStatus();
                float progress = status.Percent;
                downLoad.value = progress;
                //Debug.Log(progress);
                yield return null;
            }
            //释放句柄
            //Addressables.Release(handle);

            ILCodeManager.instance.LoadUpdateDllAsync();

            //Addressables.InstantiateAsync("Cube", Vector3.zero, Quaternion.identity).Completed += OnInited;
            // Addressables.LoadAssetAsync<GameObject>("Cube").Completed += OnInited;

            //Addressables.LoadAssetAsync<GameObject>("Cube").Completed += OnInited;
        }
        else
        {
           // Addressables.LoadAssetAsync<GameObject>("Cube").Completed += OnInited;
            ILCodeManager.instance.LoadUpdateDllAsync();
        }
    }

    private void OnDownLoaded(AsyncOperationHandle obj)
    {
        //GameObject go = obj.Result;
        //Instantiate(go);
        Debug.Log(obj.Result);
    }

    private void OnInited(AsyncOperationHandle<GameObject> obj)
    {

        // Addressables.ReleaseInstance(go);
        for (int i = 0; i < 2; i++)
        {
            Vector3 position = UnityEngine.Random.insideUnitSphere * 5;
            GameObject go = obj.Result;
            Instantiate(go, position, Quaternion.identity);
        }

        //Addressables.Release(obj);
    }

}
