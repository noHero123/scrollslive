using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Threading;


namespace scrollslive.Mod
{

   public class CommunicationManager : ICommListener, IOkCancelCallback
    {

       public DateTime lastone = DateTime.Now;

       public int testing = 0;
       public GoogleImporterExporter google;
        public List<string> viewers = new List<string>();
        public List<string> senders = new List<string>();

        public Dictionary<string, DateTime> pendingviewers = new Dictionary<string, DateTime>();
        public Dictionary<string, DateTime> pendingsenders = new Dictionary<string, DateTime>();

        private List<string> messageparts = new List<string>();

        private Recorder recer = null;
        private Player plyr = null;

        long bttlnumber = 0;
        long messagecounter = 0;
        long paintcounter = 0;

        long lastplayermssg = 0;

        long invtime = 0;

        long vmsrBattlenr = 0;

        public int sendings = 0;


        private static CommunicationManager instance;

        public static CommunicationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CommunicationManager();
                }
                return instance;
            }
        }

        private CommunicationManager()
        {
            try
            {
                App.Communicator.addListener(this);
            }
            catch
            {
 
            }
            this.bttlnumber = getUnix();
        }

        public void setPlayer(Player p)
        {
            this.plyr =p;
        }

        public void setgoogle(GoogleImporterExporter g)
        {
            this.google = g;
        }

        //vmwr : someone wants to watch your game
        //vmsr : someone wants you to watch his game

        //vmgm :game message with this content:
        //vmgm xxyy zzzzzzz part of Message       x = actual part, y = maximum parts zzzzzzz = messagenumber

        //vmpt xy part of message (paint message)

        public void handleMessage(Message msg)
        {

            if (this.sendings >= 1 )
            {
                if (msg is FailMessage && (msg as FailMessage).info.StartsWith("Could not find the user"))
                {
                    this.notification("the user is offline or doesnt exist");
                    this.sendings--;
                    return;
                }

                if (msg is CliResponseMessage && (msg as CliResponseMessage).text.StartsWith("That user is not online."))
                {
                    this.notification("the user is not online");
                    this.sendings--;
                    return;
                }
                
            }

            if (msg is WhisperMessage)
            {
                WhisperMessage wm = msg as WhisperMessage;

                Console.WriteLine("##whisper from " + wm.from + ": " + wm.text);

                if (testing == 0 && wm.from == App.MyProfile.ProfileInfo.name) // ignore own messages!
                {

                    if (wm.text.StartsWith("vmsr") || wm.text.StartsWith("vmwr")) 
                    { 
                        this.sendings--; 
                    }
                    return;
                }

                

                //requesting stuff and decline--------------------------------------
                if (wm.text.StartsWith("vmwr"))
                {
                    //if (this.viewers.Contains(wm.from)) return;

                    //ask if you wants that

                    //App.Popups.ShowMultibutton(this, "vmwr: " + wm.from, wm.from + " want to watch your game", new GUIContent[] { new GUIContent("Allow"), new GUIContent("Cancel") });
                    App.Popups.ShowOkCancel(this, "vmwr: " + wm.from, "Spectating Request", wm.from + "\r\nwants to watch your game", "Allow", "Cancel");
                }


               
                /*
                if (wm.text.StartsWith("vmsr") && this.senders.Count >= 1)
                {
                    if (this.senders.Contains(wm.from)) return;
                    sendMessage("", wm.from, "vmsc");//cant watch 2 games
                }*/

                if (wm.text.StartsWith("vmwo") && this.pendingsenders.ContainsKey(wm.from) && ((DateTime.Now - this.pendingsenders[wm.from]).TotalMinutes<=1))
                {


                    this.pendingsenders.Clear();

                    this.invtime = getUnix();
                    if(!this.senders.Contains(wm.from))this.senders.Add(wm.from);

                    this.notification(wm.from + " allows you to watch his game, please wait...");
                    string text = wm.text;
                    if (text.StartsWith("vmwo bn:"))
                    {
                        this.invtime = Convert.ToInt64(text.Split(':')[1]);
                    }
                    Console.WriteLine("inv time is now: " + this.invtime);
                    if(this.google.isWatching==false ) new Thread(new ThreadStart(this.google.watcherthread)).Start();

                }

                if (wm.text.StartsWith("vmwc") && this.pendingsenders.ContainsKey(wm.from) && ((DateTime.Now - this.pendingsenders[wm.from]).TotalMinutes <= 1))
                {
                    this.pendingsenders.Clear();
                    this.notification(wm.from + " didnt allow you to watch his game");
                }




                if (wm.text.StartsWith("vmsr"))//&& this.senders.Count == 0)
                {
                    //if (this.senders.Contains(wm.from)) return;

                    //ask if you wants that
                    string text = wm.text;
                    this.vmsrBattlenr = 0;
                    if (text.StartsWith("vmsr bn:"))
                    {
                        this.vmsrBattlenr = Convert.ToInt64(text.Split(':')[1]);
                    }
                    //App.Popups.ShowMultibutton(this, "vmsr: " + wm.from, wm.from + " invites you to his game", new GUIContent[] { new GUIContent("Allow"), new GUIContent("Cancel") });

                    App.Popups.ShowOkCancel(this, "vmsr: " + wm.from, "Spectate Invite", wm.from + "\r\ninvites you to his game", "Allow", "Cancel");
                }

                if (wm.text.StartsWith("vmso") && this.pendingviewers.ContainsKey(wm.from) && ((DateTime.Now - this.pendingviewers[wm.from]).TotalMinutes <= 1))
                {


                    this.pendingsenders.Remove(wm.from);

                    if (!this.viewers.Contains(wm.from)) this.viewers.Add(wm.from);

                    this.notification(wm.from + " will watch your game");


                }

                if (wm.text.StartsWith("vmsc") && this.pendingviewers.ContainsKey(wm.from) && ((DateTime.Now - this.pendingviewers[wm.from]).TotalMinutes <= 1))
                {
                    this.pendingsenders.Remove(wm.from);
                    this.notification(wm.from + " dont want to watch your game");
                }
            
            
            //------------------------------------------------------------------------------------------

                //game message
                if (wm.text.StartsWith("vmgm") && this.senders.Contains(wm.from))
                {
                    this.receiveMessage(wm.text);//send them to the PLAYER
                }
            
            
            
            
            }
        }

        public void onConnect(OnConnectData ocd)
        {
            return;
        }



        public bool blockWhisper(WhisperMessage wm)
        {

            if (wm.from == App.MyProfile.ProfileInfo.name && (wm.text.StartsWith("vmwr") || wm.text.StartsWith("vmwc") || wm.text.StartsWith("vmsr") || wm.text.StartsWith("vmsc") || wm.text.StartsWith("vmso") || wm.text.StartsWith("vmwo")))
            {
                return true;
            }

            if (wm.text.StartsWith("vmwr") || wm.text.StartsWith("vmsr")) return true;

            if ((wm.text.StartsWith("vmwo") || wm.text.StartsWith("vmwc"))) return true;

            if ((wm.text.StartsWith("vmso") || wm.text.StartsWith("vmsc"))) return true;

            //if (!this.viewers.Contains(wm.from) && !this.senders.Contains(wm.from)) return false;

            return false;
        }


        public void addWatcher(string nameofwatcher)
        {
            //add watcher to list and send him (if recording) the messages!

            if(!this.viewers.Contains(nameofwatcher))this.viewers.Add(nameofwatcher);




        }

        public void broadcastBattleMessageToWatchers(string text)
        {
            if (this.viewers.Count == 0) return;

            List<string> e = new List<string>();
            this.messagecounter++;

            if (text.EndsWith("\"msg\":\"ServerInfo\"}"))
            {
                this.bttlnumber = getUnix();
                this.messagecounter = 0;
                this.paintcounter = 0;
            }
            long ind = this.bttlnumber + this.messagecounter;
            e.Add(App.MyProfile.ProfileInfo.name + " " + ind);
            string t = text.Replace("\r\n", "");
            t = t.Replace("\n", "");
            t = t.Replace("\t", "");
            e.Add(t);

            

            this.google.sendDataThreaded(e);

            if (text.StartsWith("{\"effects\":[{\"EndGame"))
            {
                this.senders.Clear();
                this.viewers.Clear();
                this.bttlnumber = 0;
            }

            //this.google.postDataToGoogleForm(e);
            /*foreach (string target in this.viewers)
            {
                sendMessage(text, target, "vmgm");
            }*/
        }

        public void sendPaintMessage()
        {
            if (this.viewers.Count == 0 && this.senders.Count == 0) return;
            List<string> e = new List<string>();

            


            long ind = this.bttlnumber;// +this.paintcounter;
            e.Add(App.MyProfile.ProfileInfo.name + " " + ind);
            string s = DrawerThing.Instance.getMouseData();
            e.Add(s);
            if (s == "") return;//dont send no data!

            this.paintcounter = 1;
            this.google.sendDataThreaded(e);
            DrawerThing.Instance.deleteOwnDrawings();

        }

        public void sendPaintStopMessage()
        {
            if (this.paintcounter == 0) return;
            if (this.viewers.Count == 0 && this.senders.Count == 0) return;
            List<string> e = new List<string>();
            this.paintcounter=0;
            long ind = this.bttlnumber;// +this.paintcounter;
            e.Add(App.MyProfile.ProfileInfo.name + " " + ind);
            string s = "pnte";
            e.Add(s);
            this.google.sendDataThreaded(e);

        }


        public void sendMessage(string text, string target, string type)
        {
            if (text == "") text = " ";
            string t = text;

            if (t == " " && this.recer != null && this.recer.recording && (type == "vmsr" || type == "vmwo"))
            {
                t = "bn:"+this.bttlnumber;
            }
            WhisperMessage wm = new WhisperMessage();
            wm.toProfileName = target;

            wm.text = type + " " + t;

            Console.WriteLine("#--# send " + wm.text + " to " + wm.toProfileName);

            App.Communicator.sendRequest(wm);
        }


        public void receiveMessage(string text)
        {
            string parts = text.Split(' ')[1];
            int x = Convert.ToInt32( parts.Substring(0, 2));
            int y = Convert.ToInt32( parts.Substring(2, 2));

            if (x == y)
            {
                if (x == 1)
                {
                    this.sendMessageToPlayer(text.Substring(18));
                    return;
                }

                string messgnr = text.Split(' ')[2];

                List<string> thismsg = new List<string>();
                foreach (string s in this.messageparts)
                {
                    if (s.Split(' ')[2] == messgnr)
                    {
                        thismsg.Add(s);
                    }
                }

                this.messageparts.RemoveAll(a => a.Split(' ')[2] == messgnr);//remove that messages from list.

                thismsg.Add(text);
                string startm = text.Split(' ')[0]+" ";
                string maxy = y.ToString("D2");

                //add the message parts in correct order

                string completemessage = "";
                for (int i = 0; i < x; i++)
                {
                    string m = thismsg.Find(a=> a.StartsWith(startm + (i+1).ToString("D2") + maxy));
                    completemessage += m.Substring(18);
                }

                this.sendMessageToPlayer(completemessage);
                return;

            }
            else
            {
                this.messageparts.Add(text);
            }

        }


        public void sendMessageToPlayer(string m)
        {
            Console.WriteLine("send to player: " + m);
            if (m.EndsWith("\"msg\":\"ServerInfo\"}"))
            {
                //start of the game!
                this.plyr.LaunchReplayer(m);
                return;
            }


            this.plyr.computeMessage(m);
        }

        public void PopupOk(string popupType)
        {

            if (popupType.StartsWith("vmwr: ") )
            {
                string name = (popupType.Split(new string[] { ": " }, StringSplitOptions.None))[1];
                this.senders.Clear();//you will stream-> you have no streamers
                if (this.recer != null && this.recer.recording)
                {
                    sendMessage("" + this.bttlnumber, name, "vmwo");
                }
                else
                {
                    sendMessage("", name, "vmwo");
                }
                this.addWatcher(name);
                notification("added " + name + " to your viewers");

            }

            if (popupType.StartsWith("vmsr: "))
            {
                string name = (popupType.Split(new string[] { ": " }, StringSplitOptions.None))[1];
                this.viewers.Clear();//you will watch someones stream-> you have no watchers
                this.senders.Clear();
                this.invtime = getUnix();
                if (this.vmsrBattlenr != 0)
                {
                    this.invtime = this.vmsrBattlenr;
                    this.vmsrBattlenr = 0;
                }
                this.senders.Add(name);

                if (testing == 0) sendMessage("", name, "vmso");

                notification("added " + name + " to your streamers, please wait you will watch his current/next game");
                Console.WriteLine("inv time is now: " + this.invtime);

                if (testing == 1) this.invtime = Convert.ToInt64("1423231611222");
                if (testing == 2) this.invtime = Convert.ToInt64("1423517579763");

                if (this.google.isWatching == false) new Thread(new ThreadStart(this.google.watcherthread)).Start();

            }

        }
        

       public void PopupCancel(string popupType)
        {
            if (popupType.StartsWith("vmwr: "))
            {
                string name = (popupType.Split(new string[] { ": " }, StringSplitOptions.None))[1];
                sendMessage("", name, "vmwc");
            }

            if (popupType.StartsWith("vmsr: "))
            {
                string name = (popupType.Split(new string[] { ": " }, StringSplitOptions.None))[1];
                sendMessage("", name, "vmsc");
            }

            return;
        }


        public void notification(string text)
        { 
            WhisperMessage wm = new WhisperMessage();
            wm.from ="scrollslive";
            wm.text = text;
            //App.ChatUI.handleMessage(wm);
            //App.ArenaChat.handleMessage(wm);
            App.Popups.ShowOk(this, "info", "ScrollsLive", text, "OK");
        }


        public long getUnix()
        {
            return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
        }


        public void stopwatching()
        {
            this.senders.Clear();
            this.viewers.Clear();
            this.google.isWatching = false;
            this.google.recmsgs.Clear();

            this.lastplayermssg = 0;
            this.testing = 0;

            bttlnumber = 0;
            messagecounter = 0;
            this.paintcounter = 0;
            lastplayermssg = 0;
            invtime = 0;
            vmsrBattlenr = 0;

            this.bttlnumber = getUnix();
 
        }


        public void updateDrawings()
        {
            List<datastuff> mylist = new List<datastuff>();
            Dictionary<string, string> dings = new Dictionary<string, string>();
            string myname = App.MyProfile.ProfileInfo.name;
            mylist.AddRange(this.google.recmsgs.FindAll(a => a.index == this.bttlnumber && a.data[0] == 'p'));
            /*
            if (this.viewers.Count >= 1)
            {

                mylist.AddRange(this.google.recmsgs.FindAll(a => a.index == this.bttlnumber));
                mylist.AddRange(this.google.recmsgs.FindAll(a => this.viewers.Contains(a.uploader)));
                mylist.AddRange(this.google.recmsgs.FindAll(a => myname == a.uploader && a.data[0]=='p'));
            }

            if (this.senders.Count >= 1)
            {
                mylist.AddRange(this.google.recmsgs.FindAll(a => a.index == this.bttlnumber));
                mylist.AddRange(this.google.recmsgs.FindAll(a => this.senders.Contains(a.uploader)));
                mylist.AddRange(this.google.recmsgs.FindAll(a => myname == a.uploader && a.data[0] == 'p'));
            }*/

            mylist.Reverse();


            bool draw = false;
            foreach (datastuff ds in mylist)
            {
                //Console.WriteLine("paintdata: " + ds.uploader + " " + ds.index + " " + ds.data);
                if (this.bttlnumber != ds.index) continue;
                if (!ds.data.StartsWith("pnt")) continue;
                if (dings.ContainsKey(ds.uploader)) continue;

                dings.Add(ds.uploader, ds.data);
                if(ds.data.StartsWith("pnt:"))
                {
                    draw=true;
                }
            }

            if(draw)
            {
                DrawerThing.Instance.setMouseData(dings);
            }
            else
            {
                DrawerThing.Instance.clearMouseData();
            }
            
        }

        public void updatePlayer()
        {
            bool searchServerInfo = false;
            if (this.lastplayermssg == 0)
            {
                searchServerInfo = true;
            }

            List<datastuff> mylist = new List<datastuff>(this.google.recmsgs.FindAll(a=> a.uploader == this.senders[0]));
            string msg = "";
            Console.WriteLine("# update: wait for message " + this.lastplayermssg);
            foreach (datastuff ds in mylist)
            {
                if (this.invtime > ds.index) continue;

                //Console.WriteLine("searching..." + ds.index);
                if (searchServerInfo && ds.data.EndsWith("\"msg\":\"ServerInfo\"}"))
                {
                    msg = ds.data;
                    this.lastplayermssg = ds.index+1;
                    this.bttlnumber = ds.index;
                    break;
                }
                /*
                if (!searchServerInfo && ds.index == this.lastplayermssg)
                {
                    if (!searchServerInfo && ds.data.EndsWith("\"msg\":\"GameInfo\"}"))
                    {
                        msg = ds.data;
                        this.lastplayermssg += 1;

                        //search for last Gamestatemessage:
                        long lastgamestate = this.lastplayermssg;
                        Console.WriteLine("#search for turnbegin");
                        foreach (datastuff dds in mylist)
                        {
                            if (dds.index < this.lastplayermssg) continue;
                            Console.WriteLine("#poss mess: " + dds.data.Substring(0, Math.Min(dds.data.Length-2, 20)));
                            if (dds.data.StartsWith("{\"effects\":[{\"EndGame\"")) break;
                            if (dds.data.StartsWith("{\"effects\":[{\"TurnBegin\"")) lastgamestate = dds.index;
                        }
                        if (this.lastplayermssg <= lastgamestate)
                        {
                            this.lastplayermssg = lastgamestate;
                            Console.WriteLine("#skipping to " + this.lastplayermssg);
                        }


                        break;
                    }
                }
                */
                if (!searchServerInfo && ds.index == this.lastplayermssg)
                {
                    msg = ds.data;
                    this.lastplayermssg++;
                    break;
                }
            }


            if(msg != "") this.sendMessageToPlayer(msg);


        }
    }
}
