﻿// <eddie_source_header>
// This file is part of Eddie/AirVPN software.
// Copyright (C)2014-2016 AirVPN (support@airvpn.org) / https://airvpn.org )
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
using System.Linq;
//using Foundation;
//using AppKit;
using Foundation;
using AppKit;
using Eddie.Core;

namespace Eddie.UI.Cocoa.Osx
{
	public partial class WindowFrontMessageController : AppKit.NSWindowController
	{
		public Json Message;

		#region Constructors
		// Called when created from unmanaged code
		public WindowFrontMessageController(IntPtr handle) : base(handle)
		{
			Initialize();
		}
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public WindowFrontMessageController(NSCoder coder) : base(coder)
		{
			Initialize();
		}
		// Call to load from the XIB/NIB file
		public WindowFrontMessageController() : base("WindowFrontMessage")
		{
			Initialize();
		}
		// Shared initialization code
		void Initialize()
		{
		}
		#endregion
		//strongly typed window accessor
		public new WindowFrontMessage Window
		{
			get
			{
				return (WindowFrontMessage)base.Window;
			}
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			Window.Title = Constants.Name + " - " + LanguageManager.GetText("WindowsFrontMessageTitle");

			GuiUtils.SetButtonDefault(Window, CmdClose);

            TxtMessage.StringValue = Message["text"].Value as string;
			CmdClose.Title = LanguageManager.GetText("WindowsFrontMessageAccept");			

			if (Message.HasKey("link"))
            {
                CmdMore.Title = Message["link"].Value as string;
            }
            else
            {
				GuiUtils.SetHidden(CmdMore, true);
            }

			CmdClose.Activated += (object sender, EventArgs e) =>
			{
				Window.Close();
			};

			CmdMore.Activated += (object sender, EventArgs e) =>
			{
                //GuiUtils.OpenUrl(UiClient.Instance.Data["links"]["help"]["website"].Value as string);
                GuiUtils.OpenUrl(Message["url"].Value as string);
			};
		}
	}
}

