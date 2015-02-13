using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Mono.Cecil;
using UnityEngine;
using ScrollsModLoader.Interfaces;

namespace scrollslive.Mod
{
	public class Player : ICommListener, IOkStringCancelCallback, IOkCancelCallback
	{
        private BattleMode bm = null;
        private List<string> logList = new List<string>();
        private Dictionary<int, int> turnToLogLine = new Dictionary<int, int>();
        private int minTurn, maxTurn;

		Thread replay = null;
        public volatile bool playing = false;
		private volatile bool readNextMsg = true;
        private volatile bool paused = false;

		//private String saveFolder;
		private String gameinfo;

		private BattleModeUI battleModeUI;
		private GameObject endGameButton;
		private Texture2D pauseButton;
		private Texture2D playButton;
        private MethodInfo dispatchMessages;

        private bool didButtonStyle = false;
        private GUIStyle buttonStyle;

        private int seekTurn = 0;
        private string lastturnbegin = "";
        private bool readedGameState=false;

        public void setbm(BattleMode b) { this.bm = b; }
        private FieldInfo effectsField=  typeof(BattleMode).GetField("effects", BindingFlags.NonPublic | BindingFlags.Instance);

        //for drawing
        List<Vector2> linePoints = new List<Vector2>();
        public float threshold = 0.001f;
        Vector3 lastPos = Vector3.one * float.MaxValue;

        CommunicationManager cm;

		public Player(CommunicationManager c)
		{
			App.Communicator.addListener(this);
            dispatchMessages = typeof(MiniCommunicator).GetMethod("_dispatchMessageToListeners", BindingFlags.NonPublic | BindingFlags.Instance);
        
            this.cm =c;
        }

        private void setButtonStyle()
        {
            GUISkin skin = (GUISkin)ResourceManager.Load("_GUISkins/LobbyMenu");
            this.buttonStyle = skin.button;
            this.buttonStyle.normal.background = this.buttonStyle.hover.background;
            this.buttonStyle.normal.textColor = new Color(1f, 1f, 1f, 1f);
            this.buttonStyle.fontSize = (int)((10 + Screen.height / 72) * 0.65f);
            this.buttonStyle.hover.textColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            this.buttonStyle.active.background = this.buttonStyle.hover.background;
            this.buttonStyle.active.textColor = new Color(0.60f, 0.60f, 0.60f, 1f);
        
        }

		public static MethodDefinition[] GetPlayerHooks(TypeDefinitionCollection scrollsTypes, int version)
		{
            return new MethodDefinition[] { 
                                            //scrollsTypes["MiniCommunicator"].Methods.GetMethod("_handleMessage")[0],
											scrollsTypes["GUIBattleModeMenu"].Methods.GetMethod("toggleMenu")[0],
											//scrollsTypes["BattleMode"].Methods.GetMethod("OnGUI")[0],
											//scrollsTypes["BattleMode"].Methods.GetMethod("runEffect")[0],
											//scrollsTypes["BattleModeUI"].Methods.GetMethod("Start")[0],
											//scrollsTypes["BattleModeUI"].Methods.GetMethod("Init")[0],
											//scrollsTypes["BattleModeUI"].Methods.GetMethod("Raycast")[0],
											scrollsTypes["BattleModeUI"].Methods.GetMethod("ShowEndTurn")[0],
										   };
		}

		public bool WantsToReplace (InvocationInfo info)
		{
            
			if (!playing)
				return false;

			switch ((String)info.targetMethod)
			{
				/*case "runEffect":
				{
					return paused;
				}
                case "_handleMessage":
				{
					return paused | !readNextMsg;
				}*/
				case "ShowEndTurn":
				{
					return true;
				}
			}
			return false;
		}

		public void ReplaceMethod (InvocationInfo info, out object returnValue) {
            returnValue = null;
			switch ((String)info.targetMethod) {
				case "ShowEndTurn":
				case "runEffect":
					returnValue = null;
					return;
                case "_handleMessage":
					returnValue = true;
					return;
			}
		}

		public void BeforeInvoke(InvocationInfo info)
		{

			switch ((String)info.targetMethod)
			{
                /*case "OnGUI":
                    {
                        if (playing) {
                            typeof(BattleMode).GetMethod ("deselectAllTiles", BindingFlags.Instance | BindingFlags.NonPublic).Invoke (info.target, null);
                        }
                    }
                    break;
                case "_handleMessage":
                    {
                        if (playing && readNextMsg != false)
                        {
                            readNextMsg = false;
                        }
                    }
                    break;
                 */
                case "toggleMenu":
					{
						if (playing)
						{ //quit on Esc/Back Arrow
							playing = false;
                            this.cm.stopwatching();
							App.Communicator.setData("");
							SceneLoader.loadScene("_Lobby");
                            this.replay.Abort();
                            this.replay = null;
						}
					}
					break;
			}
		}

		public void handleMessage(Message msg)
		{
			if (playing && msg is NewEffectsMessage && msg.getRawText().Contains("EndGame"))
			{
				playing = false;
                this.cm.stopwatching();
				//App.Communicator.setData("");
			}
		}
		
        public void onConnect(OnConnectData ocd)
		{
			return;
		}

		public void LaunchReplayer(string m)
		{

			replay = new Thread(new ThreadStart(LaunchReplay));
			replay.Start();

            this.computeMessage(m);
		}


		private void LaunchReplay()
		{
			playing = true;
			String line = "";
			String idWhite = null;
			String idBlack = null;
			String realID = null;
            
            // save log to list:
            this.logList.Clear();
            this.turnToLogLine.Clear();
            this.minTurn = 100000000;
            this.maxTurn = 0;
            
            App.SceneValues.battleMode = new SceneValues.SV_BattleMode(GameMode.Replay);//GameMode.Play
			SceneLoader.loadScene("_BattleModeView");


            this.readedGameState = false;
            //int lineindex = 0;
            //line = this.logList[lineindex];

            

			
            Console.WriteLine("viewer is ready");

		}

        public void computeMessage(string line)
        {

            if (line.StartsWith("#vmd:")) // a draw message!
            {

                return;
            }
            Console.WriteLine("# nxt play mssg: " + line);
            Message msg = MessageFactory.create(MessageFactory.getMessageName(line), line);
            Console.WriteLine("# nxt play mssg: "+ msg.GetType() + " " + msg.getRawText() );
            if (msg is CardInfoMessage) // CardInfoMessages are not very informative for players :D
            {
                return;
            }

            /*if (msg is GameStateMessage && this.readedGameState)
            {
                this.cm.lastone = DateTime.Now.AddSeconds(2.0);
            }*/

            if (msg is NewEffectsMessage && msg.getRawText().StartsWith("{\"effects\":[{\"TurnBegin"))
            {
                if (this.lastturnbegin == msg.getRawText()) this.readedGameState = false;

                this.lastturnbegin = msg.getRawText();
            }

            if (msg is GameStateMessage && this.readedGameState)
            {
                return;
            }
            else 
            { 
                if (msg is GameStateMessage) this.readedGameState = true; 
            }

            

            Console.WriteLine("# dispatch");
            dispatchMessages.Invoke(App.Communicator, new object[] { msg });// <---the whole magic
            if (line.Contains("EndGame") && !(msg is GameChatMessageMessage) && !(msg is RoomChatMessageMessage))
            {
                playing = false;
                this.cm.stopwatching();

            }

        }

		public void AfterInvoke(InvocationInfo info, ref object returnValue)
		{


		}



        // Round seeking
        public void PopupCancel(String type)
        {

        }

        public void PopupOk(String type)
        {

        }

        public void PopupOk(String type, String choice)
        {
            if (type == "turn")
            {
                seekTurn = Convert.ToInt16(choice);
                if (seekTurn <= this.minTurn)
                    seekTurn = this.minTurn;
                if (seekTurn >= this.maxTurn)
                    seekTurn = this.maxTurn;
                paused = false;
                this.readedGameState = false;
                
            }
        }


        void saveMousestuff()
        {
            UpdateLine();
            if (Input.GetButton("Fire1"))
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
                    i += 2;
                    continue;
                }
                //Console.WriteLine("draw line from " + linePoints[i] + " to " + linePoints[i+1] );
                Drawing.DrawLine(linePoints[i], linePoints[i + 1], Color.green, 3, false);
            }
        }


	}



    //source from
    // http://answers.unity3d.com/questions/186601/display-a-line-in-gui-without-texture-gui-line-.html

    public static class Drawing
    {
        private static Texture2D aaLineTex = null;
        private static Texture2D lineTex = null;
        private static Material blitMaterial = null;
        private static Material blendMaterial = null;
        private static Rect lineRect = new Rect(0, 0, 1, 1);

        // Draw a line in screen space, suitable for use from OnGUI calls from either
        // MonoBehaviour or EditorWindow. Note that this should only be called during repaint
        // events, when (Event.current.type == EventType.Repaint).
        //
        // Works by computing a matrix that transforms a unit square -- Rect(0,0,1,1) -- into
        // a scaled, rotated, and offset rectangle that corresponds to the line and its width.
        // A DrawTexture call used to draw a line texture into the transformed rectangle.
        //
        // More specifically:
        //      scale x by line length, y by line width
        //      rotate around z by the angle of the line
        //      offset by the position of the upper left corner of the target rectangle
        //
        // By working out the matrices and applying some trigonometry, the matrix calculation comes
        // out pretty simple. See https://app.box.com/s/xi08ow8o8ujymazg100j for a picture of my
        // notebook with the calculations.
        public static void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width, bool antiAlias)
        {
            // Normally the static initializer does this, but to handle texture reinitialization
            // after editor play mode stops we need this check in the Editor.
#if UNITY_EDITOR
         if (!lineTex)
         {
             Initialize();
         }
#endif

            // Note that theta = atan2(dy, dx) is the angle we want to rotate by, but instead
            // of calculating the angle we just use the sine (dy/len) and cosine (dx/len).
            float dx = pointB.x - pointA.x;
            float dy = pointB.y - pointA.y;
            float len = Mathf.Sqrt(dx * dx + dy * dy);

            // Early out on tiny lines to avoid divide by zero.
            // Plus what's the point of drawing a line 1/1000th of a pixel long??
            if (len < 0.001f)
            {
                return;
            }

            // Pick texture and material (and tweak width) based on anti-alias setting.
            Texture2D tex;
            Material mat;
            if (antiAlias)
            {
                // Multiplying by three is fine for anti-aliasing width-1 lines, but make a wide "fringe"
                // for thicker lines, which may or may not be desirable.
                width = width * 3.0f;
                tex = aaLineTex;
                mat = blendMaterial;
            }
            else
            {
                tex = lineTex;
                mat = blitMaterial;
            }

            float wdx = width * dy / len;
            float wdy = width * dx / len;

            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.m00 = dx;
            matrix.m01 = -wdx;
            matrix.m03 = pointA.x + 0.5f * wdx;
            matrix.m10 = dy;
            matrix.m11 = wdy;
            matrix.m13 = pointA.y - 0.5f * wdy;

            // Use GL matrix and Graphics.DrawTexture rather than GUI.matrix and GUI.DrawTexture,
            // for better performance. (Setting GUI.matrix is slow, and GUI.DrawTexture is just a
            // wrapper on Graphics.DrawTexture.)
            GL.PushMatrix();
            GL.MultMatrix(matrix);
            Graphics.DrawTexture(lineRect, tex, lineRect, 0, 0, 0, 0, color, mat);
            GL.PopMatrix();
        }

        // Other than method name, DrawBezierLine is unchanged from Linusmartensson's original implementation.
        public static void DrawBezierLine(Vector2 start, Vector2 startTangent, Vector2 end, Vector2 endTangent, Color color, float width, bool antiAlias, int segments)
        {
            Vector2 lastV = CubeBezier(start, startTangent, end, endTangent, 0);
            for (int i = 1; i < segments; ++i)
            {
                Vector2 v = CubeBezier(start, startTangent, end, endTangent, i / (float)segments);
                Drawing.DrawLine(lastV, v, color, width, antiAlias);
                lastV = v;
            }
        }

        private static Vector2 CubeBezier(Vector2 s, Vector2 st, Vector2 e, Vector2 et, float t)
        {
            float rt = 1 - t;
            return rt * rt * rt * s + 3 * rt * rt * t * st + 3 * rt * t * t * et + t * t * t * e;
        }

        // This static initializer works for runtime, but apparently isn't called when
        // Editor play mode stops, so DrawLine will re-initialize if needed.
        static Drawing()
        {
            Initialize();
        }

        private static void Initialize()
        {
            if (lineTex == null)
            {
                lineTex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                lineTex.SetPixel(0, 1, Color.white);
                lineTex.Apply();
            }
            if (aaLineTex == null)
            {
                // TODO: better anti-aliasing of wide lines with a larger texture? or use Graphics.DrawTexture with border settings
                aaLineTex = new Texture2D(1, 3, TextureFormat.ARGB32, false);
                aaLineTex.SetPixel(0, 0, new Color(1, 1, 1, 0));
                aaLineTex.SetPixel(0, 1, Color.white);
                aaLineTex.SetPixel(0, 2, new Color(1, 1, 1, 0));
                aaLineTex.Apply();
            }

            // GUI.blitMaterial and GUI.blendMaterial are used internally by GUI.DrawTexture,
            // depending on the alphaBlend parameter. Use reflection to "borrow" these references.
            blitMaterial = (Material)typeof(GUI).GetMethod("get_blitMaterial", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            blendMaterial = (Material)typeof(GUI).GetMethod("get_blendMaterial", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        }
    }

}

