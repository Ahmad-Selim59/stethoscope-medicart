using mintti_sdk.ble.utils;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinttiSDK
{
    public class FileUtil
    {
        private static string fileName;
        private static string directory;
        private static string wavFileName;

        public static void CreateRecordFile()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            //Console.WriteLine("C:\\Users\\ws13275841808\\Desktop\\pcmData\\");
            directory = desktopPath+"\\pcmData\\" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);//创建路径
            fileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".pcm";
            wavFileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".wav";
        }
        //写入pcm数据
        public static void WritePcmData(short[] data)
        {
            //Console.WriteLine("写文件1");
            //存储文件的路径
            string path = Path.Combine(directory, fileName);
            using (FileStream fsWtire = new FileStream(path, FileMode.Append, FileAccess.Write))
            {
                byte[] bytes = DataUtil.ShortArray2ByteArray(data);
                //Console.WriteLine("写文件2");
                fsWtire.Write(bytes, 0, bytes.Length);
            }
        }

        public static string GetWavFilePath()
        {
            if (directory != null && wavFileName != null)
            {
                return Path.Combine(directory, wavFileName);
            }
            else
            {
                return null;
            }
        }


        public static void PcmToWav()
        {
            string pcmPath = Path.Combine(directory, fileName);
            string wavPath = Path.Combine(directory, wavFileName);
            var s = new RawSourceWaveStream(File.OpenRead(pcmPath), new WaveFormat(8000, 1));
            WaveFileWriter.CreateWaveFile(wavPath, s);
        }

        public static long GetFileSize(string path)
        {
            FileInfo fileInfo = null;
            try
            {
                fileInfo = new FileInfo(path);
            }
            catch
            {
                return 0;
            }
            if (fileInfo != null && fileInfo.Exists)
            {
                return fileInfo.Length;
            }
            else
            {
                return 0;
            }
        }


        // <summary>
        /// 计算文件的MD5校验
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetMD5HashFromFile()
        {
            try
            {
                FileStream file = new FileStream(GetWavFilePath(), FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
            }
        }

        public static long getAudioDuration()
        {
            long fileLength = GetFileSize(Path.Combine(directory, wavFileName));
            long pcmLength = fileLength - 46;
            long pcmDuration = pcmLength / 16;
            return pcmDuration;
        }

        public static void DeleteFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        public static void DeleteDir(string path)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, true);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }

            }
        }

        /// <summary>
        /// 获取上级文件夹路径
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="degree">级数</param>
        /// <returns></returns>
        public static string GetParentDirectory(string path, int degree = 0)
        {
            DirectoryInfo pathInfo = new DirectoryInfo(path);
            string newPath = pathInfo.Parent.FullName;

            for (int i = 0; i < degree; i++)
            {
                pathInfo = new DirectoryInfo(newPath);
                newPath = pathInfo.Parent.FullName;
            }
            return newPath;
        }

    }


}
