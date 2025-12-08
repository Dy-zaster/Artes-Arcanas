object Form1: TForm1
  Left = 893
  Top = 190
  Width = 643
  Height = 539
  Caption = 'Manual de Artefactos '
  Color = 12628128
  Font.Charset = ANSI_CHARSET
  Font.Color = clWindowText
  Font.Height = -15
  Font.Name = 'Times New Roman'
  Font.Style = []
  KeyPreview = True
  Menu = MainMenu1
  OldCreateOrder = True
  Position = poScreenCenter
  Scaled = False
  OnActivate = FormActivate
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  PixelsPerInch = 96
  TextHeight = 17
  object Bevel2: TBevel
    Left = 360
    Top = 160
    Width = 261
    Height = 172
  end
  object Bevel3: TBevel
    Left = 4
    Top = 160
    Width = 353
    Height = 61
  end
  object Bevel1: TBevel
    Left = 10
    Top = 9
    Width = 42
    Height = 42
  end
  object Label8: TLabel
    Left = 62
    Top = 35
    Width = 60
    Height = 19
    Caption = 'C'#243'digo:'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -17
    Font.Name = 'Times New Roman'
    Font.Style = [fsBold]
    ParentFont = False
  end
  object LbCodigo: TLabel
    Left = 129
    Top = 35
    Width = 88
    Height = 21
    AutoSize = False
    Caption = '0 , (0/0)'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -17
    Font.Name = 'Times New Roman'
    Font.Style = [fsBold]
    ParentFont = False
  end
  object Label3: TLabel
    Left = 10
    Top = 168
    Width = 37
    Height = 17
    Caption = 'Costo:'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -15
    Font.Name = 'Times New Roman'
    Font.Style = []
    ParentFont = False
  end
  object Label4: TLabel
    Left = 91
    Top = 168
    Width = 18
    Height = 17
    Caption = 'mo'
  end
  object Label5: TLabel
    Left = 143
    Top = 168
    Width = 18
    Height = 17
    Caption = 'mp'
  end
  object LB_Modificador: TLabel
    Left = 10
    Top = 195
    Width = 163
    Height = 20
    AutoSize = False
  end
  object Label7: TLabel
    Left = 365
    Top = 166
    Width = 104
    Height = 17
    Caption = 'Por raza no usa:'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -15
    Font.Name = 'Times New Roman'
    Font.Style = [fsBold]
    ParentFont = False
  end
  object Label9: TLabel
    Left = 477
    Top = 166
    Width = 111
    Height = 17
    Caption = 'Por clase no usa:'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -15
    Font.Name = 'Times New Roman'
    Font.Style = [fsBold]
    ParentFont = False
  end
  object LB_rango: TLabel
    Left = 228
    Top = 195
    Width = 76
    Height = 20
    AutoSize = False
  end
  object PaintBox: TPaintBox
    Left = 11
    Top = 10
    Width = 40
    Height = 40
    OnPaint = PaintBoxPaint
  end
  object LBDescripcion: TLabel
    Left = 222
    Top = 37
    Width = 135
    Height = 18
    AutoSize = False
  end
  object LbCostoMP: TLabel
    Left = 168
    Top = 168
    Width = 69
    Height = 17
    AutoSize = False
  end
  object Label2: TLabel
    Left = 393
    Top = 36
    Width = 192
    Height = 17
    Caption = 'Nivel m'#237'nimo para usar el objeto:'
  end
  object Bevel4: TBevel
    Left = 360
    Top = 336
    Width = 261
    Height = 133
  end
  object GB_armadura: TGroupBox
    Left = 4
    Top = 222
    Width = 353
    Height = 49
    Caption = 'Armadura [0..4]'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -15
    Font.Name = 'Times New Roman'
    Font.Style = []
    ParentFont = False
    TabOrder = 21
    Visible = False
    object Label6: TLabel
      Left = 8
      Top = 25
      Width = 32
      Height = 17
      Caption = 'Punz:'
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = []
      ParentFont = False
    end
    object Label12: TLabel
      Left = 92
      Top = 25
      Width = 29
      Height = 17
      Caption = 'Cort:'
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = []
      ParentFont = False
    end
    object Label13: TLabel
      Left = 172
      Top = 25
      Width = 49
      Height = 17
      Caption = 'Contun.:'
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = []
      ParentFont = False
    end
    object Label14: TLabel
      Left = 267
      Top = 25
      Width = 40
      Height = 17
      Caption = 'Magia:'
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = []
      ParentFont = False
    end
    object e_Cort: TEdit
      Left = 129
      Top = 19
      Width = 36
      Height = 24
      Ctl3D = False
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Book Antiqua'
      Font.Style = [fsBold]
      ParentCtl3D = False
      ParentFont = False
      TabOrder = 0
      OnExit = E_TipoDannoExit
    end
    object E_punz: TEdit
      Left = 49
      Top = 19
      Width = 36
      Height = 24
      Ctl3D = False
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Book Antiqua'
      Font.Style = [fsBold]
      ParentCtl3D = False
      ParentFont = False
      TabOrder = 1
      OnExit = E_TipoDannoExit
    end
    object E_golp: TEdit
      Left = 225
      Top = 19
      Width = 36
      Height = 24
      Ctl3D = False
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Book Antiqua'
      Font.Style = [fsBold]
      ParentCtl3D = False
      ParentFont = False
      TabOrder = 2
      OnExit = E_TipoDannoExit
    end
    object E_magi: TEdit
      Left = 309
      Top = 19
      Width = 36
      Height = 24
      Ctl3D = False
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Book Antiqua'
      Font.Style = [fsBold]
      ParentCtl3D = False
      ParentFont = False
      TabOrder = 3
      OnExit = E_TipoDannoExit
    end
  end
  object GB_arma: TGroupBox
    Left = 4
    Top = 222
    Width = 353
    Height = 111
    Caption = 'Arma:'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -15
    Font.Name = 'Times New Roman'
    Font.Style = []
    ParentFont = False
    TabOrder = 22
    object Label16: TLabel
      Left = 8
      Top = 26
      Width = 101
      Height = 17
      Hint = 'Da'#241'o contra jugadores y monstruos medianos o peque'#241'os'
      Caption = 'Da'#241'o P/M y PJs:'
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = []
      ParentFont = False
      ParentShowHint = False
      ShowHint = True
    end
    object Label17: TLabel
      Left = 208
      Top = 26
      Width = 49
      Height = 17
      Hint = 'Da'#241'o contra criaturas grandes a gigantes'
      Caption = 'Da'#241'o G:'
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = []
      ParentFont = False
      ParentShowHint = False
      ShowHint = True
    end
    object Label18: TLabel
      Left = 148
      Top = 26
      Width = 7
      Height = 17
      Caption = 'a'
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = []
      ParentFont = False
    end
    object Label19: TLabel
      Left = 300
      Top = 26
      Width = 7
      Height = 17
      Caption = 'a'
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = []
      ParentFont = False
    end
    object Label10: TLabel
      Left = 8
      Top = 56
      Width = 27
      Height = 17
      Hint = 'Liegra=segunda arma, Pesada=ambas manos'
      Caption = 'Uso:'
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = []
      ParentFont = False
      ParentShowHint = False
      ShowHint = True
    end
    object Label11: TLabel
      Left = 180
      Top = 56
      Width = 35
      Height = 17
      Hint = 'Tipo de da'#241'o del arma'
      Caption = 'Da'#241'o:'
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = []
      ParentFont = False
      ParentShowHint = False
      ShowHint = True
    end
    object lbTipo: TLabel
      Left = 8
      Top = 85
      Width = 66
      Height = 17
      Caption = 'Animaci'#243'n:'
    end
    object D1_base: TEdit
      Left = 114
      Top = 21
      Width = 32
      Height = 24
      Hint = 'Da'#241'o base'
      Ctl3D = False
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Book Antiqua'
      Font.Style = [fsBold]
      ParentCtl3D = False
      ParentFont = False
      ParentShowHint = False
      ShowHint = True
      TabOrder = 0
      OnExit = D_baseExit
    end
    object D2_base: TEdit
      Left = 266
      Top = 21
      Width = 32
      Height = 24
      Hint = 'Da'#241'o base'
      Ctl3D = False
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Book Antiqua'
      Font.Style = [fsBold]
      ParentCtl3D = False
      ParentFont = False
      ParentShowHint = False
      ShowHint = True
      TabOrder = 1
      OnExit = D_baseExit
    end
    object d1_plus: TEdit
      Left = 159
      Top = 21
      Width = 32
      Height = 24
      Hint = 'Maximo da'#241'o'
      Ctl3D = False
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Book Antiqua'
      Font.Style = [fsBold]
      ParentCtl3D = False
      ParentFont = False
      ParentShowHint = False
      ShowHint = True
      TabOrder = 2
      OnExit = D_plusExit
    end
    object d2_plus: TEdit
      Left = 311
      Top = 21
      Width = 32
      Height = 24
      Hint = 'Maximo da'#241'o'
      Ctl3D = False
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Book Antiqua'
      Font.Style = [fsBold]
      ParentCtl3D = False
      ParentFont = False
      ParentShowHint = False
      ShowHint = True
      TabOrder = 3
      OnExit = D_plusExit
    end
    object cb_peso: TComboBox
      Left = 46
      Top = 50
      Width = 123
      Height = 25
      Style = csDropDownList
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = [fsBold]
      ItemHeight = 17
      ParentFont = False
      TabOrder = 4
      Items.Strings = (
        'Una mano'
        'Mano derecha'
        'Dos manos'
        'Munici'#243'n')
    end
    object cb_tipo: TComboBox
      Left = 222
      Top = 50
      Width = 119
      Height = 25
      Style = csDropDownList
      Ctl3D = True
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = [fsBold]
      ItemHeight = 17
      ParentCtl3D = False
      ParentFont = False
      TabOrder = 5
      Items.Strings = (
        'Cortante'
        'Punzante'
        'Contundente'
        'Veneno'
        'Fuego'
        'Hielo'
        'Rayo'
        'Hechizo'
        'Munici'#243'n'
        'No es Arma')
    end
    object cb_animacion: TComboBox
      Left = 78
      Top = 79
      Width = 163
      Height = 25
      Style = csDropDownList
      Ctl3D = True
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = [fsBold]
      ItemHeight = 17
      ParentCtl3D = False
      ParentFont = False
      TabOrder = 6
      Items.Strings = (
        '<No definido>'
        'Pu'#241'o'
        'Daga'
        'Espada'
        'Hacha'
        'Polearma'
        'Maza'
        'Mangual'
        'Arco'
        'Ballesta'
        'Arcabuz'
        'Ballesta de mano'
        'Mano (Magia)'
        'Cayado (Mele'#233')'
        'Cetro de Mago'
        'S'#237'mbolo Sagrado'
        'Espada congelante')
    end
  end
  object CB_Clase: TComboBox
    Left = 62
    Top = 6
    Width = 295
    Height = 27
    Style = csDropDownList
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Times New Roman'
    Font.Style = [fsBold]
    ItemHeight = 19
    ParentFont = False
    TabOrder = 0
    OnChange = CB_ClaseChange
    OnEnter = CB_ClaseEnter
    Items.Strings = (
      'Manos/Dinero'
      'Armas M'#225'gicas'
      'Armas Melee'
      'Armas Melee'
      'Armas Melee'
      'Armas Rango'
      'Municiones'
      'Armaduras'
      'Armaduras'
      'H'#225'bitos/Tunicas'
      'Escudos'
      'Cascos'
      'Anillos/brazaletes'
      'Amuletos'
      'Objetos Mago/Cl'#233'rigo'
      'Herramientas'
      'Herramientas'
      'Herramientas Consumibles'
      'Comidas'
      'Bebidas'
      'Pociones'
      'Ingredientes'
      'Llaves'
      'Gemas'
      'Gemas no talladas'
      'Minerales'
      'Recursos'
      'Recursos'
      'Recursos'
      'Herramientas'
      'Artefactos'
      'Equipables')
  end
  object SG_Nombres: TStringGrid
    Left = 4
    Top = 62
    Width = 617
    Height = 95
    Color = clSilver
    ColCount = 2
    DefaultColWidth = 305
    DefaultRowHeight = 22
    FixedCols = 0
    RowCount = 4
    FixedRows = 0
    Options = [goFixedVertLine, goFixedHorzLine, goVertLine, goHorzLine, goRangeSelect, goDrawFocusSelected, goEditing, goAlwaysShowEditor]
    ScrollBars = ssNone
    TabOrder = 1
    OnSelectCell = SG_NombresSelectCell
  end
  object E_MO: TEdit
    Left = 51
    Top = 164
    Width = 38
    Height = 25
    Ctl3D = False
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Times New Roman'
    Font.Style = [fsBold]
    ParentCtl3D = False
    ParentFont = False
    TabOrder = 2
    OnChange = E_MPChange
    OnExit = E_MOExit
  end
  object E_MP: TEdit
    Left = 115
    Top = 164
    Width = 26
    Height = 25
    Ctl3D = False
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Times New Roman'
    Font.Style = [fsBold]
    ParentCtl3D = False
    ParentFont = False
    TabOrder = 3
    OnChange = E_MPChange
    OnExit = E_MPExit
  end
  object E_ModADC: TEdit
    Left = 180
    Top = 191
    Width = 41
    Height = 25
    Ctl3D = False
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Times New Roman'
    Font.Style = [fsBold]
    ParentCtl3D = False
    ParentFont = False
    ParentShowHint = False
    ShowHint = False
    TabOrder = 4
    OnExit = E_ModADCExit
  end
  object CheckBox1: TCheckBox
    Left = 366
    Top = 188
    Width = 90
    Height = 19
    Caption = 'Humano'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -15
    Font.Name = 'Times New Roman'
    Font.Style = []
    ParentFont = False
    TabOrder = 5
  end
  object CheckBox2: TCheckBox
    Left = 366
    Top = 205
    Width = 90
    Height = 19
    Caption = 'Elfo'
    TabOrder = 6
  end
  object CheckBox3: TCheckBox
    Left = 366
    Top = 222
    Width = 90
    Height = 19
    Caption = 'Enano'
    TabOrder = 7
  end
  object CheckBox4: TCheckBox
    Left = 366
    Top = 239
    Width = 90
    Height = 19
    Caption = 'Gnomo'
    TabOrder = 8
  end
  object CheckBox5: TCheckBox
    Left = 366
    Top = 256
    Width = 90
    Height = 19
    Caption = 'Semielfo'
    TabOrder = 9
  end
  object CheckBox6: TCheckBox
    Left = 366
    Top = 273
    Width = 90
    Height = 19
    Caption = 'Orco'
    TabOrder = 10
  end
  object CheckBox7: TCheckBox
    Left = 366
    Top = 290
    Width = 90
    Height = 19
    Caption = 'Drow'
    TabOrder = 11
  end
  object CheckBox8: TCheckBox
    Left = 366
    Top = 307
    Width = 90
    Height = 19
    Caption = 'Deva'
    TabOrder = 12
    Visible = False
  end
  object CheckBox9: TCheckBox
    Left = 477
    Top = 188
    Width = 102
    Height = 19
    Caption = 'Guerrero'
    TabOrder = 13
  end
  object CheckBox10: TCheckBox
    Left = 477
    Top = 205
    Width = 102
    Height = 19
    Caption = 'Cl'#233'rigo'
    TabOrder = 14
  end
  object CheckBox11: TCheckBox
    Left = 477
    Top = 222
    Width = 102
    Height = 19
    Caption = 'Mago'
    TabOrder = 15
  end
  object CheckBox12: TCheckBox
    Left = 477
    Top = 239
    Width = 102
    Height = 19
    Caption = 'Brib'#243'n'
    TabOrder = 16
  end
  object CheckBox13: TCheckBox
    Left = 477
    Top = 256
    Width = 114
    Height = 19
    Caption = 'Montaraz'
    TabOrder = 17
  end
  object CheckBox14: TCheckBox
    Left = 477
    Top = 273
    Width = 114
    Height = 19
    Caption = 'Palad'#237'n'
    TabOrder = 18
  end
  object CheckBox15: TCheckBox
    Left = 477
    Top = 290
    Width = 107
    Height = 19
    Caption = 'Bardo'
    TabOrder = 19
  end
  object CheckBox16: TCheckBox
    Left = 477
    Top = 307
    Width = 114
    Height = 19
    Caption = 'Guerrero-Mago'
    TabOrder = 20
  end
  object gbRecursos: TGroupBox
    Left = 4
    Top = 335
    Width = 353
    Height = 134
    Caption = 'Construcci'#243'n y reparaci'#243'n:'
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -15
    Font.Name = 'Times New Roman'
    Font.Style = []
    ParentFont = False
    TabOrder = 23
    object Lb_recurso1: TLabel
      Left = 192
      Top = 52
      Width = 106
      Height = 20
      AutoSize = False
      Caption = 'Lb_recurso1'
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = []
      ParentFont = False
    end
    object Lb_recurso2: TLabel
      Left = 192
      Top = 80
      Width = 101
      Height = 20
      AutoSize = False
      Caption = 'Lb_recurso2'
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = []
      ParentFont = False
    end
    object Lb_recurso3: TLabel
      Left = 192
      Top = 108
      Width = 105
      Height = 20
      AutoSize = False
      Caption = 'Lb_recurso3'
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = []
      ParentFont = False
    end
    object LB_cantidad: TLabel
      Left = 8
      Top = 108
      Width = 99
      Height = 17
      Hint = '0=>modificador del 3er recurso'
      Caption = 'Cantidad/Estado:'
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = []
      ParentFont = False
      ParentShowHint = False
      ShowHint = True
    end
    object Lb_NivelConst: TLabel
      Left = 8
      Top = 80
      Width = 129
      Height = 17
      Caption = 'Nivel para construirlo:'
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = []
      ParentFont = False
    end
    object Lb_Recursos: TLabel
      Left = 192
      Top = 24
      Width = 153
      Height = 17
      Caption = 'Recursos para construirlo:'
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = []
      ParentFont = False
    end
    object E_recurso1: TEdit
      Left = 314
      Top = 48
      Width = 32
      Height = 23
      Ctl3D = False
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = [fsBold]
      ParentCtl3D = False
      ParentFont = False
      TabOrder = 0
      OnExit = recursoExit
    end
    object E_recurso2: TEdit
      Left = 314
      Top = 76
      Width = 32
      Height = 23
      Ctl3D = False
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = [fsBold]
      ParentCtl3D = False
      ParentFont = False
      TabOrder = 1
      OnExit = recursoExit
    end
    object E_recurso3: TEdit
      Left = 314
      Top = 104
      Width = 32
      Height = 23
      Ctl3D = False
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = [fsBold]
      ParentCtl3D = False
      ParentFont = False
      TabOrder = 2
      OnExit = recursoExit
    end
    object E_nivelConst: TEdit
      Left = 142
      Top = 76
      Width = 32
      Height = 23
      Ctl3D = False
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = [fsBold]
      ParentCtl3D = False
      ParentFont = False
      TabOrder = 3
      OnExit = E_nivelConstExit
    end
    object E_Cantidad: TEdit
      Left = 142
      Top = 104
      Width = 32
      Height = 23
      Ctl3D = False
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = [fsBold]
      ParentCtl3D = False
      ParentFont = False
      TabOrder = 4
      OnExit = recursoExit
    end
    object CB_Tipoconstructor: TComboBox
      Left = 8
      Top = 17
      Width = 169
      Height = 25
      Style = csDropDownList
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = [fsBold]
      ItemHeight = 17
      ParentFont = False
      TabOrder = 5
      OnChange = CB_TipoconstructorChange
      Items.Strings = (
        '<No se contruye>'
        'Herrero'
        'Gran Herrero'
        'Alquimista'
        'Gran Alquimista'
        'Sastre'
        'Gran Sastre'
        'Carpintero-Escudos'
        'Herbalista'
        'Carpintero'
        'Gran Carpintero')
    end
    object CB_Reparacion: TComboBox
      Left = 8
      Top = 45
      Width = 169
      Height = 25
      Style = csDropDownList
      Font.Charset = ANSI_CHARSET
      Font.Color = clWindowText
      Font.Height = -15
      Font.Name = 'Times New Roman'
      Font.Style = [fsBold]
      ItemHeight = 17
      ParentFont = False
      TabOrder = 6
      Items.Strings = (
        'No es Reparable'
        'Para Afilar'
        'Para Aceitar'
        'Para Martillar'
        'Para Coser')
    end
  end
  object E_nivelminimo: TEdit
    Left = 591
    Top = 32
    Width = 26
    Height = 25
    Ctl3D = False
    Font.Charset = ANSI_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Times New Roman'
    Font.Style = [fsBold]
    ParentCtl3D = False
    ParentFont = False
    TabOrder = 24
    Text = '0'
    OnChange = E_MPChange
    OnExit = E_nivelConstExit
  end
  object MainMenu1: TMainMenu
    Left = 588
    Top = 164
    object Archivo1: TMenuItem
      Caption = 'Salir'
      OnClick = Salir1Click
    end
    object Herramientas1: TMenuItem
      Caption = 'Herramientas'
      object Abrirnuevamente1: TMenuItem
        Caption = 'Abrir de nuevo (Descartar cambios)'
        OnClick = BtAbrirClick
      end
      object N2: TMenuItem
        Caption = '-'
      end
      object GuardarTodo1: TMenuItem
        Caption = 'Modificar Versi'#243'n (Guardar cambios)'
        OnClick = GuardarClick
      end
      object N1: TMenuItem
        Caption = '-'
      end
      object Crearmanualdearmas1: TMenuItem
        Caption = 'Crear manual de armas'
        OnClick = Crearmanualdearmas1Click
      end
      object Crearmanualdearmasconjuradas1: TMenuItem
        Caption = 'Crear manual de armas conjurables'
        OnClick = Crearmanualdearmasconjuradas1Click
      end
      object Crearmanualdearmadurasvestimentas1: TMenuItem
        Caption = 'Crear manual de armaduras'
        OnClick = Crearmanualdearmadurasvestimentas1Click
      end
      object Crearlistadeiconos1: TMenuItem
        Caption = 'Crear iconos'
        Visible = False
      end
    end
  end
end
