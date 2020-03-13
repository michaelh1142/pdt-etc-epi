using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace EtcEPI 
{
    public class EtcCommand
    {
        public string _cmd { get; set; }

        public EtcCommand(string cmdType, string cmdAction, string sceneName, string space, string fade, string etx)
        {

            _cmd = string.Format("{0} {1} {2}, {3}, {4}{5}", cmdType, cmdAction, sceneName, space, fade, etx);

        }

        public EtcCommand(string cmdType, string cmdAction, string sceneName, string space, string etx)
        {

            _cmd = string.Format("{0} {1} {2}, {3}{4}", cmdType, cmdAction, sceneName, space, etx);
        
        }

        public EtcCommand(string cmdType, string cmdAction, string sceneName, string etx)
        {
            
            _cmd = string.Format("{0} {1} {2}{3}", cmdType, cmdAction, sceneName, etx);

        }

        public EtcCommand(string command)
        {

            _cmd = command;

        }
    }
}