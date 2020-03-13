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

            EtcDeviceJoinMap deviceJoinMap = new EtcDeviceJoinMap(joinStart);
            EtcSceneJoinMap sceneMap = new EtcSceneJoinMap(joinStart);

            ushort offset = 1;

            Debug.Console(2, EtcDevice, "Linking Zones to Trilist '{0}'", trilist.ID.ToString("X"));

            //Digtials 
            trilist.SetSigTrueAction(deviceJoinMap.PollScenes, () => EtcDevice.PollScenes());

            //Serials
            trilist.SetStringSigAction(deviceJoinMap.TestRx, s => { EtcDevice.ParseRx(s); });
            trilist.SetStringSigAction(deviceJoinMap.CommandPassThruTx, s => { EtcDevice.SendLineRaw(s); });
            
            trilist.SetStringSigAction(deviceJoinMap.RecallScene, s => { EtcDevice.SetSceneByName(s); });

            //To Bridge
            EtcDevice.TestTxFeedback.LinkInputSig(trilist.StringInput[deviceJoinMap.TestTx]);
            EtcDevice.CommandPassThruFeedback.LinkInputSig(trilist.StringInput[deviceJoinMap.CommandPassThruRx]);
            EtcDevice.ActivePresetFeedback.LinkInputSig(trilist.StringInput[deviceJoinMap.ActivePreset]);



            foreach (var scene in EtcDevice.Scenes)
            {
                Debug.Console(2, "Zone: Num: {0} is {1} at Offset: {2}", offset, scene.Value.sceneName, offset);

                var genericScene = scene.Value;
                Debug.Console(2, "Linking scenes");

                //Digtials
                trilist.SetSigTrueAction(sceneMap.PollScene + offset, () => genericScene.pollScene());
                trilist.SetSigTrueAction(sceneMap.SceneSet + offset, () => genericScene.SetScene());

                trilist.BooleanInput[sceneMap.Enable + offset].BoolValue = genericScene.enabled;

                //Serials
                Debug.Console(2, "Generic Zone Name: {0} Enabled: {1}", genericScene.sceneName, genericScene.enabled);
              
                trilist.StringInput[sceneMap.Name + offset].StringValue = genericScene.sceneName;
                
                
                Debug.Console(2, "Linking Feedbacks");

                genericScene.SceneFeedback.LinkInputSig(trilist.BooleanInput[sceneMap.SceneFb + offset]);
                

                offset++;
            }
		}
    }

    #region JoinMaps
    public class EtcDeviceJoinMap : JoinMapBase
	{

        public uint IsOnline {get; set; }
        public uint CommandPassThruTx { get; set; }
        public uint CommandPassThruRx { get; set; }
        public uint TestTx { get; set; }
        public uint TestRx { get; set; }
        public uint ActivePreset { get; set; }
        public uint PollScenes { get; set; }
        public uint RecallScene { get; set; }

		public EtcDeviceJoinMap(uint JoinStart)
		{
            IsOnline = 1;

            CommandPassThruTx = 1;
            CommandPassThruRx = 1;

            TestRx = 2;
            TestTx = 2;

            ActivePreset = 5;
            PollScenes = 5;
            RecallScene = 5;

            OffsetJoinNumbers(JoinStart);

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

        public EtcSceneJoinMap(uint JoinStart)
        {
            //Digitals
            Enable = 10;
            SceneSet = 30;
            SceneFb = 30;
            PollScene = 50;

            //Serial
            Name = 10;

            OffsetJoinNumbers(JoinStart);
        }

        public override void OffsetJoinNumbers(uint joinStart) {
            var joinOffset = joinStart - 1;
            var properties = this.GetType().GetCType().GetProperties().Where(o => o.PropertyType == typeof(uint)).ToList();
            foreach (var property in properties) {
                property.SetValue(this, (uint)property.GetValue(this, null) + joinOffset, null);
            }
        }

    }
    #endregion




}