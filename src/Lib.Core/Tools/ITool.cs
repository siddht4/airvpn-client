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

namespace Eddie.Core.Tools
{
	public class ITool
	{
		public string Code = "";
		public string Path = ""; // TOFIX: must be private, use GetPath everywhere
		public string Version = "";
		public string Location = "missing";

		public void UpdatePath()
		{
			try
			{
				OnUpdatePath();
				OnUpdateVersion();
				OnNormalizeVersion();
			}
			catch (Exception ex)
			{
				Engine.Instance.Logs.Log(LogType.Verbose, LanguageManager.GetText("BundleExecutableError", Code, Path));
				Engine.Instance.Logs.Log(LogType.Verbose, ex.Message);
				Engine.Instance.Logs.Log(LogType.Verbose, Platform.Instance.GetExecutableReport(Path));

				Path = "";
				Version = "";
				Location = "missing";
			}
		}

		public virtual bool Available()
		{
			return (Version != "");
		}

		public virtual void OnUpdatePath()
		{
			string filename = GetFileName();
			FindResource(filename);
		}

		public virtual void OnUpdateVersion()
		{
			string finalPath = GetPath();
			if (finalPath != "")
			{
				string argument = GetVersionArgument();
				Version = SystemExec.Exec1(finalPath, argument).Trim();
				if ((Version.StartsWithInv("Error:")) || (Version == ""))
					throw new Exception(Version);
			}
		}

		public virtual void OnNormalizeVersion()
		{
			if (Version.IndexOfInv("\n") != -1) // First line only
				Version = Version.Substring(0, Version.IndexOfInv("\n"));
		}

		public virtual void ExceptionIfRequired()
		{

		}

		public virtual string GetFileName()
		{
			return Code;
		}

		public virtual string GetVersionArgument()
		{
			return "";
		}

		public string GetPath()
		{
			if (Location == "custom")
				return Path;
			if (Location == "system")
				return Path;

			/*
			string realHash = ComputeHash();
			if (realHash != Hash)
			{
				//Engine.Instance.Logs.Log(LogType.Error, MessagesFormatter.Format("Unexpected hash of executable '{1}': {2} vs {3}", Path, realHash, Hash));
				return "";
			}
			*/

			if (Platform.Instance.FileExists(Path))
				Platform.Instance.FileEnsureExecutablePermission(Path);

			return Path;
		}

		public bool VersionUnder(string v)
		{
			return Version.VersionUnder(v);
		}

		public bool VersionAboveOrEqual(string v)
		{
			return Version.VersionAboveOrEqual(v);
		}

		public string GetVersionDesc()
		{
			if (Version == "")
				return LanguageManager.GetText("NotAvailable");
			else
				return Version + " (" + Path + ")";
		}

		public void FindResource(string filename)
		{
			string searchLocation = "";
			string customLocationOption = "tools." + Code + ".location"; // "" (auto), "bundle", "system", "custom";
			if (Engine.Instance.ProfileOptions.Exists(customLocationOption))
				searchLocation = Engine.Instance.ProfileOptions.Get(customLocationOption);

			string customPathOption = "tools." + Code + ".path";
			if (Engine.Instance.ProfileOptions.Exists(customPathOption))
			{
				string path = Engine.Instance.ProfileOptions.Get(customPathOption).Trim();
				if (path != "")
				{
					path = Platform.Instance.NormalizePath(path);
					if (Platform.Instance.FileExists(path))
					{
						Path = path;
						Location = "custom";
						return;
					}
				}
			}

			// Tools path
			{
				string path = Platform.Instance.NormalizePath(Engine.Instance.GetPathTools() + "/" + filename);
				if (Platform.Instance.FileExists(path))
				{
					Path = path;
					Location = "bundle";
					return;
				}
			}

			// System
			List<string> names = new List<string>();
			if (filename == "stunnel")
			{
				// For example, under Ubuntu is 'stunnel4', under Fedora is 'stunnel'.
				names.Add("stunnel5");
				names.Add("stunnel4");
			}
			names.Add(filename);

			foreach (string fileNameAlt in names)
			{
				string pathBin = Platform.Instance.LocateExecutable(fileNameAlt);
				if (pathBin != "")
				{
					Path = pathBin;
					Location = "system";
					break;
				}
			}
		}
	}
}
