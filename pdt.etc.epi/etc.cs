using System;
using Crestron.SimplSharp;                          				// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;                         // For Basic SIMPL#Pro classes

using System.Collections.Generic;
using PepperDash.Essentials.Core.Devices;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

using PepperDash.Essentials.Bridges;
using PepperDash.Core;
using pdt.etc.epi.commandTimer;


namespace EtcEPI 
{
	public class etc : Device, IBridge
    {
        #region Plugin Support
        public static void LoadPlugin()
		{
			DeviceFactory.AddFactoryForType("etclighting", etc.BuildDevice);	
		}

		public static etc BuildDevice(DeviceConfig dc)
		{
            Debug.Console(2, "Build Device - Lutron");
            var comm = CommFactory.CreateCommForDevice(dc);
	
			var newMe = new etc(dc.Key, dc.Name, comm, dc);
			return newMe;
        }
        #endregion 

        public Dictionary<string,EtcScene> Scenes { get; set; }

        public string DeviceRx { get; set; }
        public string ActivePreset { get; set; }

        public IBasicCommunication Communication { get; private set; }
        public CommunicationGather PortGather { get; private set; }
        public GenericCommunicationMonitor CommunicationMonitor { get; private set; }

        CrestronQueue CommandQueue;

        bool CommandQueueInProgress = false;

		//GenericSecureTcpIpClient_ForServer Client;

        public EventHandler<SceneEventArgs> ActivePresetUpdate;
        public EventHandler<EventArgs> PollResponseReceived;

        public StringFeedback CommandPassThruFeedback { get; set; }
        public StringFeedback TestTxFeedback { get; set; }
        public StringFeedback ActivePresetFeedback { get; set; }

        public string TestRx { get; set; }
        public string TestTx;

        CommandTimer cmdTimer;

        DeviceConfig _Dc;
 
        public virtual void RaiseEvent_ActivePresetUpdate(EtcScene Scene)
        {
            if (ActivePresetUpdate != null)
                ActivePresetUpdate(this, new SceneEventArgs(Scene) { SceneUpdated = Scene });

        }

        public virtual void RaiseEvent_ResponseReceived()
        {
            if (PollResponseReceived != null)
                PollResponseReceived(this, new EventArgs());
        }


        public void Initialize()
        {
            this.ActivePresetUpdate += HandleEvent_ActivePresetUpdate;
            this.PollResponseReceived += HandleEvent_ResponseReceived;
            this.cmdTimer.TimerCompleted += HandleEvent_TimerCompleted;
        }


		public etc(string key, string name, IBasicCommunication comm, DeviceConfig dc)
			: base(key,name)
		{
            _Dc = dc;

 
            Communication = comm;
            var socket = comm as ISocketStatus;
            if (socket != null)
            {
                socket.ConnectionChange += new EventHandler<GenericSocketStatusChageEventArgs>(socket_ConnectionChange);
            }

            var config = JsonConvert.DeserializeObject<EtcConfigObject>(dc.Properties.ToString());


            PortGather = new CommunicationGather(Communication, "\x0D");
            PortGather.LineReceived += this.Port_LineReceived;
            
            CommunicationMonitor = new GenericCommunicationMonitor(this, Communication, 20000, 120000, 300000, "?ETHERNET, 0\x0D");
            CommunicationMonitor.Start();

            TestTxFeedback = new StringFeedback(() => TestTx);
            CommandPassThruFeedback = new StringFeedback(() => DeviceRx);
            ActivePresetFeedback = new StringFeedback(() => ActivePreset);

            CommandQueue = new CrestronQueue(100);
            cmdTimer = new CommandTimer();
            

            Scenes = new Dictionary<string, EtcScene>();

            if (config.scenes != null)
            {
                Debug.Console(2, this, "Zone List Exists");
                foreach (KeyValuePair<string, EtcSceneConfig> scene in config.scenes)
                {
                    string itemkey = scene.Key;
                    var value = scene.Value;
                    Debug.Console(2, this, "Adding: Key: {0} Value Label: {1} Enabled: {2}", itemkey, value.sceneName, value.enabled);
                    EtcScene thisScene = new EtcScene(value, this);
                    Scenes.Add(itemkey, thisScene);
                }
            }

            Initialize();
           
        }

        void socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs e)
        {
            Debug.Console(2, this, "Socket Status Change: {0}", e.Client.ClientStatus.ToString());

            if (e.Client.IsConnected)
            {
                //SubscribeToAttributes();
            }
            else
            {
                // Cleanup items from this session
               // CommandQueue.Clear();
                //CommandQueueInProgress = false;
            }

        }

        #region Command Queue
        public void SendNextQueuedCommand()
        {
            if (!CommandQueue.IsEmpty)
            {
                cmdTimer.StartTimer(60);
                CommandQueueInProgress = true;
                if (CommandQueue.Peek() is EtcCommand)
                {
                    EtcCommand nextCommand = (EtcCommand)CommandQueue.Dequeue();

                    SendCommand(nextCommand);
                    
                }
                else
                {
                    string nextCommand = (string)CommandQueue.Dequeue();

                    SendLine(nextCommand);
                }
            }
            else
            {
                CommandQueueInProgress = false;
                cmdTimer.StopTimer();
            }

        }

        public void EnqueueCommand(EtcCommand CommandToQueue)
        {
            CommandQueue.Enqueue(CommandToQueue);


            if (!CommandQueueInProgress)
            {

                SendNextQueuedCommand();
            }
        }

        public void SendCommand(EtcCommand s)
        {
            Debug.Console(2, this, "Command: {0}", s._cmd);
            Communication.SendText(s._cmd);
            TestTx = s._cmd;
            TestTxFeedback.FireUpdate();
        }
        #endregion

        #region System Commands
        public void SendLine(string s)
        {
            Debug.Console(2, this, "TX: '{0}'", s);
            Communication.SendText(s + "\x0D");
        }

        public void SendLineRaw(string s)
        {
            Debug.Console(2, this, "TX: '{0}'", s);
            Communication.SendText(s);
        }

        public void SetSceneByName(string name)
        {
            EtcScene thisScene = new EtcScene(null, this);

            thisScene.SetScene(name);

        }

        public void PollScenes()
        {
            CommandQueueInProgress = false;
            CommandQueue.Clear();

            foreach (KeyValuePair<string, EtcScene> scene in Scenes)
            {
                if (scene.Value.enabled == true)
                {
                    scene.Value.pollScene();
                }
            }
        }
        #endregion

        #region Rx
        // Parse RX - Exampe response: 
        //
        // pst act presetname, spacename\x0D
        //
        // presetname may have spaces in it.
        // spacename not supported
        public void ParseRx(string Data)
        {
            try
            {
                char[] trimChar = { ' ' };

                Debug.Console(2, "ParseRX {0}", Data);

                string[] _data = Data.Split(' ');

                foreach (string item in _data)
                    Debug.Console(2, "Item: {0}", item);

                var type = _data[0].TrimEnd(' ', ',');
                var action = _data[1].TrimEnd(' ', ',');
                var parameters = string.Empty;
                var space = _data[_data.GetLength(0) - 1].TrimEnd(' ', ',' ,'\x0D');


                

                if (type == "pst")
                {
                    if (_data.GetLength(0) > 4)  // If number of elements greater than 4 then the preset name has one or more spaces
                        parameters = string.Join(" ", _data, 2, _data.GetLength(0) - 3).TrimEnd(' ', ',');  // join and remove any end comma
                    else if (_data.GetLength(0) == 4)  // If number of elements = to 4 then preset name does not have a space
                        parameters = _data[2].TrimEnd(' ', ',');
                    else  // need 4 elements for valid preset name.
                        parameters = "";
                    Debug.Console(2, this, "Type: {0} :: action: {1}:: Parameter {2} :: Space {3}", type, action, parameters, space);


                    foreach (KeyValuePair<string, EtcScene> thisEtcScene in Scenes)
                    {
                        Debug.Console(2, this, "thisEtcScene: scene Name: {0} :: Scene Space: {1}", thisEtcScene.Value.sceneName, thisEtcScene.Value.space);
                        if (thisEtcScene.Value.sceneName == parameters)
                        {
                            Debug.Console(2, this, "Parameter match");
                            if (thisEtcScene.Value.space != null)
                            {
                                Debug.Console(2, this, "space is not 0 or null");
                                if (thisEtcScene.Value.space == space)
                                {
                                    Debug.Console(2, this, "Scene :: Found type {0}, Action {1}, paramter {2}, space {3}", type, action, parameters, space);

                                    thisEtcScene.Value.processRx(type, action, parameters, space);
                                    break;
                                }
                            }
                            else
                            {
                                Debug.Console(2, this, "Space is 0 or null");
                                thisEtcScene.Value.processRx(type, action, parameters, space);
                                break;
                            }
                        }

                    }
                }
            }
            catch (Exception e)
            {
                Debug.Console(2, "{0}", e.ToString());
            }
            finally
            {
                RaiseEvent_ResponseReceived(); 
            }
                           
        }


        void Port_LineReceived(object dev, GenericCommMethodReceiveTextArgs args)
        {
            try
            {
                Debug.Console(2, this, "Line Received :::: {0}", args.Text);
                DeviceRx = args.Text;
                CommandPassThruFeedback.FireUpdate();
                ParseRx(DeviceRx);


            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine("{0}: Execption Thrown on Line Received {1},{2}", this, e, e.StackTrace);
            }

        }
        #endregion


        #region IBridge Members

        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey)
        {
            Debug.Console(2, this, "Link To API"    );
            this.LinkToApiExt(trilist, joinStart, joinMapKey);
        }

        #endregion


        #region EventHandlers
        public void HandleEvent_ActivePresetUpdate(object source, SceneEventArgs e)
        {
            ActivePreset = e.SceneName;
            Debug.Console(2, "Active Preset Changed to {0}", ActivePreset);
            ActivePresetFeedback.FireUpdate();
        }

        public void HandleEvent_ResponseReceived(object source, EventArgs e)
        {
            Debug.Console(2, "Response Received.. Send Next");
            cmdTimer.StartTimer(60);
            SendNextQueuedCommand();
        }

        public void HandleEvent_TimerCompleted(object source, EventArgs e)
        {
            Debug.Console(2, "Command Timer Completed");
            CommandQueueInProgress = false;
            SendNextQueuedCommand();

        }
        #endregion
    }
}

