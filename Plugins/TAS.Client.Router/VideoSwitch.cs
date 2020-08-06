﻿using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces;
using System.Linq;
using jNet.RPC.Server;
using TAS.Server.VideoSwitch.Model;
using TAS.Server.VideoSwitch.Communicators;
using jNet.RPC;
using TAS.Database.Common;
using System.ComponentModel.Composition;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TAS.Server.VideoSwitch
{
    public class VideoSwitch : ServerObjectBase, IVideoSwitch, IPlugin
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IVideoSwitchCommunicator _routerCommunicator;
        private IVideoSwitchPort _selectedInputPort;
        private bool _isConnected;
        
        public VideoSwitch(VideoSwitchType type = VideoSwitchType.Unknown)
        {
            Type = type;
            switch (type)
            {
                case VideoSwitchType.Nevion:
                    _routerCommunicator = new NevionCommunicator(this);
                    break;
                case VideoSwitchType.BlackmagicSmartVideoHub:
                    _routerCommunicator = new BlackmagicSmartVideoHubCommunicator(this);
                    break;
                case VideoSwitchType.Atem:
                    _routerCommunicator = new AtemCommunicator(IpAddress);
                    break;
                default:
                    return;
            }
            _routerCommunicator.OnInputPortChangeReceived += Communicator_OnInputPortChangeReceived;
            _routerCommunicator.OnRouterPortsStatesReceived += Communicator_OnRouterPortStateReceived;
            _routerCommunicator.OnRouterConnectionStateChanged += Communicator_OnRouterConnectionStateChanged;            
        }
       
        #region Configuration
        
        [Hibernate]
        public bool IsEnabled { get; set; }

        [Hibernate]
        public string IpAddress { get; set; }        
        [Hibernate]
        public VideoSwitchType Type { get; set; }
        [Hibernate]
        public int Level { get; set; }
        [Hibernate]
        public string Login { get; set; }
        [Hibernate]
        public string Password { get; set; }
        [Hibernate]
        public short[] OutputPorts { get; set; }

        #endregion

        [DtoMember]
        public IVideoSwitchPort SelectedInputPort
        {
            get => _selectedInputPort;
            set => SetField(ref _selectedInputPort, value);
        }

        [DtoMember]
        public IList<IVideoSwitchPort> InputPorts { get; } = new List<IVideoSwitchPort>();

        [DtoMember]
        public bool IsConnected
        {
            get => _isConnected;
            private set => SetField(ref _isConnected, value);
        }              

        public void SelectInput(int inPort)
        {            
             _routerCommunicator.SelectInput(inPort);
        }

        public async void Connect()
        {
            if (_routerCommunicator == null)
                return;

            try
            {
                IsConnected = await _routerCommunicator.Connect();
                if (IsConnected)                    
                    ParseInputMeta(await _routerCommunicator.GetInputPorts());
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private async void ParseInputMeta(PortInfo[] ports)
        {
            if (ports == null)
                return;

            foreach (var port in ports)
            {
                if (InputPorts.FirstOrDefault(inPort => inPort.PortId == port.Id && inPort.PortName != port.Name) is RouterPort foundPort)
                    foundPort.PortName = port.Name;
                else if (InputPorts.All(inPort => inPort.PortId != port.Id))
                    InputPorts.Add(new RouterPort(port.Id, port.Name));
            }
            NotifyPropertyChanged(nameof(InputPorts));
            var selectedInput = await _routerCommunicator.GetCurrentInputPort();

            if (selectedInput == null)
            {
                //ParseInputMeta(ports);
                return;
            }                

            if (SelectedInputPort == null || SelectedInputPort.PortId != selectedInput.InPort)            
                SelectedInputPort = InputPorts.FirstOrDefault(port => port.PortId == selectedInput.InPort);
        }

        private void Communicator_OnRouterConnectionStateChanged(object sender, EventArgs<bool> e)
        {
            IsConnected = e.Value;
            if (e.Value)
                return;            

            Connect();           
        }        

        private void Communicator_OnRouterPortStateReceived(object sender, EventArgs<PortState[]> e)
        {
            foreach (var port in InputPorts)
                ((RouterPort)port).IsSignalPresent = e.Value?.FirstOrDefault(param => param.PortId == port.PortId)?.IsSignalPresent;
        }

        private void Communicator_OnInputPortChangeReceived(object sender, EventArgs<CrosspointInfo> e)
        {
            if (OutputPorts.Length == 0)
                return;
            var port = OutputPorts[0];
            var changedIn = e.Value.OutPort == port ? e.Value : null;
            if (changedIn == null)
                return;
            SelectedInputPort = InputPorts.FirstOrDefault(param => param.PortId == changedIn.InPort);
        }

        protected override void DoDispose()
        {
            _routerCommunicator.OnInputPortChangeReceived -= Communicator_OnInputPortChangeReceived;
            _routerCommunicator.OnRouterPortsStatesReceived -= Communicator_OnRouterPortStateReceived;
            _routerCommunicator.OnRouterConnectionStateChanged -= Communicator_OnRouterConnectionStateChanged;
            _routerCommunicator.Dispose();
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum VideoSwitchType
        {
            Nevion,
            BlackmagicSmartVideoHub,
            Atem,
            Ross,
            Unknown
        }
    }
}
