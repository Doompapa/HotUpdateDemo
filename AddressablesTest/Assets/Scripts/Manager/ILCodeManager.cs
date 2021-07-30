using ILRuntime.CLR.Method;
using ILRuntime.CLR.TypeSystem;
using ILRuntime.Runtime.Enviorment;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using ILRuntime.Runtime.Intepreter;
using ILRuntime.Runtime.Stack;

public delegate void LoadFinished(int a);

namespace Assets.Scripts.Manager
{
    public class ILCodeManager : MonoSingleton<ILCodeManager>
    {
        //AppDomain是ILRuntime的入口，最好是在一个单例类中保存，整个游戏全局就一个，这里为了示例方便，每个例子里面都单独做了一个
        //大家在正式项目中请全局只创建一个AppDomain
        AppDomain appdomain;

        System.IO.MemoryStream fs;
        System.IO.MemoryStream p;

        //委托
        public static LoadFinished loadFinished;
        public static System.Action<GameObject> LoadFinishedAction;


        protected override void Awake()
        {
            base.Awake();
        }

        public void LoadUpdateDllAsync()
        {
#if UNITY_EDITOR
            StartCoroutine(LoadDllLocal());
#else
            Addressables.LoadAssetAsync<TextAsset>("HotUpdate.dll").Completed += OnLoaded;
#endif

        }

        IEnumerator LoadDllLocal()
        {
            //首先实例化ILRuntime的AppDomain，AppDomain是一个应用程序域，每个AppDomain都是一个独立的沙盒
            appdomain = new ILRuntime.Runtime.Enviorment.AppDomain();
            //正常项目中应该是自行从其他地方下载dll，或者打包在AssetBundle中读取，平时开发以及为了演示方便直接从StreammingAssets中读取，
            //正式发布的时候需要大家自行从其他地方读取dll

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //这个DLL文件是直接编译HotFix_Project.sln生成的，已经在项目中设置好输出目录为StreamingAssets，在VS里直接编译即可生成到对应目录，无需手动拷贝
#if UNITY_ANDROID
            WWW www = new WWW(Application.dataPath + "/HotUpdate_DLL/HotUpdate.dll");
#else
          WWW www = new WWW(Application.dataPath + "/HotUpdate_DLL/HotUpdate.dll");
#endif
            while (!www.isDone)
                yield return null;
            if (!string.IsNullOrEmpty(www.error))
                UnityEngine.Debug.LogError(www.error);
            byte[] dll = www.bytes;
            www.Dispose();

            //PDB文件是调试数据库，如需要在日志中显示报错的行号，则必须提供PDB文件，不过由于会额外耗用内存，正式发布时请将PDB去掉，下面LoadAssembly的时候pdb传null即可
#if UNITY_ANDROID
            www = new WWW(Application.dataPath + "/HotUpdate_DLL/HotUpdate.pdb");
#else
        www = new WWW("file:///" + Application.dataPath + "/HotUpdate_DLL/HotUpdate.pdb");
#endif
            while (!www.isDone)
                yield return null;
            if (!string.IsNullOrEmpty(www.error))
                UnityEngine.Debug.LogError(www.error);
            byte[] pdb = www.bytes;
            fs = new MemoryStream(dll);
            p = new MemoryStream(pdb);
            try
            {
                appdomain.LoadAssembly(fs, p, new ILRuntime.Mono.Cecil.Pdb.PdbReaderProvider());
                Debug.LogError(
                    "=================本地加载DLL成功===============");
            }
            catch
            {
                Debug.LogError(
                    "加载热更DLL失败，请确保已经通过VS打开编译过热更DLL");
            }

            InitializeILRuntime();
            OnHotFixLoaded();
        }

        private void OnLoaded(AsyncOperationHandle<TextAsset> obj)
        {
            Debug.Log(obj.Result);
            StartCoroutine(LoadHotFixAssemblyByHotUpdate(obj.Result.bytes));
        }

        /// <summary>
        /// load dll by addressables
        /// </summary>
        /// <param name="dll"></param>
        /// <returns></returns>
        IEnumerator LoadHotFixAssemblyByHotUpdate(byte[] dll)
        {
            //首先实例化ILRuntime的AppDomain，AppDomain是一个应用程序域，每个AppDomain都是一个独立的沙盒
            appdomain = new ILRuntime.Runtime.Enviorment.AppDomain();

            fs = new MemoryStream(dll);
            try
            {
                appdomain.LoadAssembly(fs, p, new ILRuntime.Mono.Cecil.Pdb.PdbReaderProvider());
                Debug.LogError("通过addressable加载热更DLL成功");
            }
            catch
            {
                Debug.LogError("通过addressable加载热更DLL失败");
            }

            InitializeILRuntime();
            OnHotFixLoaded();
            yield return null;
        }

        void InitializeILRuntime()
        {
#if DEBUG && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IPHONE)
            //由于Unity的Profiler接口只允许在主线程使用，为了避免出异常，需要告诉ILRuntime主线程的线程ID才能正确将函数运行耗时报告给Profiler
            appdomain.UnityMainThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
#endif
            //这里做一些ILRuntime的注册，
            appdomain.RegisterCrossBindingAdaptor(new MonoBehaviourAdapter());

            //使用Couroutine时，C#编译器会自动生成一个实现了IEnumerator，IEnumerator<object>，IDisposable接口的类，因为这是跨域继承，所以需要写CrossBindAdapter
            appdomain.RegisterCrossBindingAdaptor(new CoroutineAdapter());

            //Action<GameObject> 的参数为一个GameObject  注册委托
            appdomain.DelegateManager.RegisterMethodDelegate<GameObject>();

            //启动调试代码,port
            appdomain.DebugService.StartDebugService(56000);
            SetupCLRRedirection();
        }



        void OnHotFixLoaded()
        {
            //预先获得IMethod，可以减低每次调用查找方法耗用的时间
            IType type = appdomain.LoadedTypes["HotUpdate.GameController"];
            //根据方法名称和参数个数获取方法
            IMethod method = type.GetMethod("Initialize", 0);
            //PS:此方法调用重载不会报错,依然匹配到重载的第一个
            appdomain.Invoke(method, null);

            //using (var ctx = appdomain.BeginInvoke(method))
            //{
            //    ctx.Invoke();
            //}

            //   appdomain.Invoke("HotUpdate.GameController", "Initialize", null, null);


            // Addressables.LoadAssetAsync<GameObject>("dese").Completed += (e) =>
            // {
            //     GameObject go = e.Result;
            //     appdomain.Invoke("HotUpdate.GameController", "TestInstantiate", null, go);
            // };
            //
            // Addressables.LoadAssetAsync<GameObject>("Cube").Completed += (e) =>
            // {
            //     GameObject go = e.Result;
            //     GameObject.Instantiate(go, Vector3.zero, Quaternion.identity);
            // };
        }


        public void LoadGoByAddressale(string name)
        {
            Addressables.LoadAssetAsync<GameObject>(name).Completed += (e) =>
            {
                GameObject go = e.Result;
                LoadFinishedAction(go);
            };

        }


        public void DoCoroutine(IEnumerator coroutine)
        {
            StartCoroutine(coroutine);
        }

        unsafe void SetupCLRRedirection()
        {
            //这里面的通常应该写在InitializeILRuntime，这里为了演示写这里
            var arrAdd = typeof(GameObject).GetMethods();
            foreach (var i in arrAdd)
            {
                if (i.Name == "AddComponent" && i.GetGenericArguments().Length == 1)
                {
                    appdomain.RegisterCLRMethodRedirection(i, AddComponent);
                }
            }

            //这里面的通常应该写在InitializeILRuntime，这里为了演示写这里
            var arrGet = typeof(GameObject).GetMethods();
            foreach (var i in arrGet)
            {
                if (i.Name == "GetComponent" && i.GetGenericArguments().Length == 1)
                {
                    appdomain.RegisterCLRMethodRedirection(i, GetComponent);
                }
            }
        }

        MonoBehaviourAdapter.Adaptor GetComponent(ILType type)
        {
            var arr = GetComponents<MonoBehaviourAdapter.Adaptor>();
            for (int i = 0; i < arr.Length; i++)
            {
                var instance = arr[i];
                if (instance.ILInstance != null && instance.ILInstance.Type == type)
                {
                    return instance;
                }
            }
            return null;
        }


        unsafe static StackObject* AddComponent(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            //CLR重定向的说明请看相关文档和教程，这里不多做解释
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;

            var ptr = __esp - 1;
            //成员方法的第一个参数为this
            GameObject instance = StackObject.ToObject(ptr, __domain, __mStack) as GameObject;
            if (instance == null)
                throw new System.NullReferenceException();
            __intp.Free(ptr);

            var genericArgument = __method.GenericArguments;
            //AddComponent应该有且只有1个泛型参数
            if (genericArgument != null && genericArgument.Length == 1)
            {
                var type = genericArgument[0];
                object res;
                if (type is CLRType)
                {
                    //Unity主工程的类不需要任何特殊处理，直接调用Unity接口
                    res = instance.AddComponent(type.TypeForCLR);
                }
                else
                {
                    //热更DLL内的类型比较麻烦。首先我们得自己手动创建实例
                    var ilInstance = new ILTypeInstance(type as ILType, false);//手动创建实例是因为默认方式会new MonoBehaviour，这在Unity里不允许
                                                                               //接下来创建Adapter实例
                    var clrInstance = instance.AddComponent<MonoBehaviourAdapter.Adaptor>();
                    //unity创建的实例并没有热更DLL里面的实例，所以需要手动赋值
                    clrInstance.ILInstance = ilInstance;
                    clrInstance.AppDomain = __domain;
                    //这个实例默认创建的CLRInstance不是通过AddComponent出来的有效实例，所以得手动替换
                    ilInstance.CLRInstance = clrInstance;

                    res = clrInstance.ILInstance;//交给ILRuntime的实例应该为ILInstance

                    clrInstance.Awake();//因为Unity调用这个方法时还没准备好所以这里补调一次
                }

                return ILIntepreter.PushObject(ptr, __mStack, res);
            }

            return __esp;
        }

        unsafe static StackObject* GetComponent(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            //CLR重定向的说明请看相关文档和教程，这里不多做解释
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;

            var ptr = __esp - 1;
            //成员方法的第一个参数为this
            GameObject instance = StackObject.ToObject(ptr, __domain, __mStack) as GameObject;
            if (instance == null)
                throw new System.NullReferenceException();
            __intp.Free(ptr);

            var genericArgument = __method.GenericArguments;
            //AddComponent应该有且只有1个泛型参数
            if (genericArgument != null && genericArgument.Length == 1)
            {
                var type = genericArgument[0];
                object res = null;
                if (type is CLRType)
                {
                    //Unity主工程的类不需要任何特殊处理，直接调用Unity接口
                    res = instance.GetComponent(type.TypeForCLR);
                }
                else
                {
                    //因为所有DLL里面的MonoBehaviour实际都是这个Component，所以我们只能全取出来遍历查找
                    var clrInstances = instance.GetComponents<MonoBehaviourAdapter.Adaptor>();
                    for (int i = 0; i < clrInstances.Length; i++)
                    {
                        var clrInstance = clrInstances[i];
                        if (clrInstance.ILInstance != null)//ILInstance为null, 表示是无效的MonoBehaviour，要略过
                        {
                            if (clrInstance.ILInstance.Type == type)
                            {
                                res = clrInstance.ILInstance;//交给ILRuntime的实例应该为ILInstance
                                break;
                            }
                        }
                    }
                }

                return ILIntepreter.PushObject(ptr, __mStack, res);
            }

            return __esp;
        }

        private void OnDestroy()
        {
            if (fs != null)
                fs.Close();
            if (p != null)
                p.Close();
            fs = null;
            p = null;
        }
    }
}