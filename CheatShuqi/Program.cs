﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;

namespace Shuqi
{
    class BookInfo
    {
        public string bookId;
        public string bookName;
        public string authorName;
        public string chapterNum;
        public List<ChapterInfo> chapterList;
    }

    class ChapterInfo
    {
        public string chapterId;
        public string chapterName;
        public string chapterOrdid;
    }

    class Program
    {
        /// <summary>
        /// 章节列表(手机浏览器)
        /// POST http://walden1.shuqireader.com/webapi/book/chapterlist
        /// timestamp=时间戳
        /// user_id = 用户名（似乎随便填）
        /// bookId=书id
        /// sign = md5(bookId + timestamp + user_id + "37e81a9d8f02596e1b895d07c171d5c9")
        /// 
        /// 章节内容(uc小说中抓包得到的)
        /// GET http://c1.shuqireader.com/httpserver/filecache/get_book_content_书id_章节id.xml
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            try
            {
                Console.Write("bookid:");
                string bookid = Console.ReadLine();
                var book = ReadBookInfo(bookid);

                Console.WriteLine("书名 : " + book.bookName);
                Console.WriteLine("作者 : " + book.authorName);
                Console.WriteLine("章节数 : " + book.chapterNum);
                Console.WriteLine("开始下载...");
                Thread.Sleep(2000);

                StreamWriter w = new StreamWriter(book.bookName + ".txt", false, Encoding.UTF8);
                foreach (var item in book.chapterList)
                {
                    string result = System.Text.RegularExpressions.Regex.Replace(item.chapterName, @"[^0-9]+", "");
                    if (result != null && !string.IsNullOrEmpty(result))
                    {
                        var replacestr = (item.chapterName.Contains("第") ? "" : "第") + NumberToChinese(Convert.ToInt32(result)) + (item.chapterName.Contains("章") ? "" : "章");

                        item.chapterName = item.chapterName.Replace(result, replacestr);
                    }


                    Console.WriteLine("{0} : {1}, chapterId : {2}", item.chapterOrdid.PadLeft(5, ' '), item.chapterName, item.chapterId);

                    string content = ReadChapterContent(bookid, item);
                    w.WriteLine(item.chapterName);
                    w.WriteLine();
                    w.WriteLine(content.Trim());
                    w.WriteLine();
                }
                w.Close();

                Console.WriteLine("全书下载完成,按任意键退出");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.ReadKey(true);
        }

        /// <summary>
        /// 数字转中文
        /// </summary>
        /// <param name="number">eg: 22</param>
        /// <returns></returns>
        public static string NumberToChinese(int number)
        {
            string res = string.Empty;
            string str = number.ToString();
            string schar = str.Substring(0, 1);
            switch (schar)
            {
                case "1":
                    res = "一";
                    break;
                case "2":
                    res = "二";
                    break;
                case "3":
                    res = "三";
                    break;
                case "4":
                    res = "四";
                    break;
                case "5":
                    res = "五";
                    break;
                case "6":
                    res = "六";
                    break;
                case "7":
                    res = "七";
                    break;
                case "8":
                    res = "八";
                    break;
                case "9":
                    res = "九";
                    break;
                default:
                    res = "零";
                    break;
            }
            if (str.Length > 1)
            {
                switch (str.Length)
                {
                    case 2:
                    case 6:
                        res += "十";
                        break;
                    case 3:
                    case 7:
                        res += "百";
                        break;
                    case 4:
                        res += "千";
                        break;
                    case 5:
                        res += "万";
                        break;
                    default:
                        res += "";
                        break;
                }
                res += NumberToChinese(int.Parse(str.Substring(1, str.Length - 1)));
            }
            return res;
        }


        public static BookInfo ReadBookInfo(string bookid)
        {
            string userid = "8000000";
            long timestamp = 1514984538213;
            string signcontent = string.Concat(bookid, timestamp, userid, "37e81a9d8f02596e1b895d07c171d5c9");
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] output = md5.ComputeHash(Encoding.UTF8.GetBytes(signcontent));
            string byte2String = null;

            for (int i = 0; i < output.Length; i++)
            {
                byte2String += output[i].ToString("x2");
            }

            byte[] postData = Encoding.UTF8.GetBytes(string.Format("timestamp={0}&user_id={1}&bookId={2}&sign={3}", timestamp, userid, bookid, byte2String));
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://walden1.shuqireader.com/webapi/book/chapterlist");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postData.Length;
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(postData, 0, postData.Length);
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader stream = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string srcString = stream.ReadToEnd();
            response.Close();
            stream.Close();

            var content = fastJSON.JSON.Parse(srcString) as Dictionary<string, object>;
            if (string.Equals(content["state"], "200") == false)
            {
                throw new System.Exception(content["message"] as string);
            }

            var data = content["data"] as Dictionary<string, object>;
            var datacl = data["chapterList"] as List<object>;

            BookInfo book = new BookInfo();
            book.bookName = data["bookName"] as string;
            book.authorName = data["authorName"] as string;
            book.chapterNum = data["chapterNum"] as string;
            book.chapterList = new List<ChapterInfo>();
            foreach (var item in datacl)
            {
                var aaa = item as Dictionary<string, object>;
                var volumeList = aaa["volumeList"] as List<object>;
                foreach (var volumeRaw in volumeList)
                {
                    var volume = volumeRaw as Dictionary<string, object>;
                    ChapterInfo info = new ChapterInfo();
                    info.chapterId = volume["chapterId"] as string;
                    info.chapterName = volume["chapterName"] as string;
                    info.chapterOrdid = volume["chapterOrdid"] as string;
                    book.chapterList.Add(info);
                }
            }

            return book;
        }

        public static string ReadChapterContent(string bookid, ChapterInfo chapter)
        {
            while (true)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format("http://c1.shuqireader.com/httpserver/filecache/get_book_content_{0}_{1}_1463557822_1_0.xml", bookid, chapter.chapterId));
                    request.Method = "GET";
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    StreamReader stream = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                    string srcString = stream.ReadToEnd();
                    response.Close();
                    stream.Close();

                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.LoadXml(srcString);

                    string badbase64 = xmldoc.LastChild.InnerText;

                    string content = DecodeChapterContent(badbase64);
                    return content.Replace("<br/>", "\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("下载失败, 章节 : {0}", chapter.chapterId);
                }
            }
        }

        /// <summary>
        /// uc.apk
        ///     novel.jar
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string DecodeChapterContent(string code)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(code);
            for (int i = 0; i < bytes.Length; i++)
            {
                byte charAt = bytes[i];
                if ('A' <= charAt && charAt <= 'Z')
                {
                    charAt = (byte)(charAt + 13);
                    if (charAt > 'Z')
                    {
                        charAt = (byte)(((charAt % 90) + 65) - 1);
                    }
                }
                else if ('a' <= charAt && charAt <= 'z')
                {
                    charAt = (byte)(charAt + 13);
                    if (charAt > 'z')
                    {
                        charAt = (byte)(((charAt % 122) + 97) - 1);
                    }
                }
                bytes[i] = charAt;
            }
            code = Encoding.UTF8.GetString(bytes);
            byte[] bbb = Convert.FromBase64String(code);
            return Encoding.UTF8.GetString(bbb);
        }
    }
}
