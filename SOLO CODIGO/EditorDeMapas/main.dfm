object FCmundo: TFCmundo
  Left = 657
  Top = 252
  BorderIcons = [biSystemMenu, biMinimize]
  BorderStyle = bsSingle
  Caption = 'Editor de Mapas '
  ClientHeight = 790
  ClientWidth = 1432
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -11
  Font.Name = 'MS Sans Serif'
  Font.Style = []
  KeyPreview = True
  Menu = MainMenu1
  OldCreateOrder = True
  Position = poScreenCenter
  Scaled = False
  OnCloseQuery = FormCloseQuery
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnKeyDown = FormKeyDown
  OnMouseDown = FormMouseDown
  OnMouseMove = FormMouseMove
  OnMouseUp = FormMouseUp
  OnPaint = FormPaint
  OnResize = FormResize
  PixelsPerInch = 96
  TextHeight = 13
  object PB_Mmapa: TPaintBox
    Left = 1296
    Top = 0
    Width = 128
    Height = 128
    Color = clBtnFace
    ParentColor = False
    OnMouseDown = PB_MmapaMouseDown
    OnMouseMove = PB_MmapaMouseMove
    OnMouseUp = PB_MmapaMouseUp
    OnPaint = PB_MmapaPaint
  end
  object LabelMensaje: TLabel
    Left = 4
    Top = 612
    Width = 649
    Height = 13
    AutoSize = False
  end
  object Label19: TLabel
    Left = 1308
    Top = 576
    Width = 112
    Height = 13
    Caption = 'Posici'#243'n Guardada:'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -11
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
  end
  object Lb_PosicionGuardada: TLabel
    Left = 1308
    Top = 592
    Width = 113
    Height = 13
    AutoSize = False
    Caption = 'Mapa 0: 0,0'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -11
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
  end
  object c_GTerreno: TCheckBox
    Left = 1296
    Top = 256
    Width = 113
    Height = 17
    Caption = 'Graficos de Terreno'
    Checked = True
    State = cbChecked
    TabOrder = 5
    OnClick = c_GTerrenoClick
  end
  object ScrollBarY: TScrollBar
    Left = 1280
    Top = 0
    Width = 13
    Height = 592
    Kind = sbVertical
    Max = 255
    PageSize = 0
    Position = 18
    TabOrder = 11
    OnChange = ScrollBarYChange
  end
  object ScrollBarX: TScrollBar
    Left = 0
    Top = 594
    Width = 1280
    Height = 13
    Max = 255
    PageSize = 0
    Position = 24
    TabOrder = 12
    OnChange = ScrollBarYChange
  end
  object C_AlphaT: TCheckBox
    Left = 1296
    Top = 196
    Width = 97
    Height = 17
    Caption = 'Alpha terreno'
    Checked = True
    State = cbChecked
    TabOrder = 2
    OnClick = c_GTerrenoClick
  end
  object pageControl1: TPageControl
    Left = 0
    Top = 629
    Width = 1432
    Height = 161
    ActivePage = TS_terreno
    Align = alBottom
    TabIndex = 1
    TabOrder = 13
    OnChange = pageControl1Change
    object TS_General: TTabSheet
      Caption = 'Datos Generales'
      object Label2: TLabel
        Left = 4
        Top = 8
        Width = 70
        Height = 13
        Caption = 'Nombre Mapa:'
      end
      object Label3: TLabel
        Left = 224
        Top = 32
        Width = 79
        Height = 13
        Caption = 'Mapa del Norte: '
      end
      object Label4: TLabel
        Left = 224
        Top = 8
        Width = 155
        Height = 13
        Hint = 'Si no existe no se viaja a otro mapa.'
        Caption = 'Indicar el nro. de mapa contiguo:'
        ParentShowHint = False
        ShowHint = True
      end
      object Label5: TLabel
        Left = 224
        Top = 52
        Width = 69
        Height = 13
        Caption = 'Mapa del Sur: '
      end
      object Label6: TLabel
        Left = 224
        Top = 92
        Width = 81
        Height = 13
        Caption = 'Mapa del Oeste: '
      end
      object Label7: TLabel
        Left = 224
        Top = 72
        Width = 74
        Height = 13
        Caption = 'Mapa del Este: '
      end
      object Label16: TLabel
        Left = 4
        Top = 32
        Width = 112
        Height = 13
        Caption = 'Tipo de clima del mapa:'
      end
      object Label22: TLabel
        Left = 4
        Top = 80
        Width = 68
        Height = 13
        Caption = 'Tipo de mapa:'
      end
      object Label24: TLabel
        Left = 408
        Top = 8
        Width = 210
        Height = 13
        Hint = 'S'#243'lo posiciones distintas de 0,0,0 tienen efecto.'
        Caption = 'Posici'#243'n (mapa,posx,posy) para "/RETorno"'
        ParentShowHint = False
        ShowHint = True
      end
      object E_nombre: TEdit
        Left = 76
        Top = 4
        Width = 137
        Height = 21
        TabOrder = 0
      end
      object E_norte: TEdit
        Left = 312
        Top = 28
        Width = 32
        Height = 21
        TabOrder = 3
        Text = '0'
      end
      object e_sur: TEdit
        Left = 312
        Top = 48
        Width = 32
        Height = 21
        TabOrder = 4
        Text = '0'
      end
      object e_oeste: TEdit
        Left = 312
        Top = 88
        Width = 32
        Height = 21
        TabOrder = 6
        Text = '0'
      end
      object e_este: TEdit
        Left = 312
        Top = 68
        Width = 32
        Height = 21
        TabOrder = 5
        Text = '0'
      end
      object cbTipoClimaMapa: TComboBox
        Left = 4
        Top = 48
        Width = 209
        Height = 21
        Style = csDropDownList
        ItemHeight = 13
        TabOrder = 1
        Items.Strings = (
          'Bosques/Pastos'
          'Desiertos'
          'Glaciares'
          'Mazmorras/Minas/Abismos'
          'Interior de construcci'#243'n'
          'Bosques oscuros')
      end
      object cbTipoMapa: TComboBox
        Left = 4
        Top = 96
        Width = 209
        Height = 21
        Style = csDropDownList
        ItemHeight = 13
        TabOrder = 2
        Items.Strings = (
          'Mapa com'#250'n'
          'Arena de combate'
          'Mapa seguro (No pierdes equipo)'
          'Mapa seguro y arena de combate ')
      end
      object Button2: TButton
        Left = 704
        Top = 4
        Width = 75
        Height = 21
        Caption = 'Espejo Horiz.'
        TabOrder = 7
        OnClick = Button2Click
      end
      object Button3: TButton
        Left = 704
        Top = 32
        Width = 75
        Height = 21
        Caption = 'Espejo Vert.'
        TabOrder = 8
        OnClick = Button3Click
      end
      object E_RetornoX: TEdit
        Left = 452
        Top = 28
        Width = 32
        Height = 21
        TabOrder = 9
        Text = '0'
      end
      object E_RetornoY: TEdit
        Left = 492
        Top = 28
        Width = 32
        Height = 21
        TabOrder = 10
        Text = '0'
      end
      object Btn_AsignarPosRET: TButton
        Left = 532
        Top = 28
        Width = 141
        Height = 21
        Caption = 'Asignar posici'#243'n guardada'
        TabOrder = 11
        OnClick = Btn_AsignarPosRETClick
      end
      object Button4: TButton
        Left = 412
        Top = 60
        Width = 193
        Height = 21
        Caption = 'Actualizar valor Z y Ordenar gr'#225'ficos'
        TabOrder = 12
        OnClick = Button4Click
      end
      object Button5: TButton
        Left = 412
        Top = 88
        Width = 193
        Height = 21
        Caption = 'Actualizar items de comercios'
        TabOrder = 13
        OnClick = Button5Click
      end
      object E_RetornoM: TEdit
        Left = 412
        Top = 28
        Width = 32
        Height = 21
        TabOrder = 14
        Text = '0'
      end
      object cb_AbismoVacio: TCheckBox
        Left = 224
        Top = 112
        Width = 177
        Height = 17
        Hint = 'Permite el paso de conjuros y municiones por el terreno "Abismo"'
        Caption = 'Tipo de terreno "Abismo" vac'#237'o'
        ParentShowHint = False
        ShowHint = True
        TabOrder = 15
        OnClick = cb_AbismoVacioClick
      end
    end
    object TS_terreno: TTabSheet
      Caption = 'Terreno'
      object Label1: TLabel
        Left = 10
        Top = 12
        Width = 75
        Height = 13
        Caption = 'Tipo de terreno:'
      end
      object CB_terreno: TComboBox
        Left = 96
        Top = 8
        Width = 177
        Height = 21
        Style = csDropDownList
        ItemHeight = 13
        TabOrder = 0
        Items.Strings = (
          'Abismo (Lleno)'
          'Nieve con hierbas'
          'Nieve'
          'Piedras y hierbas'
          'Hierbas'
          'Hongos y l'#237'quenes'
          'Pasto oscuro'
          'Pasto medio'
          'Pasto claro'
          'Pasto seco'
          'Tierra seca'
          'Tierra'
          'Arena'
          'Caverna azul'
          'Caverna '#225'mbar'
          'Caverna gris'
          'Hielo'
          'M'#225'rmol verde'
          'M'#225'rmol blanco'
          'M'#225'rmol '#225'mbar'
          'Piso de madera'
          'Piso parquet'
          'Mosaico rojo'
          'Mosaico azul'
          'Piso de ladrillos'
          'Cmn. losas.'
          'Cmn. piedras osc.'
          'Cmn. piedras cla.'
          'Brea'
          'Lava'
          'Pantano'
          'Agua')
      end
    end
    object TS_graficos: TTabSheet
      Caption = 'Gr'#225'ficos'
      object Label25: TLabel
        Left = 504
        Top = 4
        Width = 99
        Height = 13
        Caption = 'Variaciones, efectos:'
      end
      object CB_graficos: TComboBox
        Left = 116
        Top = 4
        Width = 177
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 1
        OnClick = CB_graficosClick
      end
      object RB_codigo1_G: TRadioButton
        Left = 4
        Top = 7
        Width = 109
        Height = 17
        Caption = 'Gr'#225'fico del terreno:'
        Checked = True
        TabOrder = 0
        TabStop = True
        OnClick = RB_codigo1_GClick
      end
      object RB_auto_G: TRadioButton
        Left = 308
        Top = 51
        Width = 185
        Height = 17
        Caption = 'Al azar de acuerdo al terreno'
        TabOrder = 11
      end
      object RB_borrar_G: TRadioButton
        Left = 308
        Top = 8
        Width = 113
        Height = 17
        Caption = 'Borrar gr'#225'fico'
        TabOrder = 10
        OnEnter = RB_borrar_GEnter
      end
      object RB_codigo2_G: TRadioButton
        Left = 4
        Top = 55
        Width = 97
        Height = 17
        Caption = 'Construcciones:'
        TabOrder = 4
        OnClick = RB_codigo2_GClick
      end
      object Cb_edificios: TComboBox
        Left = 116
        Top = 52
        Width = 177
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 5
        OnClick = Cb_edificiosClick
      end
      object RB_riscos: TRadioButton
        Left = 4
        Top = 31
        Width = 105
        Height = 17
        Caption = 'Riscos, monta'#241'as:'
        TabOrder = 2
        OnClick = RB_riscosClick
      end
      object CB_riscos: TComboBox
        Left = 116
        Top = 28
        Width = 177
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 3
        OnClick = CB_riscosClick
      end
      object cb_modulos: TComboBox
        Left = 116
        Top = 76
        Width = 177
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 7
        OnClick = cb_modulosClick
      end
      object RB_muros: TRadioButton
        Left = 4
        Top = 79
        Width = 106
        Height = 17
        Caption = 'Partes de edificios:'
        TabOrder = 6
        OnClick = RB_murosClick
      end
      object cb_me_Graficos: TCheckBox
        Left = 308
        Top = 72
        Width = 189
        Height = 17
        Caption = '&Seleccionar y describir el apuntado'
        TabOrder = 12
      end
      object RB_sprites: TRadioButton
        Left = 4
        Top = 103
        Width = 105
        Height = 17
        Caption = 'Animaciones:'
        TabOrder = 8
        OnClick = RB_spritesClick
      end
      object cb_sprites: TComboBox
        Left = 116
        Top = 100
        Width = 177
        Height = 21
        Style = csDropDownList
        ItemHeight = 13
        TabOrder = 9
        OnClick = cb_spritesClick
        Items.Strings = (
          'Fogata'
          'Fragua'
          'Fl.Azul'
          'Fl.Altar'
          'Fl.Portal'
          'Exp.R'
          'Exp.V'
          'Exp.A'
          'HumoChim'
          'Humo'
          'Estandarte'
          'Fl.Blanca'
          'Antor.Ama'
          'Antor.Cel'
          'Antor.Mag'
          'Antor.R'
          'Antor.V'
          'Antor.A'
          'Altar.Ama'
          'Altar.Cel'
          'Altar.Mag'
          'Altar.R'
          'Altar.V'
          'Altar.A'
          'Portal'
          'Port.Ama'
          'Port.Cel'
          'Port.Mag'
          'fx0'
          'fx0.R'
          'fx0.V'
          'fx0.A'
          'fx1'
          'fx1.R'
          'fx1.V'
          'fx1.A'
          'fx2'
          'fx2.R'
          'fx2.V'
          'fx2.A')
      end
      object cb_reflejo: TCheckBox
        Left = 504
        Top = 24
        Width = 133
        Height = 17
        Caption = '&Reflejar en eje vertical'
        TabOrder = 13
      end
      object CB_levitacion: TCheckBox
        Left = 504
        Top = 44
        Width = 133
        Height = 17
        Caption = 'Efecto de levitaci'#243'n'
        TabOrder = 14
      end
      object cb_transparente: TCheckBox
        Left = 504
        Top = 64
        Width = 133
        Height = 17
        Caption = 'Siempre transparente'
        TabOrder = 15
      end
      object cb_sensible_flags: TCheckBox
        Left = 504
        Top = 104
        Width = 133
        Height = 17
        Caption = 'Visibilidad por bandera'
        TabOrder = 16
        OnClick = cb_sensible_flagsClick
      end
      object cb_inverso: TCheckBox
        Left = 728
        Top = 102
        Width = 57
        Height = 17
        Caption = 'Inverso'
        TabOrder = 17
        Visible = False
      end
      object cb_flag_grafico: TComboBox
        Left = 640
        Top = 100
        Width = 85
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 18
        Visible = False
      end
      object cb_ilusion: TCheckBox
        Left = 504
        Top = 84
        Width = 101
        Height = 17
        Caption = 'Ilusi'#243'n'
        TabOrder = 19
      end
      object cb_borrar: TCheckBox
        Left = 308
        Top = 28
        Width = 181
        Height = 17
        Caption = 'Preguntar antes de borrar'
        TabOrder = 20
      end
    end
    object TS_Comerciante: TTabSheet
      Caption = 'Comerciantes'
      object Label18: TLabel
        Left = 6
        Top = 84
        Width = 226
        Height = 13
        Caption = 'Texto del comerciante\nombre del comerciante:'
      end
      object Label20: TLabel
        Left = 6
        Top = 8
        Width = 100
        Height = 13
        Caption = 'Tipo de comerciante:'
      end
      object Label21: TLabel
        Left = 6
        Top = 36
        Width = 130
        Height = 13
        Caption = 'Animaci'#243'n del comerciante:'
      end
      object CbBorrarComerciante: TCheckBox
        Left = 408
        Top = 4
        Width = 121
        Height = 17
        Caption = 'Borrar comerciantes'
        TabOrder = 1
      end
      object Cb_ME_Comerciante: TCheckBox
        Left = 408
        Top = 32
        Width = 217
        Height = 17
        Caption = '&Seleccionar y describir el apuntado'
        TabOrder = 2
      end
      object CmbTipoComerciante: TComboBox
        Left = 140
        Top = 4
        Width = 241
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 0
      end
      object EdtTextoComerciante: TEdit
        Left = 6
        Top = 104
        Width = 769
        Height = 21
        MaxLength = 79
        TabOrder = 4
      end
      object cmbAniComerciante: TComboBox
        Left = 140
        Top = 32
        Width = 241
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 3
      end
    end
    object TS_NPC: TTabSheet
      Caption = 'PNJ'
      TabVisible = False
      object Label8: TLabel
        Left = 8
        Top = 32
        Width = 30
        Height = 13
        Caption = 'Texto:'
      end
      object Label9: TLabel
        Left = 8
        Top = 56
        Width = 26
        Height = 13
        Caption = 'Dato:'
      end
      object Label17: TLabel
        Left = 160
        Top = 64
        Width = 184
        Height = 13
        Caption = 'NO DISPONIBLE POR EL MOMENTO'
      end
      object e_texto_npc: TEdit
        Left = 44
        Top = 28
        Width = 737
        Height = 21
        MaxLength = 111
        TabOrder = 0
      end
      object cb_tipo_npc: TComboBox
        Left = 8
        Top = 4
        Width = 257
        Height = 21
        Style = csDropDownList
        ItemHeight = 13
        TabOrder = 1
        Items.Strings = (
          'Informante'
          'Herrero'
          'Carpintero'
          'Minero'
          'Le'#241'ador'
          'Costurero'
          'Mago'
          'Clerigo'
          'Ladr'#243'n')
      end
      object e_dato1_npc: TEdit
        Left = 44
        Top = 52
        Width = 61
        Height = 21
        TabOrder = 2
      end
      object c_borrar_npc: TCheckBox
        Left = 276
        Top = 4
        Width = 57
        Height = 17
        Caption = 'Borrar'
        TabOrder = 3
      end
      object cb_me_npc: TCheckBox
        Left = 340
        Top = 4
        Width = 97
        Height = 17
        Caption = 'Modo Edicion'
        TabOrder = 4
      end
    end
    object TS_Sensor: TTabSheet
      Caption = 'Sensor'
      object Label10: TLabel
        Left = 296
        Top = 60
        Width = 63
        Height = 13
        Caption = 'Objeto Llave:'
      end
      object Label_ds1: TLabel
        Left = 4
        Top = 60
        Width = 3
        Height = 13
      end
      object Label_ds2: TLabel
        Left = 4
        Top = 84
        Width = 3
        Height = 13
      end
      object Label_ds3: TLabel
        Left = 4
        Top = 108
        Width = 3
        Height = 13
      end
      object Label15: TLabel
        Left = 4
        Top = 36
        Width = 67
        Height = 13
        Caption = 'Mensaje N\U:'
      end
      object Label23: TLabel
        Left = 296
        Top = 84
        Width = 58
        Height = 13
        Caption = 'Modificador:'
        ParentShowHint = False
        ShowHint = False
      end
      object Label_ds4: TLabel
        Left = 156
        Top = 108
        Width = 3
        Height = 13
      end
      object cb_tipo_sensor: TComboBox
        Left = 4
        Top = 4
        Width = 257
        Height = 21
        Style = csDropDownList
        ItemHeight = 13
        TabOrder = 0
        OnChange = cb_tipo_sensorChange
        Items.Strings = (
          'Salud: Regenera puntos de salud'
          'Man'#225': Regenera puntos de man'#225
          'Vida: Resucita al avatar'
          'Portal: Mueve al avatar a otra posici'#243'n o mapa'
          'Objeto: Intercambia el objeto llave por otro'
          'F-B: Fijar Bandera'
          'L-B: Limpiar Bandera'
          'C-B: Cambiar Bandera'
          'XX:No usar'
          'Clan: Para fundar un clan'
          'XX:No usar'
          'XX:No usar')
      end
      object e_llave2_sensor: TEdit
        Left = 364
        Top = 80
        Width = 53
        Height = 21
        Hint = 'Modificador, 0=cualquier modificador'
        ParentShowHint = False
        ShowHint = True
        TabOrder = 10
      end
      object e_dato1_sensor: TEdit
        Left = 84
        Top = 56
        Width = 40
        Height = 21
        TabOrder = 4
        Visible = False
      end
      object e_dato2_sensor: TEdit
        Left = 84
        Top = 80
        Width = 40
        Height = 21
        TabOrder = 5
        Visible = False
      end
      object e_dato3_sensor: TEdit
        Left = 84
        Top = 104
        Width = 40
        Height = 21
        TabOrder = 16
        Visible = False
      end
      object e_dato4_sensor: TEdit
        Left = 228
        Top = 104
        Width = 40
        Height = 21
        TabOrder = 6
        Visible = False
      end
      object e_texto_sensor: TEdit
        Left = 84
        Top = 32
        Width = 565
        Height = 21
        Hint = 'Necesitas...\Usaste...\Descripci'#243'n\Mensaje'
        MaxLength = 127
        ParentShowHint = False
        ShowHint = True
        TabOrder = 3
      end
      object c_borrar_sensor: TCheckBox
        Left = 284
        Top = 4
        Width = 101
        Height = 17
        Caption = 'Borrar sensores'
        TabOrder = 1
      end
      object cb_me_sensor: TCheckBox
        Left = 396
        Top = 4
        Width = 217
        Height = 17
        Caption = '&Seleccionar y describir el apuntado'
        TabOrder = 2
      end
      object Btn_AsignarPos: TButton
        Left = 140
        Top = 76
        Width = 133
        Height = 25
        Caption = 'Asignar Posicion Guardada'
        TabOrder = 7
        Visible = False
        OnClick = Btn_AsignarPosClick
      end
      object CmbObjetoLlave: TComboBox
        Left = 364
        Top = 56
        Width = 261
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 8
      end
      object BtnObjeto2: TButton
        Left = 444
        Top = 80
        Width = 157
        Height = 21
        Caption = 'Copiar c'#243'digo al objeto obtenido'
        TabOrder = 12
        Visible = False
        OnClick = BtnObjeto2Click
      end
      object b_se1: TButton
        Left = 128
        Top = 56
        Width = 21
        Height = 21
        Caption = '...'
        TabOrder = 13
        Visible = False
        OnClick = boton_flagsClick
      end
      object b_se2: TButton
        Left = 128
        Top = 80
        Width = 21
        Height = 21
        Caption = '...'
        TabOrder = 14
        Visible = False
        OnClick = boton_flagsClick
      end
      object b_se3: TButton
        Left = 128
        Top = 104
        Width = 21
        Height = 21
        Caption = '...'
        TabOrder = 15
        Visible = False
        OnClick = boton_flagsClick
      end
      object b_se4: TButton
        Left = 272
        Top = 104
        Width = 21
        Height = 21
        Caption = '...'
        TabOrder = 17
        Visible = False
        OnClick = boton_flagsClick
      end
      object c_consumirLlave: TCheckBox
        Left = 664
        Top = 32
        Width = 121
        Height = 17
        Caption = 'Consumir la llave'
        TabOrder = 19
        OnClick = c_GTerrenoClick
      end
      object c_solofantasma: TCheckBox
        Left = 664
        Top = 72
        Width = 125
        Height = 17
        Caption = 'S'#243'lo fantasmas'
        TabOrder = 21
        OnClick = c_GTerrenoClick
      end
      object c_soloclan: TCheckBox
        Left = 664
        Top = 52
        Width = 125
        Height = 17
        Caption = 'S'#243'lo Clan Dominante'
        TabOrder = 20
        OnClick = c_GTerrenoClick
      end
      object c_soloaprendiz: TCheckBox
        Left = 664
        Top = 92
        Width = 125
        Height = 17
        Caption = 'S'#243'lo aprendices'
        TabOrder = 22
        OnClick = c_GTerrenoClick
      end
      object c_repeler: TCheckBox
        Left = 664
        Top = 12
        Width = 121
        Height = 17
        Hint = 'Mueve al avatar fuera del sensor'
        Caption = 'Repeler al avatar'
        ParentShowHint = False
        ShowHint = True
        TabOrder = 18
        OnClick = c_GTerrenoClick
      end
      object c_ParteDelCastillo: TCheckBox
        Left = 664
        Top = 112
        Width = 125
        Height = 17
        Caption = 'Parte del castillo'
        TabOrder = 23
        OnClick = c_GTerrenoClick
      end
      object BtnAyudaLlave: TButton
        Left = 628
        Top = 56
        Width = 21
        Height = 20
        Caption = '?'
        TabOrder = 9
        OnClick = BtnAyudaLlaveClick
      end
      object Button8: TButton
        Left = 420
        Top = 80
        Width = 21
        Height = 20
        Caption = '?'
        TabOrder = 11
        OnClick = Button8Click
      end
    end
    object TS_Nido: TTabSheet
      Caption = 'Nido de criaturas'
      object Label30c: TLabel
        Left = 8
        Top = 36
        Width = 111
        Height = 13
        Caption = 'Cantidad de monstruos:'
      end
      object cb_tipo_nido: TComboBox
        Left = 8
        Top = 4
        Width = 257
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 0
      end
      object e_dato1_nido: TEdit
        Left = 128
        Top = 32
        Width = 41
        Height = 21
        TabOrder = 3
      end
      object c_borrar_nido: TCheckBox
        Left = 284
        Top = 4
        Width = 93
        Height = 17
        Caption = 'Borrar nidos'
        TabOrder = 1
      end
      object cb_me_nido: TCheckBox
        Left = 284
        Top = 32
        Width = 225
        Height = 17
        Caption = '&Seleccionar y describir el apuntado'
        TabOrder = 2
        OnClick = cb_me_nidoClick
      end
    end
    object TS_Banderas: TTabSheet
      Caption = 'Banderas del Mapa'
      object Bevel3: TBevel
        Left = 216
        Top = 4
        Width = 373
        Height = 125
      end
      object Label26: TLabel
        Left = 4
        Top = 8
        Width = 43
        Height = 13
        Caption = 'Bandera:'
      end
      object Label27: TLabel
        Left = 224
        Top = 20
        Width = 76
        Height = 13
        Caption = 'Efecto Servidor:'
      end
      object Label28: TLabel
        Left = 224
        Top = 76
        Width = 53
        Height = 13
        Caption = 'Posici'#243'n X:'
      end
      object Label29: TLabel
        Left = 224
        Top = 104
        Width = 53
        Height = 13
        Caption = 'Posici'#243'n Y:'
      end
      object Label11: TLabel
        Left = 224
        Top = 48
        Width = 69
        Height = 13
        Caption = 'Efecto Cliente:'
      end
      object cb_flag: TComboBox
        Left = 56
        Top = 4
        Width = 149
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 0
        OnChange = cb_flagChange
      end
      object cb_efS: TComboBox
        Left = 304
        Top = 16
        Width = 277
        Height = 21
        Style = csDropDownList
        ItemHeight = 13
        TabOrder = 1
        OnChange = cb_efSChange
        Items.Strings = (
          '<Ninguno>'
          'Desactiva bloqueo 2x2'
          'Desactiva bloqueo 3x3'
          'Desactiva bloqueo 2x2 (que deja pasar municiones)')
      end
      object c_flag_activo: TCheckBox
        Left = 56
        Top = 36
        Width = 153
        Height = 17
        Caption = 'Activar al inicializar el mapa'
        TabOrder = 2
        OnClick = c_flag_activoClick
      end
      object E_flag_d1: TEdit
        Left = 304
        Top = 72
        Width = 65
        Height = 21
        TabOrder = 3
        OnChange = E_flag_Change
      end
      object E_flag_d2: TEdit
        Left = 304
        Top = 100
        Width = 65
        Height = 21
        TabOrder = 4
        OnChange = E_flag_Change
      end
      object Button6: TButton
        Left = 404
        Top = 84
        Width = 141
        Height = 25
        Caption = 'Asignar Posicion Guardada'
        TabOrder = 5
        OnClick = Button6Click
      end
      object c_limpiar_flag: TCheckBox
        Left = 56
        Top = 60
        Width = 129
        Height = 17
        Caption = 'Limpiar en cada turno'
        TabOrder = 6
        OnClick = c_limpiar_flagClick
      end
      object cb_efC: TComboBox
        Left = 304
        Top = 44
        Width = 277
        Height = 21
        Style = csDropDownList
        ItemHeight = 13
        TabOrder = 7
        OnChange = cb_efCChange
        Items.Strings = (
          '<Ninguno>'
          'Sonido de puerta'
          'Sonido de porticullis'
          'Sonido de palanca')
      end
      object Button7: TButton
        Left = 596
        Top = 4
        Width = 173
        Height = 25
        Caption = 'Copiar bandera a Sensor y Grafico'
        TabOrder = 8
        OnClick = Button7Click
      end
    end
  end
  object c_transparente: TCheckBox
    Left = 1296
    Top = 236
    Width = 125
    Height = 17
    Caption = 'Gr'#225'ficos transparentes'
    TabOrder = 4
    OnClick = c_GTerrenoClick
  end
  object c_marcas: TCheckBox
    Left = 1296
    Top = 216
    Width = 125
    Height = 17
    Caption = 'Ver marcas de objetos'
    Checked = True
    State = cbChecked
    TabOrder = 3
    OnClick = c_GTerrenoClick
  end
  object c_edificiosMM: TCheckBox
    Left = 1296
    Top = 152
    Width = 121
    Height = 17
    Caption = 'Edificios en MiniMapa'
    Checked = True
    State = cbChecked
    TabOrder = 1
    OnClick = c_edificiosMMClick
  end
  object c_Techos: TCheckBox
    Left = 1296
    Top = 296
    Width = 125
    Height = 17
    Caption = 'Mostrar Techos'
    Checked = True
    State = cbChecked
    TabOrder = 7
    OnClick = c_GTerrenoClick
  end
  object BtnFx: TButton
    Left = 1296
    Top = 380
    Width = 37
    Height = 21
    Caption = 'Fx:'
    TabOrder = 8
    OnClick = BtnFxClick
  end
  object cbFx: TComboBox
    Left = 1336
    Top = 380
    Width = 89
    Height = 21
    Style = csDropDownList
    ItemHeight = 13
    TabOrder = 9
    Items.Strings = (
      'Rayo'
      'lluvia'
      'nieve'
      'niebla'
      'Noche '
      'Noche + Infravision')
  end
  object cb_testE: TCheckBox
    Left = 1296
    Top = 404
    Width = 105
    Height = 17
    Caption = 'x 2000'
    TabOrder = 10
  end
  object cbDefTerMiniMapa: TCheckBox
    Left = 1296
    Top = 132
    Width = 125
    Height = 17
    Hint = 'Definir terreno en el minimapa'
    Caption = 'Def. terr. en minimapa'
    ParentShowHint = False
    ShowHint = True
    TabOrder = 0
  end
  object c_normales: TCheckBox
    Left = 1296
    Top = 276
    Width = 125
    Height = 17
    Caption = 'Graficos normales'
    Checked = True
    State = cbChecked
    TabOrder = 6
    OnClick = c_GTerrenoClick
  end
  object c_sprites: TCheckBox
    Left = 1296
    Top = 316
    Width = 125
    Height = 17
    Caption = 'Mostrar Sprites'
    Checked = True
    State = cbChecked
    TabOrder = 14
    OnClick = c_GTerrenoClick
  end
  object c_zoom: TCheckBox
    Left = 1296
    Top = 336
    Width = 125
    Height = 17
    Caption = 'Zoom'
    TabOrder = 15
    OnClick = c_GTerrenoClick
  end
  object MainMenu1: TMainMenu
    Left = 1080
    Top = 656
    object Archivo1: TMenuItem
      Caption = 'Archivo'
      object NuevoMapa1: TMenuItem
        Caption = 'Nuevo Mapa'
        OnClick = NuevoMapa1Click
      end
      object AbrirMapa1: TMenuItem
        Caption = 'Abrir Mapa'
        OnClick = AbrirMapa1Click
      end
      object GuardarMapa1: TMenuItem
        Caption = 'Guardar Mapa'
        OnClick = GuardarMapa1Click
      end
      object N2: TMenuItem
        Caption = '-'
      end
      object GuardarMiniMapa1: TMenuItem
        Caption = 'Guardar MiniMapa'
        OnClick = GuardarMiniMapa1Click
      end
      object N1: TMenuItem
        Caption = '-'
      end
      object Salir1: TMenuItem
        Caption = 'Salir'
        OnClick = Salir1Click
      end
    end
  end
  object OpenDialog: TOpenDialog
    DefaultExt = 'mpv'
    Filter = 'Mapas de mundos|*.mpv'
    Options = [ofHideReadOnly, ofFileMustExist, ofNoTestFileCreate, ofNoNetworkButton, ofNoDereferenceLinks, ofEnableSizing]
    Title = 'Abrir'
    Left = 1048
    Top = 656
  end
  object SaveDialog: TSaveDialog
    DefaultExt = 'mpv'
    Filter = 'Mapas de mundos|*.mpv'
    Options = [ofOverwritePrompt, ofHideReadOnly]
    Title = 'Guardar'
    Left = 1112
    Top = 656
  end
end
