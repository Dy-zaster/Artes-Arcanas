object F_Banderas: TF_Banderas
  Left = 253
  Top = 189
  BorderStyle = bsDialog
  Caption = 'Definir Banderas Afectadas'
  ClientHeight = 214
  ClientWidth = 187
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -11
  Font.Name = 'MS Sans Serif'
  Font.Style = []
  OldCreateOrder = True
  OnCreate = FormCreate
  PixelsPerInch = 96
  TextHeight = 13
  object CheckBox1: TCheckBox
    Left = 20
    Top = 28
    Width = 80
    Height = 17
    TabOrder = 1
  end
  object CheckBox2: TCheckBox
    Left = 20
    Top = 48
    Width = 80
    Height = 17
    TabOrder = 2
  end
  object CheckBox3: TCheckBox
    Left = 20
    Top = 68
    Width = 80
    Height = 17
    TabOrder = 3
  end
  object CheckBox4: TCheckBox
    Left = 20
    Top = 88
    Width = 80
    Height = 17
    TabOrder = 4
  end
  object CheckBox5: TCheckBox
    Left = 20
    Top = 108
    Width = 80
    Height = 17
    TabOrder = 5
  end
  object CheckBox6: TCheckBox
    Left = 20
    Top = 128
    Width = 80
    Height = 17
    TabOrder = 6
  end
  object CheckBox7: TCheckBox
    Left = 20
    Top = 148
    Width = 80
    Height = 17
    TabOrder = 7
  end
  object CheckBox0: TCheckBox
    Left = 20
    Top = 8
    Width = 80
    Height = 17
    TabOrder = 0
  end
  object Button1: TButton
    Left = 52
    Top = 180
    Width = 75
    Height = 25
    Caption = 'Aceptar'
    TabOrder = 8
    OnClick = Button1Click
  end
  object Button3: TButton
    Left = 104
    Top = 68
    Width = 77
    Height = 37
    Caption = 'Limpiar'
    TabOrder = 9
    TabStop = False
    OnClick = Button3Click
  end
end
