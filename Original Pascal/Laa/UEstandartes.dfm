object FEstandartes: TFEstandartes
  Left = 359
  Top = 159
  BorderStyle = bsNone
  ClientHeight = 272
  ClientWidth = 480
  Color = clBlack
  Ctl3D = False
  Font.Charset = ANSI_CHARSET
  Font.Color = 12644596
  Font.Height = -15
  Font.Name = 'Times New Roman'
  Font.Style = [fsBold]
  KeyPreview = True
  OldCreateOrder = True
  Position = poScreenCenter
  Scaled = False
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnKeyDown = FormKeyDown
  PixelsPerInch = 96
  TextHeight = 17
  object lbColor: TLabel
    Left = 320
    Top = 92
    Width = 17
    Height = 19
    AutoSize = False
    Caption = '0'
    Transparent = True
  end
  object sb_color: TScrollBar
    Left = 80
    Top = 92
    Width = 230
    Height = 18
    Max = 9
    PageSize = 0
    TabOrder = 1
    OnChange = sb_colorChange
  end
  object sb_rojo: TScrollBar
    Left = 80
    Top = 124
    Width = 121
    Height = 18
    Max = 3
    PageSize = 0
    TabOrder = 2
    OnChange = sb_rojoChange
  end
  object sb_verde: TScrollBar
    Left = 80
    Top = 156
    Width = 121
    Height = 18
    Max = 3
    PageSize = 0
    TabOrder = 3
    OnChange = sb_verdeChange
  end
  object sb_azul: TScrollBar
    Left = 80
    Top = 188
    Width = 121
    Height = 18
    Max = 3
    PageSize = 0
    TabOrder = 4
    OnChange = sb_azulChange
  end
  object cb_Predef: TComboBox
    Left = 20
    Top = 48
    Width = 316
    Height = 25
    Style = csDropDownList
    Color = clBlack
    ItemHeight = 17
    TabOrder = 0
    OnChange = cb_PredefChange
  end
  object TimerAnimacion: TTimer
    Enabled = False
    Interval = 55
    OnTimer = GTimer1Timer
    Left = 260
    Top = 4
  end
end
