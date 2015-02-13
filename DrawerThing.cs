using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace scrollslive.Mod
{

    public class drawdata
    {
        public List<Vector2> points;
        public int screenw;
        public int screenh;
        public string source;
        public bool show = true;


        public drawdata(string s)
        {
            
            this.source = s;

            string res = s.Split(':')[1];
            this.screenw = Convert.ToInt32(res.Split(',')[0]);
            this.screenh = Convert.ToInt32(res.Split(',')[1]);
            this.points = getPoints(s);
        }

        private static List<Vector2> getPoints(string p)
        {
            List<Vector2> retval = new List<Vector2>();
            if(p == "pnte") return retval;

            string res = p.Split(':')[1];
            float xx = Convert.ToInt32(res.Split(',')[0]);
            float yy = Convert.ToInt32(res.Split(',')[1]);
            float xfactor = (float)Screen.width;
            float yfactor = (float)Screen.height;


            string data = p.Split(':')[2];

            foreach (string s in data.Split(';'))
            {

                if (s == "" || s == null) continue;

                float x = Convert.ToInt32( s.Split(',')[0]);
                float y = Convert.ToInt32(s.Split(',')[1]);

                //calculate new points
                if (x != -1000)
                {
                    x = (x * xfactor) / xx;
                    y = (y * yfactor) / yy;
                }

                retval.Add(new Vector2(x, y));
            }


            return retval;
        }

    }

    public class DrawerThing
    {
        Rect closeownrect;
        Rect bigownrect;
        Rect smallownrect;
        Rect tempprev;


        private Dictionary<string, drawdata> otherLists = new Dictionary<string, drawdata>(); 

        private List<Vector2> linePoints = new List<Vector2>();
        public float threshold = 10.0f;
        Vector3 lastPos = Vector3.one * float.MaxValue;
        public Color currentColor = Color.green;
        private static DrawerThing instance;
        private CommunicationManager cm;

        public GUISkin cardListPopupSkin;
        public GUISkin cardListPopupGradientSkin;
        public GUISkin cardListPopupBigLabelSkin;
        public GUISkin cardListPopupLeftButtonSkin;

        public static DrawerThing Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DrawerThing();
                }
                return instance;
            }
        }

        private DrawerThing()
        {
            this.cm = CommunicationManager.Instance;
            this.setskins((GUISkin)ResourceManager.Load("_GUISkins/CardListPopup"), (GUISkin)ResourceManager.Load("_GUISkins/CardListPopupGradient"), (GUISkin)ResourceManager.Load("_GUISkins/CardListPopupBigLabel"), (GUISkin)ResourceManager.Load("_GUISkins/CardListPopupLeftButton"));

            this.setrects();
        }

        public void setskins(GUISkin cllps, GUISkin clpgs, GUISkin clpbls, GUISkin clplbs)
        {
            this.cardListPopupSkin = cllps;
            this.cardListPopupGradientSkin = clpgs;
            this.cardListPopupBigLabelSkin = clpbls;
            this.cardListPopupLeftButtonSkin = clplbs;

        }


        public string getMouseData()
        {
            string retval = "pnt:";
            retval += Screen.width + "," + Screen.height + ":";
            foreach (Vector2 v in this.linePoints)
            {
                retval += (int)v.x + "," + (int)v.y + ";" ;
            }

            if (this.linePoints.Count == 0) retval = "";
            return retval;
        }

        public void setMouseData(Dictionary<string, string> pnt)
        {
            foreach (KeyValuePair<string, string> kvp in pnt)
            {
                if (this.otherLists.ContainsKey(kvp.Key))
                {
                    if (kvp.Value == "pnte")
                    {
                        this.otherLists.Remove(kvp.Key);
                        continue;
                    }

                    if (this.otherLists[kvp.Key].source == kvp.Value) continue; //lists are equal

                    this.otherLists[kvp.Key] = new drawdata(kvp.Value);

                }
                else
                {
                    if (kvp.Value != "pnte")
                    {
                        this.otherLists.Add(kvp.Key, new drawdata(kvp.Value));
                    }
                }
            }


        }

        public void clearMouseData()
        {
           if(this.otherLists.Count>=1) this.otherLists.Clear();
        }

        public void Update(bool p)
        {
            saveMousestuff(true);
            drawMenu();

        }


        private void setrects()
        {
            float smallheight = (float)(((float)Screen.height) / 10f);
            float smallwidth = (float)(((float)Screen.width) / 10f);
            int smallstarty = Screen.height / 15;
            int smallstartx = 0;
            bigownrect = new Rect(smallstartx, smallstarty, 1.5f * smallwidth, 5 * smallheight);
            smallownrect = new Rect(smallstartx, smallstarty, 0.8f * smallwidth, 0.5f * smallheight);
            closeownrect = new Rect(smallstartx, bigownrect.yMax, smallwidth, 0.5f * smallheight);
            tempprev = new Rect(this.smallownrect);
            tempprev.width = bigownrect.width*0.9f;

            bigownrect.x = Screen.width - bigownrect.width-5;
            smallownrect.x = Screen.width - smallownrect.width - 5;
            closeownrect.x = Screen.width - closeownrect.width - 5;
            tempprev.x = Screen.width - tempprev.width - 5;

        }

        public void drawMenu()
        {
            if (this.otherLists.Count == 0) return;
            GUI.skin = cardListPopupSkin;
            GUI.Box(this.smallownrect, String.Empty);
            if (GUI.Button(this.smallownrect, "SL sketches"))
            {
                //this.ownsmall = false;
            }
            

            

            Rect temprect = new Rect(this.tempprev);
            
            foreach (KeyValuePair<string, drawdata> kvp in this.otherLists)
            {
                GUI.color = Color.white;
                if (!kvp.Value.show) GUI.color = new Color(1, 1, 1, 0.5f);
                temprect.y += temprect.height + 2;
                GUI.Box(temprect, string.Empty);
                string s = kvp.Key;
                string text = s.Substring(0, Math.Min(15, s.Length));
                if (GUI.Button(temprect, text))
                {
                    kvp.Value.show = !kvp.Value.show;
                    //this.ownsmall = false;
                }
                GUI.color = Color.white;
            }
            GUI.color = Color.white;
        }


        public void deleteOwnDrawings()
        {
            this.linePoints.Clear();
        }


        void saveMousestuff(bool clickp)
        {
            DrawOtherLine();
            UpdateLine();


            if (Input.GetButton("Fire1") && ((clickp && Input.GetKey(KeyCode.O)) || !clickp))
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.z = 0;

                float dist = Vector3.Distance(lastPos, mousePos);

                if (dist <= threshold)
                    return;

                lastPos = mousePos;
                if (linePoints == null)
                    linePoints = new List<Vector2>();
                mousePos.y = Screen.height - mousePos.y;
                linePoints.Add(mousePos);


            }
            else
            {
                if (linePoints.Count >= 1 && linePoints[linePoints.Count - 1].x != -1000)
                {
                    linePoints.Add(new Vector2(-1000, 0));
                }
            }

            if (Input.GetButton("Fire2"))
            {
                linePoints.Clear();
            }
        }

        void UpdateLine()
        {
            for (int i = 0; i < linePoints.Count - 1; i++)
            {
                if (linePoints[i].x == -1000)
                {
                    continue;
                }
                if (linePoints[i + 1].x == -1000)
                {
                    i += 1;
                    continue;
                }
                //Console.WriteLine("draw line from " + linePoints[i] + " to " + linePoints[i+1] );
                Drawing.DrawLine(linePoints[i], linePoints[i + 1], currentColor, 3, false);
            }

            
        }

        public void DrawOtherLine()
        {
           
            foreach (KeyValuePair<string, drawdata> dd in this.otherLists)
            {
                if (!dd.Value.show) continue;

                List<Vector2> listp = dd.Value.points;
                Color c = Color.grey;
                for (int i = 0; i < listp.Count - 1; i++)
                {
                    if (listp[i].x == -1000)
                    {
                        continue;
                    }

                    if (listp[i + 1].x == -1000)
                    {
                        i += 1;
                        continue;
                    }
                    //Console.WriteLine("draw line from " + linePoints[i] + " to " + linePoints[i+1] );
                    Drawing.DrawLine(listp[i], listp[i + 1], c, 3, false);
                }

            }
        }




    }
}
