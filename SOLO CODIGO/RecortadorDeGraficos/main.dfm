object Form1: TForm1
  Left = 1278
  Top = 265
  Width = 551
  Height = 493
  Caption = 'Recorta zonas no usadas o transparentes de im'#225'genes'
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -11
  Font.Name = 'MS Sans Serif'
  Font.Style = []
  Menu = MainMenu1
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  PixelsPerInch = 96
  TextHeight = 13
  object Panel2: TPanel
    Left = 0
    Top = 29
    Width = 535
    Height = 402
    Align = alClient
    BevelOuter = bvLowered
    TabOrder = 1
    object Pantalla: TPaintBox
      Left = 1
      Top = 1
      Width = 533
      Height = 400
      Align = alClient
      Color = clBlack
      ParentColor = False
      OnPaint = PantallaPaint
    end
  end
  object Panel1: TPanel
    Left = 0
    Top = 0
    Width = 535
    Height = 29
    Align = alTop
    TabOrder = 0
    object ColorTrans: TLabel
      Left = 108
      Top = 4
      Width = 45
      Height = 21
      AutoSize = False
      Color = 2621440
      ParentColor = False
      OnClick = ColorTransClick
    end
    object Label1: TLabel
      Left = 324
      Top = 7
      Width = 36
      Height = 13
      Hint = 'Recortar si la diferencia es menor'
      Caption = 'Umbral:'
      ParentShowHint = False
      ShowHint = True
    end
    object Button2: TButton
      Left = 4
      Top = 4
      Width = 97
      Height = 21
      Caption = 'Color transparente:'
      TabOrder = 0
      OnClick = Button2Click
    end
    object CB_autoColor: TCheckBox
      Left = 164
      Top = 6
      Width = 125
      Height = 17
      Hint = 'El color del pixel superior izquierdo.'
      Caption = 'Color autom'#225'tico (0,0)'
      Checked = True
      ParentShowHint = False
      ShowHint = True
      State = cbChecked
      TabOrder = 1
    end
    object E_Umbral: TEdit
      Left = 364
      Top = 4
      Width = 29
      Height = 21
      TabOrder = 2
      Text = '400'
    end
    object CB_escala: TCheckBox
      Left = 424
      Top = 6
      Width = 105
      Height = 17
      Hint = 'Asegura que el color negro pueda usarse como transparente'
      Caption = 'Ajustar a [8..255]'
      Checked = True
      ParentShowHint = False
      ShowHint = True
      State = cbChecked
      TabOrder = 3
    end
  end
  object MainMenu1: TMainMenu
    Left = 128
    Top = 344
    object Exit1: TMenuItem
      Caption = 'Archivo'
      object Procesarimgenes1: TMenuItem
        Caption = 'Abrir imagen'
        OnClick = AbrirArchivo
      end
      object N2: TMenuItem
        Caption = '-'
      end
      object Guardarimagenoptimizada1: TMenuItem
        Caption = 'Guardar imagen'
        OnClick = Guardarimagenoptimizada1Click
      end
      object N1: TMenuItem
        Caption = '-'
      end
      object Salir1: TMenuItem
        Caption = 'Salir'
        OnClick = Salir1Click
      end
    end
    object Herramientas1: TMenuItem
      Caption = 'Herramientas'
      object Pintardemagenta1: TMenuItem
        Caption = 'Optimizar (Corta partes no usadas)'
        OnClick = Pintardemagenta1Click
      end
      object N3: TMenuItem
        Caption = '-'
      end
      object ObtenermscaraAND1: TMenuItem
        Caption = 'Obtener m'#225'scara AND'
        OnClick = Button1Click
      end
      object ObtenermscaraOR1: TMenuItem
        Caption = 'Obtener m'#225'scara OR'
        OnClick = up1dClick
      end
      object N4: TMenuItem
        Caption = '-'
      end
      object Enfondonegro1: TMenuItem
        Caption = 'En fondo negro'
        Checked = True
        OnClick = Enfondonegro1Click
      end
    end
  end
  object SaveDialog: TSaveDialog
    DefaultExt = 'bmp'
    Filter = 'Im'#225'genes BMP|*.bmp'
    InitialDir = 'c:\akar\anis'
    Options = [ofOverwritePrompt, ofHideReadOnly]
    Title = 'Guardar lista de frames'
    Left = 96
    Top = 344
  end
  object ColorDialog: TColorDialog
    Ctl3D = True
    CustomColors.Strings = (
      'ColorA=280000'
      'ColorB=002800'
      'ColorC=000028'
      'ColorD=000000'
      '')
    Left = 224
    Top = 344
  end
  object OpenDialog: TOpenDialog
    DefaultExt = 'bmp'
    Filter = 'Imagen BMP|*.bmp'
    InitialDir = 'c:\arkhos\anis'
    Options = [ofReadOnly, ofHideReadOnly, ofPathMustExist, ofFileMustExist]
    Title = 'Abrir BMP inicial'
    Left = 160
    Top = 344
  end
end
