using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinttiSDK.Utils
{
    class FileUtil
    {
        /// <summary>
        /// 字节数组转float数组
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static float[] FloatArrayFromByteArray(byte[] input)
        {
            float[] output = new float[input.Length / 4];
            Buffer.BlockCopy(input, 0, output, 0, input.Length);
            return output;
        }
        public static short[] byte2ShortArray(byte[] byteArr)
        {
            short[] shortArr = new short[byteArr.Length / 2];
            for (int j = 0; j < shortArr.Length; j++)
            {
                shortArr[j] = (short)((byteArr[2 * j] & 0xFF) | ((byteArr[2 * j + 1] << 8) & 0xFF00));
            }
            return shortArr;
        }

        public static short[] byteArray2ShortArray(byte[] byteArr)
        {
            short[] shortArr = new short[byteArr.Length / 2];
            for (int j = 0; j < shortArr.Length; j++)
            {
                shortArr[j] = BitConverter.ToInt16(new byte[] { byteArr[2 * j], byteArr[2 * j + 1] },0);
            }
            return shortArr;
        }

        public static byte[] Short2Bytes(short paramShort)
        {
            byte[] arrayOfByte = new byte[2];

            arrayOfByte[0] = ((byte)(paramShort & 0xFF));
            arrayOfByte[1] = ((byte)(paramShort >> 8 & 0xFF));
            return arrayOfByte;
        }

        public static byte[] ShortArray2Bytes(short[] paramShort)
        {
            byte[] arrayOfByte = new byte[paramShort.Length*2];
            for(int i = 0; i < paramShort.Length; i++)
            {
                arrayOfByte[2 * i] = ((byte)(paramShort[i] & 0xFF));
                arrayOfByte[1+2*i] = ((byte)(paramShort[i] >> 8 & 0xFF));
            }    
            return arrayOfByte;
        }

        public static byte[] ShortArray2ByteArray(short[] paramShort)
        {
            byte[] arrayOfByte = new byte[paramShort.Length * 2];
            for (int i = 0; i < paramShort.Length; i++)
            {
                arrayOfByte[2 * i] = BitConverter.GetBytes(paramShort[i])[0];
                arrayOfByte[1 + 2 * i] = BitConverter.GetBytes(paramShort[i])[1];
                
            }
            return arrayOfByte;
        }
        public static byte[] ShortArray2ByteArray2(short[] paramShort)
        {
            byte[] arrayOfByte = new byte[paramShort.Length * 2];
            for (int i = 0; i < paramShort.Length; i++)
            {
                arrayOfByte[2 * i] = (byte)(paramShort[i] >> 8);
                arrayOfByte[1 + 2 * i] = (byte)paramShort[i] ;

            }
            return arrayOfByte;
        }


    }
}
