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


namespace EtcEPI 
{

           

	public class etc : Device, IBridge

	{
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

        public Dictionary<string,EtcScene> Scenes { get; set; }

        public string DeviceRx { get; set; }
        public string ActivePreset { get; set; }

        public IBasicCommunication Communication { get; private set; }
        public CommunicationGather PortGather { get; private set; }
        public GenericCommunicationMonitor CommunicationMonitor { get; private set; }

		//GenericSecureTcpIpClient_ForServer Client;

        public EventHandler<SceneEventArgs> ActivePresetUpdate;

        public StringFeedback CommandPassThruFeedback { get; set; }
        public StringFeedback TestTxFeedback { get; set; }

        public string TestRx { get; set; }
        public string TestTx;


        DeviceConfig _Dc;

        public StringFeedback ActivePresetFeedback { get; set; }

 
        public virtual void RaiseEvent_ActivePresetUpdate(EtcScene Scene)
        {
            if (ActivePresetUpdate != null)
                ActivePresetUpdate(this, new SceneEventArgs(Scene) { SceneUpdated = Scene });
        }


        public void Initialize(DeviceConfig dc)
        {
            this.ActivePresetUpdate += HandleEvent_ActivePresetUpdate;

            Scenes = new Dictionary<string,EtcScene>();

            var config = JsonConvert.DeserializeObject<EtcConfigObject>(dc.Properties.ToString());

            TestTxFeedback = new StringFeedback(() => TestTx);
            CommandPassThruFeedback = new StringFeedback(() => DeviceRx);

            if (config.scenes != null)
            {
                Debug.Console(2, this, "Zone List Exists");
                foreach (KeyValuePair<string,  EtcSceneConfig> scene in config.scenes)
                {
                    string itemkey = scene.Key;
                    var value = scene.Value;
                    Debug.Console(2, this, "Adding: Key: {0} Value Label: {1} Enabled: {2}", value.sceneName, value.enabled);
                    EtcScene thisScene = new EtcScene(value, this);
                    Scenes.Add(itemkey, thisScene);
                }
            }
        }


		public etc(string key, string name, IBasicCommunication comm, DeviceConfig dc)
			: base(key,name)
		{
            Debug.Console(2, this, "Constructor - etc");
            _Dc = dc;

 
            Communication = comm;
            var socket = comm as ISocketStatus;
            if (socket != null)
            {
                socket.ConnectionChange += new EventHandler<GenericSocketStatusChageEventArgs>(socket_ConnectionChange);
            }

            PortGather = new CommunicationGather(Communication, "\x0D\x0A");
            PortGather.LineReceived += this.Port_LineReceived;
            CommunicationMonitor = new GenericCommunicationMonitor(this, Communication, 20000, 120000, 300000, "?ETHERNET, 0\x0D");
            CommunicationMonitor.Start();

            Initialize(_Dc);
           
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

        public void SendCommand(string s)
        {
            Debug.Console(2, this, "Command: {0}", s);
            Communication.SendText(s);
            TestTx = s;
            TestTxFeedback.FireUpdate();
            Debug.Console(2, this, "TestFeedbackTx: {0} - TestTx: {1}", TestTxFeedback.StringValue, TestTx);         
    
        }


        public void ParseRx(string Data)
        {
            string type;
            string action;
            string parameters;
            string space;
            char[] trimChar = { ' ', '\x0D', ','};

            Debug.Console(2, "ParseRX {0}", Data );

            if (Data.Length > 0)
            {
                string[] _data = Data.Split(trimChar);

                Debug.Console(2, "Contains Text");
                type = _data[1].TrimEnd(trimChar);
                action = _data[2].TrimEnd(trimChar);
                parameters = _data[3].TrimEnd(trimChar);
                space = _data[5].TrimEnd(trimChar);


                foreach (KeyValuePair<string, EtcScene> thisEtcScene in Scenes)
                {
                    if (thisEtcScene.Value.sceneName == parameters)
                    {
                        Debug.Console(2, this, "Scene :: Found type {0}, Action {1}, paramter {2}", type, action, parameters);

                        thisEtcScene.Value.processRx(type, action, parameters);
                        return;
                    }
                }             
            }
        }

        public void PollScenes()
        {
            foreach (KeyValuePair<string,  EtcScene> scene in Scenes)
            {
                if (scene.Value.enabled == true)
                {
                    scene.Value.pollScene();
                }
            }
        }

        void Port_LineReceived(object dev, GenericCommMethodReceiveTextArgs args)
        {

            try
            {
                Debug.Console(2, this, "Line Received :::: {0}", args.Text);
                DeviceRx = args.Text;
                this.CommandPassThruFeedback.FireUpdate();
                ParseRx(DeviceRx);             
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine("{0}: Execption Thrown on Line Received {1},{2}", this, e, e.StackTrace);
            }

        }


        #region IBridge Members

        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey)
        {
            Debug.Console(2, this, "Link To API"    );
            this.LinkToApiExt(trilist, joinStart, joinMapKey);
        }

        #endregion



        public void HandleEvent_ActivePresetUpdate(object source, SceneEventArgs e)
        {
            ActivePreset = e.SceneName;
            ActivePresetFeedback.FireUpdate();
        }
	}
}

