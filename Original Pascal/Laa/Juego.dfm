object JForm: TJForm
  Left = 900
  Top = 251
  Cursor = 1
  HorzScrollBar.Visible = False
  VertScrollBar.Visible = False
  BorderIcons = [biSystemMenu, biMinimize]
  BorderStyle = bsSingle
  ClientHeight = 683
  ClientWidth = 1004
  Color = clBlack
  Font.Charset = ANSI_CHARSET
  Font.Color = 10541248
  Font.Height = -15
  Font.Name = 'Times New Roman'
  Font.Style = []
  KeyPreview = True
  OldCreateOrder = True
  Position = poDefault
  Scaled = False
  OnActivate = FormActivate
  OnClose = FormClose
  OnCloseQuery = FormCloseQuery
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnKeyDown = FormKeyDown
  PixelsPerInch = 96
  TextHeight = 17
  object PBInterfaz: TPaintBox
    Left = 0
    Top = 592
    Width = 1280
    Height = 128
    Cursor = 1
    OnMouseDown = PBInterfazMouseDown
    OnMouseMove = PBInterfazMouseMove
    OnPaint = PBInterfazPaint
  end
  object PBMain: TPaintBox
    Left = 0
    Top = 0
    Width = 1280
    Height = 592
    OnMouseDown = PantallaMouseDown
    OnMouseMove = FormMouseMove
    OnPaint = PBMainPaint
  end
  object E_Identificador: TEdit
    Left = 300
    Top = 204
    Width = 150
    Height = 21
    AutoSize = False
    BorderStyle = bsNone
    Color = clBlack
    Font.Charset = ANSI_CHARSET
    Font.Color = 7190204
    Font.Height = -19
    Font.Name = 'Times New Roman'
    Font.Style = []
    MaxLength = 16
    ParentFont = False
    TabOrder = 1
    Visible = False
    OnKeyPress = EditKeyPress
  end
  object EditMensaje: TEdit
    Left = 432
    Top = 594
    Width = 400
    Height = 13
    TabStop = False
    AutoSelect = False
    AutoSize = False
    BorderStyle = bsNone
    Color = clBlack
    Font.Charset = ANSI_CHARSET
    Font.Color = clWhite
    Font.Height = -11
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    MaxLength = 127
    ParentFont = False
    TabOrder = 0
    Visible = False
    OnKeyPress = EditMensajeKeyPress
  end
  object E_contrasenna: TEdit
    Left = 316
    Top = 252
    Width = 128
    Height = 21
    AutoSize = False
    BorderStyle = bsNone
    Color = clBlack
    Font.Charset = ANSI_CHARSET
    Font.Color = 7190204
    Font.Height = -19
    Font.Name = 'Times New Roman'
    Font.Style = []
    MaxLength = 64
    ParentFont = False
    PasswordChar = '*'
    TabOrder = 3
    Visible = False
    OnKeyPress = EditKeyPress
  end
  object E_nombre: TEdit
    Left = 88
    Top = 150
    Width = 150
    Height = 21
    AutoSize = False
    BorderStyle = bsNone
    Color = clBlack
    Font.Charset = ANSI_CHARSET
    Font.Color = clWhite
    Font.Height = -19
    Font.Name = 'Times New Roman'
    Font.Style = []
    MaxLength = 16
    ParentFont = False
    TabOrder = 2
    Visible = False
    OnKeyPress = EditKeyPress
  end
  object E_confirmar: TEdit
    Left = 112
    Top = 314
    Width = 128
    Height = 21
    AutoSize = False
    BorderStyle = bsNone
    Color = clBlack
    Font.Charset = ANSI_CHARSET
    Font.Color = 7190204
    Font.Height = -19
    Font.Name = 'Times New Roman'
    Font.Style = []
    MaxLength = 64
    ParentFont = False
    PasswordChar = '*'
    TabOrder = 4
    Visible = False
    OnKeyPress = EditKeyPress
  end
end
