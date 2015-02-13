using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace scrollslive.Mod
{
    class GameStateCreator
    {
        private int[] whiteIdols = { 10, 10, 10, 10, 10 };
        private int[] blackIdols = { 10, 10, 10, 10, 10 };
        private int[] whiteIdolsMax = { 10, 10, 10, 10, 10 };
        private int[] blackIdolsMax = { 10, 10, 10, 10, 10 };
        private int turnnumber = 1;

        private FieldInfo leftPlayerField = typeof(BattleMode).GetField("leftPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo rightPlayerField = typeof(BattleMode).GetField("rightPlayer", BindingFlags.NonPublic | BindingFlags.Instance);

        private FieldInfo rulesField = typeof(BMPlayer.PersistentRules).GetField("_rules", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo lastdurationField = typeof(PersistentRuleCardView).GetField("_lastDuration", BindingFlags.NonPublic | BindingFlags.Instance);

        private FieldInfo gameTypeField = typeof(BattleMode).GetField("gameType", BindingFlags.NonPublic | BindingFlags.Instance);
        //private MethodInfo unitsMethod = typeof(BattleMode).GetMethod("getUnitsFor", BindingFlags.NonPublic | BindingFlags.Instance);

        public string whitePlayerName = "me";
        public string blackPlayerName = "you";


        // Creates a GameState Message 
        public string create(BattleMode bm, BattleModeUI bmUI, bool whitesTurn)
        {

            BMPlayer leftPlayer= (BMPlayer)this.leftPlayerField.GetValue(bm);
            BMPlayer rightPlayer = (BMPlayer)this.rightPlayerField.GetValue(bm);
            string leftPlayerName = leftPlayer.name;
            //string blackPlayerName = ((string)typeof(BattleMode).GetField("rightPlayerName", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bm));
            //TileColor activeColor = ((TileColor)typeof(BattleMode).GetField("activeColor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bm));
            //int turnNumber = ((int)typeof(BattleMode).GetField("currentTurn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bm));
            GameType gameType = (GameType)gameTypeField.GetValue(bm);
            int secondsleft = -1;
            if (gameType == GameType.MP_RANKED) secondsleft = 90;
            if (gameType == GameType.MP_QUICKMATCH) secondsleft = 60;
            if (gameType == GameType.MP_LIMITED) secondsleft = 90;

            TileColor activeColor = TileColor.white;
            if (!whitesTurn) activeColor = TileColor.black;

            BMPlayer whiteBMPlayer = leftPlayer;
            BMPlayer blackBMplayer = rightPlayer ;
            PlayerAssets whiteplayer = bmUI.GetResources(true);
            PlayerAssets blackplayer = bmUI.GetResources(false);
            if (leftPlayerName == blackPlayerName) 
            {
                whiteBMPlayer = rightPlayer;
                blackBMplayer = leftPlayer;

                whiteplayer = bmUI.GetResources(false);
                blackplayer = bmUI.GetResources(true);
            }
            //ResourceGroup whiteRessisAvail = ((ResourceGroup)typeof(BattleModeUI).GetField("leftAvailable", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bmUI));
            //ResourceGroup blackRessisAvail = ((ResourceGroup)typeof(BattleModeUI).GetField("rightAvailable", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bmUI));
            //ResourceGroup whiteRessisMax = ((ResourceGroup)typeof(BattleModeUI).GetField("leftMax", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bmUI));
            //ResourceGroup blackRessisMax = ((ResourceGroup)typeof(BattleModeUI).GetField("rightMax", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bmUI));

            string retval = "";
            //{"whiteGameState":{"playerName":"Easy AI","board":{"color":"white","tiles":[{"card":{"id":7837,"typeId":127,"tradable":true,"isToken":false,"level":0},"ap":4,"ac":2,"hp":3,"position":"1,0","buffs":[{"name":"Crown of Strength","description":"Enchanted unit gains +1 Attack and +2 Health.","type":"ENCHANTMENT"}]},{"card":{"id":7834,"typeId":126,"tradable":true,"isToken":false,"level":0},"ap":1,"ac":2,"hp":2,"position":"1,1"},{"card":{"id":7838,"typeId":127,"tradable":true,"isToken":false,"level":0},"ap":3,"ac":2,"hp":3,"position":"1,2"}],"idols":[10,10,0,10,9]},"assets":{"availableResources":{"DECAY":0,"ORDER":4,"ENERGY":0,"GROWTH":0},"outputResources":{"DECAY":0,"ORDER":5,"ENERGY":0,"GROWTH":0},"handSize":4,"librarySize":30,"graveyardSize":12}},"blackGameState":{"playerName":"fuj1n","board":{"color":"black","tiles":[{"card":{"id":6151538,"typeId":68,"tradable":false,"isToken":false,"level":0},"ap":5,"ac":1,"hp":4,"position":"2,1"},{"card":{"id":6151539,"typeId":68,"tradable":false,"isToken":false,"level":0},"ap":5,"ac":1,"hp":4,"position":"2,2"}],"idols":[10,6,10,10,10]},"assets":{"availableResources":{"DECAY":0,"ORDER":0,"ENERGY":6,"GROWTH":0},"outputResources":{"DECAY":0,"ORDER":0,"ENERGY":6,"GROWTH":0},"handSize":3,"librarySize":26,"graveyardSize":19}},"activeColor":"black","phase":"Main","turn":26,"hasSacrificed":false,"secondsLeft":-1,"msg":"GameState"}

            retval = "{\"whiteGameState\":{\"playerName\":\"" + whitePlayerName + "\",\"board\":{\"color\":\"white\",\"tiles\":[";
            //get cards:
            retval = retval + this.getTiles(bm, true);
            //],"idols":[10,10,0,10,9]},
            //get white idols
            retval = retval + "],\"idols\":[" + this.whiteIdols[0] + "," + this.whiteIdols[1] + "," + this.whiteIdols[2] + "," + this.whiteIdols[3] + "," + this.whiteIdols[4] + "]},";
            //board finished
            //"assets":{"availableResources":{"DECAY":0,"ORDER":4,"ENERGY":0,"GROWTH":0},"outputResources":{"DECAY":0,"ORDER":5,"ENERGY":0,"GROWTH":0},
            retval = retval + "\"assets\":{\"availableResources\":{\"DECAY\":" + whiteplayer.availableResources.DECAY + ",\"ORDER\":" + whiteplayer.availableResources.ORDER + ",\"ENERGY\":" + whiteplayer.availableResources.ENERGY + ",\"GROWTH\":" + whiteplayer.availableResources.GROWTH + ",\"SPECIAL\":" + whiteplayer.availableResources.SPECIAL + "},";
            retval = retval + "\"outputResources\":{\"DECAY\":" + whiteplayer.outputResources.DECAY + ",\"ORDER\":" + whiteplayer.outputResources.ORDER + ",\"ENERGY\":" + whiteplayer.outputResources.ENERGY + ",\"GROWTH\":" + whiteplayer.outputResources.GROWTH + ",\"SPECIAL\":" + whiteplayer.outputResources.SPECIAL + "},";

            //"ruleUpdates":[... ];
            retval += getRuleChanges(bm, whiteBMPlayer);

            //"handSize":4,"librarySize":30,"graveyardSize":12}},
            retval = retval + "\"handSize\":" + whiteplayer.handSize + ",\"librarySize\":" + whiteplayer.librarySize +",\"graveyardSize\":" + whiteplayer.graveyardSize +"}},";
            
            //black
            //"blackGameState":{"playerName":"Easy AI","board":{"color":"black","tiles":[{"card":{"id":6151538,"typeId":68,"tradable":false,"isToken":false,"level":0},"ap":5,"ac":1,"hp":4,"position":"2,1"},{"card":{"id":6151539,"typeId":68,"tradable":false,"isToken":false,"level":0},"ap":5,"ac":1,"hp":4,"position":"2,2"}
            retval = retval + "\"blackGameState\":{\"playerName\":\"" + blackPlayerName + "\",\"board\":{\"color\":\"black\",\"tiles\":[";

            retval = retval + this.getTiles(bm, false);
            // ],"idols":[10,6,10,10,10]},
            retval = retval + "],\"idols\":[" + this.blackIdols[0] + "," + this.blackIdols[1] + "," + this.blackIdols[2] + "," + this.blackIdols[3]  +"," + this.blackIdols[4] + "]},";
            //board finished
            //"assets":{"availableResources":{"DECAY":0,"ORDER":0,"ENERGY":6,"GROWTH":0},"outputResources":{"DECAY":0,"ORDER":0,"ENERGY":6,"GROWTH":0},
            retval = retval + "\"assets\":{\"availableResources\":{\"DECAY\":" + blackplayer.availableResources.DECAY + ",\"ORDER\":" + blackplayer.availableResources.ORDER + ",\"ENERGY\":" + blackplayer.availableResources.ENERGY + ",\"GROWTH\":" + blackplayer.availableResources.GROWTH + ",\"SPECIAL\":" + whiteplayer.availableResources.SPECIAL + "},";
            retval = retval + "\"outputResources\":{\"DECAY\":" + blackplayer.outputResources.DECAY + ",\"ORDER\":" + blackplayer.outputResources.ORDER + ",\"ENERGY\":" + blackplayer.outputResources.ENERGY + ",\"GROWTH\":" + blackplayer.outputResources.GROWTH + ",\"SPECIAL\":" + whiteplayer.outputResources.SPECIAL + "},";

            //"ruleUpdates":[... ];
            retval += getRuleChanges(bm, blackBMplayer);

            //"handSize":3,"librarySize":26,"graveyardSize":19}},
            retval = retval + "\"handSize\":" + blackplayer.handSize + ",\"librarySize\":" + blackplayer.librarySize + ",\"graveyardSize\":" + blackplayer.graveyardSize + "}},";
            
            //"activeColor":"black","phase":"Main","turn":26,"hasSacrificed":false,"secondsLeft":-1,"msg":"GameState"}
            retval = retval + "\"activeColor\":\"" + activeColor.ToString() +"\",\"phase\":\"Main\",\"turn\":"+ this.turnnumber + ",\"hasSacrificed\":false,\"secondsLeft\":" + secondsleft + ",\"msg\":\"GameState\"}";
            
            return retval;
        }

        //{"whiteGameState":{"playerName":"Chace","board":{"color":"white","tiles":[{"card":{"id":27440610,"typeId":357,"tradable":true,"isToken":false,"level":0},"ap":5,"ac":5,"hp":5,"position":"1,2"}],"idols":[10,3,0,5,10]},"mulliganAllowed":false,"assets":{"availableResources":{"DECAY":0,"ORDER":1,"ENERGY":0,"GROWTH":0,"SPECIAL":0},"outputResources":{"DECAY":2,"ORDER":7,"ENERGY":0,"GROWTH":0,"SPECIAL":1},"ruleUpdates":[],"handSize":7,"librarySize":23,"graveyardSize":16}},"blackGameState":{"playerName":"ebass01","board":{"color":"black","tiles":[{"card":{"id":-1,"typeId":164,"tradable":true,"isToken":true,"level":0},"ap":3,"ac":1,"hp":1,"position":"1,0","buffs":[{"name":"Brain Lice","description":"Enchanted creature becomes [poisoned]. When it is destroyed, draw 1 scroll.","type":"ENCHANTMENT"},{"name":"Poison","description":"Unit takes 1 damage at the beginning of owner's turn.","type":"BUFF"}]},{"card":{"id":37844921,"typeId":131,"tradable":true,"isToken":false,"level":0},"ap":2,"ac":1,"hp":1,"position":"2,0"},{"card":{"id":27497640,"typeId":163,"tradable":false,"isToken":false,"level":0},"ap":3,"ac":2,"hp":2,"position":"3,0"},{"card":{"id":27883438,"typeId":131,"tradable":true,"isToken":false,"level":0},"ap":2,"ac":1,"hp":1,"position":"1,1"},{"card":{"id":28159797,"typeId":268,"tradable":true,"isToken":false,"level":0},"ap":3,"ac":4,"hp":2,"position":"2,1","buffs":[{"name":"Languid","description":"Enchanted unit's Attack is decreased by 2. When Languid comes into play, draw 1 scroll.","type":"ENCHANTMENT"}]},{"card":{"id":28074011,"typeId":172,"tradable":true,"isToken":false,"level":0},"ap":6,"ac":3,"hp":4,"position":"2,2","buffs":[{"name":"Languid","description":"Enchanted unit's Attack is decreased by 2. When Languid comes into play, draw 1 scroll.","type":"ENCHANTMENT"}]}],"idols":[10,10,10,10,10]},"mulliganAllowed":false,"assets":{"availableResources":{"DECAY":0,"ORDER":0,"ENERGY":0,"GROWTH":0,"SPECIAL":0},"outputResources":{"DECAY":3,"ORDER":1,"ENERGY":0,"GROWTH":0,"SPECIAL":1},"ruleUpdates":[{"card":{"id":41964355,"typeId":313,"tradable":true,"isToken":false,"level":0},"color":"black","roundsLeft":4}],"handSize":2,"librarySize":31,"graveyardSize":11}},"activeColor":"white","phase":"Main","turn":18,"hasSacrificed":true,"secondsLeft":62,"msg":"GameState"}



        private string getTiles(BattleMode bm, bool iswhite)
        {
            TileColor tc = TileColor.black;
            if (iswhite) tc = TileColor.white;
            List<Unit> units = bm.getUnitsFor(tc);//(List<Unit>)unitsMethod.Invoke(bm, new object[] { tc });
            string retval = "";
            //{"card":{"id":7837,"typeId":127,"tradable":true,"isToken":false,"level":0},"ap":4,"ac":2,"hp":3,"position":"1,0","buffs":[{"name":"Crown of Strength","description":"Enchanted unit gains +1 Attack and +2 Health.","type":"ENCHANTMENT"}]}
            foreach (Unit u in units)
            {
                Card c = u.getCard();
                if (retval != "") retval = retval + ",";
                //{"card":{"id":7837,"typeId":127,"tradable":true,"isToken":false,"level":0},
                retval = retval + "{\"card\":{\"id\":"+ c.getId()+ ",\"typeId\":"+ c.getType() + ",\"tradable\":"+c.tradable.ToString().ToLower()+",\"isToken\":"+c.isToken.ToString().ToLower()+",\"level\":"+c.level+"},";
                //"ap":4,"ac":2,"hp":3,"position":"1,0"
                retval = retval + "\"ap\":" + u.getAttackPower() + ",\"ac\":" + u.getAttackInterval() + ",\"hp\":" + u.getHitPoints() + ",\"position\":\"" + u.getTilePosition().row + "," + u.getTilePosition().column + "\"";
                //,"buffs":[{"name":"Crown of Strength","description":"Enchanted unit gains +1 Attack and +2 Health.","type":"ENCHANTMENT"}]
                List<EnchantmentInfo> b = u.getBuffs();
                if (b.Count >= 1)
                {
                    retval = retval + ",\"buffs\":[";
                    string buffes="";
                    foreach (EnchantmentInfo e in b)
                    {
                        if (buffes != "") buffes = buffes + ",";
                        buffes = buffes + "{\"name\":\"" + e.name + "\",\"description\":\"" + e.description + "\",\"type\":\"" + e.type.ToString().ToUpper() + "\"}";
                    }
                    retval = retval + buffes+ "]";
                }
                retval = retval + "}";
            }

            return retval;
        }

        private string getRuleChanges(BattleMode bm, BMPlayer player)
        {
            //,"outputResources":{...},"ruleUpdates":[{"card":{"id":41964355,"typeId":313,"tradable":true,"isToken":false,"level":0},"color":"black","roundsLeft":4}],"handSize":2,
            string retval = "\"ruleUpdates\":[";

            string rules = "";

            List<PersistentRuleCardView> _rules = (List<PersistentRuleCardView>) this.rulesField.GetValue(player.rules());

            if (_rules != null)
            {
                foreach (PersistentRuleCardView emra in _rules)
                {
                    if (rules != "") rules += ",";
                    Card c = emra.card();
                    int roundsleft = (int)this.lastdurationField.GetValue(emra);
                    rules += "{\"card\":{\"id\":" + c.id + ",\"typeId\":" + c.typeId + ",\"tradable\":true,\"isToken\":false,\"level\":0},\"color\":\"" + emra.playerColor() + "\",\"roundsLeft\":" + roundsleft + "}";
                }
            }
            retval += rules + "],";

            return retval;
        }

        public void updateIdols(List<EffectMessage> upid)
        {
            foreach (EffectMessage em in upid)
            {
                IdolInfo ium = (em as EMIdolUpdate).idol;
                if (ium.color == TileColor.white) { this.whiteIdols[ium.position] = ium.hp; }
                else { this.blackIdols[ium.position] = ium.hp; }
            }
        
        }

        public void updateIdols(IdolInfo[] upid)
        {
            foreach (IdolInfo ium in upid)
            {
                if (ium.color == TileColor.white) { this.whiteIdols[ium.position] = ium.hp; }
                else { this.blackIdols[ium.position] = ium.hp; }
            }

        }

        public void GameStateupdateIdols(int[] idolhp, bool iswhite)
        {
            for (int i = 0; i < idolhp.Length; i++)
            {
                if (iswhite) { this.whiteIdols[i] = idolhp[i]; }
                else { this.blackIdols[i] = idolhp[i]; }
            }

        }

        public void setTurn(int t) { this.turnnumber = t; }


    }
}
