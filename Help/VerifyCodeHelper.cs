using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AutoTicket
{
    class UnCodebase
    {
        public Bitmap bmpobj;
        public UnCodebase(Bitmap pic)
        {
            bmpobj = new Bitmap(pic);    //转换为Format32bppRgb
        }

        /// <summary>
        /// 根据RGB，计算灰度值
        /// </summary>
        /// <param name="posClr">Color值</param>
        /// <returns>灰度值，整型</returns>
        private int GetGrayNumColor(System.Drawing.Color posClr)
        {
            return (posClr.R * 19595 + posClr.G * 38469 + posClr.B * 7472) >> 16;
        }

        /// <summary>
        /// 灰度转换,逐点方式
        /// </summary>
        public void GrayByPixels()
        {
            for (int i = 0; i < bmpobj.Height; i++)
            {
                for (int j = 0; j < bmpobj.Width; j++)
                {
                    int tmpValue = GetGrayNumColor(bmpobj.GetPixel(j, i));
                    bmpobj.SetPixel(j, i, Color.FromArgb(tmpValue, tmpValue, tmpValue));
                }
            }
        }

        /// <summary>
        /// 去图形边框
        /// </summary>
        /// <param name="borderWidth"></param>
        public void ClearPicBorder(int borderWidth)
        {
            for (int i = 0; i < bmpobj.Height; i++)
            {
                for (int j = 0; j < bmpobj.Width; j++)
                {
                    if (i < borderWidth || j < borderWidth || j > bmpobj.Width - 1 - borderWidth || i > bmpobj.Height - 1 - borderWidth)
                        bmpobj.SetPixel(j, i, Color.FromArgb(255, 255, 255));
                }
            }
        }

        /// <summary>
        /// 灰度转换,逐行方式
        /// </summary>
        public void GrayByLine()
        {
            Rectangle rec = new Rectangle(0, 0, bmpobj.Width, bmpobj.Height);
            BitmapData bmpData = bmpobj.LockBits(rec, ImageLockMode.ReadWrite, bmpobj.PixelFormat);// PixelFormat.Format32bppPArgb);
            //    bmpData.PixelFormat = PixelFormat.Format24bppRgb;
            IntPtr scan0 = bmpData.Scan0;
            int len = bmpobj.Width * bmpobj.Height;
            int[] pixels = new int[len];
            Marshal.Copy(scan0, pixels, 0, len);

            //对图片进行处理
            int GrayValue = 0;
            for (int i = 0; i < len; i++)
            {
                GrayValue = GetGrayNumColor(Color.FromArgb(pixels[i]));
                pixels[i] = (byte)(Color.FromArgb(GrayValue, GrayValue, GrayValue)).ToArgb();      //Color转byte
            }

            bmpobj.UnlockBits(bmpData);
        }

        /// <summary>
        /// 得到有效图形并调整为可平均分割的大小
        /// </summary>
        /// <param name="dgGrayValue">灰度背景分界值</param>
        /// <param name="CharsCount">有效字符数</param>
        /// <returns></returns>
        public void GetPicValidByValue(int dgGrayValue, int CharsCount)
        {
            int posx1 = bmpobj.Width; int posy1 = bmpobj.Height;
            int posx2 = 0; int posy2 = 0;
            for (int i = 0; i < bmpobj.Height; i++)      //找有效区
            {
                for (int j = 0; j < bmpobj.Width; j++)
                {
                    int pixelValue = bmpobj.GetPixel(j, i).R;
                    if (pixelValue < dgGrayValue)     //根据灰度值
                    {
                        if (posx1 > j) posx1 = j;
                        if (posy1 > i) posy1 = i;

                        if (posx2 < j) posx2 = j;
                        if (posy2 < i) posy2 = i;
                    };
                };
            };
            // 确保能整除
            int Span = CharsCount - (posx2 - posx1 + 1) % CharsCount;   //可整除的差额数
            if (Span < CharsCount)
            {
                int leftSpan = Span / 2;    //分配到左边的空列 ，如span为单数,则右边比左边大1
                if (posx1 > leftSpan)
                    posx1 = posx1 - leftSpan;
                if (posx2 + Span - leftSpan < bmpobj.Width)
                    posx2 = posx2 + Span - leftSpan;
            }
            //复制新图
            Rectangle cloneRect = new Rectangle(posx1, posy1, posx2 - posx1 + 1, posy2 - posy1 + 1);
            bmpobj = bmpobj.Clone(cloneRect, bmpobj.PixelFormat);
        }

        /// <summary>
        /// 得到有效图形,图形为类变量
        /// </summary>
        /// <param name="dgGrayValue">灰度背景分界值</param>
        /// <param name="CharsCount">有效字符数</param>
        /// <returns></returns>
        public void GetPicValidByValue(int dgGrayValue)
        {
            int posx1 = bmpobj.Width; int posy1 = bmpobj.Height;
            int posx2 = 0; int posy2 = 0;
            for (int i = 0; i < bmpobj.Height; i++)      //找有效区
            {
                for (int j = 0; j < bmpobj.Width; j++)
                {
                    int pixelValue = bmpobj.GetPixel(j, i).R;
                    if (pixelValue < dgGrayValue)     //根据灰度值
                    {
                        if (posx1 > j) posx1 = j;
                        if (posy1 > i) posy1 = i;

                        if (posx2 < j) posx2 = j;
                        if (posy2 < i) posy2 = i;
                    };
                };
            };
            //复制新图
            Rectangle cloneRect = new Rectangle(posx1, posy1, posx2 - posx1 + 1, posy2 - posy1 + 1);
            bmpobj = bmpobj.Clone(cloneRect, bmpobj.PixelFormat);
        }

        /// <summary>
        /// 得到有效图形,图形由外面传入
        /// </summary>
        /// <param name="dgGrayValue">灰度背景分界值</param>
        /// <param name="CharsCount">有效字符数</param>
        /// <returns></returns>
        public Bitmap GetPicValidByValue(Bitmap singlepic, int dgGrayValue)
        {
            int posx1 = singlepic.Width; int posy1 = singlepic.Height;
            int posx2 = 0; int posy2 = 0;
            for (int i = 0; i < singlepic.Height; i++)      //找有效区
            {
                for (int j = 0; j < singlepic.Width; j++)
                {
                    int pixelValue = singlepic.GetPixel(j, i).R;
                    if (pixelValue < dgGrayValue)     //根据灰度值
                    {
                        if (posx1 > j) posx1 = j;
                        if (posy1 > i) posy1 = i;

                        if (posx2 < j) posx2 = j;
                        if (posy2 < i) posy2 = i;
                    };
                };
            };
            //复制新图
            Rectangle cloneRect = new Rectangle(posx1, posy1, posx2 - posx1 + 1, posy2 - posy1 + 1);
            return singlepic.Clone(cloneRect, singlepic.PixelFormat);
        }

        /// <summary>
        /// 平均分割图片
        /// </summary>
        /// <param name="RowNum">水平上分割数</param>
        /// <param name="ColNum">垂直上分割数</param>
        /// <returns>分割好的图片数组</returns>
        public Bitmap[] GetSplitPics(int RowNum, int ColNum)
        {
            if (RowNum == 0 || ColNum == 0)
                return null;
            int singW = bmpobj.Width / RowNum;
            int singH = bmpobj.Height / ColNum;
            Bitmap[] PicArray = new Bitmap[RowNum * ColNum];

            Rectangle cloneRect;
            for (int i = 0; i < ColNum; i++)      //找有效区
            {
                for (int j = 0; j < RowNum; j++)
                {
                    cloneRect = new Rectangle(j * singW, i * singH, singW, singH);
                    PicArray[i * RowNum + j] = bmpobj.Clone(cloneRect, bmpobj.PixelFormat);//复制小块图
                }
            }
            return PicArray;
        }

        /// <summary>
        /// 返回灰度图片的点阵描述字串，1表示灰点，0表示背景
        /// </summary>
        /// <param name="singlepic">灰度图</param>
        /// <param name="dgGrayValue">背前景灰色界限</param>
        /// <returns></returns>
        public string GetSingleBmpCode(Bitmap singlepic, int dgGrayValue)
        {
            Color piexl;
            string code = "";
            for (int posy = 0; posy < singlepic.Height; posy++)
                for (int posx = 0; posx < singlepic.Width; posx++)
                {
                    piexl = singlepic.GetPixel(posx, posy);
                    if (piexl.R < dgGrayValue)    // Color.Black )
                        code = code + "1";
                    else
                        code = code + "0";
                }
            return code;
        }
    }

    class unCodeAiYing : UnCodebase
    {
        //字符表 顺序为0..9,A..Z,a..z
        string[] CodeArray = new string[] {
"001100010010100001100001100001100001100001100001010010001100",
"00100011001010000100001000010000100001000010011111",
"011110100001100001000001000110001000010000100000100000111111",
"011110100001100001000001001110000001000001100001100001011110",
"000010000110001010010010100010100010111111000010000010000010",
"111111100000100000100000111110000001000001000001100001011110",
"001110010000100000100000111110100001100001100001100001011110",
"111111000001000001000010000010000010000100000100000100000100",
"011110100001100001100001011110100001100001100001100001011110",
"011110100001100001100001011111000001000001000001000010011100",
"001100010010100001100001100001111111100001100001100001100001",
"111110100001100001100001111110100001100001100001100001111110",
"011110100001100000100000100000100000100000100000100001011110",
"111100100010100001100001100001100001100001100001100010111100",
"111111100000100000100000111110100000100000100000100000111111",
"111111100000100000100000111110100000100000100000100000100000",
"011110100001100000100000100000100111100001100001100011011101",
"100001100001100001100001111111100001100001100001100001100001",
"11111001000010000100001000010000100001000010011111",
"000111000010000010000010000010000010000010100010100010011100",
"100001100010100100101000110000110000101000100100100010100001",
"100000100000100000100000100000100000100000100000100000111111",
"1000001110001111000111010101101010110010011001001100000110000011000001",
"100001110001110001101001101001100101100101100011100011100001",
"011110100001100001100001100001100001100001100001100001011110",
"111110100001100001100001111110100000100000100000100000100000",
"01111001000010100001010000101000010100001010000101011010110011001111000000011",
"111110100001100001100001111110100100100010100010100001100001",
"011110100001100001100000011000000110000001100001100001011110",
"1111111000100000010000001000000100000010000001000000100000010000001000",
"100001100001100001100001100001100001100001100001100001011110",
"1000001100000110000010100010010001001000100010100001010000010000001000",
"1000001100000110000011001001100100110010011001001101010110101010100010",
"100001100001010010010010001100001100010010010010100001100001",
"1000001100000101000100100010001010000010000001000000100000010000001000",
"111111000001000001000010000100001000010000100000100000111111",
"011110100001000111011001100001100011011101",
"100000100000100000101110110001100001100001100001110001101110",
"011110100001100000100000100000100001011110",
"000001000001000001011101100011100001100001100001100011011101",
"011110100001100001111111100000100000011110",
"001110010001010000010000111110010000010000010000010000010000",
"000001011101100010100010100010011100010000011110100001100001011110",
"100000100000100000101110110001100001100001100001100001100001",
"00100001000000001100001000010000100001000010011111",
"00001000010000000011000010000100001000010000100001000011001001100",
"100000100000100000100010100100101000111000100100100010100001",
"01100001000010000100001000010000100001000010011111",
"1110110100100110010011001001100100110010011001001",
"101110110001100001100001100001100001100001",
"011110100001100001100001100001100001011110",
"101110110001100001100001100001110001101110100000100000100000",
"011101100011100001100001100001100011011101000001000001000001",
"101110110001100000100000100000100000100000",
"011110100001100000011110000001100001011110",
"001000001000111110001000001000001000001000001000001001000110",
"100001100001100001100001100001100011011101",
"100001100001100001010010010010001100001100",
"1000001100100110010011001001100100110101010100010",
"100001100001010010001100010010100001100001",
"100001100001100001100001100001010011001101000001000010011100",
"111111000010000100001000010000100000111111"
        };

        public unCodeAiYing(Bitmap pic)
            : base(pic)
        {
        }

        public string getPicnum()
        {
            GrayByPixels(); //灰度处理
            GetPicValidByValue(128, 4); //得到有效空间
            Bitmap[] pics = GetSplitPics(4, 1);     //分割

            if (pics.Length != 4)
            {
                return ""; //分割错误
            }
            else  // 重新调整大小
            {
                pics[0] = GetPicValidByValue(pics[0], 128);
                pics[1] = GetPicValidByValue(pics[1], 128);
                pics[2] = GetPicValidByValue(pics[2], 128);
                pics[3] = GetPicValidByValue(pics[3], 128);
            }

            //      if (!textBoxInput.Text.Equals(""))
            string result = "";
            char singleChar = ' ';
            {
                for (int i = 0; i < 4; i++)
                {
                    string code = GetSingleBmpCode(pics[i], 128);   //得到代码串

                    for (int arrayIndex = 0; arrayIndex < CodeArray.Length; arrayIndex++)
                    {
                        if (CodeArray[arrayIndex].Equals(code))  //相等
                        {
                            if (arrayIndex < 10)   // 0..9
                                singleChar = (char)(48 + arrayIndex);
                            else if (arrayIndex < 36) //A..Z
                                singleChar = (char)(65 + arrayIndex - 10);
                            else
                                singleChar = (char)(97 + arrayIndex - 36);
                            result = result + singleChar;
                        }
                    }
                }
            }
            return result;
        }
    }
}