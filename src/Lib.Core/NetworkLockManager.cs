﻿// <eddie_source_header>
// This file is part of Eddie/AirVPN software.
// Copyright (C)2014-2019 AirVPN (support@airvpn.org) / https://airvpn.org
//
// Eddie is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Eddie is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Eddie. If not, see <http://www.gnu.org/licenses/>.
// </eddie_source_header>

using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Xml;

namespace Eddie.Core
{
	public class NetworkLockManager
	{
		public List<NetworkLockPlugin> Modes = new List<NetworkLockPlugin>();

		private NetworkLockPlugin m_current = null;

		public NetworkLockManager()
		{
		}

		public void Init()
		{
			Platform.Instance.OnNetworkLockManagerInit();
			//Modes.Add(new NetworkLocks.RoutingTable());
		}

		public void AddPlugin(NetworkLockPlugin plugin)
		{
			//Engine.Instance.Storage.SetDefaultBool("advanced.netlock." + plugin.GetCode() + ".enabled", true, "");
			if (plugin.GetSupport())
			{
				plugin.Init();
				Modes.Add(plugin);
			}
		}

		public bool IsActive()
		{
			return m_current != null;
		}

		public NetworkLockPlugin GetActive()
		{
			return m_current;
		}

		public bool CanEnabled()
		{
			if (IsActive()) // This because if is active, the button need anyway to be showed to deactivated it.
				return true;

			if (Modes.Count == 0) // 2.10.1
				return false;

			if (Engine.Instance.ProfileOptions.GetLower("netlock.mode") == "none")
				return false;

			if (Engine.Instance.ProfileOptions.GetLower("network.ipv4.mode") == "out")
				return false;

			if (Engine.Instance.ProfileOptions.GetLower("network.ipv6.mode") == "out")
				return false;

			return true;
		}

		public bool Activation()
		{
			bool result = false;

			try
			{
				if (m_current != null)
					throw new Exception(LanguageManager.GetText("NetworkLockUnexpectedAlreadyActive"));

				NetworkLockPlugin nextCurrent = null;

				string requestedMode = Engine.Instance.ProfileOptions.GetLower("netlock.mode");
				if (requestedMode == "auto")
					requestedMode = Platform.Instance.OnNetworkLockRecommendedMode();

				if (requestedMode != "none")
				{
					foreach (NetworkLockPlugin plugin in Modes)
					{
						if (plugin.GetSupport())
						{
							if (requestedMode == "")
								requestedMode = plugin.GetCode();

							if (requestedMode == plugin.GetCode())
							{
								nextCurrent = plugin;
								break;
							}
						}
					}
				}

				if (nextCurrent == null)
				{
					Engine.Instance.Logs.Log(LogType.Fatal, LanguageManager.GetText("NetworkLockNoMode"));
				}
				else
				{
					string message = LanguageManager.GetText("NetworkLockActivation") + " - " + nextCurrent.GetName();
					Engine.Instance.WaitMessageSet(message, false);
					Engine.Instance.Logs.Log(LogType.InfoImportant, message);

					// This is not useless: resolve hostnames (available later as cache) before a possible lock of DNS server.
					nextCurrent.GetIpsAllowlistOutgoing(true);

					nextCurrent.Activation();

					m_current = nextCurrent;

					result = true;
				}
			}
			catch (Exception ex)
			{
				Engine.Instance.Logs.Log(LogType.Fatal, ex);
			}

			Recovery.Save();

			return result;
		}

		public void Deactivation(bool onExit)
		{
			if (m_current != null)
			{
				if (onExit == false)
				{
					Engine.Instance.WaitMessageSet(LanguageManager.GetText("NetworkLockDeactivation"), false);
					Engine.Instance.Logs.Log(LogType.InfoImportant, LanguageManager.GetText("NetworkLockDeactivation"));
				}
				else
					Engine.Instance.Logs.Log(LogType.Verbose, LanguageManager.GetText("NetworkLockDeactivation"));

				try
				{
					m_current.Deactivation();
				}
				catch (Exception ex)
				{
					Engine.Instance.Logs.Log(ex);
				}

				m_current = null;
			}

			Recovery.Save();
		}

		public bool IsDnsResolutionAvailable(string host)
		{
			if (m_current == null)
				return true;
			else
				return m_current.IsDnsResolutionAvailable(host);
		}

		public void OnUpdateIps()
		{
			if (m_current != null)
			{
				try
				{
					m_current.OnUpdateIps();
					Recovery.Save();
				}
				catch (Exception ex)
				{
					Engine.Instance.Logs.Log(ex);
				}
			}
		}

		public virtual void OnVpnEstablished()
		{
			if (m_current != null)
			{
				try
				{
					m_current.OnVpnEstablished();
					Recovery.Save();
				}
				catch (Exception ex)
				{
					Engine.Instance.Logs.Log(ex);
				}
			}
		}

		public virtual void OnVpnDisconnected()
		{
			if (m_current != null)
			{
				try
				{
					m_current.OnVpnDisconnected();
					Recovery.Save();
				}
				catch (Exception ex)
				{
					Engine.Instance.Logs.Log(ex);
				}
			}
		}

		public void OnRecoveryLoad(XmlElement root)
		{
			try
			{
				if (m_current != null)
					throw new Exception(LanguageManager.GetText("NetworkLockRecoveryWhenActive"));

				XmlElement node = root.GetFirstElementByTagName("netlock");
				if (node != null)
				{
					string code = node.GetAttribute("mode");

					foreach (NetworkLockPlugin lockPlugin in Engine.Instance.NetworkLockManager.Modes)
					{
						if (lockPlugin.GetCode() == code)
						{
							m_current = lockPlugin;
							break;
						}
					}

					if (m_current != null)
						m_current.OnRecoveryLoad(node);
					else
						Engine.Instance.Logs.Log(LogType.Warning, LanguageManager.GetText("NetworkLockRecoveryUnknownMode"));

					Deactivation(false);
				}
			}
			catch (Exception ex)
			{
				Engine.Instance.Logs.Log(ex);
			}
		}

		public void OnRecoverySave(XmlElement root)
		{
			try
			{
				if (m_current != null)
				{
					XmlElement node = (XmlElement)root.AppendChild(root.OwnerDocument.CreateElement("netlock"));

					node.SetAttribute("mode", m_current.GetCode());

					m_current.OnRecoverySave(node);
				}
			}
			catch (Exception ex)
			{
				Engine.Instance.Logs.Log(ex);
			}
		}

		public virtual void AllowProgram(string path)
		{
			if (m_current != null)
				m_current.AllowProgram(path);
		}

		public virtual void DeallowProgram(string path)
		{
			if (m_current != null)
				m_current.DeallowProgram(path);
		}

		public virtual void AllowInterface(NetworkInterface networkInterface)
		{
			if (m_current != null)
				m_current.AllowInterface(networkInterface);
		}

		public virtual void DeallowInterface(NetworkInterface networkInterface)
		{
			if (m_current != null)
				m_current.DeallowInterface(networkInterface);
		}
	}
}
