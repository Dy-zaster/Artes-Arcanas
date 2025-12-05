object Form1: TForm1
  Left = 612
  Top = 172
  BorderStyle = bsSingle
  Caption = 'Editor de Monstruos '
  ClientHeight = 501
  ClientWidth = 748
  Color = 9484496
  Font.Charset = ANSI_CHARSET
  Font.Color = clWindowText
  Font.Height = -13
  Font.Name = 'Arial'
  Font.Style = []
  Menu = MainMenu1
  OldCreateOrder = True
  Position = poScreenCenter
  OnCloseQuery = FormCloseQuery
  OnCreate = FormCreate
  PixelsPerInch = 96
  TextHeight = 16
  object Bevel3: TBevel
    Left = 283
    Top = 65
    Width = 455
    Height = 384
  end
  object Bevel2: TBevel
    Left = 2
    Top = 296
    Width = 279
    Height = 53
  end
  object Bevel1: TBevel
    Left = 2
    Top = 65
    Width = 279
    Height = 228
  end
  object Label1: TLabel
    Left = 326
    Top = 4
    Width = 418
    Height = 19
    Caption = 'Animaciones de Avatares: 0..99 '#183' Monstruos: 101..183'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -17
    Font.Name = 'Book Antiqua'
    Font.Style = [fsBold]
    ParentFont = False
  end
  object Label2: TLabel
    Left = 428
    Top = 38
    Width = 62
    Height = 19
    Caption = 'C'#243'digo:'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -17
    Font.Name = 'Book Antiqua'
    Font.Style = [fsBold]
    ParentFont = False
  end
  object Label3: TLabel
    Left = 4
    Top = 38
    Width = 67
    Height = 19
    Caption = 'Nombre:'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Book Antiqua'
    Font.Style = [fsBold]
    ParentFont = False
  end
  object Label7: TLabel
    Left = 585
    Top = 95
    Width = 98
    Height = 16
    Caption = 'Nivel de defensa:'
    ParentShowHint = False
    ShowHint = False
  end
  object Label8: TLabel
    Left = 585
    Top = 70
    Width = 89
    Height = 16
    Caption = 'Puntos de vida:'
    ParentShowHint = False
    ShowHint = False
  end
  object Label10: TLabel
    Left = 290
    Top = 318
    Width = 74
    Height = 16
    Caption = 'Ataque 1 de:'
  end
  object Label12: TLabel
    Left = 411
    Top = 318
    Width = 7
    Height = 16
    Caption = 'a'
  end
  object Label26: TLabel
    Left = 4
    Top = 98
    Width = 98
    Height = 16
    Caption = 'Armas Cortantes'
  end
  object Label27: TLabel
    Left = 4
    Top = 146
    Width = 122
    Height = 16
    Caption = 'Armas Contundentes'
  end
  object Label28: TLabel
    Left = 4
    Top = 122
    Width = 104
    Height = 16
    Caption = 'Armas Punzantes'
  end
  object Label30: TLabel
    Left = 585
    Top = 121
    Width = 97
    Height = 16
    Caption = 'Modif. de tesoro:'
    ParentShowHint = False
    ShowHint = False
  end
  object Label31: TLabel
    Left = 4
    Top = 170
    Width = 123
    Height = 16
    Caption = 'Venenos Enfermedad'
  end
  object Label32: TLabel
    Left = 290
    Top = 121
    Width = 42
    Height = 16
    Caption = 'Tesoro:'
  end
  object Label33: TLabel
    Left = 290
    Top = 223
    Width = 98
    Height = 16
    Caption = #193'ngulo de visi'#243'n:'
  end
  object Label11: TLabel
    Left = 4
    Top = 476
    Width = 147
    Height = 16
    Caption = 'Velocidad de movimiento:'
    ParentShowHint = False
    ShowHint = True
  end
  object Label13: TLabel
    Left = 290
    Top = 70
    Width = 49
    Height = 16
    Caption = 'Tama'#241'o:'
  end
  object Label19: TLabel
    Left = 290
    Top = 196
    Width = 64
    Height = 16
    Caption = 'Alineaci'#243'n:'
  end
  object Label22: TLabel
    Left = 290
    Top = 95
    Width = 99
    Height = 16
    Caption = 'Comportamiento:'
  end
  object Label5: TLabel
    Left = 4
    Top = 194
    Width = 84
    Height = 16
    Caption = 'Fuego, '#193'cidos'
  end
  object Label29: TLabel
    Left = 4
    Top = 242
    Width = 132
    Height = 16
    Caption = 'Rayo, Energ'#237'a m'#225'gica '
  end
  object Label34: TLabel
    Left = 4
    Top = 218
    Width = 59
    Height = 16
    Caption = 'Hielo, Fr'#237'o'
  end
  object Label37: TLabel
    Left = 4
    Top = 266
    Width = 118
    Height = 16
    Hint = 'Resistente=50% resistencia, Debil mismo efecto que Normal'
    Caption = 'Resistencia m'#225'gica:'
    ParentShowHint = False
    ShowHint = True
  end
  object Label38: TLabel
    Left = 4
    Top = 69
    Width = 271
    Height = 19
    Hint = 'Avat.: D'#233'bil,Normal=0% Resis.,Invul.=25%'
    Caption = 'Resistencias: Modificaci'#243'n de da'#241'o'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Book Antiqua'
    Font.Style = [fsBold]
    ParentFont = False
    ParentShowHint = False
    ShowHint = True
  end
  object Label23: TLabel
    Left = 290
    Top = 260
    Width = 79
    Height = 18
    Caption = 'COMBATE:'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -15
    Font.Name = 'Book Antiqua'
    Font.Style = [fsBold]
    ParentFont = False
  end
  object Label39: TLabel
    Left = 585
    Top = 221
    Width = 72
    Height = 16
    Hint = 'Exp. Por golpe: Melee=Vida/2+Da'#241'o, Otros=Vida/4+Da'#241'o'
    Caption = 'Experiencia:'
    ParentShowHint = False
    ShowHint = True
  end
  object Label40: TLabel
    Left = 290
    Top = 292
    Width = 191
    Height = 16
    Caption = 'Nombre Ataque "ataque con ... ":'
  end
  object Label15: TLabel
    Left = 290
    Top = 369
    Width = 74
    Height = 16
    Caption = 'Ataque 2 de:'
  end
  object Label16: TLabel
    Left = 411
    Top = 369
    Width = 7
    Height = 16
    Caption = 'a'
  end
  object Label17: TLabel
    Left = 290
    Top = 344
    Width = 191
    Height = 16
    Caption = 'Nombre Ataque "ataque con ... ":'
  end
  object Label20: TLabel
    Left = 290
    Top = 420
    Width = 74
    Height = 16
    Caption = 'Ataque 3 de:'
  end
  object Label21: TLabel
    Left = 411
    Top = 420
    Width = 7
    Height = 16
    Caption = 'a'
  end
  object Label24: TLabel
    Left = 290
    Top = 395
    Width = 191
    Height = 16
    Caption = 'Nombre Ataque "ataque con ... ":'
  end
  object Label6: TLabel
    Left = 585
    Top = 196
    Width = 83
    Height = 16
    Caption = 'Regeneraci'#243'n:'
    ParentShowHint = False
    ShowHint = False
  end
  object Label14: TLabel
    Left = 468
    Top = 318
    Width = 25
    Height = 16
    Caption = 'tipo:'
  end
  object Label18: TLabel
    Left = 468
    Top = 369
    Width = 25
    Height = 16
    Caption = 'tipo:'
  end
  object Label35: TLabel
    Left = 468
    Top = 420
    Width = 25
    Height = 16
    Caption = 'tipo:'
  end
  object Label36: TLabel
    Left = 585
    Top = 170
    Width = 96
    Height = 16
    Caption = 'Nivel de criatura:'
  end
  object Label41: TLabel
    Left = 290
    Top = 170
    Width = 85
    Height = 16
    Hint = 'Tesoro al azar'
    Caption = 'Tesoro al azar:'
    ParentShowHint = False
    ShowHint = True
  end
  object Label4: TLabel
    Left = 4
    Top = 355
    Width = 139
    Height = 16
    Caption = 'Variantes de animaci'#243'n:'
  end
  object Label9: TLabel
    Left = 4
    Top = 399
    Width = 236
    Height = 16
    Caption = 'Efecto en el '#225'rea al morir este monstruo:'
  end
  object Label25: TLabel
    Left = 4
    Top = 448
    Width = 112
    Height = 16
    Caption = 'Animaci'#243'n al morir:'
  end
  object Label42: TLabel
    Left = 447
    Top = 262
    Width = 121
    Height = 16
    Caption = 'Velocidad de ataque:'
  end
  object Label43: TLabel
    Left = 290
    Top = 145
    Width = 53
    Height = 16
    Caption = 'Tesoro 2:'
  end
  object Label44: TLabel
    Left = 585
    Top = 145
    Width = 84
    Height = 16
    Caption = 'Mod. tesoro 2:'
    ParentShowHint = False
    ShowHint = False
  end
  object EditNombre: TEdit
    Left = 72
    Top = 34
    Width = 349
    Height = 26
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Book Antiqua'
    Font.Style = []
    MaxLength = 31
    ParentFont = False
    TabOrder = 0
    OnChange = EditNombreChange
  end
  object C_ZonaCivilizada: TCheckBox
    Left = 8
    Top = 300
    Width = 121
    Height = 22
    Caption = 'Terreno civilizado'
    TabOrder = 9
  end
  object C_ZonaSalvaje: TCheckBox
    Left = 160
    Top = 300
    Width = 117
    Height = 22
    Caption = 'Terreno salvaje'
    TabOrder = 10
  end
  object E_Defensa: TEdit
    Left = 684
    Top = 92
    Width = 50
    Height = 24
    Hint = '0=Indefenso, 50=Normal, 100=H'#233'roe, 200=M'#225'ximo'
    ParentShowHint = False
    ShowHint = True
    TabOrder = 20
  end
  object E_HPB: TEdit
    Left = 684
    Top = 67
    Width = 50
    Height = 24
    Hint = '+/- 25%'
    ParentShowHint = False
    ShowHint = True
    TabOrder = 19
  end
  object E_PA1: TEdit
    Left = 426
    Top = 311
    Width = 34
    Height = 26
    Hint = 'Da'#241'o m'#225'ximo'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Book Antiqua'
    Font.Style = []
    ParentFont = False
    ParentShowHint = False
    ShowHint = True
    TabOrder = 27
  end
  object E_BA1: TEdit
    Left = 369
    Top = 311
    Width = 35
    Height = 26
    Hint = 'Da'#241'o base m'#237'nimo'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Book Antiqua'
    Font.Style = []
    ParentFont = False
    ParentShowHint = False
    ShowHint = True
    TabOrder = 26
  end
  object CB_Cortantes: TComboBox
    Left = 140
    Top = 96
    Width = 140
    Height = 23
    Style = csDropDownList
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -12
    Font.Name = 'Arial'
    Font.Style = []
    ItemHeight = 15
    ParentFont = False
    TabOrder = 1
    Items.Strings = (
      'Muy d'#233'bil: 187.5%'
      'Muy d'#233'bil: 175%'
      'Muy d'#233'bil: 162.5%'
      'D'#233'bil: 150%'
      'D'#233'bil: 137.5%'
      'D'#233'bil: 125%'
      'Normal: 112.5%'
      'Normal: 100%'
      'Normal: 87.5%'
      'Fuerte:  75%'
      'Fuerte: 62.5%'
      'Fuerte: 50%'
      'Muy fuerte: 37.5%'
      'Muy fuerte: 25%'
      'Muy fuerte: 12.5%'
      'Invulnerable')
  end
  object CB_Contundentes: TComboBox
    Left = 140
    Top = 144
    Width = 140
    Height = 23
    Style = csDropDownList
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -12
    Font.Name = 'Arial'
    Font.Style = []
    ItemHeight = 15
    ParentFont = False
    TabOrder = 3
    Items.Strings = (
      'Muy d'#233'bil: 187.5%'
      'Muy d'#233'bil: 175%'
      'Muy d'#233'bil: 162.5%'
      'D'#233'bil: 150%'
      'D'#233'bil: 137.5%'
      'D'#233'bil: 125%'
      'Normal: 112.5%'
      'Normal: 100%'
      'Normal: 87.5%'
      'Fuerte:  75%'
      'Fuerte: 62.5%'
      'Fuerte: 50%'
      'Muy fuerte: 37.5%'
      'Muy fuerte: 25%'
      'Muy fuerte: 12.5%'
      'Invulnerable')
  end
  object CB_Punzantes: TComboBox
    Left = 140
    Top = 120
    Width = 140
    Height = 23
    Style = csDropDownList
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -12
    Font.Name = 'Arial'
    Font.Style = []
    ItemHeight = 15
    ParentFont = False
    TabOrder = 2
    Items.Strings = (
      'Muy d'#233'bil: 187.5%'
      'Muy d'#233'bil: 175%'
      'Muy d'#233'bil: 162.5%'
      'D'#233'bil: 150%'
      'D'#233'bil: 137.5%'
      'D'#233'bil: 125%'
      'Normal: 112.5%'
      'Normal: 100%'
      'Normal: 87.5%'
      'Fuerte:  75%'
      'Fuerte: 62.5%'
      'Fuerte: 50%'
      'Muy fuerte: 37.5%'
      'Muy fuerte: 25%'
      'Muy fuerte: 12.5%'
      'Invulnerable')
  end
  object CB_Veneno: TComboBox
    Left = 140
    Top = 168
    Width = 140
    Height = 23
    Style = csDropDownList
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -12
    Font.Name = 'Arial'
    Font.Style = []
    ItemHeight = 15
    ParentFont = False
    TabOrder = 4
    Items.Strings = (
      'Muy d'#233'bil: 187.5%'
      'Muy d'#233'bil: 175%'
      'Muy d'#233'bil: 162.5%'
      'D'#233'bil: 150%'
      'D'#233'bil: 137.5%'
      'D'#233'bil: 125%'
      'Normal: 112.5%'
      'Normal: 100%'
      'Normal: 87.5%'
      'Fuerte:  75%'
      'Fuerte: 62.5%'
      'Fuerte: 50%'
      'Muy fuerte: 37.5%'
      'Muy fuerte: 25%'
      'Muy fuerte: 12.5%'
      'Invulnerable')
  end
  object C_TerrenoSolido: TCheckBox
    Left = 160
    Top = 324
    Width = 105
    Height = 22
    Caption = 'Terreno s'#243'lido'
    TabOrder = 11
  end
  object CB_codigo: TComboBox
    Left = 2
    Top = 2
    Width = 317
    Height = 26
    Style = csDropDownList
    Color = 11591920
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Book Antiqua'
    Font.Style = []
    ItemHeight = 18
    ParentFont = False
    TabOrder = 38
    OnChange = CB_codigoChange
    OnEnter = CB_codigoEnter
  end
  object CB_guardar: TCheckBox
    Left = 556
    Top = 42
    Width = 169
    Height = 18
    Caption = 'Guardar este monstruo'
    TabOrder = 39
    OnClick = CB_guardarClick
  end
  object E_ModTesoro: TEdit
    Left = 684
    Top = 117
    Width = 50
    Height = 24
    Hint = 'Cantidad o tipo;  0=autom'#225'tico'
    ParentShowHint = False
    ShowHint = True
    TabOrder = 21
  end
  object CB_tamanno: TComboBox
    Left = 391
    Top = 67
    Width = 188
    Height = 23
    Style = csDropDownList
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -12
    Font.Name = 'Arial'
    Font.Style = []
    ItemHeight = 15
    ParentFont = False
    TabOrder = 14
    Items.Strings = (
      'Diminuto  (<0.70m)'
      'Peque'#241'o (<1.40m)'
      'Mediano (1.80m)'
      'Grande (>2.50m)'
      'Gigante (>4.00m)')
  end
  object EditCodigo: TEdit
    Left = 496
    Top = 38
    Width = 43
    Height = 25
    BorderStyle = bsNone
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -17
    Font.Name = 'Book Antiqua'
    Font.Style = []
    ParentColor = True
    ParentFont = False
    TabOrder = 37
    OnChange = EditCodigoChange
  end
  object CB_alineacion: TComboBox
    Left = 391
    Top = 192
    Width = 188
    Height = 23
    Style = csDropDownList
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -12
    Font.Name = 'Arial'
    Font.Style = []
    ItemHeight = 15
    ParentFont = False
    TabOrder = 17
    Items.Strings = (
      'Neutral'
      'Herbivoro'
      'Carnivoro'
      'Malvado/Agresivo')
  end
  object CB_sociedad: TComboBox
    Left = 391
    Top = 216
    Width = 188
    Height = 23
    Style = csDropDownList
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -12
    Font.Name = 'Arial'
    Font.Style = []
    ItemHeight = 15
    ParentFont = False
    TabOrder = 18
    Items.Strings = (
      '45'#186' +3'
      '135'#186' +2'
      '225'#186' +1'
      '360'#186' ')
  end
  object CB_combate: TComboBox
    Left = 391
    Top = 92
    Width = 188
    Height = 23
    Hint = 'Comportamiento'
    Style = csDropDownList
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -12
    Font.Name = 'Arial'
    Font.Style = []
    ItemHeight = 15
    ParentFont = False
    ParentShowHint = False
    ShowHint = True
    TabOrder = 15
    Items.Strings = (
      'Pac'#237'fico, ataque mele'#233
      'Territorial, ataque mele'#233
      'Agresivo, ataque mele'#233
      'Arquero, ataque a distancia'
      'Escapa del agresor'
      'Guardi'#225'n, ataca si te acercas'
      'Mago, consume man'#225
      'Agresivo, mago para Ataque 3'
      'Est'#225'tico, no ataca'
      'Defensa est'#225'tica, hechizos')
  end
  object CB_tesoro: TComboBox
    Left = 391
    Top = 117
    Width = 188
    Height = 23
    Hint = 'ID Objeto Tesoro, Vacio=Sin tesoro'
    Style = csDropDownList
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -12
    Font.Name = 'Arial'
    Font.Style = []
    ItemHeight = 15
    ParentFont = False
    ParentShowHint = False
    ShowHint = True
    TabOrder = 16
  end
  object CB_Fuego: TComboBox
    Left = 140
    Top = 192
    Width = 140
    Height = 23
    Style = csDropDownList
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -12
    Font.Name = 'Arial'
    Font.Style = []
    ItemHeight = 15
    ParentFont = False
    TabOrder = 5
    Items.Strings = (
      'Muy d'#233'bil: 187.5%'
      'Muy d'#233'bil: 175%'
      'Muy d'#233'bil: 162.5%'
      'D'#233'bil: 150%'
      'D'#233'bil: 137.5%'
      'D'#233'bil: 125%'
      'Normal: 112.5%'
      'Normal: 100%'
      'Normal: 87.5%'
      'Fuerte:  75%'
      'Fuerte: 62.5%'
      'Fuerte: 50%'
      'Muy fuerte: 37.5%'
      'Muy fuerte: 25%'
      'Muy fuerte: 12.5%'
      'Invulnerable')
  end
  object CB_Rayo: TComboBox
    Left = 140
    Top = 240
    Width = 140
    Height = 23
    Style = csDropDownList
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -12
    Font.Name = 'Arial'
    Font.Style = []
    ItemHeight = 15
    ParentFont = False
    TabOrder = 7
    Items.Strings = (
      'Muy d'#233'bil: 187.5%'
      'Muy d'#233'bil: 175%'
      'Muy d'#233'bil: 162.5%'
      'D'#233'bil: 150%'
      'D'#233'bil: 137.5%'
      'D'#233'bil: 125%'
      'Normal: 112.5%'
      'Normal: 100%'
      'Normal: 87.5%'
      'Fuerte:  75%'
      'Fuerte: 62.5%'
      'Fuerte: 50%'
      'Muy fuerte: 37.5%'
      'Muy fuerte: 25%'
      'Muy fuerte: 12.5%'
      'Invulnerable')
  end
  object CB_Hielo: TComboBox
    Left = 140
    Top = 216
    Width = 140
    Height = 23
    Style = csDropDownList
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -12
    Font.Name = 'Arial'
    Font.Style = []
    ItemHeight = 15
    ParentFont = False
    TabOrder = 6
    Items.Strings = (
      'Muy d'#233'bil: 187.5%'
      'Muy d'#233'bil: 175%'
      'Muy d'#233'bil: 162.5%'
      'D'#233'bil: 150%'
      'D'#233'bil: 137.5%'
      'D'#233'bil: 125%'
      'Normal: 112.5%'
      'Normal: 100%'
      'Normal: 87.5%'
      'Fuerte:  75%'
      'Fuerte: 62.5%'
      'Fuerte: 50%'
      'Muy fuerte: 37.5%'
      'Muy fuerte: 25%'
      'Muy fuerte: 12.5%'
      'Invulnerable')
  end
  object CB_Magia: TComboBox
    Left = 140
    Top = 264
    Width = 140
    Height = 23
    Style = csDropDownList
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -12
    Font.Name = 'Arial'
    Font.Style = []
    ItemHeight = 15
    ParentFont = False
    TabOrder = 8
    Items.Strings = (
      'Sin resistencia'
      '25%'
      '37.5%'
      '50%'
      '62.5%'
      '75%'
      '87.5%'
      'Invulnerable')
  end
  object C_agua: TCheckBox
    Left = 8
    Top = 324
    Width = 65
    Height = 22
    Caption = 'Agua'
    TabOrder = 12
  end
  object C_fuego: TCheckBox
    Left = 84
    Top = 324
    Width = 57
    Height = 22
    Caption = 'Fuego'
    TabOrder = 13
  end
  object CB_TA1: TComboBox
    Left = 496
    Top = 315
    Width = 128
    Height = 24
    Style = csDropDownList
    DropDownCount = 9
    ItemHeight = 16
    TabOrder = 28
    Items.Strings = (
      'Cortante'
      'Punzante'
      'Contundente'
      'Veneno'
      'Fuego,Acido'
      'Hielo,Frio'
      'Rayo'
      'Hechizo')
  end
  object E_exp: TEdit
    Left = 684
    Top = 218
    Width = 50
    Height = 24
    Hint = 'Exp. por matar al monstruo.'
    ParentShowHint = False
    ShowHint = True
    TabOrder = 23
  end
  object CB_NA1: TComboBox
    Left = 486
    Top = 289
    Width = 138
    Height = 24
    Style = csDropDownList
    ItemHeight = 16
    TabOrder = 25
  end
  object E_ataque: TEdit
    Left = 379
    Top = 256
    Width = 56
    Height = 26
    Hint = '0=M'#237'nimo, 50=Guerrero, 100=H'#233'roe, 200=M'#225'ximo'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Book Antiqua'
    Font.Style = []
    ParentFont = False
    ParentShowHint = False
    ShowHint = True
    TabOrder = 24
  end
  object E_PA2: TEdit
    Left = 426
    Top = 362
    Width = 34
    Height = 26
    Hint = 'Da'#241'o m'#225'ximo'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Book Antiqua'
    Font.Style = []
    ParentFont = False
    ParentShowHint = False
    ShowHint = True
    TabOrder = 31
  end
  object E_BA2: TEdit
    Left = 369
    Top = 362
    Width = 35
    Height = 26
    Hint = 'Da'#241'o base m'#237'nimo'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Book Antiqua'
    Font.Style = []
    ParentFont = False
    ParentShowHint = False
    ShowHint = True
    TabOrder = 30
  end
  object CB_TA2: TComboBox
    Left = 496
    Top = 366
    Width = 128
    Height = 24
    Style = csDropDownList
    DropDownCount = 9
    ItemHeight = 16
    TabOrder = 32
    Items.Strings = (
      'Cortante'
      'Punzante'
      'Contundente'
      'Veneno'
      'Fuego,Acido'
      'Hielo,Frio'
      'Rayo'
      'Hechizo')
  end
  object CB_NA2: TComboBox
    Left = 486
    Top = 340
    Width = 138
    Height = 24
    Style = csDropDownList
    ItemHeight = 16
    TabOrder = 29
  end
  object E_PA3: TEdit
    Left = 426
    Top = 413
    Width = 34
    Height = 26
    Hint = 'Da'#241'o m'#225'ximo'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Book Antiqua'
    Font.Style = []
    ParentFont = False
    ParentShowHint = False
    ShowHint = True
    TabOrder = 35
  end
  object E_BA3: TEdit
    Left = 369
    Top = 413
    Width = 35
    Height = 26
    Hint = 'Da'#241'o base m'#237'nimo'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Book Antiqua'
    Font.Style = []
    ParentFont = False
    ParentShowHint = False
    ShowHint = True
    TabOrder = 34
  end
  object CB_TA3: TComboBox
    Left = 496
    Top = 417
    Width = 128
    Height = 24
    Style = csDropDownList
    DropDownCount = 9
    ItemHeight = 16
    TabOrder = 36
    Items.Strings = (
      'Cortante'
      'Punzante'
      'Contundente'
      'Veneno'
      'Fuego,Acido'
      'Hielo,Frio'
      'Rayo'
      'Hechizo')
  end
  object CB_NA3: TComboBox
    Left = 486
    Top = 392
    Width = 138
    Height = 24
    Style = csDropDownList
    ItemHeight = 16
    TabOrder = 33
  end
  object E_regeneracion: TEdit
    Left = 684
    Top = 192
    Width = 50
    Height = 24
    Hint = 'Regenerado cada 4 turnos.'
    ParentShowHint = False
    ShowHint = True
    TabOrder = 22
  end
  object E_nivel: TEdit
    Left = 684
    Top = 167
    Width = 50
    Height = 24
    Hint = '1=m'#237'nimo 20=h'#233'roe 100=maximo'
    ParentShowHint = False
    ShowHint = True
    TabOrder = 40
  end
  object CB_indice: TComboBox
    Left = 152
    Top = 472
    Width = 125
    Height = 24
    Style = csDropDownList
    ItemHeight = 16
    TabOrder = 41
    Items.Strings = (
      'Muy lento'
      'Lento'
      'Casi normal'
      'Normal')
  end
  object CB_TesoroAzar: TComboBox
    Left = 391
    Top = 167
    Width = 188
    Height = 23
    Style = csDropDownList
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -12
    Font.Name = 'Arial'
    Font.Style = []
    ItemHeight = 15
    ParentFont = False
    TabOrder = 42
    Items.Strings = (
      'Ninguno'
      'Ingredientes de p'#243'cimas'
      'Gemas sin tallar'
      'Pergaminos de magia'
      'Gemas talladas'
      'Pociones m'#225'gicas'
      'Monedas de oro')
  end
  object CB_EstiloAnimacion: TComboBox
    Left = 4
    Top = 373
    Width = 273
    Height = 24
    Style = csDropDownList
    ItemHeight = 16
    TabOrder = 43
    Items.Strings = (
      'Normal'
      'Ataque al azar al estar parado'
      'Movimiento sin desplazarse'
      'Movimiento sin desplazarse pausado'
      'Levitaci'#243'n')
  end
  object cb_consecuenciaMuerte: TComboBox
    Left = 4
    Top = 417
    Width = 273
    Height = 24
    Style = csDropDownList
    ItemHeight = 16
    TabOrder = 44
    Items.Strings = (
      'Ninguno'
      'Castillo y territorio pasan al vencedor')
  end
  object cb_estiloMuerte: TComboBox
    Left = 124
    Top = 445
    Width = 153
    Height = 24
    Style = csDropDownList
    ItemHeight = 16
    TabOrder = 45
    Items.Strings = (
      'Sangre Roja'
      'Sangre Negra'
      'Sangre Verde'
      'Energ'#237'a disipada')
  end
  object cb_Paralizar: TCheckBox
    Left = 516
    Top = 452
    Width = 77
    Height = 22
    Hint = 'El monstruo puede paralizar.'
    Caption = 'Paralizar'
    ParentShowHint = False
    ShowHint = True
    TabOrder = 46
  end
  object cb_VisionVerdadera: TCheckBox
    Left = 668
    Top = 452
    Width = 61
    Height = 22
    Hint = 'El monstruo puede lanzar el hechizo Visi'#243'n Verdadera'
    Caption = 'Visi'#243'n'
    ParentShowHint = False
    ShowHint = True
    TabOrder = 47
  end
  object CheckBox3: TCheckBox
    Left = 568
    Top = 476
    Width = 61
    Height = 22
    Caption = 'Fuerza'
    TabOrder = 48
    Visible = False
  end
  object CheckBox4: TCheckBox
    Left = 640
    Top = 476
    Width = 85
    Height = 22
    Caption = 'Apresurar'
    TabOrder = 49
    Visible = False
  end
  object CheckBox5: TCheckBox
    Left = 284
    Top = 476
    Width = 61
    Height = 22
    Caption = 'Curar'
    TabOrder = 50
    Visible = False
  end
  object CheckBox6: TCheckBox
    Left = 348
    Top = 476
    Width = 89
    Height = 22
    Caption = 'Invisibilidad'
    TabOrder = 51
    Visible = False
  end
  object cb_Aturdir: TCheckBox
    Left = 596
    Top = 452
    Width = 69
    Height = 22
    Hint = 'Puede usar el hechizo aturdir'
    Caption = 'Aturdir'
    ParentShowHint = False
    ShowHint = True
    TabOrder = 52
  end
  object cb_Disipar: TCheckBox
    Left = 448
    Top = 452
    Width = 65
    Height = 22
    Hint = 'Disipa los conjuros positivos del enemigo'
    Caption = 'Disipar'
    ParentShowHint = False
    ShowHint = True
    TabOrder = 53
  end
  object CheckBox9: TCheckBox
    Left = 440
    Top = 476
    Width = 121
    Height = 22
    Caption = 'Protecci'#243'n Divina'
    TabOrder = 54
    Visible = False
  end
  object CB_TiempoEntreAtaques: TComboBox
    Left = 574
    Top = 258
    Width = 115
    Height = 24
    Hint = 'Turnos entre ataques'
    Style = csDropDownList
    DropDownCount = 9
    ItemHeight = 16
    ParentShowHint = False
    ShowHint = True
    TabOrder = 55
    Items.Strings = (
      '2 (r'#225'pido)'
      '4 (normal)'
      '8 (lento)'
      '16 (muy lento)')
  end
  object cb_Liderazgo: TCheckBox
    Left = 284
    Top = 452
    Width = 77
    Height = 22
    Hint = 'Puede pedir ayuda a sus aliados'
    Caption = 'Liderazgo'
    ParentShowHint = False
    ShowHint = True
    TabOrder = 56
  end
  object cb_encantar: TCheckBox
    Left = 368
    Top = 452
    Width = 73
    Height = 22
    Hint = 'Convierte monstruos enemigos en aliados'
    Caption = 'Encantar'
    ParentShowHint = False
    ShowHint = True
    TabOrder = 57
  end
  object CB_Tesoro2: TComboBox
    Left = 391
    Top = 142
    Width = 188
    Height = 23
    Hint = 'ID Objeto Tesoro, Vacio=Sin tesoro'
    Style = csDropDownList
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -12
    Font.Name = 'Arial'
    Font.Style = []
    ItemHeight = 15
    ParentFont = False
    ParentShowHint = False
    ShowHint = True
    TabOrder = 58
  end
  object E_ModTesoro2: TEdit
    Left = 684
    Top = 142
    Width = 50
    Height = 24
    Hint = 'Cantidad o tipo;  0=autom'#225'tico'
    ParentShowHint = False
    ShowHint = True
    TabOrder = 59
  end
  object MainMenu1: TMainMenu
    Left = 716
    Top = 440
    object Cerrar1: TMenuItem
      Caption = 'Archivo'
      object AbrirManual1: TMenuItem
        Caption = 'Abrir Manual'
        OnClick = AbrirManual1Click
      end
      object Guardar1: TMenuItem
        Caption = 'Guardar'
        OnClick = Guardar1Click
      end
      object GuardaresteManual1: TMenuItem
        Caption = 'Guardar con otro nombre'
        OnClick = GuardaresteManual1Click
      end
      object N1: TMenuItem
        Caption = '-'
      end
      object Cerrar2: TMenuItem
        Caption = 'Cerrar'
        OnClick = Cerrar2Click
      end
    end
    object Herramientas1: TMenuItem
      Caption = 'Herramientas'
      object Llenarconvaloresusuales1: TMenuItem
        Caption = 'Llenar con valores usuales'
        OnClick = Llenarconvaloresusuales1Click
      end
    end
  end
  object SaveDialog: TSaveDialog
    DefaultExt = 'mon'
    Filter = 'Manuales de monstruos|*.mon'
    InitialDir = '..\laa\bin'
    Options = [ofOverwritePrompt, ofHideReadOnly]
    Title = 'Guardar Manual'
    Left = 716
    Top = 408
  end
  object OpenDialog: TOpenDialog
    DefaultExt = 'mon'
    Filter = 'Manuales de monstruos|*.mon'
    InitialDir = '..\laa\bin'
    Title = 'Abrir Manual'
    Left = 716
    Top = 376
  end
end
