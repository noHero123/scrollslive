using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace scrollslive.Mod
{
	public class Recorder : ICommListener, IOkCancelCallback
	{
		public List<String> messages = new List<String>();

		private String saveFolder;
		private string gameID;
		private Mod uiClass;
		//private DateTime timestamp;
        private MiniCommunicator comm;
        public bool recording = false;
        private BattleMode bttlmd = null;
        private BattleModeUI bttlmdUI = null;
        private GameStateCreator gsc;

        private int lastwasturn = 0;
        private bool lastwasturnwhite=true;

        private List<Message> tempmessages = new List<Message>();
        private Settings settings;

        private CommunicationManager cm;

        public bool isReplay = false;

        public Recorder(String saveFolder, Mod uiClass, Settings stngs, CommunicationManager c)
		{
            this.settings = stngs;
            this.cm = c;
			this.saveFolder = saveFolder;
			//App.Communicator.addListener(this);
			this.comm = App.Communicator;
            this.comm.addListener(this);
                /*IpPort address = App.SceneValues.battleMode.address;
                if (!address.Equals(App.Communicator.getAddress()))
                {
                    this.comm = App.SceneValues.battleMode.specCommGameObject.GetComponent<MiniCommunicator>();
                    this.comm.addListener(this);
                    this.comm.setEnabled(true, true);
                }
                */
            this.uiClass = uiClass;
            this.recording = true;
			//timestamp = DateTime.Now;
            

            gsc = new GameStateCreator();

		}


        public void addMessage(string s)
        {
            messages.Add(s);
            //send message to watchers :D
            cm.broadcastBattleMessageToWatchers(s);
        }


        public void recordSpectator() 
        {
            this.comm.removeListener(this);

            this.comm = new Communicator();
            IpPort address = App.SceneValues.battleMode.address;
            if (!address.Equals(App.Communicator.getAddress()))
            {
                this.comm = App.SceneValues.battleMode.specCommGameObject.GetComponent<MiniCommunicator>();
                this.comm.addListener(this);
                this.comm.setEnabled(true, true);
            }
            addMessage("{\"version\":\"1.1.0\",\"assetURL\":\"http://download.scrolls.com/assets/\",\"roles\":\"GAME,RESOURCE\",\"msg\":\"ServerInfo\"}");
            addMessage(App.SceneValues.battleMode.msg.getRawText());



            gameID = (App.SceneValues.battleMode.msg as GameInfoMessage).gameId.ToString();

                gsc.updateIdols((App.SceneValues.battleMode.msg as GameInfoMessage).whiteIdols);
                gsc.updateIdols((App.SceneValues.battleMode.msg as GameInfoMessage).blackIdols);
                gsc.setTurn(0);
                gsc.whitePlayerName = (App.SceneValues.battleMode.msg as GameInfoMessage).white;
                gsc.blackPlayerName = (App.SceneValues.battleMode.msg as GameInfoMessage).black;
        }

        public void handleMessage(Message msg)
        {
            // save the message on help-list


            if (!(msg is GameInfoMessage) && !(msg is GameStateMessage) && !(msg is NewEffectsMessage) && !(msg is ActiveResourcesMessage) && !(msg is ServerInfoMessage)) return;

            if (msg is GameInfoMessage)
            {
                gameID = (msg as GameInfoMessage).gameId.ToString();
            }
            tempmessages.Add(msg);

            //set the idol hp
            if (msg is GameStateMessage)//updating idols, for start of game
            {
                gsc.GameStateupdateIdols((msg as GameStateMessage).whiteGameState.board.idols, true);
                gsc.GameStateupdateIdols((msg as GameStateMessage).blackGameState.board.idols, false);
                gsc.setTurn((msg as GameStateMessage).turn);
                gsc.whitePlayerName = (msg as GameStateMessage).whiteGameState.playerName;
                gsc.blackPlayerName = (msg as GameStateMessage).blackGameState.playerName;

            }
            if (msg is GameInfoMessage)//updating idols, for start of game
            {
                gsc.updateIdols((msg as GameInfoMessage).whiteIdols);
                gsc.updateIdols((msg as GameInfoMessage).blackIdols);
                gsc.setTurn(0);
                gsc.whitePlayerName = (msg as GameInfoMessage).white;
                gsc.blackPlayerName = (msg as GameInfoMessage).black;
            }


            saveTillBeginTurn(); // try to save the tempmessages in messages

        }


        private void saveTillBeginTurn()
        {
            while (tempmessages.Count >= 1 && !tempmessages[0].getRawText().Contains("TurnBegin"))
            {
                Message msg = tempmessages[0];
                tempmessages.RemoveAt(0);

                addMessage(msg.getRawText());
                Console.WriteLine("## add to recorder:" + msg.getRawText() + "\r\n###");

                if (msg is NewEffectsMessage && msg.getRawText().Contains("IdolUpdate"))//updating idols, battlemode is to slow for us
                {
                    List<EffectMessage> idolupdates = new List<EffectMessage>();
                    foreach (EffectMessage current in NewEffectsMessage.parseEffects(msg.getRawText()))
                    {
                        if (current.type == "IdolUpdate") { idolupdates.Add(current); }
                    }
                    gsc.updateIdols(idolupdates);
                }

                if (msg is NewEffectsMessage && msg.getRawText().Contains("EndGame"))
                {
                    this.stoprec();
                    
                }
            
            }
        
        }

       public void turnBeginEnds() 
        {
            saveTillBeginTurn();// maybe there are messages in front of turnbegin messages (prob. = 0)

           // save beginTurn effect
           if(tempmessages.Count==0) return;
            Message msg = tempmessages[0];
            addMessage(msg.getRawText());
            Console.WriteLine("### add to recorder (turnbegin):" + msg.getRawText() + "\r\n###");

            if (msg is NewEffectsMessage && msg.getRawText().Contains("TurnBegin"))
            {
                List<EffectMessage> idolupdates = new List<EffectMessage>();
                foreach (EffectMessage current in NewEffectsMessage.parseEffects(msg.getRawText()))
                {
                    if (current.type == "TurnBegin") { gsc.setTurn((current as EMTurnBegin).turn); }
                }
                this.lastwasturnwhite = true;
                if (msg.getRawText().Contains("black")) this.lastwasturnwhite = false;
            }

            tempmessages.RemoveAt(0); //remove the turnbegin messages

           // create gamestatus + save it
            string mygsc = gsc.create(this.bttlmd, this.bttlmdUI, this.lastwasturnwhite);
            if (!this.isReplay)
            {
                Console.WriteLine("##created:" + mygsc);
                addMessage(mygsc);
            }

            // add the next saved messages
            saveTillBeginTurn();
            
        }


		public void onConnect(OnConnectData ocd)
		{
			return; //I (still) don't care
		}

        public void stoprecording()
        {
            
            addMessage("{\"effects\":[{\"EndGame\":{\"winner\":\"black\",\"whiteStats\":{\"profileId\":\"RobotEasy\",\"idolDamage\":0,\"unitDamage\":0,\"unitsPlayed\":0,\"spellsPlayed\":0,\"enchantmentsPlayed\":0,\"scrollsDrawn\":0,\"totalMs\":1,\"mostDamageUnit\":0,\"idolsDestroyed\":0},\"blackStats\":{\"profileId\":\"RobotEasy\",\"idolDamage\":0,\"unitDamage\":0,\"unitsPlayed\":0,\"spellsPlayed\":0,\"enchantmentsPlayed\":0,\"scrollsDrawn\":0,\"totalMs\":1,\"mostDamageUnit\":0,\"idolsDestroyed\":0},\"whiteGoldReward\":{\"matchReward\":0,\"matchCompletionReward\":0,\"idolsDestroyedReward\":0,\"totalReward\":0},\"blackGoldReward\":{\"matchReward\":0,\"matchCompletionReward\":0,\"idolsDestroyedReward\":0,\"totalReward\":0}}}],\"msg\":\"NewEffects\"}");
            stoprec();
        }

        public void stoprec()
        {
            this.recording = false;
            this.cm.google.isWatching = false;
            this.comm.removeListener(this);
            this.cm.stopwatching();
        }

        public void setBm(BattleMode b)
        {
            this.bttlmd = b;
        }
        public void setBmUI(BattleModeUI bui)
        {
            this.bttlmdUI = bui;
        }


        public void PopupOk(string s)
        {
        }

        public void PopupCancel(string s)
        { 
        }


	}
}

