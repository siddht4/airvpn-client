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

using Eddie.Core;
using Eddie.Forms.Skin;
using System;
using System.Collections.Generic;

namespace Eddie.Forms.Forms
{
	public partial class WindowProviderAdd : Eddie.Forms.Skin.SkinForm
	{
		public String Provider;

		private List<string> m_choices = new List<string>();

		public WindowProviderAdd()
		{
			OnPreInitializeComponent();
			InitializeComponent();
			OnInitializeComponent();
		}

		public override void OnInitializeComponent()
		{
			base.OnInitializeComponent();
		}

		public override void OnApplySkin()
		{
			base.OnApplySkin();

			SkinUtils.FixHeightVs(lblProvider, cboProvider);
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			CommonInit(LanguageManager.GetText("WindowsProviderAddTitle"));

			Json jProvidersAdd = Engine.Instance.ProvidersManager.GetDataAddProviders();

			foreach (Json jProvider in jProvidersAdd.GetArray())
			{
				string code = jProvider["code"].ValueString;
				string t = jProvider["title"].ValueString;
				t += " - " + jProvider["subtitle"].ValueString;
				t += " - " + jProvider["href"].ValueString;
				cboProvider.Items.Add(t);
				m_choices.Add(code);
			}

			if (cboProvider.Items.Count > 0)
				cboProvider.SelectedIndex = 0;

			EnableIde();
		}

		private void EnableIde()
		{
			cmdOk.Enabled = ((cboProvider.Items.Count > 0) && (cboProvider.SelectedIndex != -1));
		}

		private void cmdOk_Click(object sender, EventArgs e)
		{
			Provider = m_choices[cboProvider.SelectedIndex];
		}
	}
}
