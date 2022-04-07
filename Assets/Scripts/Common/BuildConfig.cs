using System.IO;
public class BuildConfig
{
    /// <summary> ab包目录的名字 </summary>
    public static string SERVER_URL = "http://192.168.1.28:10101/HttpServer/U3D/ABResource/";

    /// <summary> ab包目录的名字 </summary>
    public static string BUILD_TO_NAME = "ABResource";

    /// <summary> ab包在发布游戏目录的名字 </summary>
    public static string RELEASE_TO_NAME = "ABResource";

    /// <summary> ab包在发布游戏热更临时目录的名字 </summary>
    public static string RELEASE_TEMP_TO_NAME = "TempAB";

    /// <summary> ab包版本文件的名字 </summary>
    public static string VERSION_FILE_NAME = "version.txt";

    /// <summary> ab包资源记录文件的名字 </summary>
    public static string ASSET_LIST_FILE_NAME = "AssetList.csv";

    /// <summary> ab包发布游戏热更临时资源记录文件的名字 </summary>
    public static string ASSET_LIST_TEMP_FILE_NAME = "TempAssetList.csv";
}