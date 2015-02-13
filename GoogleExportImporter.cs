using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using JsonFx.Json;
using System.Threading;

namespace scrollslive.Mod
{
    public struct datastuff
    {
        public string uploader;
        public string data;
        public long index;

    }

    public class GoogleImporterExporter
    {
        //Database https://docs.google.com/spreadsheet/ccc?key=0AhhxijYPL-BGdDVOVFhUVzN3U3RyVTlGR1FYQ1VqUGc&usp=drive_web#gid=0
        //PostSite 
        string form = "https://docs.google.com/forms/d/1u3Wbm2HxVGYBxUM0daOxf0yGbvffZ_RBbOSxRsFt20w/";

        public bool workthreadready = true;
        public int spreadcount = 0;

        public List<datastuff> recmsgs = new List<datastuff>();
        public volatile bool isWatching = false;
        private volatile bool watching = false;

        public struct sharedItem
        {
            public string time;
            public string player;
            public string deckname;
            public string link;
            public string desc;
        }

        List<string> entrys = new List<string>();
        List<string> keys = new List<string>();
        public List<sharedItem> sharedDecks = new List<sharedItem>();

        public GoogleImporterExporter()
        {
            this.keys.Clear();
            this.keys.Add("entry." + "883932315" + "=");//id
            this.keys.Add("entry." + "1675315819" + "=");//text

        }

        public string getDataFastFromGoogleDocs()
        {
            WebRequest myWebRequest;

            string googledatakey = "176ZbSYWcQhY4ITST0xPlnxf-Lx_aQQRg9IqkRNPV480";
            //
            //https://docs.google.com/spreadsheets/d/176ZbSYWcQhY4ITST0xPlnxf-Lx_aQQRg9IqkRNPV480/export?format=tsv&id=176ZbSYWcQhY4ITST0xPlnxf-Lx_aQQRg9IqkRNPV480&gid=874661350
            //https://docs.google.com/spreadsheets/d/1WD7NMAXOJUcn3mm5-ZCQj7nynlvjVug8ZWetHWuTFuU/export?format=tsv&id=1WD7NMAXOJUcn3mm5-ZCQj7nynlvjVug8ZWetHWuTFuU&gid=0
            //new sheets:
            myWebRequest = WebRequest.Create("https://docs.google.com/spreadsheets/d/" + googledatakey + "/export?format=tsv&id=" + googledatakey + "&gid=874661350");
            Console.WriteLine("https://docs.google.com/spreadsheets/d/" + googledatakey + "/export?format=tsv&id=" + googledatakey + "&gid=874661350");

            //old sheets:
            //myWebRequest = WebRequest.Create("https://docs.google.com/spreadsheet/pub?key=" + googledatakey + "&output=txt");
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, ce, ca, p) => true;// or you get an exeption, because mono doesnt trust anyone
            myWebRequest.Timeout = 10000;

            int loaded = 0;
            string ressi = "";
            while (loaded < 10)
            {
                try
                {
                    WebResponse myWebResponse = myWebRequest.GetResponse();
                    System.IO.Stream stream = myWebResponse.GetResponseStream();
                    System.IO.StreamReader reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8);
                    ressi = reader.ReadToEnd();

                    loaded = 10;
                }
                catch
                {
                    loaded++;
                    Console.WriteLine("tof");
                }
            }

            return ressi;
        }

        public string getMicroFastFromGoogleDocs(int i)
        {
            WebRequest myWebRequest;

            string googledatakey = "176ZbSYWcQhY4ITST0xPlnxf-Lx_aQQRg9IqkRNPV480";
            //
            //https://docs.google.com/spreadsheets/d/176ZbSYWcQhY4ITST0xPlnxf-Lx_aQQRg9IqkRNPV480/gviz/tq?tqx=out:json&tq=select+*+LIMIT+1+OFFSET+"+ i + "&gid=0&pli=1";
            //new sheets:
            string url = "https://docs.google.com/spreadsheets/d/" + googledatakey + "/gviz/tq?tqx=out:csv&tq=select+*+OFFSET+" + i + "&gid=0&pli=1";
            myWebRequest = WebRequest.Create(url);
            //Console.WriteLine(url);

            //old sheets:
            //myWebRequest = WebRequest.Create("https://docs.google.com/spreadsheet/pub?key=" + googledatakey + "&output=txt");
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, ce, ca, p) => true;// or you get an exeption, because mono doesnt trust anyone
            myWebRequest.Timeout = 10000;

            int loaded = 0;
            string ressi = "";
            while (loaded < 10)
            {
                try
                {
                    WebResponse myWebResponse = myWebRequest.GetResponse();
                    System.IO.Stream stream = myWebResponse.GetResponseStream();
                    System.IO.StreamReader reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8);
                    ressi = reader.ReadToEnd();

                    loaded = 10;
                }
                catch
                {
                    loaded++;
                    Console.WriteLine("tof");
                }
            }

            return ressi;
        }


        public string getDataFromGoogleDocs()
        {
            WebRequest myWebRequest;
            myWebRequest = WebRequest.Create("https://spreadsheets.google.com/feeds/list/" + "0AhhxijYPL-BGdDVOVFhUVzN3U3RyVTlGR1FYQ1VqUGc" + "/od6/public/values?alt=json");
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, ce, ca, p) => true;// or you get an exeption, because mono doesnt trust anyone
            myWebRequest.Timeout = 10000;
            WebResponse myWebResponse = myWebRequest.GetResponse();
            System.IO.Stream stream = myWebResponse.GetResponseStream();
            System.IO.StreamReader reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8);
            string ressi = reader.ReadToEnd();
            return ressi;
        }


        public void postDataToGoogleForm(List<string> entrys)
        {
            string txt = "";
            int i = 0;
            foreach (string t in entrys)
            {
                txt = txt + this.keys[i] + System.Uri.EscapeDataString(t) + "&";
                i++;
            }
            string txt1 = txt;
            Console.WriteLine("##sendTogoogle: " + txt1);
            byte[] bytes = Encoding.ASCII.GetBytes(txt1 + "draftResponse=%5B%5D%0D%0A&pageHistory=0");

            HttpWebRequest webRequest = HttpWebRequest.Create(form + "formResponse") as HttpWebRequest;
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, ce, ca, p) => true;// or you get an exeption, because mono doesnt trust anyone
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Method = "POST";
            webRequest.Referer = form + "viewform";
            webRequest.ContentLength = bytes.Length;
            Stream requestStream = webRequest.GetRequestStream();
            requestStream.Write(bytes, 0, bytes.Length);
            requestStream.Close();
            HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
            Stream receiveStream = response.GetResponseStream();
            StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
            String data = readStream.ReadToEnd();
            Console.WriteLine(data);
            receiveStream.Close();
            readStream.Close();
            response.Close();
        }

        public void postDataToGoogleFormThreaded(Object o)
        {
            List<string> entrys = (List<string>)o;
            string txt = "";
            int i = 0;
            foreach (string t in entrys)
            {
                txt = txt + this.keys[i] + System.Uri.EscapeDataString(t) + "&";
                i++;
            }
            string txt1 = txt;
            //Console.WriteLine("##sendTogoogle: " + txt1);
            byte[] bytes = Encoding.ASCII.GetBytes(txt1 + "draftResponse=%5B%5D%0D%0A&pageHistory=0");

            HttpWebRequest webRequest = HttpWebRequest.Create(form + "formResponse") as HttpWebRequest;
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, ce, ca, p) => true;// or you get an exeption, because mono doesnt trust anyone
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Method = "POST";
            webRequest.Referer = form + "viewform";
            webRequest.ContentLength = bytes.Length;
            Stream requestStream = webRequest.GetRequestStream();
            requestStream.Write(bytes, 0, bytes.Length);
            requestStream.Close();
            HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
            Stream receiveStream = response.GetResponseStream();
            StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
            String data = readStream.ReadToEnd();
            //Console.WriteLine(data);
            receiveStream.Close();
            readStream.Close();
            response.Close();
        }



        //todo make it more robust or switch to json
        public void readJsonfromGoogleFast(string txt)
        {
            //Console.WriteLine(txt);

            String[] lines = txt.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            this.spreadcount = lines.Length-1;

            List<datastuff> lds = new List<datastuff>();

            for (int i = 1; i < lines.Length; i++)//there is no relevant data for i=0!
            {

                string[] data = lines[i].Split(new string[] { "\t" }, StringSplitOptions.None);
                string id = "";
                string msg = "";
                if (data.Length >= 1)
                {
                  //timestamp
                    string stamp = data[0];
                }
                if (data.Length >= 2)
                {
                    //id
                    id = data[1];
                }
                if (data.Length >= 3)
                {
                    msg = data[2];
                }

                datastuff di;
                di.uploader = id.Split(' ')[0];
                di.index = Convert.ToInt64(id.Split(' ')[1]);
                di.data = msg;
                Console.WriteLine("#going to add: " + di.uploader + " " + di.index + " " + di.data); 
                lds.Add(di);
                
            }

            if (lds.Count >= 1)
            {
                lds.Sort(delegate(datastuff p1, datastuff p2) { return p1.index.CompareTo(p2.index); });
                this.recmsgs.Clear();
                this.recmsgs.AddRange(lds);
            }

        }

        public void readJsonFromGoogleMicro(string txt)
        {
            txt = txt.Replace("\r\n", "");
            String[] lines = txt.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            List<datastuff> lds = new List<datastuff>();

            if (lines.Length >= 2) this.spreadcount += lines.Length - 1;

            for (int i = 1; i < lines.Length; i++)//there is no relevant data for i=0!
            {

                string[] data = lines[i].Split(new string[] { "," }, StringSplitOptions.None);
                string id = "";
                string msg = "";
                string stamp = "";
                //Console.WriteLine("###" + lines[i]); 
                if (data.Length >= 1)
                {
                    //timestamp
                    stamp = data[0].Substring(1,data[0].Length-2);
                }
                //Console.WriteLine("#" + stamp); 
                if (data.Length >= 2)
                {
                    //id
                    id = data[1].Substring(1, data[1].Length - 2);
                }
                //Console.WriteLine("#" + id); 
                if (data.Length >= 3)
                {
                    string begin = "\"" + stamp + "\",\"" + id + "\",\"";
                    string d = lines[i].Substring(begin.Length);
                    msg = d.Substring(0,d.Length-1) ;
                    msg = msg.Replace("\"\"", "\"");
                }
                //Console.WriteLine("#" + msg); 
                datastuff di;
                di.uploader = id.Split(' ')[0];
                di.index = Convert.ToInt64(id.Split(' ')[1]);
                di.data = msg;
                Console.WriteLine("#going to add: " + di.uploader + " " + di.index + " " + di.data); 
                lds.Add(di);

            }

            if (lds.Count >= 1)
            {
                lds.Sort(delegate(datastuff p1, datastuff p2) { return p1.index.CompareTo(p2.index); });
                this.recmsgs.AddRange(lds);
            }
        }

        public void readJsonfromGoogle(string txt)
        {
            Console.WriteLine(txt);
            JsonReader jsonReader = new JsonReader();
            Dictionary<string, object> dictionary = (Dictionary<string, object>)jsonReader.Read(txt);
            dictionary = (Dictionary<string, object>)dictionary["feed"];
            Dictionary<string, object>[] entrys = (Dictionary<string, object>[])dictionary["entry"];
            sharedDecks.Clear();
            for (int i = 0; i < entrys.GetLength(0); i++)
            {
                sharedItem si = new sharedItem();
                dictionary = (Dictionary<string, object>)entrys[i]["gsx$timestamp"];
                si.time = (string)dictionary["$t"];
                dictionary = (Dictionary<string, object>)entrys[i]["gsx$playername"];
                si.player = (string)dictionary["$t"];
                dictionary = (Dictionary<string, object>)entrys[i]["gsx$link"];
                si.link = (string)dictionary["$t"];
                dictionary = (Dictionary<string, object>)entrys[i]["gsx$deckname"];
                si.deckname = (string)dictionary["$t"];
                dictionary = (Dictionary<string, object>)entrys[i]["gsx$description"];
                si.desc = (string)dictionary["$t"];
                if (si.link.StartsWith("DELETE")) continue;
                this.sharedDecks.Add(si);
                Console.WriteLine(si.player + " " + si.deckname);
            }


        }


        public void sendDataThreaded(List<string> data)
        {
           Thread threadl = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(this.postDataToGoogleFormThreaded));
           threadl.Start((object)data);
        }

        public void workthread()
        {
            this.workthreadready = false;
            //this.readJsonfromGoogle(this.getDataFromGoogleDocs());

            this.readJsonfromGoogleFast(this.getDataFastFromGoogleDocs());

            this.workthreadready = true;
        }

        public void watcherthread()
        {

            while (this.watching)
            {
                System.Threading.Thread.Sleep(100);
            }
            Console.WriteLine("#### start scrolls live watching...");
            this.isWatching = true;
            this.watching = true;
            this.readJsonfromGoogleFast(this.getDataFastFromGoogleDocs());
            System.Threading.Thread.Sleep(500);

            while (this.isWatching)
            {
                this.readJsonFromGoogleMicro(this.getMicroFastFromGoogleDocs(this.spreadcount));
                System.Threading.Thread.Sleep(500);
            }
            this.recmsgs.Clear();
            this.watching = false;
            Console.WriteLine("#### stopped scrolls live watching...");
        }


    }

}
