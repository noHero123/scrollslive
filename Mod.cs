using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using Mono.Cecil;
using UnityEngine;
using ScrollsModLoader.Interfaces;
using System.ComponentModel;
using System.Net;
using System.Text.RegularExpressions;
using JsonFx.Json;

namespace scrollslive.Mod
{
	public class Mod : BaseMod, ICommListener
	{
		private Recorder recorder = null;
		private Player player;
        private Settings sttngs;
		private String recordFolder;
        private bool spectating = false;
        private BattleMode bm = null;
        private BattleModeUI bmui = null;

        private bool loadedguiSkin = false;
        private GUISkin guiSkin;

        DateTime lastpainting = DateTime.Now;

        GoogleImporterExporter gie;

        private FieldInfo currentEffectField=typeof(BattleMode).GetField("currentEffect", BindingFlags.NonPublic | BindingFlags.Instance);

        private FieldInfo replaynexts = typeof(BattleMode).GetField("replayNexts", BindingFlags.NonPublic | BindingFlags.Instance);


        private FieldInfo frameRectField = typeof(ProfileMenu).GetField("frameRect", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo showEditField = typeof(ProfileMenu).GetField("showEdit", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo showAchiveField = typeof(ProfileMenu).GetField("_showAchievementFrame", BindingFlags.Instance | BindingFlags.NonPublic);

		//private MethodInfo getButtonRectMethod= typeof(ProfileMenu).GetMethod("getButtonRect", BindingFlags.NonPublic | BindingFlags.Instance);
        //private FieldInfo guiSkinField=typeof(ProfileMenu).GetField("guiSkin", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo usernameStyleField=typeof(ProfileMenu).GetField("usernameStyle", BindingFlags.Instance | BindingFlags.NonPublic);

        private CommunicationManager cm = CommunicationManager.Instance;

        private bool noti = false;

        public Mod()
		{
            this.loadGuiSkin();
			//recordFolder = this.OwnFolder() + Path.DirectorySeparatorChar + "Records";
            string homePath = (Environment.OSVersion.Platform == PlatformID.Unix ||Environment.OSVersion.Platform == PlatformID.MacOSX)? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            recordFolder = homePath + Path.DirectorySeparatorChar + "ScrollsRecords";

            if (!Directory.Exists(recordFolder + Path.DirectorySeparatorChar))
			{
				Directory.CreateDirectory(recordFolder + Path.DirectorySeparatorChar);
			}
            gie = new GoogleImporterExporter();
			player = new Player(cm);
            sttngs = new Settings(recordFolder);

            this.cm.setPlayer(player);
            this.cm.setgoogle(gie);

			try {
				App.Communicator.addListener(this);
			} catch {}

            Console.WriteLine("loaded liveviewer");

            return;


            //testing units:
            //cm.sendMessage("abcdefghijklmnopqrstuvwxyz", "mrx", "vmgm");

            /*cm.receiveMessage("vmgm 0106 1234567 hallo ");
            cm.receiveMessage("vmgm 0206 1234567 mein");
            cm.receiveMessage("vmgm 0406 1234567  ich ");
            cm.receiveMessage("vmgm 0306 1234567  schatz");
            cm.receiveMessage("vmgm 0506 1234567 liebe");
            cm.receiveMessage("vmgm 0606 1234567  dich!");*/


           

            List<string> ls = new List<string>();
            ls.Add("1234 12345"); ls.Add("msg12345231344");
            gie.postDataToGoogleForm(ls);
            DateTime t1 = DateTime.Now;
            string data =gie.getDataFastFromGoogleDocs();
            Console.WriteLine("data from google: "+data + "\r\n"+(DateTime.Now-t1).TotalMilliseconds);
            gie.readJsonfromGoogleFast(data);

            int oldc = gie.spreadcount;
            gie.spreadcount = 16;
            DateTime t2 = DateTime.Now;
            data = gie.getMicroFastFromGoogleDocs(gie.spreadcount);
            Console.WriteLine("data from google: " + data + "\r\n" + (DateTime.Now - t2).TotalMilliseconds);
            gie.readJsonFromGoogleMicro(data);
            Console.WriteLine("old count " + oldc + " vs new " + gie.spreadcount);

		}

        private void loadGuiSkin()
        {
            this.guiSkin = (GUISkin)ResourceManager.Load("_GUISkins/Lobby");
        }

		public static string GetName()
		{
			return "ScrollsLive";
		}

		public static int GetVersion()
		{
			return 161;
		}

		public void handleMessage(Message msg)
		{
            if (msg is RoomChatMessageMessage)
            {
                RoomChatMessageMessage rcmm = (RoomChatMessageMessage)msg;
                if (rcmm.text.StartsWith("You have joined") && noti==false)
                {
                    WhisperMessage nwm = new WhisperMessage();
                    nwm.from = "Summoner";
                    nwm.text = "<color=#a59585>ScrollsLive:</color> " + "share your battles live with friends!";
                    nwm.text += "\r\nto add a friend to your next match write: /sl inv NameofYourFriend";
                    nwm.text += "\r\nto request a friend to watch his next match write: /sl inv NameofYourFriend";
                    nwm.text += "\r\n(both users have to install this mod)";
                    nwm.text += "\r\npress \"o\" and hold leftclick to paint something during the match with your mouse";
                    nwm.text +="\r\njust rightclick somewhere to delete that green drawing";
                    nwm.text += "\r\npress \"INSTERT\" to upload your drawings, so that the other participants of the streamed battle can see your drawing";
                    nwm.text += "\r\npress \"DELETE\" to remove your uploaded painting from the other participants";
                    nwm.text += "\r\nnote: if you draw something with the battle-paintmod, you cant upload it (except you pressed \"o\" while drawing)";


                    App.ArenaChat.handleMessage(nwm);
                    noti = true;

                }
            }

			try {
				if (msg is BattleRedirectMessage)
				{
                    this.spectating = false;
					recorder = new Recorder(recordFolder, this, this.sttngs, this.cm);
					Console.WriteLine("Recorder started: " + recorder);
				}
			} catch {}

            try
            {
                // replay streaming....
                if (msg is GameInfoMessage && (this.recorder == null || (this.recorder!=null && this.recorder.recording == false)) && !this.player.playing && this.cm.viewers.Count>=1)
                {
                    this.spectating = false;
                    recorder = new Recorder(recordFolder, this, this.sttngs, this.cm);
                    Console.WriteLine("Recorder started (replay): " + recorder);
                    this.recorder.isReplay = true;
                    this.recorder.addMessage("{\"version\":\"1.1.0\",\"assetURL\":\"http://download.scrolls.com/assets/\",\"roles\":\"GAME,RESOURCE\",\"msg\":\"ServerInfo\"}");
                    this.recorder.addMessage(msg.getRawText());
                }
            }
            catch { }

            //only own battles!
            /*
            //try
            //{
                if (msg is SpectateRedirectMessage)
                {
                    this.spectating = true;
                    recorder = new Recorder(recordFolder, this, this.sttngs);
                    Console.WriteLine("Recorder started: " + recorder + " (Spectator)");

                    
                }
            //}
            //catch { }
            */
		}

		public void onConnect(OnConnectData ocd)
		{
			return;
		}

		public String getRecordFolder()
		{
			return recordFolder;
		}

		public Player getPlayer()
		{
			return player;
		}
		
		public static MethodDefinition[] GetHooks(TypeDefinitionCollection scrollsTypes, int version)
		{
			try {
				MethodDefinition[] defs = new MethodDefinition[] {
                    scrollsTypes["BattleMode"].Methods.GetMethod("Start")[0],
                    scrollsTypes["BattleMode"].Methods.GetMethod("effectDone")[0],

                    scrollsTypes["BattleMode"].Methods.GetMethod("handleHandUpdate", new Type[]{typeof(EMHandUpdate)}),

                    scrollsTypes["BattleModeUI"].Methods.GetMethod("Start")[0],
                    scrollsTypes["SettingsMenu"].Methods.GetMethod("OnGUI")[0],

                    scrollsTypes["Communicator"].Methods.GetMethod("joinLobby", new Type[]{typeof(bool)}),

                    scrollsTypes["Communicator"].Methods.GetMethod("send", new Type[]{typeof(Message)}),

                     scrollsTypes["ArenaChat"].Methods.GetMethod("handleMessage", new Type[]{typeof(Message)}),

                     scrollsTypes["BattleMode"].Methods.GetMethod("OnGUI")[0],
                     scrollsTypes["ChatUI"].Methods.GetMethod("OnGUI")[0],
                    
				};

				List<MethodDefinition> list = new List<MethodDefinition>(defs);
				list.AddRange(Player.GetPlayerHooks(scrollsTypes, version));

				return list.ToArray();
			} catch {
				return new MethodDefinition[] {};
			}
		}

		public override bool WantsToReplace (InvocationInfo info)
		{


            if (info.target is ArenaChat && info.targetMethod.Equals("handleMessage")) // return true to not display the whispermessage
            {
                Message msg = (Message)info.arguments[0];
                if (msg is WhisperMessage)
                {
                    WhisperMessage wmsg = (WhisperMessage)msg;
                    //dont want to see whisper messages from/to the auctionbot
                    bool res = cm.blockWhisper(wmsg);
                    //Console.WriteLine("handlemessage block? " + res);
                    return res;
                }
            }
            if (info.target is Communicator && (info.targetMethod.Equals("sendRequest") || info.targetMethod.Equals("send")) && ((info.arguments[0] is RoomChatMessageMessage && (info.arguments[0] as RoomChatMessageMessage).text.StartsWith("/sl ")) || (info.arguments[0] is WhisperMessage && (info.arguments[0] as WhisperMessage).text.StartsWith("/sl "))))
            {
                Console.WriteLine("slsendrequest");
                return true;
            }

            if (recorder != null && recorder.recording && recorder.isReplay)
            {
                if (info.target is GUIBattleModeMenu && info.targetMethod.Equals("toggleMenu"))
                {
                    this.recorder.stoprecording();
                }
            }


			return player.WantsToReplace(info);
		}


        MethodInfo dispatchMessages = typeof(MiniCommunicator).GetMethod("_dispatchMessageToListeners", BindingFlags.NonPublic | BindingFlags.Instance);

		public override void ReplaceMethod (InvocationInfo info, out object returnValue) {


            if (info.target is Communicator && info.targetMethod.Equals("send") && ((info.arguments[0] is RoomChatMessageMessage && (info.arguments[0] as RoomChatMessageMessage).text.StartsWith("/sl ")) || (info.arguments[0] is WhisperMessage && (info.arguments[0] as WhisperMessage).text.StartsWith("/sl "))))
            {
                returnValue = true;
                this.cm.testing = 0;
                /*RoomChatMessageMessage rcmm = info.arguments[0] as RoomChatMessageMessage;
                rcmm.from = "ScrollsLive";*/
                string msgtest = (info.arguments[0] is RoomChatMessageMessage) ? ((RoomChatMessageMessage)info.arguments[0]).text : ((WhisperMessage)info.arguments[0]).text;

                if (msgtest.StartsWith("/sl inv "))
                {
                    string player = msgtest.Split(' ')[2];
                    cm.sendings++;
                    cm.sendMessage("", player, "vmsr");
                    if (cm.pendingviewers.ContainsKey(player))
                    {
                        cm.pendingviewers.Remove(player);
                    }
                    cm.pendingviewers.Add(player, DateTime.Now);
                }

                if (msgtest.StartsWith("/sl req "))
                {
                    string player = msgtest.Split(' ')[2];
                    cm.sendings++;
                    cm.sendMessage("", player, "vmwr");
                    if (cm.pendingsenders.ContainsKey(player))
                    {
                        cm.pendingsenders.Remove(player);
                    }
                    cm.pendingsenders.Add(player, DateTime.Now);
                }

                if (msgtest.StartsWith("/sl test "))
                {
                    string test = msgtest.Split(' ')[2];
                    if (test == "1")
                    {
                        WhisperMessage wm = new WhisperMessage();
                        this.cm.testing = 1;
                        wm.from = "ueHero";
                        wm.text = "vmsr";
                        dispatchMessages.Invoke(App.Communicator, new object[] { wm });
                    }

                    if (test == "2")
                    {
                        WhisperMessage wm = new WhisperMessage();
                        this.cm.testing = 2;
                        wm.from = "SeeMeScrollin";
                        wm.text = "vmsr";
                        dispatchMessages.Invoke(App.Communicator, new object[] { wm });
                    }

                    if (test == "4")
                    {
                        WhisperMessage wm = new WhisperMessage();
                        this.cm.testing = 2;
                        wm.from = "SeeMeScrollin";
                        wm.text = "vmwr";
                        dispatchMessages.Invoke(App.Communicator, new object[] { wm });
                    }

                    if (test == "3")
                    {
                        WhisperMessage wm = new WhisperMessage();
                        wm.from = "playerx";
                        wm.text = "vmgm 0101 0001005 {\"version\":\"1.1.0\",\"assetURL\":\"http://download.scrolls.com/assets/\",\"newsURL\":\"http://scrolls.com/news\",\"roles\":\"GAME,RESOURCE\",\"msg\":\"ServerInfo\"}";
                        dispatchMessages.Invoke(App.Communicator, new object[] { wm });

                        wm.text ="vmgm 0103 0001006 {\"white\":\"ueHero\",\"black\":\"Medium AI\",\"gameType\":\"SP_QUICKMATCH\",\"gameId\":11453851,\"color\":\"white\",\"roundTimerSeconds\":90,\"phase\":\"Init\",\"whiteAvatar\":{\"profileId\":68964,\"head\":117,\"body\":102,\"leg\":74,\"armBack\":99,\"armFront\":107},\"blackAvatar\":{\"profileId\":128093,\"head\":22,\"body\":8,\"leg\":40,\"armBack\":5,\"armFront\":18},\"whiteIdolTypes\":{\"profileId\":68964,\"type\":\"DEFAULT\",\"idol1\":2,\"idol2\":2,\"idol3\":2,\"idol4\":2,\"idol5\":2},\"blackIdolTypes\":{\"profileId\":128093,\"type\":\"DEFAULT\",\"idol1\":1,\"";                        dispatchMessages.Invoke(App.Communicator, new object[] { wm });
                        wm.text ="vmgm 0203 0001006 idol2\":1,\"idol3\":1,\"idol4\":1,\"idol5\":1},\"rewardForIdolKill\":10,\"nodeId\":\"54.172.48.186\",\"port\":8081,\"whiteIdols\":[{\"color\":\"white\",\"position\":0,\"hp\":10,\"maxHp\":10},{\"color\":\"white\",\"position\":1,\"hp\":10,\"maxHp\":10},{\"color\":\"white\",\"position\":2,\"hp\":10,\"maxHp\":10},{\"color\":\"white\",\"position\":3,\"hp\":10,\"maxHp\":10},{\"color\":\"white\",\"position\":4,\"hp\":10,\"maxHp\":10}],\"blackIdols\":[{\"color\":\"black\",\"position\":0,\"hp\":10,\"maxHp\":10},{\"color\":\"black\",\"position\":1,\"hp\":10,\"maxHp\":10},{\"color\":\"b";                        dispatchMessages.Invoke(App.Communicator, new object[] { wm });
                        wm.text ="vmgm 0303 0001006 lack\",\"position\":2,\"hp\":10,\"maxHp\":10},{\"color\":\"black\",\"position\":3,\"hp\":10,\"maxHp\":10},{\"color\":\"black\",\"position\":4,\"hp\":10,\"maxHp\":10}],\"refId\":0,\"maxTierRewardMultiplier\":0.5,\"tierRewardMultiplierDelta\":[0.025,0.05,0.1,0.1,0.1],\"msg\":\"GameInfo\"}";                        dispatchMessages.Invoke(App.Communicator, new object[] { wm });

                    }
                }
                return;
            }

            if (info.target is ProfileMenu && info.targetMethod.Equals("getButtonRect"))
            {
                returnValue = (Rect)typeof(ProfileMenu).GetMethod("getButtonRect", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(info.target, new object[] { 2f });
            }
            else
            {
                player.ReplaceMethod(info, out returnValue);
            }
		}

		public override void BeforeInvoke(InvocationInfo info)
		{
			
            if (info.target is BattleMode && info.targetMethod.Equals("effectDone"))
            {
                    EffectMessage currentEffect = ((EffectMessage)currentEffectField.GetValue(info.target));
                    if (currentEffect != null && currentEffect.type == "TurnBegin" && currentEffect.getRawText() != null && currentEffect.getRawText().Contains("{\"TurnBegin\":") && recorder != null && recorder.recording == true)
                    {
                        //Console.WriteLine("turnbegin end of: " + currentEffect.getRawText()); //{"TurnBegin":
                        recorder.turnBeginEnds();
                    }

            }

            if (!(info.target is ProfileMenu))
            {
                player.BeforeInvoke(info);
            }
		}

        


		public override void AfterInvoke(InvocationInfo info, ref object returnValue)
        {


            if (((info.target is BattleMode && player.playing && this.cm.senders.Count >= 1) || (info.target is BattleMode && recorder != null && recorder.recording && this.cm.viewers.Count >= 1)) && info.targetMethod.Equals("OnGUI"))
            //if ((info.target is BattleMode))
            {
                DrawerThing.Instance.Update(false);
                if ((DateTime.Now - this.lastpainting).TotalMilliseconds >= 1000)
                {
                    if (Input.GetKey(KeyCode.Insert))
                    {
                        Console.WriteLine("#" + DateTime.Now.ToShortTimeString() + " "+DrawerThing.Instance.getMouseData());
                        this.lastpainting = DateTime.Now;
                        //debug
                        /*
                        WhisperMessage wm = new WhisperMessage();
                        wm.from = "Scrollslive";
                        wm.text = DrawerThing.Instance.getMouseData();
                        ((BattleMode)info.target).handleMessage(wm);*/

                        //send data!
                        this.cm.sendPaintMessage();
                        //App.ChatUI.handleMessage(wm);
                    }
                }

                if (Input.GetKey(KeyCode.Delete))
                {
                    
                    this.cm.sendPaintStopMessage();
                }

            }

            
            if (((info.target is BattleMode && player.playing) || info.target is ChatUI ) && info.targetMethod.Equals("OnGUI") && this.cm.senders.Count>=1)
            {
               //check for new messages

                

                if ((DateTime.Now - this.cm.lastone).TotalMilliseconds >= 250)
                {
                    if ((info.target is BattleMode) && ((int)this.replaynexts.GetValue(info.target)) != 2147483647)
                    {
                        this.replaynexts.SetValue(info.target, 2147483647);
                    }

                    this.cm.updatePlayer();
                    this.cm.updateDrawings();
                    this.cm.lastone = DateTime.Now;
                }
            }

            if ((info.target is BattleMode && recorder != null && recorder.recording) && info.targetMethod.Equals("OnGUI") && this.cm.viewers.Count >= 1)
            //if (info.target is BattleMode )
            {
                //check for new messages



                if ((DateTime.Now - this.cm.lastone).TotalMilliseconds >= 250)
                {
                    this.cm.updateDrawings();
                    this.cm.lastone = DateTime.Now;
                }
            }

            
            if (info.target is Communicator && info.targetMethod.Equals("joinLobby") && recorder != null && recorder.recording == true)
            {
                recorder.stoprecording();
            }
            if (info.target is BattleMode && info.targetMethod.Equals("Start"))
            {
                player.setbm(info.target as BattleMode);
            }

            if (info.target is BattleMode && info.targetMethod.Equals("Start") && this.player.playing)
            {
                App.ChatUI.Show(false);
                App.ChatUI.SetMode(OnlineState.SPECTATE);
                App.ChatUI.SetLocked(false, (float)Screen.height * 0.25f);
                App.Communicator.setEnabled(true, true);
                App.ChatUI.SetEnabled(true);
                Console.WriteLine("playing + battlemodestart");
            }

            if (info.target is BattleMode && info.targetMethod.Equals("handleHandUpdate") && this.player.playing)
            {
                EMHandUpdate m = (EMHandUpdate)info.arguments[0];
                BattleMode dis = (BattleMode)info.target;
                //test
                FieldInfo bmActiveColor = typeof(BattleMode).GetField("activeColor", BindingFlags.Instance | BindingFlags.NonPublic);
                //MethodInfo getPlayer = typeof(BattleMode).GetMethod("getPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo getPlayer = typeof(BattleMode).GetMethod("getPlayer", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(TileColor) }, null);
                FieldInfo bmHandManager = typeof(BattleMode).GetField("handManager", BindingFlags.Instance | BindingFlags.NonPublic);
                TileColor activeColorr = ((TileColor)bmActiveColor.GetValue(dis));
                BMPlayer playerr = (BMPlayer)getPlayer.Invoke(dis, new object[] { activeColorr });
                if (m.profileId == playerr.profileId)
                {
                    ResourceGroup availableResources = ((BattleMode)info.target).battleUI.GetResources(dis.isLeftColor(activeColorr)).availableResources;
                    HandManager handManager = ((HandManager)bmHandManager.GetValue(dis));
                    handManager.SetHand(m.cards, availableResources, m.profileId);
                }
            }

            if (info.target is BattleMode && info.targetMethod.Equals("Start") && recorder != null && recorder.recording == true)
            {
                Console.WriteLine("## set Bm");
                recorder.setBm(info.target as BattleMode);
                if (this.spectating)
                {
                    recorder.recordSpectator();
                    Console.WriteLine("## (Spectator)");
                }
            }


            if (info.target is BattleModeUI && info.targetMethod.Equals("Start") && recorder != null && recorder.recording == true)
            {
                Console.WriteLine("## set Bmui");
                recorder.setBmUI(info.target as BattleModeUI);

                if (this.cm.google.isWatching == false) new Thread(new ThreadStart(this.cm.google.watcherthread)).Start();
                
            }

            if (!(info.target is BattleMode) || (info.target is BattleMode && (!info.targetMethod.Equals("Start") && !info.targetMethod.Equals("effectDone"))) || (info.target is Communicator && !info.targetMethod.Equals("joinLobby")))
            {
                player.AfterInvoke(info, ref returnValue);
            }



        }


    
    }


}

