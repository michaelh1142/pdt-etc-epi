using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PepperDash.Essentials.Core;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Bridges;
using Crestron.SimplSharp.Reflection;
using pdt.etc.epi.commandTimer;

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


    public class EtcScene
    {
        public string sceneName { get; set; }
        public string space { get; set; }
        public string fadeTime { get; set; }

        public string _etx = "\x0D";

        public bool enabled;
        
        public bool _SceneFb;
        private etc _Parent;

        private string cmdType;
        private string cmdAction;

        public BoolFeedback SceneFeedback { get; set; }


        CommandTimer devPollTimer;

        public void initialize()
        {
            this.devPollTimer.TimerCompleted += HandleEvent_DevTimerCompleted;
            this.devPollTimer.TimerUpdate += HandleEvent_DevTimerUpdate;
        }


        public EtcScene(EtcSceneConfig config, etc parent)
        {
            _Parent = parent;
            
            enabled = config.enabled;
            sceneName = config.sceneName;
            space = config.spaceName;
            fadeTime = config.fadeTime;

            devPollTimer = new CommandTimer();
            SceneFeedback = new BoolFeedback(() => _SceneFb); 

            Debug.Console(2, "Scene name: {0}", config.sceneName);

            initialize();
        }

        public void SetScene()
        {
            EtcCommand cmd;
            Debug.Console(2, "Set Scene: {0}", sceneName);
            cmdType = "pst";
            cmdAction = "act";
            if (this.space != null)
            {
                if (this.fadeTime != null)
                {
                    cmd = new EtcCommand(this.cmdType, this.cmdAction, this.sceneName, this.space, this.fadeTime, _etx);
                }
                else
                {
                    cmd = new EtcCommand(this.cmdType, this.cmdAction, this.sceneName, this.space, _etx);
                }
            }
            else
            {
                cmd = new EtcCommand(this.cmdType, this.cmdAction, this.sceneName, _etx);
            }

            devPollTimer.StartTimer(1);
            _Parent.SendCommand(cmd);
            //_Parent.EnqueueCommand(cmd);

        }

        public void SetScene(string name)
        {
            Debug.Console(2, "Set Scene by Name: {0}", name);

            EtcCommand cmd = new EtcCommand(string.Format("pst act {0}{1}", name, _etx));
            devPollTimer.StartTimer(1);
            _Parent.SendCommand(cmd);
            //_Parent.EnqueueCommand(cmd);

        }

        public void pollScene()
        {
            EtcCommand cmd = new EtcCommand(string.Format("pst get {0}{1}", this.sceneName, _etx));
            //_Parent.SendCommand(cmd);
            _Parent.EnqueueCommand(cmd);
        }

        public void processRx(string type, string action, string parameter, string space)
        {

            if (type == "pst")
            {
                if (action == "act")
                {
                    _SceneFb = true;
                    SceneFeedback.FireUpdate();
                    _Parent.ActivePreset = this.sceneName;
                    _Parent.ActivePresetFeedback.FireUpdate();
                    _Parent.RaiseEvent_ActivePresetUpdate(this);
                }
                else if (action == "dact")
                {
                    _SceneFb = false;
                    SceneFeedback.FireUpdate();
                }
                else if (action == "alt")
                {
                    Debug.Console(2, Debug.ErrorLogLevel.Notice, "{0}:: Preset {0} returned status as altered. Check Lighting program",_Parent.ToString(), this.sceneName);
                }
            }
        }

        public void HandleEvent_DevTimerCompleted(object source, EventArgs e)
        {
            _Parent.PollScenes();
        }

        public void HandleEvent_DevTimerUpdate(object source, EventArgs e)
        {
        }
    }
  
}