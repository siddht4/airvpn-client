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
using System.Drawing;
using System.Windows.Forms;

#pragma warning disable CA1416 // Windows only

namespace Eddie.Forms.Skin
{
	public class ListView : System.Windows.Forms.ListView
	{
		public int ImageSpace = 2;
		public int TextSpace = 2;

		public System.Resources.ResourceManager ResourceManager;
		public String ImageIconResourcePrefix = "";
		public String ImageStateResourcePrefix = "";
		public ImageList ImageListIcon = null;
		public ImageList ImageListState = null;

		private int m_sortColumn = -1;

		private bool m_supportResizeColumn = true;

		private Dictionary<String, Bitmap> m_imageResourceCache = new Dictionary<String, Bitmap>();

		public ListView()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

			UpdateStyles();

			FullRowSelect = true;
			GridLines = false;
			HideSelection = false;

			ColumnClick += new ColumnClickEventHandler(OnListViewColumnClick);

			OwnerDraw = true;
			this.DrawColumnHeader += new DrawListViewColumnHeaderEventHandler(OnListViewDrawColumnHeader);
			this.DrawSubItem += new DrawListViewSubItemEventHandler(OnListViewDrawSubItem);
		}

		public static void WorkaroundMonoListViewHeadersBug(System.Windows.Forms.Control c)
		{
			if (c is ListView)
			{
				// Unresolved mono bug: https://bugzilla.novell.com/show_bug.cgi?id=620618
				// Workaround.
				ListView l = (c as ListView);
				l.Columns.Insert(l.Columns.Count, "");
				l.Columns.RemoveAt(l.Columns.Count - 1);
			}
			else
			{
				foreach (Control c2 in c.Controls)
					WorkaroundMonoListViewHeadersBug(c2);
			}

		}

		public Image GetResourceImage(string name)
		{
			// Accessing Properties.Resources.xxx is a lot cpu extensive, probably conversions every time. We cache image resources.
			if (m_imageResourceCache.ContainsKey(name))
				return m_imageResourceCache[name];
			else
			{
				Bitmap i = (Bitmap)ResourceManager.GetObject(name);
				m_imageResourceCache[name] = i;
				return i;
			}
		}

		public void ResizeColumnsAuto()
		{
			for (int c = 0; c < Columns.Count; c++)
			{
				ResizeColumnAuto(c);
			}
		}

		public void ResizeColumnAuto(int column)
		{
			if (m_supportResizeColumn == false)
				return;

			// It ignore Image if present, .Net bug.
			//AutoResizeColumn(column, ColumnHeaderAutoResizeStyle.ColumnContent);

			Graphics g = this.CreateGraphics();

			int minWidth = SkinUtils.GetFontSize(g, Font, Columns[column].Text).Width + 10;

			foreach (ListViewItem item in Items)
			{
				if (column >= item.SubItems.Count)
					continue;

				string t = "";
				if (column == 0)
					t = item.Text;
				else
					t = item.SubItems[column].Text;
				Size s = SkinUtils.GetFontSize(g, Font, t);
				if (minWidth == -1)
					minWidth = s.Width;
				else if (minWidth < s.Width)
					minWidth = s.Width;
			}
			g.Dispose();

			if (Items.Count > 0)
				if ((ImageIconResourcePrefix != "") || (SmallImageList != null))
					minWidth += GetItemRect(0).Height * 2;

			minWidth += Convert.ToInt32(minWidth * 0.2); // 20% margin

			Columns[column].Width = minWidth;
		}

		public void ResizeColumnMax(int column)
		{
			if (m_supportResizeColumn == false)
				return;

			Columns[column].Width = 6000;
		}

		public void ResizeColumnString(int column, int charsLen)
		{
			if (m_supportResizeColumn == false)
				return;

			ResizeColumnString(column, new string('W', charsLen));
		}

		public void ResizeColumnString(int column, string text)
		{
			if (m_supportResizeColumn == false)
				return;

			Graphics g = this.CreateGraphics();
			Size s = SkinUtils.GetFontSize(g, Font, text);
			g.Dispose();

			s.Width += Convert.ToInt32(s.Width * 0.1);

			Columns[column].Width = s.Width;
		}

		public string GetUserPrefs()
		{
			string t = "";
			t += this.m_sortColumn + "," + (this.Sorting == SortOrder.Ascending ? "A" : "D") + ";";
			foreach (System.Windows.Forms.ColumnHeader col in Columns)
			{
				t += col.DisplayIndex + "," + col.Width + ";";
			}
			return t;
		}

		public void SetUserPrefs(string value)
		{
			if (value.Trim() == "")
				return;

			string[] parts = value.Split(';');
			if (parts.Length >= 1)
			{
				string[] sortParts = parts[0].Split(',');
				if (sortParts.Length == 2)
				{
					if (HeaderStyle == ColumnHeaderStyle.Clickable)
					{
						int sortColumn = Convert.ToInt32(sortParts[0]);
						string sortDirection = sortParts[1];
						SetSort(sortColumn, sortDirection == "A" ? SortOrder.Ascending : SortOrder.Descending);
					}
				}

				int colIndex = 0;
				foreach (System.Windows.Forms.ColumnHeader col in Columns)
				{
					if (parts.Length > colIndex + 1)
					{
						string[] colParts = parts[colIndex + 1].Split(',');
						if (colParts.Length == 2)
						{
							int displayIndex = Convert.ToInt32(colParts[0]);
							int width = Convert.ToInt32(colParts[1]);

							col.DisplayIndex = displayIndex;
							col.Width = width;
						}
					}
					colIndex++;
				}
			}
		}

		public void DrawSubItemBackground(DrawListViewSubItemEventArgs e)
		{
			if (this.Visible == false)
				return;

			Rectangle r = e.Bounds;

			Brush b;

			if (this.Enabled == false)
				b = SkinForm.Skin.BackDisabledBrush;
			else
			{
				if (e.Item.Focused)
					b = SkinForm.Skin.ListViewFocusedBackBrush;
				else if (e.Item.Selected)
					b = SkinForm.Skin.ListViewSelectedBackBrush;
				else
				{
					b = OnSubItemBrush(e);

					if (b == null)
					{
						if (e.ItemIndex % 2 == 0)
							b = SkinForm.Skin.ListViewNormalBackBrush;
						else
							b = SkinForm.Skin.ListViewNormal2BackBrush;
					}
				}
			}

			SkinForm.FillRectangle(e.Graphics, b, r);
			SkinForm.DrawRectangle(e.Graphics, SkinForm.Skin.ListViewGridPen, r);

		}

		public virtual bool GetDrawSubItemFull(int columnIndex)
		{
			return true;
		}

		public virtual Brush OnSubItemBrush(DrawListViewSubItemEventArgs e)
		{
			return null;
		}

		public virtual void OnListViewDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
		{
			e.DrawDefault = true;
		}

		public virtual void OnListViewDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
		{
			e.DrawDefault = false;

			if (e.Item.ListView == null)
				return;

			SkinForm.Skin.GraphicsCommon(e.Graphics);

			DrawSubItemBackground(e);

			if (GetDrawSubItemFull(e.ColumnIndex) == false)
				return;

			if (e.ColumnIndex == 0)
			{
				Rectangle r = e.Bounds;
				r.Height--;

				Image imageState = null;
				Image imageIcon = null;

				if (ImageStateResourcePrefix != "")
				{
					imageState = GetResourceImage(ImageStateResourcePrefix + e.Item.StateImageIndex.ToString());
					if (imageState != null)
					{
						int imageWidth = r.Height;

						Rectangle rImage = r;
						rImage.X += ImageSpace;
						rImage.Width = imageWidth;

						SkinForm.DrawImageContain(e.Graphics, imageState, rImage, 20);
						//e.Graphics.DrawImage(imageState, rImage);

						r.X += (imageWidth + ImageSpace);
						r.Width -= (imageWidth + ImageSpace);
					}
				}

				if (ImageIconResourcePrefix != "")
				{
					imageIcon = GetResourceImage(ImageIconResourcePrefix + e.Item.ImageKey);
				}

				/*
				if ((imageIcon == null) && (ImageListIcon != null) && (ImageListIcon.Images.ContainsKey(e.Item.ImageKey)))
					imageIcon = ImageListIcon.Images[e.Item.ImageKey];
				else if ((imageIcon == null) && (e.Item.ListView != null) && (e.Item.ListView.LargeImageList != null) && (e.Item.ListView.LargeImageList.Images.ContainsKey(e.Item.ImageKey)))
					imageIcon = e.Item.ListView.LargeImageList.Images[e.Item.ImageKey];
				else if ((imageIcon == null) && (e.Item.ListView != null) && (e.Item.ListView.SmallImageList != null) && (e.Item.ListView.SmallImageList.Images.ContainsKey(e.Item.ImageKey)))
					imageIcon = e.Item.ListView.SmallImageList.Images[e.Item.ImageKey];				
					*/
				if (imageIcon != null)
				{
					//int imageWidth = r.Height;
					int imageHeight = r.Height;
					int imageWidth = (imageHeight * imageIcon.Width) / imageIcon.Height;

					Rectangle rImage = r;
					rImage.X += ImageSpace;
					rImage.Width = imageWidth;

					//e.Graphics.DrawImage(imageIcon, rImage);
					SkinForm.DrawImageContain(e.Graphics, imageIcon, rImage, 20);

					r.X += (imageWidth + ImageSpace);
					r.Width -= (imageWidth + ImageSpace);
				}


				r.X += TextSpace;
				r.Width -= TextSpace;

				SkinForm.DrawString(e.Graphics, e.Item.Text, e.Item.Font, SkinForm.Skin.ForeBrush, r, SkinUtils.StringFormatLeftMiddleNoWrap);

			}
			else
			{
				Rectangle r = e.Bounds;
				r.Height--;

				r.X += TextSpace;
				r.Width -= TextSpace;

				StringFormat stringFormat = SkinUtils.StringFormatLeftMiddleNoWrap;
				if (e.Item.ListView.Columns[e.ColumnIndex].TextAlign == HorizontalAlignment.Center)
					stringFormat = SkinUtils.StringFormatCenterMiddleNoWrap;
				else if (e.Item.ListView.Columns[e.ColumnIndex].TextAlign == HorizontalAlignment.Right)
					stringFormat = SkinUtils.StringFormatRightMiddleNoWrap;
				SkinForm.DrawString(e.Graphics, e.Item.SubItems[e.ColumnIndex].Text, e.Item.Font, SkinForm.Skin.ForeBrush, r, stringFormat);
			}
		}

		public virtual void OnListViewColumnClick(object sender, ColumnClickEventArgs e)
		{
			if (HeaderStyle == System.Windows.Forms.ColumnHeaderStyle.Nonclickable) // Mono workaround
				return;

			if (e.Column == m_sortColumn)
			{
				// Determine what the last sort order was and change it.
				if (Sorting == SortOrder.Ascending)
					Sorting = SortOrder.Descending;
				else
					Sorting = SortOrder.Ascending;
			}

			SetSort(e.Column, Sorting);
		}

		public void SetSort(int col, SortOrder order)
		{
			m_sortColumn = col;
			Sorting = order;

			ListViewItemSorter = new ListViewItemComparer(m_sortColumn, Sorting);

			Sort();
		}

		public virtual int OnSortItem(int col, SortOrder order, ListViewItem pi1, ListViewItem pi2)
		{
			int returnVal = -1;
			returnVal = String.Compare(pi1.SubItems[col].Text, pi2.SubItems[col].Text);
			// Determine whether the sort order is descending.
			if (order == SortOrder.Descending)
				// Invert the value returned by String.Compare.
				returnVal *= -1;
			return returnVal;
		}
	}
}

#pragma warning restore CA1416 // Windows only

