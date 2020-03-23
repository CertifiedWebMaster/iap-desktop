﻿using Google.Apis.Compute.v1.Data;
using Google.Solutions.CloudIap.IapDesktop.Application.Registry;
using Google.Solutions.CloudIap.IapDesktop.Application.Settings;
using Google.Solutions.CloudIap.IapDesktop.Settings;
using Google.Solutions.Compute;
using Google.Solutions.IapDesktop.Application.Adapters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Google.Solutions.CloudIap.IapDesktop.Settings.SettingsEditorWindow;

namespace Google.Solutions.CloudIap.IapDesktop.ProjectExplorer
{
    internal class CloudNode : TreeNode, IProjectExplorerCloudNode
    {
        private const int IconIndex = 0;

        public CloudNode()
            : base("Google Cloud", IconIndex, IconIndex)
        {
        }
    }

    internal abstract class InventoryNode : TreeNode, IProjectExplorerNode, ISettingsObject
    {
        private readonly InventoryNode parent;
        private readonly InventorySettingsBase settings;
        private readonly Action<InventorySettingsBase> saveSettings;

        public InventoryNode(
            string name, 
            int iconIndex, 
            InventorySettingsBase settings, 
            Action<InventorySettingsBase> saveSettings,
            InventoryNode parent)
            : base(name, iconIndex, iconIndex)
        {
            this.settings = settings;
            this.saveSettings = saveSettings;
            this.parent = parent;
        }

        public void SaveChanges()
        {
            this.saveSettings(this.settings);
        }

        internal static string ShortIdFromUrl(string url) => url.Substring(url.LastIndexOf("/") + 1);

        //---------------------------------------------------------------------
        // PropertyGrid-compatible settings properties.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [BrowsableSetting]
        [Category("Credentials")]
        [DisplayName("Username")]
        [Description("Windows logon username")]
        public string Username
        {
            get => this.settings.Username ?? this.parent?.Username;
            set => this.settings.Username = value;
        }

        public bool ShouldSerializeUsername() => this.settings.Username != null;


        [Browsable(true)]
        [BrowsableSetting]
        [Category("Credentials")]
        [DisplayName("Password")]
        [Description("Windows logon password")]
        [PasswordPropertyText(true)]
        public string Password
        {
            get => ShouldSerializePassword()
                ? new string('*', this.settings.Password.Length)
                : this.parent?.Password;
            set => this.settings.Password = SecureStringExtensions.FromClearText(value);
        }

        public bool ShouldSerializePassword() => this.settings.Password != null;


        [Browsable(true)]
        [BrowsableSetting]
        [Category("Credentials")]
        [DisplayName("Domain")]
        [Description("Windows logon domain")]
        public string Domain
        {
            get => this.settings.Domain ?? this.parent?.Domain;
            set => this.settings.Domain = value;
        }

        public bool ShouldSerializeDomain() => this.settings.Domain != null;


        [Browsable(true)]
        [BrowsableSetting]
        [Category("Display")]
        [DisplayName("Show connection bar")]
        [Description("Show connection bar in full-screen mode")]
        public RdpConnectionBarState ConnectionBar
        {
            get => ShouldSerializeConnectionBar()
                ? this.settings.ConnectionBar
                : (this.parent != null ? this.parent.ConnectionBar : RdpConnectionBarState._Default);
            set => this.settings.ConnectionBar = value;
        }

        public bool ShouldSerializeConnectionBar()
            => this.settings.ConnectionBar != RdpConnectionBarState._Default;


        [Browsable(true)]
        [BrowsableSetting]
        [Category("Display")]
        [DisplayName("Desktop size")]
        [Description("Size of remote desktop")]
        public RdpDesktopSize DesktopSize
        {
            get => ShouldSerializeDesktopSize()
                ? this.settings.DesktopSize
                : (this.parent != null ? this.parent.DesktopSize : RdpDesktopSize._Default);
            set => this.settings.DesktopSize = value;
        }

        public bool ShouldSerializeDesktopSize()
            => this.settings.DesktopSize != RdpDesktopSize._Default;


        [Browsable(true)]
        [BrowsableSetting]
        [Category("Display")]
        [DisplayName("Color depth")]
        [Description("Color depth of remote desktop")]
        public RdpColorDepth ColorDepth
        {
            get => ShouldSerializeColorDepth()
                ? this.settings.ColorDepth
                : (this.parent != null ? this.parent.ColorDepth : RdpColorDepth._Default);
            set => this.settings.ColorDepth = value;
        }

        public bool ShouldSerializeColorDepth()
            => this.settings.ColorDepth != RdpColorDepth._Default;


        [Browsable(true)]
        [BrowsableSetting]
        [Category("Connection")]
        [DisplayName("Server authentication")]
        [Description("Require server authentication when connecting")]
        public RdpAuthenticationLevel AuthenticationLevel
        {
            get => ShouldSerializeAuthenticationLevel()
                ? this.settings.AuthenticationLevel
                : (this.parent != null ? this.parent.AuthenticationLevel : RdpAuthenticationLevel._Default);
            set => this.settings.AuthenticationLevel = value;
        }

        public bool ShouldSerializeAuthenticationLevel()
            => this.settings.AuthenticationLevel != RdpAuthenticationLevel._Default;


        [Browsable(true)]
        [BrowsableSetting]
        [Category("Local resources")]
        [DisplayName("Redirect clipboard")]
        [Description("Allow clipboard contents to be shared with remote desktop")]
        public bool RedirectClipboard
        {
            get => ShouldSerializeRedirectClipboard()
                ? this.settings.RedirectClipboard
                : (this.parent != null ? this.parent.RedirectClipboard : true);
            set => this.settings.RedirectClipboard = value;
        }

        public bool ShouldSerializeRedirectClipboard()
            => !this.settings.RedirectClipboard;


        [Browsable(true)]
        [BrowsableSetting]
        [Category("Local resources")]
        [DisplayName("Audio mode")]
        [Description("Redirect audio when playing on server")]
        public RdpAudioMode AudioMode
        {
            get => ShouldSerializeAudioMode()
                ? this.settings.AudioMode
                : (this.parent != null ? this.parent.AudioMode : RdpAudioMode._Default);
            set => this.settings.AudioMode = value;
        }

        public bool ShouldSerializeAudioMode()
            => this.settings.AudioMode != RdpAudioMode._Default;
    }

    internal class ProjectNode : InventoryNode, IProjectExplorerProjectNode
    {
        private const int IconIndex = 1;

        private readonly InventorySettingsRepository settingsRepository;

        public string ProjectId => this.Text;

        public ProjectNode(InventorySettingsRepository settingsRepository, string projectId)
            : base(
                  projectId,
                  IconIndex,
                  settingsRepository.GetProjectSettings(projectId),
                  settings => settingsRepository.SetProjectSettings((ProjectSettings) settings),
                  null)
        {
            this.settingsRepository = settingsRepository;
        }

        public void Populate(IEnumerable<Instance> allInstances)
        {
            this.Nodes.Clear();

            // Narrow the list down to Windows instances - there is no point 
            // of adding Linux instanes to the list of servers.
            var instances = allInstances.Where(i => ComputeEngineAdapter.IsWindowsInstance(i));
            var zoneIds = instances.Select(i => InventoryNode.ShortIdFromUrl(i.Zone)).ToHashSet();

            foreach (var zoneId in zoneIds)
            {
                var zoneSettings = this.settingsRepository.GetZoneSettings(
                    this.ProjectId,
                    zoneId);
                var zoneNode = new ZoneNode(
                    zoneSettings, 
                    changedSettings => this.settingsRepository.SetZoneSettings(this.ProjectId, changedSettings),
                    this);

                var instancesInZone = instances
                    .Where(i => InventoryNode.ShortIdFromUrl(i.Zone) == zoneId)
                    .OrderBy(i => i.Name);

                foreach (var instance in instancesInZone)
                {
                    var instanceSettings = this.settingsRepository.GetVmInstanceSettings(
                        this.ProjectId,
                        instance.Name);
                    var instanceNode = new VmInstanceNode(
                        instance,
                        instanceSettings,
                        changedSettings => this.settingsRepository.SetVmInstanceSettings(this.ProjectId, changedSettings),
                        zoneNode);

                    zoneNode.Nodes.Add(instanceNode);
                }

                this.Nodes.Add(zoneNode);
                zoneNode.Expand();
            }

            Expand();
        }
    }

    internal class ZoneNode : InventoryNode, IProjectExplorerZoneNode
    {
        private const int IconIndex = 3;

        public string ProjectId => ((ProjectNode)this.Parent).ProjectId;
        public string ZoneId => this.Text;

        public ZoneNode(
            ZoneSettings settings, 
            Action<ZoneSettings> saveSettings,
            ProjectNode parent)
            : base(
                  settings.ZoneId,
                  IconIndex,
                  settings,
                  changedSettings => saveSettings((ZoneSettings)changedSettings),
                  parent)
        {
        }
    }

    internal class VmInstanceNode : InventoryNode, IProjectExplorerVmInstanceNode
    {
        private const int IconIndex = 4;
        private const int ActiveIconIndex = 4;

        public VmInstanceReference Reference 
            => new VmInstanceReference(this.ProjectId, this.ZoneId, this.InstanceName);

        public string ProjectId => ((ZoneNode)this.Parent).ProjectId;
        public string ZoneId => ((ZoneNode)this.Parent).ZoneId;

        private static string InternalIpFromInstance(Instance instance)
        {
            if (instance == null)
            {
                return null;
            }

            return instance
                .NetworkInterfaces
                .EnsureNotNull()
                .Select(nic => nic.NetworkIP)
                .FirstOrDefault();
        }
        private static string ExternalIpFromInstance(Instance instance)
        {
            if (instance == null)
            {
                return null;
            }

            return instance
                .NetworkInterfaces
                .EnsureNotNull()
                .Where(nic => nic.AccessConfigs != null)
                .SelectMany(nic => nic.AccessConfigs)
                .EnsureNotNull()
                .Where(accessConfig => accessConfig.Type == "ONE_TO_ONE_NAT")
                .Select(accessConfig => accessConfig.NatIP)
                .FirstOrDefault();
        }

        public VmInstanceNode(
            Instance instance, 
            VmInstanceSettings settings,
            Action<VmInstanceSettings> saveSettings, 
            ZoneNode parent)
            : base(
                  settings.InstanceName,
                  IconIndex,
                  settings,
                  changedSettings => saveSettings((VmInstanceSettings)changedSettings),
                  parent)
        {
            this.InstanceId = instance.Id.Value;
            this.Status = instance.Status;
            this.Hostname = instance.Hostname;
            this.MachineType = InventoryNode.ShortIdFromUrl(instance.MachineType);
            this.Tags = instance.Tags != null && instance.Tags.Items != null
                ? string.Join(", ", instance.Tags.Items) : null;
            this.InternalIp = InternalIpFromInstance(instance);
            this.ExternalIp = ExternalIpFromInstance(instance);
        }

        [Browsable(true)]
        [BrowsableSetting]
        [Category("VM Instance")]
        [DisplayName("Name")]
        public string InstanceName => this.Text;

        [Browsable(true)]
        [BrowsableSetting]
        [Category("VM Instance")]
        [DisplayName("ID")]
        public ulong InstanceId { get; }

        [Browsable(true)]
        [BrowsableSetting]
        [Category("VM Instance")]
        [DisplayName("Status")]
        public string Status { get; }

        [Browsable(true)]
        [BrowsableSetting]
        [Category("VM Instance")]
        [DisplayName("Hostname")]
        public string Hostname { get; }

        [Browsable(true)]
        [BrowsableSetting]
        [Category("VM Instance")]
        [DisplayName("Machine type")]
        public string MachineType { get; }

        [Browsable(true)]
        [BrowsableSetting]
        [Category("VM Instance")]
        [DisplayName("Network tags")]
        public string Tags { get; }

        [Browsable(true)]
        [BrowsableSetting]
        [Category("VM Instance")]
        [DisplayName("IP address (internal)")]
        public string InternalIp { get; }

        [Browsable(true)]
        [BrowsableSetting]
        [Category("VM Instance")]
        [DisplayName("IP address (external)")]
        public string ExternalIp { get; }
    }
}
