object MainForm: TMainForm
  Left = 911
  Top = 129
  Width = 602
  Height = 402
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -11
  Font.Name = 'MS Sans Serif'
  Font.Style = []
  Menu = MainMenu1
  OldCreateOrder = True
  Position = poScreenCenter
  OnClose = FormClose
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnResize = FormResize
  PixelsPerInch = 96
  TextHeight = 13
  object PanelBajo: TPanel
    Left = 0
    Top = 316
    Width = 586
    Height = 24
    Align = alBottom
    BevelOuter = bvNone
    TabOrder = 0
    object LbMensaje: TLabel
      Left = 4
      Top = 6
      Width = 3
      Height = 13
    end
  end
  object Memo: TMemo
    Left = 0
    Top = 0
    Width = 586
    Height = 316
    Align = alClient
    Color = clBlack
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -11
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    ReadOnly = True
    ScrollBars = ssVertical
    TabOrder = 1
    OnKeyDown = MemoKeyDown
  end
  object MainMenu1: TMainMenu
    Left = 288
    Top = 12
    object Servidor1: TMenuItem
      Caption = 'Aplicaci'#243'n'
      object TestVarios1: TMenuItem
        Caption = 'Debug'
        Visible = False
        object tamaos1: TMenuItem
          Caption = 'Tama'#241'o de Objetos'
          OnClick = tamaos1Click
        end
      end
      object FindelMundo1: TMenuItem
        Caption = 'Cerrar'
        OnClick = FindelMundo1Click
      end
    end
    object Universo1: TMenuItem
      Caption = 'Servidor'
      object Activar1: TMenuItem
        Caption = 'Activar'
        GroupIndex = 1
        OnClick = Activar1Click
      end
      object Desactivar1: TMenuItem
        Caption = 'Desactivar'
        Enabled = False
        GroupIndex = 1
        OnClick = Desactivar1Click
      end
      object N2: TMenuItem
        Caption = '-'
        GroupIndex = 2
      end
      object Opcionesdelservidor1: TMenuItem
        Caption = 'Opciones'
        GroupIndex = 2
        object ServidorModoVerificacion1: TMenuItem
          Caption = 'Modo de pruebas'
          GroupIndex = 2
          OnClick = OpcionMenuClick
        end
        object N6: TMenuItem
          Caption = '-'
          GroupIndex = 2
        end
        object Cambiarpuerto1: TMenuItem
          Caption = 'Cambiar puerto'
          GroupIndex = 2
          OnClick = Cambiarpuerto1Click
        end
        object Limpiarelsuelo1: TMenuItem
          Caption = 'Limpiar objetos del piso'
          GroupIndex = 2
          OnClick = Limpiarelsuelo1Click
        end
      end
      object Parmetrosdelservidor1: TMenuItem
        Caption = 'Par'#225'metros'
        GroupIndex = 2
        object MantenerRegistro1: TMenuItem
          Caption = 'Mantener Registro en Archivo'
          GroupIndex = 2
          OnClick = OpcionMenuClick
        end
        object FechayHoraenRegistro1: TMenuItem
          Caption = 'Fecha y Hora en Registro'
          GroupIndex = 2
          OnClick = OpcionMenuClick
        end
        object Comunicacintotalcon1: TMenuItem
          Caption = 'Comunicaci'#243'n total con %'
          Checked = True
          GroupIndex = 2
          OnClick = OpcionMenuClick
        end
        object VerConexionesImprocedentes1: TMenuItem
          Caption = 'Ver conexiones no permitidas'
          GroupIndex = 2
          OnClick = OpcionMenuClick
        end
        object Mostrarconexionesydesconexiones: TMenuItem
          Caption = 'Mostrar conexiones y desconexiones'
          GroupIndex = 2
          OnClick = OpcionMenuClick
        end
        object Permitirmultiplessesiones1: TMenuItem
          Caption = 'Permitir m'#250'ltiples sesiones'
          Checked = True
          GroupIndex = 2
          OnClick = OpcionMenuClick
        end
        object N1: TMenuItem
          Caption = '-'
          GroupIndex = 2
        end
        object Tiempoentreengendrodemonstruos1: TMenuItem
          Caption = 'Fijar tiempo de reengendro'
          GroupIndex = 2
          OnClick = Tiempoentreengendrodemonstruos1Click
        end
      end
    end
    object Utilitarios1: TMenuItem
      Caption = 'Utilitarios'
      Enabled = False
      object Enviarmensajeatodoslosjugadores1: TMenuItem
        Caption = 'Mensajes'
        object Enviarmensajeatodoslosjugadores2: TMenuItem
          Caption = 'Enviar mensaje a todos los jugadores'
          OnClick = Enviarmensajeatodoslosjugadores2Click
        end
      end
      object AdministracindePersonajes1: TMenuItem
        Caption = 'Administrar Personajes'
        object AgregarGameMaster1: TMenuItem
          Caption = 'Convertir en Amo del Calabozo'
          OnClick = AgregarAdministradorClick
        end
        object AgregarAdmin1: TMenuItem
          Caption = 'Convertir en Administrador'
          OnClick = AgregarAdministradorClick
        end
        object AgregarModerador1: TMenuItem
          Caption = 'Convertir en Moderador'
          OnClick = AgregarAdministradorClick
        end
        object Desactivarcomandosdea1: TMenuItem
          Caption = 'Quitar privilegios de Administrador'
          OnClick = Desactivarcomandosdea1Click
        end
        object N8: TMenuItem
          Caption = '-'
        end
        object ExpulsarJugador1: TMenuItem
          Caption = 'Bloquear Personaje (Expulsar)'
          OnClick = ExpulsarJugador1Click
        end
      end
      object AdministrarClanes1: TMenuItem
        Caption = 'Administrar Clanes'
        Visible = False
        object EliminarClanesinactivos1: TMenuItem
          Caption = 'Eliminar Clanes inactivos'
          OnClick = EliminarClanesinactivos1Click
        end
      end
      object AdministrarMapas1: TMenuItem
        Caption = 'Administrar Mapas'
        object Limpiarcadveres1: TMenuItem
          Caption = 'Limpiar cad'#225'veres'
          OnClick = Limpiarcadveres1Click
        end
        object Limpiarbolsas1: TMenuItem
          Caption = 'Limpiar bolsas de menos de 5mo'
          OnClick = Limpiarbolsas1Click
        end
        object Eliminarbolsasycadveres1: TMenuItem
          Caption = 'Limpiar objetos del piso por costo de contenido'
          OnClick = Limpiarelsuelo1Click
        end
        object N5: TMenuItem
          Caption = '-'
        end
        object LimpiarPeriodicamente1: TMenuItem
          Caption = 'Limpiar peri'#243'dicamente objetos del piso'
          Checked = True
          OnClick = OpcionMenuClick
        end
      end
      object Archivos1: TMenuItem
        Caption = 'Archivos'
        object GuardarPersonajes1: TMenuItem
          Caption = 'Guardar personajes'
          OnClick = GuardarPersonajes1Click
        end
        object Guardarclanes1: TMenuItem
          Caption = 'Guardar informaci'#243'n del juego'
          OnClick = Guardarclanes1Click
        end
        object N4: TMenuItem
          Caption = '-'
        end
        object AutoGuardarInformacin1: TMenuItem
          Caption = 'Guardar peri'#243'dicamente'
          Checked = True
        end
      end
      object Estadsticas1: TMenuItem
        Caption = 'Estad'#237'sticas'
        object Resertearestadsticas1: TMenuItem
          Caption = 'Colocar en 0 "bytes recibidos"'
          OnClick = Resertearestadsticas1Click
        end
        object Bytesrecibidos1: TMenuItem
          Caption = 'Flujo de datos'
          OnClick = Bytesrecibidos1Click
        end
        object N3: TMenuItem
          Caption = '-'
        end
        object Jugadoresactivos1: TMenuItem
          Caption = 'Jugadores activos'
          OnClick = Jugadoresactivos1Click
        end
        object Clanesactivos1: TMenuItem
          Caption = 'Clanes activos'
          OnClick = Clanesactivos1Click
        end
      end
    end
    object Test1: TMenuItem
      Caption = 'Pruebas'
      Enabled = False
      object TestAmb: TMenuItem
        Caption = 'Fx ambientales'
        object IniciarNoche1: TMenuItem
          Caption = 'Iniciar Noche'
          OnClick = IniciarNoche1Click
        end
        object IniciarNiebla1: TMenuItem
          Caption = 'Iniciar Niebla'
          OnClick = IniciarNiebla1Click
        end
        object IniciarLluvia1: TMenuItem
          Caption = 'Iniciar Lluvia/Nieve'
          OnClick = IniciarLluvia1Click
        end
        object TestNocheLluviosa1: TMenuItem
          Caption = 'Iniciar Noche + Lluvia/Nieve'
          OnClick = TestNocheLluviosa1Click
        end
        object Terminarefectoambiental1: TMenuItem
          Caption = 'Finalizar efecto ambiental'
          OnClick = Terminarefectoambiental1Click
        end
      end
    end
  end
end
