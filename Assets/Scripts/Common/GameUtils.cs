using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

public class GameUtils
{
    /// <summary>
    /// 到指定的目录下，找文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="extenstions">包含或者排除的文件的扩展名</param>
    /// <param name="include">true  包含   false 排除</param>
    /// <returns></returns>
    public static string[] GetAllFilesAtPath(string path, string[] extenstions = null, bool include = false)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }
        string[] allfiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
        ///如果没有排除或者包含的，那么返回所有文件。
        if (extenstions == null)
        {
            return allfiles;
        }
        ///获取到目录下所有的文件。
        if (include)
        {
            return allfiles.Where(file => extenstions.Contains(Path.GetExtension(file).ToLower())).ToArray();
        }
        else
        {
            return allfiles.Where(file => !extenstions.Contains(Path.GetExtension(file).ToLower())).ToArray();
        }
    }

    /// <summary>
    /// 获取一个文件的md5值
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string GetMD5(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        MD5 md5 = new MD5CryptoServiceProvider();
        byte[] retVal = md5.ComputeHash(data);
        return BitConverter.ToString(retVal).Replace("-", "");
    }

    /// <summary>
    /// 复制一个文件夹到指定目录
    /// </summary>
    /// <param name="strFromPath"></param>
    /// <param name="strToPath"></param>
    public static void CopyFolder(string strFromPath, string strToPath)
    {
        //如果源文件夹不存在，则创建
        if (!Directory.Exists(strFromPath))
        {
            Directory.CreateDirectory(strFromPath);
        }

        //取得要拷贝的文件夹名
        string strFolderName = strFromPath.Substring(strFromPath.LastIndexOf("\\") +
          1, strFromPath.Length - strFromPath.LastIndexOf("\\") - 1);
        //如果目标文件夹中没有源文件夹则在目标文件夹中创建源文件夹
        if (!Directory.Exists(strToPath + "\\" + strFolderName))
        {
            Directory.CreateDirectory(strToPath + "\\" + strFolderName);
        }

        //创建数组保存源文件夹下的文件名
        string[] strFiles = Directory.GetFiles(strFromPath);
        //循环拷贝文件
        for (int i = 0; i < strFiles.Length; i++)
        {
            //取得拷贝的文件名，只取文件名，地址截掉。
            string strFileName = strFiles[i].Substring(strFiles[i].LastIndexOf("\\") + 1, strFiles[i].Length - strFiles[i].LastIndexOf("\\") - 1);
            //开始拷贝文件,true表示覆盖同名文件
            File.Copy(strFiles[i], strToPath + "\\" + strFolderName + "\\" + strFileName, true);
        }
        //创建DirectoryInfo实例
        DirectoryInfo dirInfo = new DirectoryInfo(strFromPath);
        //取得源文件夹下的所有子文件夹名称
        DirectoryInfo[] ZiPath = dirInfo.GetDirectories();
        for (int j = 0; j < ZiPath.Length; j++)
        {
            //获取所有子文件夹名
            //string strZiPath = strFromPath + "\\" + ZiPath[j].ToString();
            string strZiPath = ZiPath[j].ToString();
            //把得到的子文件夹当成新的源文件夹，从头开始新一轮的拷贝
            CopyFolder(strZiPath, strToPath + "\\" + strFolderName);
        }

    }

    /// <summary>
    /// 将strFromPath 合并到 strFromPath
    /// </summary>
    /// <param name="strFromPath"></param>
    /// <param name="strToPath"></param>
    public static void MergeFolder(string strFromPath, string strToPath)
    {
        //如果源文件夹不存在，则退出
        if (!Directory.Exists(strFromPath))
        {
            return;
        }

        //如果目标文件夹不存在，则创建
        if (!Directory.Exists(strToPath))
        {
            Directory.CreateDirectory(strToPath);
        }

        //创建数组保存源文件夹下的文件名
        string[] strFiles = Directory.GetFiles(strFromPath);
        //循环拷贝文件
        for (int i = 0; i < strFiles.Length; i++)
        {
            //取得拷贝的文件名，只取文件名，地址截掉。
            string strFileName = strFiles[i].Substring(strFiles[i].LastIndexOf("\\") + 1, strFiles[i].Length - strFiles[i].LastIndexOf("\\") - 1);
            //开始拷贝文件,true表示覆盖同名文件
            File.Copy(strFiles[i], strToPath + "\\" + strFileName, true);
        }
        //创建DirectoryInfo实例
        DirectoryInfo dirInfo = new DirectoryInfo(strFromPath);
        //取得源文件夹下的所有子文件夹名称
        DirectoryInfo[] ZiPath = dirInfo.GetDirectories();
        for (int j = 0; j < ZiPath.Length; j++)
        {
            //获取所有子文件夹名
            //string strZiPath = strFromPath + "\\" + ZiPath[j].ToString();
            string strZiPath = ZiPath[j].ToString();
            //把得到的子文件夹当成新的源文件夹，从头开始新一轮的拷贝
            CopyFolder(strZiPath, strToPath);
        }

    }
}
