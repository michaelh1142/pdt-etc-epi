using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PepperDash.Essentials.Core;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Bridges;
using Crestron.SimplSharp.Reflection;

namespace EtcEPI 
{
    enum ActionEnum
    {
        SetLevel = 1,
        LevelRaise = 2,
        LevelLower = 3,
        LevelStop = 4,
        Scene = 6
    }


    public class EtcScene : EtcDevice
    {
        public int sceneNum { get; set; }
        public string sceneName { get; set; }
        public string ActivePreset { get; set; }

        public bool _SceneFb;

        public BoolFeedback SceneFeedback { get; set; }

        public void Initialize()
        {
            
        }

        public EtcScene(EtcSceneConfig config, etc parent)
            : base( config, parent)
        {

            Debug.Console(2, "Scence Constructor");
            
            enabled = config.enabled;

            sceneNum = config.sceneNum;
            sceneName = config.sceneName;

    

            Debug.Console(2, "Scene key: {0}, {1}", config.ToString(), config.sceneName);
        }

        public void SetScene()
        {
            Debug.Console(2, "Set Scene: {0} {1}", this.label, sceneName);
            string cmd = string.Format("pst atc {0}{1}", sceneName, _etx);
            Parent.SendCommand(cmd);
        }


        public void pollScene()
        {
            Debug.Console(2, "Poll Scene");
            string cmd = string.Format("pst get {0}{1}", this.label, _etx);
            Parent.SendCommand(cmd);
        }

        public void processRx(string type, string action, string parameter)
        {

            if (type == "pst")
            {
                if (action == "act")
                {
                    _SceneFb = true;
                    SceneFeedback.FireUpdate();
                    Parent.RaiseEvent_ActivePresetUpdate(this);

                }
                else if (action == "deact")
                {
                    _SceneFb = false;
                    SceneFeedback.FireUpdate();
                }
            }
        }

    }



    public class EtcDevice
    {

        public string label { get; set; }
        public bool enabled { get; set; }

        protected string _etx = "\x0D\x0A";


        public StringFeedback ActivePresetFeedback { get; set; }



        public etc Parent { get; private set; }



        public EtcDevice()
        {
        }
        public EtcDevice(object config, etc parent) 
        {
            Debug.Console(2, "Device: {0}, Constructor", this);
            Parent = parent;

            Debug.Console(2, "Feedbacks defined");

        }






    }
}