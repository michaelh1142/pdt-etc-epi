using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;

using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Bridges;
using Crestron.SimplSharp.Reflection;



namespace EtcEPI
{
	public static class EtcBridge
	{
		public static void LinkToApiExt(this etc EtcDevice, BasicTriList trilist, uint joinStart, string joinMapKey)
		{
			//var joinMap = JoinMapHelper.GetJoinMapForDevice(joinMapKey) as LutronQuantumBridgeJoinMap;

			//if (joinMap == null)
			//	joinMap = new LutronQuantumDeviceJoinMap();

            EtcDeviceJoinMap deviceJoinMap = new EtcDeviceJoinMap();
            EtcSceneJoinMap sceneMap = new EtcSceneJoinMap();



            //ushort x = 1;
            ushort offset = 1;

            //trilist.SetStringSigAction(deviceJoinMap.CommandPassThruTx, s => LutronDevice.Sen

            Debug.Console(1, EtcDevice, "Linking Zones to Trilist '{0}'", trilist.ID.ToString("X"));

            trilist.SetStringSigAction(deviceJoinMap.TestRx, s => { EtcDevice.ParseRx(s); });
            trilist.SetStringSigAction(deviceJoinMap.CommandPassThruTx, s => { EtcDevice.SendCommand(s); });
            trilist.StringInput[deviceJoinMap.CommandPassThruRx].StringValue = EtcDevice.DeviceRx;

            EtcDevice.TestTxFeedback.LinkInputSig(trilist.StringInput[deviceJoinMap.TestTx]);



            foreach (var scene in EtcDevice.Scenes)
            {
                Debug.Console(2, "Zone: Num: {0} is {1} at Offset: {2}", offset, scene.Value.sceneName, offset);

                var genericScene = scene.Value;
                Debug.Console(2, "Linking commands");


                trilist.StringInput[sceneMap.Name + offset].StringValue = genericScene.sceneName;
                Debug.Console(2, "Generic Zone Name: {0} Enabled: {1}", genericScene.label, genericScene.enabled);
                Debug.Console(2, "Zone Name: {0} Enabled {1}",genericScene.sceneName, genericScene.enabled);
                trilist.BooleanInput[sceneMap.Enable + offset].BoolValue = genericScene.enabled;

                trilist.SetSigTrueAction(sceneMap.PollScene + offset, () => genericScene.pollScene());



                Debug.Console(2, "Linking scenes");
                trilist.SetSigTrueAction(sceneMap.SceneSet + offset, () => genericScene.SetScene());
                
                Debug.Console(2, "Linking Feedbacks");

                



                genericScene.SceneFeedback.LinkInputSig(trilist.BooleanInput[sceneMap.SceneFb + offset]);
                

                offset++;
                Debug.Console(2, " Offset is now {0}", offset);

            }

           





		}
	}
    
	public class EtcDeviceJoinMap : JoinMapBase
	{

        public uint IsOnline {get; set; }
        public uint CommandPassThruTx { get; set; }
        public uint CommandPassThruRx { get; set; }
        public uint TestTx { get; set; }
        public uint TestRx { get; set; }
        public uint ActivePreset { get; set; }

		public EtcDeviceJoinMap()
		{
            IsOnline = 1;

            CommandPassThruTx = 1;
            CommandPassThruRx = 1;

            TestRx = 2;
            TestTx = 2;

            ActivePreset = 5;

		}

        public override void OffsetJoinNumbers(uint joinStart)
        {
            var joinOffset = joinStart - 1;
            var properties = this.GetType().GetCType().GetProperties().Where(o => o.PropertyType == typeof(uint)).ToList();
            foreach (var property in properties)
            {
                property.SetValue(this, (uint)property.GetValue(this, null) + joinOffset, null);
            }
        }

	}

 
    public class EtcSceneJoinMap : JoinMapBase
    {
        public uint Name { get; set; }
        public uint Enable { get; set; }
        public uint SceneSet { get; set; }
        public uint SceneFb { get; set; }
        public uint PollScene { get; set; }

        public EtcSceneJoinMap()
        {
            //Digitals
            Enable = 10;
            SceneSet = 60;
            SceneFb = 60;
            PollScene = 110;

            //Serial
            Name = 10;
        }

        public override void OffsetJoinNumbers(uint joinStart) {
            var joinOffset = joinStart - 1;
            var properties = this.GetType().GetCType().GetProperties().Where(o => o.PropertyType == typeof(uint)).ToList();
            foreach (var property in properties) {
                property.SetValue(this, (uint)property.GetValue(this, null) + joinOffset, null);
            }
        }

    }
    
 


}