/*
    Copyright (c) 2010-2015 by Genstein and Jason Lautzenheiser.

    This file is (or was originally) part of Trizbort, the Interactive Fiction Mapper.

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Trizbort
{
  internal partial class RoomPropertiesDialog : Form
  {
    private const int HORIZONTAL_MARGIN = 2;
    private const int VERTICAL_MARGIN = 2;
    private const int WIDTH = 24;
    private static Tab m_lastViewedTab = Tab.Objects;
    private bool m_adjustingPosition;
    private const string NO_COLOR_SET = "No Color Set";

    public RoomPropertiesDialog(PropertiesStartType start)
    {
      InitializeComponent();

      // load regions control
      cboRegion.Items.Clear();
      foreach (var region in Settings.Regions.OrderBy(p => p.RegionName != Trizbort.Region.DefaultRegion).ThenBy(p => p.RegionName))
      {
        cboRegion.Items.Add(region.RegionName);
      }

      cboRegion.DrawMode = DrawMode.OwnerDrawFixed;
      cboRegion.DrawItem += RegionListBox_DrawItem;
      if (Settings.Regions.Count > 0)
        cboRegion.SelectedIndex = 0;

      if (start == PropertiesStartType.Region)
      {
        m_tabControl.SelectedTabIndex = (int) Tab.Regions;
        cboRegion.Select();
      }
      else
      {
        switch (m_lastViewedTab)
        {
          case Tab.Objects:
            m_tabControl.SelectedTabIndex = 0;
            break;
          case Tab.Description:
            m_tabControl.SelectedTabIndex = 1;
            break;
          case Tab.Colors:
            m_tabControl.SelectedTabIndex = 2;
            break;
          case Tab.Regions:
            m_tabControl.SelectedTabIndex = 3;
            break;
        }
        
        if (start == PropertiesStartType.RoomName)
          txtName.Focus();
      }
    }

    public string RoomName
    {
      get { return txtName.Text; }
      set { txtName.Text = value; }
    }

    public BorderDashStyle BorderStyle
    {
      get { return (BorderDashStyle)Enum.Parse(typeof(BorderDashStyle), cboBorderStyle.SelectedItem.ToString()); }
      set { cboBorderStyle.SelectedItem = value.ToString(); }
    }
    public string RoomSubTitle
    {
      get { return txtSubTitle.Text; }
      set { txtSubTitle.Text = value; }
    }

    public string Description
    {
      get { return m_descriptionTextBox.Text; }
      set { m_descriptionTextBox.Text = value; }
    }

    public bool IsDark
    {
      get { return m_isDarkCheckBox.Checked; }
      set { m_isDarkCheckBox.Checked = value; }
    }

    public string Objects
    {
      get { return txtObjects.Text; }
      set { txtObjects.Text = value; }
    }

    public string RoomRegion
    {
      get { return cboRegion.SelectedItem != null ? cboRegion.SelectedItem.ToString() : string.Empty; }
      set { cboRegion.SelectedItem = value; }
    }

    public CompassPoint ObjectsPosition
    {
      get
      {
        if (m_nCheckBox.Checked) return CompassPoint.North;
        if (m_sCheckBox.Checked) return CompassPoint.South;
        if (m_eCheckBox.Checked) return CompassPoint.East;
        if (m_wCheckBox.Checked) return CompassPoint.West;
        if (m_neCheckBox.Checked) return CompassPoint.NorthEast;
        if (m_nwCheckBox.Checked) return CompassPoint.NorthWest;
        if (m_seCheckBox.Checked) return CompassPoint.SouthEast;
        if (m_swCheckBox.Checked) return CompassPoint.SouthWest;
        return CompassPoint.WestSouthWest;
      }
      set
      {
        switch (value)
        {
          case CompassPoint.North:
            m_nCheckBox.Checked = true;
            break;
          case CompassPoint.South:
            m_sCheckBox.Checked = true;
            break;
          case CompassPoint.East:
            m_eCheckBox.Checked = true;
            break;
          case CompassPoint.West:
            m_wCheckBox.Checked = true;
            break;
          case CompassPoint.NorthEast:
            m_neCheckBox.Checked = true;
            break;
          case CompassPoint.NorthWest:
            m_nwCheckBox.Checked = true;
            break;
          case CompassPoint.SouthEast:
            m_seCheckBox.Checked = true;
            break;
          case CompassPoint.SouthWest:
            m_swCheckBox.Checked = true;
            break;
          default:
            m_cCheckBox.Checked = true;
            break;
        }
      }
    }

    // Added for Room specific colors
    public Color RoomFillColor
    {
      get
      {
        return m_roomFillTextBox.WatermarkText == NO_COLOR_SET ? Color.Transparent : m_roomFillTextBox.BackColor;
      }
      set
      {
        if (value == Color.Transparent)
        {
          m_roomFillTextBox.BackColor = Color.White;
          m_roomFillTextBox.WatermarkText = NO_COLOR_SET;
        }
        else
        {
          m_roomFillTextBox.BackColor = value;
          m_roomFillTextBox.WatermarkText = string.Empty;
        }
      }
    }

    // Added for Room specific colors
    public Color SecondFillColor
    {
      get
      {
        return m_secondFillTextBox.WatermarkText == NO_COLOR_SET ? Color.Transparent : m_secondFillTextBox.BackColor;
      }
      set
      {
        if (value == Color.Transparent)
        {
          m_secondFillTextBox.BackColor = Color.White;
          m_secondFillTextBox.WatermarkText = NO_COLOR_SET;
        }
        else
        {
          m_secondFillTextBox.BackColor = value;
          m_secondFillTextBox.WatermarkText = string.Empty;
        }
      }
    }

    // Added for Room specific colors
    public string SecondFillLocation
    {
      get
      {
        switch (comboBox1.SelectedIndex)
        {
          case 0:
            return "Bottom";
          case 1:
            return "BottomRight";
          case 2:
            return "BottomLeft";
          case 3:
            return "Left";
          case 4:
            return "Right";
          case 5:
            return "TopRight";
          case 6:
            return "TopLeft";
          case 7:
            return "Top";
          default:
            return "Bottom";
        }
      }
      set
      {
        switch (value)
        {
          case "Bottom":
            comboBox1.SelectedIndex = 0;
            break;
          case "BottomRight":
            comboBox1.SelectedIndex = 1;
            break;
          case "BottomLeft":
            comboBox1.SelectedIndex = 2;
            break;
          case "Left":
            comboBox1.SelectedIndex = 3;
            break;
          case "Right":
            comboBox1.SelectedIndex = 4;
            break;
          case "TopRight":
            comboBox1.SelectedIndex = 5;
            break;
          case "TopLeft":
            comboBox1.SelectedIndex = 6;
            break;
          case "Top":
            comboBox1.SelectedIndex = 7;
            break;
          default:
            comboBox1.SelectedIndex = 0;
            break;
        }
      }
    }

    // Added for Room specific colors
    public Color RoomBorderColor
    {
      get
      {
        return m_roomBorderTextBox.WatermarkText == NO_COLOR_SET ? Color.Transparent : m_roomBorderTextBox.BackColor;
      }
      set
      {
        if (value == Color.Transparent)
        {
          m_roomBorderTextBox.BackColor = Color.White;
          m_roomBorderTextBox.WatermarkText = NO_COLOR_SET;
        }
        else
        {
          m_roomBorderTextBox.BackColor = value;
          m_roomBorderTextBox.WatermarkText = string.Empty;
        }
      }
    }

    // Added for Room specific colors
    public Color RoomTextColor
    {
      get
      {
        return m_roomTextTextBox.WatermarkText == NO_COLOR_SET ? Color.Transparent : m_roomTextTextBox.BackColor;
      }
      set
      {
        if (value == Color.Transparent)
        {
          m_roomTextTextBox.BackColor = Color.White;
          m_roomTextTextBox.WatermarkText = NO_COLOR_SET;
        }
        else
        {
          m_roomTextTextBox.BackColor = value;
          m_roomTextTextBox.WatermarkText = string.Empty;
        }
      }
    }

    // Added for Room specific colors
    public Color ObjectTextColor
    {
      get
      {
        return m_objectTextTextBox.WatermarkText == NO_COLOR_SET ? Color.Transparent : m_objectTextTextBox.BackColor;
      }
      set
      {
        if (value == Color.Transparent)
        {
          m_objectTextTextBox.BackColor = Color.White;
          m_objectTextTextBox.WatermarkText = NO_COLOR_SET;
        }
        else
        {
          m_objectTextTextBox.BackColor = value;
          m_objectTextTextBox.WatermarkText = string.Empty;
        }
      }
    }

    private void RegionListBox_DrawItem(object sender, DrawItemEventArgs e)
    {
      using (var palette = new Palette())
      {
        e.DrawBackground();

        var colorBounds = new Rectangle(e.Bounds.Left + HORIZONTAL_MARGIN, e.Bounds.Top + VERTICAL_MARGIN, WIDTH, e.Bounds.Height - VERTICAL_MARGIN*2);
        var textBounds = new Rectangle(colorBounds.Right + HORIZONTAL_MARGIN, e.Bounds.Top, e.Bounds.Width - colorBounds.Width - HORIZONTAL_MARGIN*2, e.Bounds.Height);
        var firstOrDefault = Settings.Regions.FirstOrDefault(p => p.RegionName == cboRegion.Items[e.Index].ToString());
        if (firstOrDefault != null) e.Graphics.FillRectangle(palette.Brush(firstOrDefault.RColor), colorBounds);
        e.Graphics.DrawRectangle(palette.Pen(e.ForeColor, 0), colorBounds);
        e.Graphics.DrawString(cboRegion.Items[e.Index].ToString(), e.Font, palette.Brush(e.ForeColor), textBounds, StringFormats.Left);
      }
    }

    private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
    {
      switch (m_tabControl.SelectedTabIndex)
      {
        default:
          m_lastViewedTab = Tab.Objects;
          break;
        case 1:
          m_lastViewedTab = Tab.Description;
          break;
        case 2:
          m_lastViewedTab = Tab.Colors;
          break;
        case 3:
          m_lastViewedTab = Tab.Regions;
          break;
      }
    }

    private void PositionCheckBox_CheckedChanged(object sender, EventArgs e)
    {
      if (m_adjustingPosition)
        return;

      m_adjustingPosition = true;
      try
      {
        var checkBox = (CheckBox) sender;
        if (checkBox.Checked)
        {
          foreach (Control other in checkBox.Parent.Controls)
          {
            var box = other as CheckBox;
            if (box != null && other != checkBox)
            {
              box.Checked = false;
            }
          }
        }
        else
        {
          m_sCheckBox.Checked = true;
        }
      }
      finally
      {
        m_adjustingPosition = false;
      }
    }

    private void changeRoomFillColor()
    {
      if (tabColors.IsSelected) RoomFillColor = Colors.ShowColorDialog(RoomFillColor, this);
    }

    // Added for Room specific colors
    private void changeSecondFillColor()
    {
      if (tabColors.IsSelected) SecondFillColor = Colors.ShowColorDialog(SecondFillColor, this);
    }

    private void changeRoomTextColor()
    {
      if (tabColors.IsSelected) RoomTextColor = Colors.ShowColorDialog(RoomTextColor, this);
    }

    private void changeRoomBorderColor()
    {
      if (tabColors.IsSelected) RoomBorderColor = Colors.ShowColorDialog(RoomBorderColor, this);
    }

    private void changeObjectTextColor()
    {
      if (tabColors.IsSelected) ObjectTextColor = Colors.ShowColorDialog(ObjectTextColor, this);
    }

    private void m_changeLargeFontButton_Click(object sender, EventArgs e)
    {
      changeRoomFillColor();
    }

    // Added for Room specific colors
    private void m_changeSecondFillButton_Click(object sender, EventArgs e)
    {
      changeSecondFillColor();
    }

    // Added for Room specific colors
    private void button1_Click(object sender, EventArgs e)
    {
      changeRoomBorderColor();
    }

    // Added for Room specific colors
    private void button2_Click(object sender, EventArgs e)
    {
      changeRoomTextColor();
    }

    // Added for Room specific colors
    private void button3_Click(object sender, EventArgs e)
    {
      changeObjectTextColor();
    }

    private enum Tab
    {
      Objects, 
      Description,
      Colors,
      Regions
    }

    private void RoomPropertiesDialog_KeyUp(object sender, KeyEventArgs e)
    {
      if (e.Alt)
      {
        switch (e.KeyCode)
        {
          case Keys.Y:
            cboBorderStyle.Focus();
            break;

          case Keys.F:
            changeRoomFillColor();
            break;

          case Keys.S:
            changeSecondFillColor();
            break;

          case Keys.B:
            changeRoomBorderColor();
            break;

          case Keys.T:
            changeRoomTextColor();
            break;

          case Keys.J:
            changeObjectTextColor();
            break;

          case Keys.O:
            m_tabControl.SelectedTabIndex = (int) Tab.Objects;
            setObjectsTabFocus();
            break;

          case Keys.E:
            m_tabControl.SelectedTabIndex = (int) Tab.Description;
            setDescriptionTabFocus();
            break;

          case Keys.G:
            m_tabControl.SelectedTabIndex = (int) Tab.Regions;
            setRegionsTabFocus();
            break;

          case Keys.C:
            m_tabControl.SelectedTabIndex = (int) Tab.Colors;
            setColorsTabFocus();
            break;
        }
      }
    }

    private void setColorsTabFocus()
    {
      m_changeRoomFillButton.Focus();
    }

    private void setRegionsTabFocus()
    {
      cboRegion.Focus();
    }

    private void setDescriptionTabFocus()
    {
      m_descriptionTextBox.Focus();
      m_descriptionTextBox.SelectAll();
    }

    private void setObjectsTabFocus()
    {
      txtObjects.Focus();
    }


    private void m_tabControl_Enter(object sender, EventArgs e)
    {
      switch (m_tabControl.SelectedTabIndex)
      {
        case (int)Tab.Objects:
          setObjectsTabFocus();
          break;
        case (int)Tab.Description:
          setDescriptionTabFocus();
          break;
        case (int)Tab.Regions:
          setRegionsTabFocus();
          break;
        case (int)Tab.Colors:
          setColorsTabFocus();
          break;
      }
    }



    private void m_roomFillTextBox_ButtonCustomClick(object sender, EventArgs e)
    {
      RoomFillColor = Color.Transparent;
      m_changeRoomFillButton.Focus();
    }

    private void m_secondFillTextBox_ButtonCustomClick(object sender, EventArgs e)
    {
      SecondFillColor = Color.Transparent;
      m_changeSecondFillButton.Focus();
    }

    private void m_roomBorderTextBox_ButtonCustomClick(object sender, EventArgs e)
    {
      RoomBorderColor = Color.Transparent;
      m_changeRoomBorderButton.Focus();
    }

    private void m_roomTextTextBox_ButtonCustomClick(object sender, EventArgs e)
    {
      RoomTextColor = Color.Transparent;
      m_changeRoomTextButton.Focus();
    }

    private void m_objectTextTextBox_ButtonCustomClick(object sender, EventArgs e)
    {
      ObjectTextColor = Color.Transparent;
      m_changeObjectTextButton.Focus();
    }

    private void m_roomFillTextBox_DoubleClick(object sender, EventArgs e)
    {
      changeRoomFillColor();
    }

    private void m_secondFillTextBox_DoubleClick(object sender, EventArgs e)
    {
      changeSecondFillColor();
    }

    private void m_roomBorderTextBox_DoubleClick(object sender, EventArgs e)
    {
      changeRoomBorderColor();
    }

    private void m_roomTextTextBox_DoubleClick(object sender, EventArgs e)
    {
      changeRoomTextColor();
    }

    private void m_objectTextTextBox_DoubleClick(object sender, EventArgs e)
    {
      changeObjectTextColor();
    }

    private void m_roomFillTextBox_Enter(object sender, EventArgs e)
    {
      m_changeRoomFillButton.Focus();
    }

    private void m_secondFillTextBox_Enter(object sender, EventArgs e)
    {
      m_changeSecondFillButton.Focus();
    }

    private void m_roomTextTextBox_Enter(object sender, EventArgs e)
    {
      m_changeRoomTextButton.Focus();
    }

    private void m_objectTextTextBox_Enter(object sender, EventArgs e)
    {
      m_changeObjectTextButton.Focus();
    }

    private void m_roomBorderTextBox_Enter(object sender, EventArgs e)
    {
      m_changeRoomBorderButton.Focus();
    }

  }
}