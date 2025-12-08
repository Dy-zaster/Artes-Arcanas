program Laa;//Leyenda de las artes arcanas.
uses
  Forms,
  Juego in 'Juego.pas' {JForm},
  UEstandartes in 'UEstandartes.pas' {FEstandartes},
  SScreen in 'SScreen.pas',
  DirectDraw in 'DirectDraw.pas',
  DirectSound in 'DirectSound.pas',
  DXCommon in 'DXCommon.pas',
  Tablero in 'Tablero.pas',
  Sprites in 'Sprites.pas',
  objetos in 'objetos.pas',
  cadenas in 'cadenas.pas',
  Graficador in 'Graficador.pas',
  Demonios in 'Demonios.pas',
  UCliente in 'UCliente.pas',
  Graficos in 'Graficos.pas',
  InterfazDS in 'InterfazDS.pas',
  LectorWAV in 'LectorWAV.pas',
  Sonidos in 'Sonidos.pas',
  UMensajes in 'UMensajes.pas',
  MundoEspejo in 'MundoEspejo.pas',
  Globales in 'Globales.pas',
  GTimer in 'GTimer.pas',
  MPlayerLite in 'mplayerLite.pas',
  ScktComp in 'scktcomp.pas',
  URapidas in 'URapidas.pas',
  UColor8 in 'UColor8.pas',
  buscar_ip in 'buscar_ip.pas',
  Gboton in 'Gboton.pas',
  HiloMusical in 'HiloMusical.pas';

{$R Laa.RES}
begin
  ID_ATOM_SOLO_UNA_INSTANCIA:=0;
  if not VerificarArchivosYConfiguraciones then exit;

  Application.Initialize;
  Application.CreateForm(TJForm, JForm);
  Application.CreateForm(TFEstandartes, FEstandartes);
  Application.Run;
end.
