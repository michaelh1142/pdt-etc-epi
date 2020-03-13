using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;



namespace EtcEPI
{
    public class SceneEventArgs : EventArgs
    {
        public EtcScene SceneUpdated;
        public string SceneName {get; set; }


        public SceneEventArgs()
        {
        }

        public SceneEventArgs(EtcScene Value)
        {
            SceneUpdated = Value;
            SceneName = Value.sceneName;
        }
    }
}