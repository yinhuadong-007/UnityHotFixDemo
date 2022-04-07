using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//1、 版本号检查
//2. 下载服务器索引文件
//3.、先临时目录索引对比(断点续传) 再跟本地索引对比   

//4.、根据差异（md5）下载需要更新的AB包

//5.、将下载的AB包放入更新的临时目录

//6.、最后将整个临时目录里面的内容 覆盖到本地的旧AB包，保存服务器的索引文件到本地，保存服务器版本号，清理临时目录


public class HotFix : MonoBehaviour
{
    public Slider progressBar;
    public Text infoTxt;
    /// <summary>
    /// 服务器的资源根目录
    /// </summary>
    private string serverURL;
    /// <summary>
    /// 本地根目录
    /// </summary>
    private string localPath;
    /// <summary>
    /// 本地临时根目录
    /// </summary>
    private string tempPath;
    /// <summary>
    /// 本地版本字符串
    /// </summary>
    private string localVersionStr; //本地的版本号字符串
    /// <summary>
    /// 本地版本
    /// </summary>
    private Version localVer;

    /// <summary>
    /// 服务器版本字符串
    /// </summary>
    private string ServerVerStr;
    /// <summary>
    /// 服务器版本。
    /// </summary>
    private Version serverVer;

    /// <summary>
    /// 解析出来的本地的资源列表（索引文件）
    /// </summary>
    private Dictionary<string, AssetItem> locaAssetList;
    /// <summary>
    /// 解析出来的本地临时的资源列表（索引文件）
    /// </summary>
    private Dictionary<string, AssetItem> tempAssetList;
    /// <summary>
    /// 解析出来的服务器端的索引文件。
    /// </summary>
    private Dictionary<string, AssetItem> serverAssetList;
    /// <summary>
    /// 服务器端的索引文件字符串
    /// </summary>
    private string serverAssetListStr;
    /// <summary>
    /// 待下载的资源列表队列
    /// </summary>
    private Queue<AssetItem> downloadAssetQueue = new Queue<AssetItem>();

    /// <summary>
    /// 待下载的资源总大小
    /// </summary>
    private double totalBytes = 0;
    private double crtBytes = 0;

    /// <summary>
    /// 并行下载的数量
    /// </summary>
    private static int parallelTotalCount = 1;

    /// <summary>
    /// 当前下载的数量
    /// </summary>
    private int parallelCurCount = 0;

    /// <summary>
    /// 重试次数
    /// </summary>
    private static UInt16 retryCount = 5;


    private void Awake()
    {
        //"http://10.161.29.2/AB";
        //serverURL = "http://10.161.29.99:8080/下载内容/AB";
        // serverURL = "http://10.161.26.26/AB";
        serverURL = BuildConfig.SERVER_URL;
        localPath = Path.Combine(Application.persistentDataPath, BuildConfig.RELEASE_TO_NAME);

        tempPath = Path.Combine(Application.persistentDataPath, BuildConfig.RELEASE_TEMP_TO_NAME);

        Debug.Log("localPath = " + localPath);


    }
    // Start is called before the first frame update
    void Start()
    {
        //1.检查版本
        checkVersion();
    }


    private void checkVersion()
    {
        //2.下载服务器版本号。
        string verURL = Path.Combine(this.serverURL, BuildConfig.VERSION_FILE_NAME);
        DownloadUrl(verURL, (x) =>
        {
            this.ServerVerStr = UTF8Encoding.UTF8.GetString(x);
            Debug.Log("下载到服务器版本 = " + this.ServerVerStr);
            serverVer = Version.Get(this.ServerVerStr);

            //1.读本地版本。
            string path = Path.Combine(localPath, BuildConfig.VERSION_FILE_NAME);
            if (File.Exists(path)) //不是第一玩了。
            {
                localVersionStr = File.ReadAllText(path);
                localVer = Version.Get(localVersionStr);

                if (this.localVer.verStr != this.serverVer.verStr)
                {
                    if (this.localVer.big != this.serverVer.big)
                    {
                        Debug.Log("调android的应用商店界面，让用户重新下载");///大版本号为应用商店上架的基础包，大版本号不一致需要到商店下载 强制更新
                    }
                    else
                    {
                        ///走热更新流程。
                        Debug.Log("服务器和客户端的版本号不一样，热更新流程");
                        HotUpdateLogic();
                    }
                }
                else
                {
                    Debug.Log("版本一致不需要更新");
                    EnterGame();
                }
            }
            else //第一次玩。
            {
                ///走热更新流程。
                Debug.Log("没有本地版本号，玩家第一次玩，走热更新流程");
                HotUpdateLogic();
            }


        },
       null,
       (x) =>
       {
           Debug.LogError("下载版本号出错" + x);
       });

    }

    /// <summary>
    /// 热更新流程。
    /// </summary>
    private void HotUpdateLogic()
    {
        //1.读取本地索引文件
        locaAssetList = new Dictionary<string, AssetItem>();
        string path = Path.Combine(localPath, BuildConfig.ASSET_LIST_FILE_NAME);
        if (File.Exists(path))
        {
            string[] lst = File.ReadAllLines(path);
            //解析
            foreach (string info in lst)
            {
                if (string.IsNullOrEmpty(info))
                {
                    continue;
                }
                string[] strs = info.Split('|');
                AssetItem ai = new AssetItem();
                ai.path = strs[0];
                ai.length = int.Parse(strs[1]);
                ai.md5 = strs[2];
                ai.origin = info;
                locaAssetList.Add(ai.path, ai);
            }
        }

        //2.读取临时目录索引文件
        tempAssetList = new Dictionary<string, AssetItem>();
        string tPath = Path.Combine(tempPath, BuildConfig.ASSET_LIST_TEMP_FILE_NAME);
        if (File.Exists(tPath))
        {
            string[] lst = File.ReadAllLines(tPath);
            //解析
            foreach (string info in lst)
            {
                if (string.IsNullOrEmpty(info))
                {
                    continue;
                }
                string[] strs = info.Split('|');
                AssetItem ai = new AssetItem();
                ai.path = strs[0];
                ai.length = int.Parse(strs[1]);
                ai.md5 = strs[2];
                ai.origin = info;
                tempAssetList.Add(ai.path, ai);
            }
        }


        ///3.下载服务器端的清单文件（索引文件）
        serverAssetList = new Dictionary<string, AssetItem>();
        string assetListURL = Path.Combine(this.serverURL, this.serverVer.verStr, BuildConfig.ASSET_LIST_FILE_NAME);
        DownloadUrl(assetListURL, (x) =>
        {
            serverAssetListStr = UTF8Encoding.UTF8.GetString(x);
            string[] tmpArrs = serverAssetListStr.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var info in tmpArrs)
            {
                if (string.IsNullOrEmpty(info))
                {
                    continue;
                }
                string[] strs = info.Split('|');
                AssetItem ai = new AssetItem();
                ai.path = strs[0];
                ai.length = int.Parse(strs[1]);
                ai.md5 = strs[2];
                ai.origin = info;
                this.serverAssetList.Add(ai.path, ai);
            }

            /// 解析完毕后，
            /// 跟本地索引和本地临时目录对比
            findDownLoadAsset();
            ////4.、根据差异（md5）下载需要更新的AB包
            DownloadAllAsset();
            ShowProgress();
        },
       null,
       (x) =>
       {
           Debug.LogError("下载服务器端的索引文件出错" + x);
       });
    }


    /// <summary>
    /// 找出所有的待下载资源包信息。
    /// </summary>
    private void findDownLoadAsset()
    {
        foreach (var item in this.serverAssetList)
        {
            bool needDown = false;
            //临时目录有该资源
            if (this.tempAssetList.ContainsKey(item.Value.path))
            {
                if (item.Value.md5 != this.tempAssetList[item.Value.path].md5)//并且不一致
                {
                    needDown = true;
                }
                else
                {
                    crtBytes += item.Value.length;//当前已经加载完成的
                    totalBytes += item.Value.length;//累加总大小
                }
            }
            else if (this.locaAssetList.ContainsKey(item.Value.path)) //本地有资源
            {
                if (item.Value.md5 != this.locaAssetList[item.Value.path].md5)//并且不一致
                {
                    needDown = true;
                }
            }
            else
            {
                needDown = true;
            }

            if (needDown)
            {
                downloadAssetQueue.Enqueue(item.Value);
                totalBytes += item.Value.length;  //累加总大小
            }
        }
    }

    private void ShowProgress()
    {
        double pro = (this.crtBytes / this.totalBytes);
        Debug.Log("下载进度=" + pro.ToString("0.00"));
        progressBar.value = (float)pro;

        this.infoTxt.text = "目标版本:" + this.serverVer.verStr + "  下载进度=" + (pro * 100).ToString("0.00") + "%     " + (this.crtBytes / 1024 / 1024).ToString("0.0") + "MB/" + (this.totalBytes / 1024 / 1024).ToString("0.0") + "MB";
    }


    /// <summary>
    /// 下载所有待下载的资源包。
    /// </summary>
    private void DownloadAllAsset()
    {
        ///该下载的资源队列时空的。
        if (downloadAssetQueue.Count <= 0 || parallelCurCount >= parallelTotalCount)
        {
            if (downloadAssetQueue.Count <= 0 && parallelCurCount == 0)
            {
                DownloadComplete();
            }
            return;
        }
        parallelCurCount++;
        AssetItem it = downloadAssetQueue.Dequeue();
        DownloadOneAsset(it, retryCount);
        DownloadAllAsset();
    }

    private void DownloadOneAsset(AssetItem it, UInt16 remianRetryCount)
    {
        string url = this.serverURL + "/" + this.serverVer.verStr + "/" + it.path;
        string tempList = Path.Combine(this.tempPath, BuildConfig.ASSET_LIST_TEMP_FILE_NAME);
        if (!Directory.Exists(this.tempPath))
        {
            Directory.CreateDirectory(this.tempPath);
        }

        DownloadUrl(url, async (data) =>
        {
            this.crtBytes += it.length; //更新下载完成字节数
            ShowProgress();
            //保存下载的东西到临时目录，记录当前的文件信息
            //要保存的目录不存在创建一波。
            string savepath = tempPath + "/" + it.path;
            string dir = Path.GetDirectoryName(savepath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            ///保存下载的资源包。
            File.WriteAllBytes(savepath, data);
            ///记录本次的文件信息
            using (StreamWriter sw = File.AppendText(tempList))
            {
                sw.WriteLine(it.origin);
            }

            parallelCurCount--;
            ///UI更新。。。
            if (downloadAssetQueue.Count > 0)
            {
                DownloadAllAsset();
            }
            else
            {
                //将本地临时文件里面的内容移动到本地ab
                DownloadComplete();
                return;
            }
        },
        (x) =>
        {

        }, (x) =>
        {
            Debug.LogError("下载资源出错" + it.path + "  msg=" + x + "remianRetryCount= " + remianRetryCount);
            if (remianRetryCount > 0)
            {
                DownloadOneAsset(it, --remianRetryCount);
            }
            else
            {
                //失败资源重新进入队列,等其他资源下载完成后再尝试
                parallelCurCount--;
                if (!it.retry)
                {
                    it.retry = true;//标记为二次下载
                    downloadAssetQueue.Enqueue(it);
                    DownloadAllAsset();
                }
                else
                {
                    //下载失败，请重试（弹出重试框） or 使用旧版进入游戏（直接进入游戏） or 继续尝试下载

                    //此处使用继续下载
                    downloadAssetQueue.Enqueue(it);
                    DownloadAllAsset();
                }
            }
        });

    }

    /// <summary>
    /// 下载完成
    /// </summary>
    private void DownloadComplete()
    {
        try
        {
            if (Directory.Exists(this.tempPath))
            {
                /// 将本地临时文件里面的内容合并到本地ab
                GameUtils.MergeFolder(this.tempPath, this.localPath);
                ///保存服务器的索引文件到本地
                saveIndexFile();
                Debug.Log("服务器清单文件保存本地完毕..");
                ///保存服务器端的版本号
                saveVersionTxt();
                ///删除临时目录
                Directory.Delete(this.tempPath, true);
                Debug.Log("版本号保存本地完毕..");
                Debug.Log("版本更新已完毕，开始进入游戏.....");
            }
            ///进入游戏场景
            EnterGame();
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    /// <summary>
    /// 进入游戏。
    /// </summary>
    private void EnterGame()
    {
        this.Invoke("InvokeEnterGame", 2.0f);
    }
    private void InvokeEnterGame()
    {
        SceneManager.LoadScene("LoginScene");
    }

    private void saveVersionTxt()
    {
        string path = Path.Combine(this.localPath, BuildConfig.VERSION_FILE_NAME);
        File.WriteAllText(path, this.ServerVerStr);
    }

    private void saveIndexFile()
    {
        string path = Path.Combine(this.localPath, BuildConfig.ASSET_LIST_FILE_NAME);
        File.WriteAllText(path, this.serverAssetListStr);
    }


    #region 下载的工具函数
    /// <summary>
    /// 
    /// </summary>
    /// <param name="url"></param>
    /// <param name="onComplete"></param>
    /// <param name="onProgress"></param>
    /// <param name="onError"></param>
    public void DownloadUrl(string url, Action<byte[]> onComplete, Action<double> onProgress = null, Action<string> onError = null)
    {
        StartCoroutine(startDownLoad(url, onComplete, onProgress, onError));
    }
    private IEnumerator startDownLoad(string url, Action<byte[]> onComplete, Action<double> onProgress, Action<string> onError)
    {
        Debug.Log(url);
        UnityWebRequest req = UnityWebRequest.Get(url);
        UnityWebRequestAsyncOperation op = req.SendWebRequest(); //开始发送网络请求，开始下载的过程
        op.completed += (x) =>
        {
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(req.result.ToString());
                onError?.Invoke(req.error);
            }
            else
            {
                byte[] data = req.downloadHandler.data;
                onComplete?.Invoke(data);
            }
        };

        while (!op.isDone)
        {
            onProgress?.Invoke(op.progress);
            yield return null; //等待一帧、
        }
    }
    #endregion

}

public class Version
{
    public int big = 0;
    public int mid;
    public int small;
    public string verStr;
    public static Version Get(string verStr)
    {
        string[] strs = verStr.Split('.');
        Version ver = new Version();
        ver.big = int.Parse(strs[0]);
        ver.mid = int.Parse(strs[1]);
        ver.small = int.Parse(strs[2]);
        ver.verStr = verStr;
        return ver;
    }
}

public class AssetItem
{
    public string path;
    public int length;
    public string md5;

    public string origin;

    public bool retry = false;//该资源是否已经是二次下载

}

